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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FoxHollow.Archiver.Shared.Classes;
using FoxHollow.Archiver.Shared.Classes.Disc;
using FoxHollow.Archiver.Shared.Models;
using FoxHollow.Archiver.Shared.Structures;
using FoxHollow.Archiver.Shared.Utilities;
using FoxHollow.Archiver.Shared.Utilities.Disc;
using Newtonsoft.Json;

namespace FoxHollow.Archiver.Shared.Operations.Disc
{
    public class DiscArchiver
    {
        public enum ProcessStep
        {
            FirstStart,
            ScanFiles,
            SizeFiles,
            DistributeFiles,
            CopyFiles,
            CreateDiscIndex,
            GenerateHashFile,
            WriteDiscInfo,
            CreateISOFile,
            ReadIsoHash,
            SaveToIndex,
            DiscComplete
        }

        public enum Status
        {
            NothingToDo = 0,
            Completed = 1,
            Canceled = 2
        }

        public struct FileCopyProgress
        {
            public TimeSpan Elapsed;
            public long CurrentFile;
            public double InstantTransferRate;
            public double AverageTransferRate;
            // TODO: rename to PercentCompleted
            public double CurrentPercent;
        }

        public struct DiscIndexProgress
        {
            public TimeSpan Elapsed;
            public long CurrentFile;
            public long TotalFiles;
            // TODO: rename to PercentCompleted
            public double CurrentPercent;
        }

        public struct GenerateHashProgress
        {
            public TimeSpan Elapsed;
            public long CurrentFile;
            public long TotalFiles;
            // TODO: rename to PercentCompleted
            public double CurrentPercent;
        }

        public struct CreateIsoProgress
        {
            public TimeSpan Elapsed;
            // TODO: rename to PercentCompleted
            public double CurrentPercent;
        }

        public delegate void StepStateDelegate(DiscDetail disc, DiscScanStats stats, ProcessStep step);
        public delegate void UpdateStatsDelegate(DiscScanStats stats);
        public delegate void UpdateSizingDelegate(DiscScanStats stats, long currentFile);
        public delegate void UpdateDistributeDelegate(DiscScanStats stats, long currentFile);
        public delegate void FileCopyProgressDelegate(DiscDetail disc, DiscScanStats stats, FileCopyProgress progress);
        public delegate void DiscIndexProgressDelegate(DiscDetail disc, DiscScanStats stats, DiscIndexProgress progress);
        public delegate void GenerateHashProgressDelegate(DiscDetail disc, DiscScanStats stats, GenerateHashProgress progress);
        public delegate void CreateIsoProgressDelegate(DiscDetail disc, DiscScanStats stats, CreateIsoProgress progress);
        public delegate void ReadIsoMd5ProgressDelegate(DiscDetail disc, DiscScanStats stats, TimeSpan elapsed, Md5Progress progress);

        public event StepStateDelegate OnStepStart;
        public event StepStateDelegate OnStepComplete;
        public event UpdateStatsDelegate OnUpdateStats;
        public event UpdateSizingDelegate OnUpdateSizing;
        public event UpdateDistributeDelegate OnUpdateDistribute;
        
        public event FileCopyProgressDelegate OnFileCopyProgress;
        public event DiscIndexProgressDelegate OnDiscIndexProgress;
        public event GenerateHashProgressDelegate OnGenerateHashProgress;
        public event CreateIsoProgressDelegate OnCreateIsoProgress;
        public event ReadIsoMd5ProgressDelegate OnReadIsoMd5Progress;

        public Func<DiscScanStats, Task<Nullable<bool>>> AskToArchiveCallback = null;

        public Status ResultStatus { get; private set; } = DiscArchiver.Status.NothingToDo;

        private List<DiscDetail> _discs;
        private DiscScanStats _stats;
        private CancellationToken _cToken = default;
        private const int _updateFrequencyMs = 1000;

