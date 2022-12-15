/**
 *  Archiver - Cross platform, multi-destination backup and archiving utility
 * 
 *  Copyright (c) 2020-2021 Steve Cross <flip@foxhollow.cc>
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *  
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *  
 *  You should have received a copy of the GNU General Public License along
 *  with this program; if not, write to the Free Software Foundation, Inc.,
 *  51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FoxHollow.Archiver.Shared.Classes.Disc;
using FoxHollow.FHM.Shared.Structures;
using FoxHollow.FHM.Shared.Classes;
using FoxHollow.FHM.Shared.Models;
using FoxHollow.FHM.Shared.Utilities;

namespace FoxHollow.Archiver.Shared.Operations.Disc
{
    public class DiscVerifier
    {
        public delegate void StatusChangedDelegate(Status status, string statusText);
        public delegate void DiscVerificationStartedDelegate(DiscDetail disc);
        public delegate void DiscVerificationProgressChangedDelegate(DiscDetail disc, Md5Progress progress, Stopwatch sw);
        public delegate void DiscVerificationCompleteDelegate(DiscDetail disc, bool success);

        public enum Status
        {
            AwaitingDisc = 0,
            Verifying = 1,
            Canceled = 2,
            Ejecting = 3,
            Complete = 4,
            NeedDifferentDisc = 5,
            VerificationStarting = 6
        }

        public event StatusChangedDelegate OnStatusChanged;
        public event DiscVerificationStartedDelegate OnDiscVerificationStarted;
        public event DiscVerificationProgressChangedDelegate OnDiscVerificationProgressChanged;
        public event DiscVerificationCompleteDelegate OnDiscVerificationComplete;

        public int DiscCount => _discCount;
        public int CompletedDiscCount => this._completedDiscIds.Count;
        public IReadOnlyList<DiscDetail> PendingDiscs => (IReadOnlyList<DiscDetail>)_pendingDiscs;

        private List<DiscDetail> _pendingDiscs => _discsToVerify.Where(x => x.DaysSinceLastVerify > 7)
                                                                .ToList();

        private List<DiscDetail> _completedDiscs => _discsToVerify.Where(x => x.DaysSinceLastVerify <= 7)
                                                                  .ToList();

        private int _discCount = 0;
        private List<DiscDetail> _discsToVerify;
        private List<int> _pendingDiscIds;
        private List<int> _completedDiscIds;

        private OpticalDrive _drive;

        public DiscVerifier(OpticalDrive drive, List<DiscDetail> discsToVerify)
        {
            _discsToVerify = discsToVerify;
            _drive = drive;
            _discCount = _discsToVerify.Count();
            _pendingDiscIds = _pendingDiscs.Select(x => x.DiscNumber).ToList();
            _completedDiscIds = _completedDiscs.Select(x => x.DiscNumber).ToList();

            this.OnStatusChanged += delegate { };
            this.OnDiscVerificationStarted += delegate { };
            this.OnDiscVerificationProgressChanged += delegate { };
            this.OnDiscVerificationComplete += delegate { };

            this.OnStatusChanged(Status.VerificationStarting, "Starting...");
        }


        public async Task StartVerificationAsync(CancellationToken cToken)
        {
            while (_pendingDiscs.Count() > 0 && !cToken.IsCancellationRequested)
            {
                DiscDetail disc = await WaitForDiscAsync(cToken);

                if (disc == null)
                    break;

                this.OnDiscVerificationStarted(disc);

                Stopwatch sw = Stopwatch.StartNew();

                using (Stream reader = OpticalDriveUtils.GetDriveRawStream(_drive))
                {
                    Md5StreamGenerator generator = new Md5StreamGenerator(reader);
                    generator.OnProgressChanged += (progress) => this.OnDiscVerificationProgressChanged(disc, progress, sw);

                    string hash = await generator.GenerateAsync(cToken);

                    if (cToken.IsCancellationRequested)
                        return;

                    sw.Stop();

                    bool discValid = (disc.Hash.ToLower() == hash.ToLower());

                    disc.RecordVerification(DateTime.UtcNow, discValid);
                    await disc.SaveToIndexAsync();

                    _pendingDiscIds.Remove(disc.DiscNumber);
                    _completedDiscIds.Add(disc.DiscNumber);

                    this.OnDiscVerificationComplete(disc, discValid);
                }

                this.OnStatusChanged(Status.Ejecting, $"Ejecting disc `{disc.DiscName}`...");
                // await Task.Delay(2000);
                OpticalDriveUtils.EjectDrive(_drive);
            }

            if (cToken.IsCancellationRequested)
                this.OnStatusChanged(Status.Canceled, "Process canceled");
            else
                this.OnStatusChanged(Status.Complete,"All Discs have been verified");
        }

        private async Task<DiscDetail> WaitForDiscAsync(CancellationToken cToken)
        {
            DiscDetail insertedDisc = default(DiscDetail);

            // TODO: Make this async by adding a method to OpticalDrive called "WaitForDiscAsync"
            OpticalDrive driveInfo = OpticalDriveUtils.GetDrives().FirstOrDefault(x => x.Name == _drive.Name);

            // DriveInfo driveInfo = DriveInfo.GetDrives().Where(x => x.Name.TrimEnd('\\').ToUpper() == _drive.ToUpper()).FirstOrDefault();

            if (driveInfo != null)
            {
                string append = "";

                if (_pendingDiscIds.Count() == 1)
                    append = " " + _pendingDiscIds[0].ToString();

                while (!cToken.IsCancellationRequested)
                {
                    driveInfo = OpticalDriveUtils.GetDrives().FirstOrDefault(x => x.Name == _drive.Name);

                    if (driveInfo.IsReady == false)
                        this.OnStatusChanged(Status.AwaitingDisc, $"Please insert archive disc{append}...");

                    else if (driveInfo.VolumeLabel != null)
                    {
                        if (driveInfo.VolumeLabel.ToLower().StartsWith("archive ") == false)
                            this.OnStatusChanged(Status.NeedDifferentDisc, "Unknown disc inserted...");

                        else
                        {
                            string discIdStr = driveInfo.VolumeLabel.Substring(8);
                            int discId = Int32.Parse(discIdStr);

                            if (_pendingDiscIds.Contains(discId))
                            {
                                this.OnStatusChanged(Status.Verifying, $"Verifying disc {discId}");
                                return _discsToVerify.FirstOrDefault(x => x.DiscNumber == discId);
                            }
                            else if (_completedDiscIds.Contains(discId))
                            {
                                if (_pendingDiscIds.Count() == 1)
                                    this.OnStatusChanged(Status.NeedDifferentDisc, $"Archive disc {discId} has already been verified, please insert disc {_pendingDiscIds[0].ToString()}");
                                else
                                    this.OnStatusChanged(Status.NeedDifferentDisc, $"Archive disc {discId} has already been verified, please insert another disc");
                            }
                            else
                            {
                                if (_pendingDiscIds.Count() == 1)
                                    this.OnStatusChanged(Status.NeedDifferentDisc, $"Archive disc {discId} does not need to be verified, please insert disc {_pendingDiscIds[0].ToString()}");
                                else
                                    this.OnStatusChanged(Status.NeedDifferentDisc, $"Archive disc {discId} does not need to be verified, please insert another disc");
                            }
                        }
                    }

                    try
                    {
                        await Task.Delay(500, cToken);
                    }
                    catch(TaskCanceledException)
                    { 
                        return null;
                    }
                }
            }

            return insertedDisc;
        }
    }
}