        public DiscArchiver(List<DiscDetail> discs)
        {
            _discs = discs ?? throw new ArgumentNullException(nameof(discs));
            _stats = new DiscScanStats(_discs);

            this.OnStepStart += delegate { };
            this.OnStepComplete += delegate { };
            this.OnUpdateStats += delegate { };
            this.OnUpdateSizing += delegate { };
            this.OnUpdateDistribute += delegate { };
            this.OnFileCopyProgress += delegate { };
            this.OnDiscIndexProgress += delegate { };
            this.OnGenerateHashProgress += delegate { };
            this.OnCreateIsoProgress += delegate { };
            this.OnReadIsoMd5Progress += delegate { };
        }

        public async Task RunArchiveAsync(bool askBeforeArchive = false, CancellationToken cToken = default)
        {
            if (askBeforeArchive && AskToArchiveCallback == null)
                throw new InvalidOperationException("When 'askBeforeArchive' is true, an 'AskToArchiveCallback' must be provided!");

            _stats.ProcessSw.Restart();

            _cToken = cToken;
            
            
            await this.IndexAndCountFilesAsync();
            if (_cToken.IsCancellationRequested) { this.ResultStatus = Status.Canceled; return; }

            if (_stats.NewlyFoundFiles == 0) { this.ResultStatus = Status.NothingToDo; return; }

            await this.SizeFilesAsync();
            if (_cToken.IsCancellationRequested) { this.ResultStatus = Status.Canceled; return; }

            await this.DistributeFilesAsync();
            if (_cToken.IsCancellationRequested) { this.ResultStatus = Status.Canceled; return; }

            if (askBeforeArchive && AskToArchiveCallback != null && await AskToArchiveCallback(_stats) != true)
            { this.ResultStatus = Status.Canceled; return; }

            await this.ProcessDiscsAsync();
            if (_cToken.IsCancellationRequested) { this.ResultStatus = Status.Canceled; return; }

            this.ResultStatus = Status.Completed;

            _stats.ProcessSw.Stop();
        }

        private async Task IndexAndCountFilesAsync()
        {
            this.OnStepStart(null, _stats, ProcessStep.ScanFiles);

            FileScanner scanner = new FileScanner(_stats);
            scanner.OnProgressChanged += (stats) => this.OnUpdateStats(stats);
            await scanner.ScanFilesAsync(_cToken);

            this.OnStepComplete(null, _stats, ProcessStep.ScanFiles);
        }

        private async Task SizeFilesAsync()
        {
            this.OnStepStart(null, _stats, ProcessStep.SizeFiles);

            FileSizer sizer = new FileSizer(_stats);
            sizer.OnProgressChanged += (stats, currentFile) => this.OnUpdateSizing(stats, currentFile);
            await sizer.SizeFilesAsync(_cToken);

            this.OnStepComplete(null, _stats, ProcessStep.SizeFiles);
        }

        private async Task DistributeFilesAsync()
        {
            this.OnStepStart(null, _stats, ProcessStep.DistributeFiles);

            FileDistributor distributor = new FileDistributor(_stats);

            distributor.OnProgressChanged += (stats, currentFile) => this.OnUpdateDistribute(stats, currentFile);

            await distributor.DistributeFilesAsync(_cToken);

            this.OnStepComplete(null, _stats, ProcessStep.DistributeFiles);
        }

        private async Task ProcessDiscsAsync()
        {
            List<DiscDetail> discsToProcess = _stats.DestinationDiscs.Where(x => x.NewDisc == true)
                                                                     .OrderBy(x => x.DiscNumber)
                                                                     .ToList();

            Stopwatch discSw = Stopwatch.StartNew();

            foreach (DiscDetail disc in discsToProcess)
            {
                discSw.Restart();

                await CopyFilesAsync(disc, discSw);
                if (_cToken.IsCancellationRequested) { this.ResultStatus = Status.Canceled; return; }
                
                await GenerateIndexFiles(disc, discSw);
                if (_cToken.IsCancellationRequested) { this.ResultStatus = Status.Canceled; return; }
                
                await GenerateHashFile(disc, discSw);
                if (_cToken.IsCancellationRequested) { this.ResultStatus = Status.Canceled; return; }
                
                await WriteDiscInfo(disc, discSw);
                if (_cToken.IsCancellationRequested) { this.ResultStatus = Status.Canceled; return; }
                
                await CreateISOFile(disc, discSw);
                if (_cToken.IsCancellationRequested) { this.ResultStatus = Status.Canceled; return; }
                
                await ReadIsoHash(disc, discSw);
                if (_cToken.IsCancellationRequested) { this.ResultStatus = Status.Canceled; return; }

                await SaveJsonData(disc, discSw);
                if (_cToken.IsCancellationRequested) { this.ResultStatus = Status.Canceled; return; }

                this.OnStepStart(disc, _stats, ProcessStep.DiscComplete);
                this.OnStepComplete(disc, _stats, ProcessStep.DiscComplete);

                discSw.Stop();
            }
        }

        private async Task CopyFilesAsync(DiscDetail disc, Stopwatch discSw)
        {
            this.OnStepStart(disc, _stats, ProcessStep.CopyFiles);
            
            // if the stage dir already exists, we need to remove it so we don't accidentally end up with data
            // on the final disc that doesn't belong there
            // TODO: find some way to make sure this doesn't delete stuff it shouldn't..
            if (Directory.Exists(disc.RootStagingPath))
                Directory.Delete(disc.RootStagingPath, true);

            disc.ArchiveDTM = DateTime.UtcNow;

            IEnumerable<DiscSourceFile> sourceFiles = disc.Files.Where(x => x.Archived == false).OrderBy(x => x.RelativePath);

            long bytesCopied = 0;
            long bytesCopiedSinceLastupdate = 0;
            int currentFile = 1;
            int currentDisc = 0;
            double averageTransferRate = 0;
            long sampleCount = 0;
            
            Stopwatch sw = new Stopwatch();
            sw.Start();

            long lastSample = sw.ElapsedMilliseconds;

            foreach (DiscSourceFile file in sourceFiles)
            {
                if (_cToken.IsCancellationRequested == true)
                    return;

                // if we moved to another disc, we reset the disc counter
                if (file.DestinationDisc.DiscNumber > currentDisc)
                {
                    currentDisc = file.DestinationDisc.DiscNumber;
                    currentFile = 1;
                }

                var copier = file.ActivateCopy();

                copier.OnProgressChanged += (progress) => {
                    bytesCopied += progress.BytesCopiedSinceLastupdate;
                    bytesCopiedSinceLastupdate += progress.BytesCopiedSinceLastupdate;
                    file.DestinationDisc.BytesCopied += progress.BytesCopiedSinceLastupdate;

                    if ((sw.ElapsedMilliseconds - lastSample) > _updateFrequencyMs)
                    {
                        sampleCount++;

                        double timeSinceLastUpdate = (double)(sw.ElapsedMilliseconds - lastSample) / 1000.0;
                        double instantTransferRate = (double)bytesCopiedSinceLastupdate / timeSinceLastUpdate;

                        if (sampleCount == 1)
                            averageTransferRate = instantTransferRate;
                        else
                            averageTransferRate = averageTransferRate + (instantTransferRate - averageTransferRate) / sampleCount;

                        this.OnFileCopyProgress(disc, _stats, new FileCopyProgress()
                        {
                            Elapsed = discSw.Elapsed,
                            CurrentFile = currentFile, 
                            InstantTransferRate = instantTransferRate,
                            AverageTransferRate = averageTransferRate,
                            CurrentPercent = ((double)disc.BytesCopied / (double)disc.DataSize)
                        });

                        bytesCopiedSinceLastupdate = 0;
                        lastSample = sw.ElapsedMilliseconds;
                    }
                };

                copier.OnComplete += (progress) => {
                    file.Hash = copier.MD5_Hash;
                };

                await copier.CopyAsync(_cToken);

                file.Copied = true;
                file.Archived = true;
                file.ArchiveTimeUtc = DateTime.UtcNow;

                currentFile++;
            }

            sw.Stop();

            this.OnStepComplete(disc, _stats, ProcessStep.CopyFiles);
        }

        private async Task GenerateIndexFiles(DiscDetail disc, Stopwatch discSw)
        {
            this.OnStepStart(disc, _stats, ProcessStep.CreateDiscIndex);

            DiscIndexProgress progress = new DiscIndexProgress()
            {
                Elapsed = discSw.Elapsed,
                CurrentFile = 0,
                TotalFiles = disc.TotalFiles,
                CurrentPercent = 0.0
            };

            this.OnDiscIndexProgress(disc, _stats, progress);

            if (!Directory.Exists(SysInfo.Directories.Index))
                Directory.CreateDirectory(SysInfo.Directories.Index);

            string txtIndexPath = PathUtils.CleanPathCombine(SysInfo.Directories.Index, "index.txt");
            string discIndexTxtPath = PathUtils.CleanPathCombine(disc.RootStagingPath, "index.txt");
 
            bool createMasterIndex = !File.Exists(txtIndexPath);      
            string headerLine = $"Disc   {"Archive Date (UTC)".PadRight(19)}   {"Create Date (UTC)".PadRight(19)}   {"Modify Date (UTC)".PadRight(19)}   {"Size".PadLeft(12)}   Path";      

            using (FileStream masterIndexFS = File.Open(txtIndexPath, FileMode.Append, FileAccess.Write))
            using (StreamWriter masterIndex = new StreamWriter(masterIndexFS))
            using (FileStream discIndexFS = File.Open(discIndexTxtPath, FileMode.Create, FileAccess.Write))
            using (StreamWriter discIndex = new StreamWriter(discIndexFS))
            {
                // if we are creating the file for the firs time, write header line
                if (createMasterIndex)
                    await masterIndex.WriteLineAsync(headerLine);

                await discIndex.WriteLineAsync(headerLine);

                // mark this as finalized so it won't be touched again after this
                disc.Finalized = true;

                Stopwatch sw = Stopwatch.StartNew();

                // Write the human readable index
                foreach (DiscSourceFile file in disc.Files.OrderBy(x => x.RelativePath))
                {
                    if (_cToken.IsCancellationRequested)
                        return;

                    progress.CurrentFile++;

                    string line = "";
                    line += disc.DiscNumber.ToString("0000");
                    line += "   ";
                    line += file.ArchiveTimeUtc.ToString("yyyy-MM-dd HH:mm:ss");
                    line += "   ";
                    line += file.CreationTimeUtc.ToString("yyyy-MM-dd HH:mm:ss");
                    line += "   ";
                    line += file.LastWriteTimeUtc.ToString("yyyy-MM-dd HH:mm:ss");
                    line += "   ";
                    line += file.Size.ToString().PadLeft(12);
                    line += "   ";
                    line += file.RelativePath;

                    Task writeLineTask1 = discIndex.WriteLineAsync(line);
                    Task writeLineTask2 = masterIndex.WriteLineAsync(line);

                    Task.WaitAll(writeLineTask1, writeLineTask2);

                    if (sw.ElapsedMilliseconds > _updateFrequencyMs)
                    {
                        progress.CurrentPercent = (double)progress.CurrentFile / (double)disc.TotalFiles;
                        this.OnDiscIndexProgress(disc, _stats, progress);

                        sw.Restart();
                    }
                }

                sw.Stop();
                await masterIndex.FlushAsync();
                await discIndex.FlushAsync();
            }

            progress.CurrentFile = progress.TotalFiles;
            progress.CurrentPercent = 1.0;
            this.OnDiscIndexProgress(disc, _stats, progress);
            this.OnStepComplete(disc, _stats, ProcessStep.CreateDiscIndex);
        }

        private async Task GenerateHashFile(DiscDetail disc, Stopwatch discSw)
        {
            this.OnStepStart(disc, _stats, ProcessStep.GenerateHashFile);

            GenerateHashProgress progress = new GenerateHashProgress()
            {
                Elapsed = discSw.Elapsed,
                CurrentFile = 0,
                TotalFiles = disc.TotalFiles,
                CurrentPercent = 0.0
            };

            this.OnGenerateHashProgress(disc, _stats, progress);

            string destinationFile = PathUtils.CleanPathCombine(disc.RootStagingPath, "hashlist.txt");

            using (FileStream fileStream = File.Open(destinationFile, FileMode.OpenOrCreate, FileAccess.Write))
            using (StreamWriter streamWriter = new StreamWriter(fileStream))
            {
                await streamWriter.WriteLineAsync($"{"MD5 Hash".PadRight(32)}   File");

                Stopwatch sw = Stopwatch.StartNew();

                foreach (DiscSourceFile file in disc.Files.Where(x => x.Size > 0 && x.Hash != null).OrderBy(x => x.RelativePath))
                {
                    if (_cToken.IsCancellationRequested)
                        return;

                    progress.CurrentFile++;
                    await streamWriter.WriteLineAsync($"{file.Hash.PadRight(32)}   {file.RelativePath}");

                    if (sw.ElapsedMilliseconds > _updateFrequencyMs)
                    {
                        progress.CurrentPercent = (double)progress.CurrentFile / (double)disc.TotalFiles;
                        this.OnGenerateHashProgress(disc, _stats, progress);

                        sw.Restart();
                    }
                }

                sw.Stop();

                streamWriter.Flush();
            }

            progress.CurrentFile = progress.TotalFiles;
            progress.CurrentPercent = 1.0;
            this.OnGenerateHashProgress(disc, _stats, progress);
            this.OnStepComplete(disc, _stats, ProcessStep.GenerateHashFile);
        }

        private async Task WriteDiscInfo(DiscDetail disc, Stopwatch masterSw)
        {
            this.OnStepStart(disc, _stats, ProcessStep.WriteDiscInfo);

            DiscSummary discInfo = disc.GetDiscInfo();

            JsonSerializerSettings settings = new JsonSerializerSettings() {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Newtonsoft.Json.Formatting.Indented
            };

            // TODO: is there any way to produce a progress?
            string jsonFilePath = PathUtils.CleanPathCombine(disc.RootStagingPath, "disc_info.json");
            string json = JsonConvert.SerializeObject(discInfo, settings);
            await File.WriteAllTextAsync(jsonFilePath, json, Encoding.UTF8);

            this.OnStepComplete(disc, _stats, ProcessStep.WriteDiscInfo);
        }

        private async Task CreateISOFile(DiscDetail disc, Stopwatch discSw)
        {
            this.OnStepStart(disc, _stats, ProcessStep.CreateISOFile);

            CreateIsoProgress progress = new CreateIsoProgress()
            {
                Elapsed = discSw.Elapsed,
                CurrentPercent = 0.0
            };

            this.OnCreateIsoProgress(disc, _stats, progress);

            if (!Directory.Exists(SysInfo.Directories.ISO))
                Directory.CreateDirectory(SysInfo.Directories.ISO);

            ISO_Creator creator = new ISO_Creator(disc.DiscName, disc.RootStagingPath, disc.IsoPath);

            creator.OnProgressChanged += (currentPercent) => {
                progress.Elapsed = discSw.Elapsed;
                progress.CurrentPercent = currentPercent;
                this.OnCreateIsoProgress(disc, _stats, progress);
            };

            await creator.CreateIsoAsync(_cToken);

            disc.IsoCreated = true;
            Directory.Delete(disc.RootStagingPath, true);

            this.OnStepComplete(disc, _stats, ProcessStep.CreateISOFile);
        }

        private async Task ReadIsoHash(DiscDetail disc, Stopwatch discSw)
        {
            this.OnStepStart(disc, _stats, ProcessStep.ReadIsoHash);

            using (FileStream fs = File.OpenRead(disc.IsoPath))
            {
                Md5StreamGenerator generator = new Md5StreamGenerator(fs);

                generator.OnProgressChanged += (progress) => this.OnReadIsoMd5Progress(disc, _stats, discSw.Elapsed, progress);

                disc.Hash = await generator.GenerateAsync(_cToken);
            }

            this.OnStepStart(disc, _stats, ProcessStep.ReadIsoHash);
        }

        private async Task SaveJsonData(DiscDetail disc, Stopwatch discSw)
        {
            this.OnStepStart(disc, _stats, ProcessStep.SaveToIndex);

            await disc.SaveToIndexAsync();

            this.OnStepComplete(disc, _stats, ProcessStep.SaveToIndex);
        }

    }
}