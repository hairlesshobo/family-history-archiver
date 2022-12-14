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
using System.IO;
using System.Linq;
using System.Text;
using FoxHollow.Archiver.Shared;
using FoxHollow.Archiver.Shared.Interfaces;
using FoxHollow.Archiver.Shared.Utilities;
using Newtonsoft.Json;

namespace FoxHollow.Archiver.Shared.Classes.CSD
{


    public class CsdDetail : IMediaDetail
    {
        #region Private members

        private int _csdNumber = 0;
        private string _csdName = "CSD000";
        private List<CsdSourceFile> _pendingFiles = new List<CsdSourceFile>();
        private long _pendingBytes = 0;
        private long _pendingBytesOnDisk = 0;
        private long _pendingFileCount = 0;
        private int _totalFiles = 0;
        private long _dataSize = 0;
        private long _dataSizeOnDisk = 0;

        #endregion Private members



        public int CsdNumber 
        { 
            get => _csdNumber;
            set
            {
                _csdNumber = value;
                _csdName = $"CSD{this.CsdNumber.ToString("000")}";
            }
        }
        public DateTime RegisterDtmUtc { get; set; }
        public List<DateTime> WriteDtmUtc { get; set; } = new List<DateTime>();
        public string CsdName => _csdName;
        public int TotalFiles => _totalFiles;
        public int BlockSize { get; set; }
        public long TotalSpace { get; set; }
        public long FreeSpace => this.TotalSpace - _dataSizeOnDisk;
        public long DataSize => _dataSize;

        public long DataSizeOnDisc => _dataSizeOnDisk;
        // public IReadOnlyList<CsdSourceFile> Files => (IReadOnlyList<CsdSourceFile>)_files;
        public List<CsdSourceFile> Files { get; set; } = new List<CsdSourceFile>();

        public CsdDriveInfo DriveInfo { get; set; } = new CsdDriveInfo();
        public long BytesCopied { get; set; }

        public List<CsdVerificationResult> Verifications { get; set; } = new List<CsdVerificationResult>();
        
        #region JSON hidden public properties
        [JsonIgnore]
        public bool DiskFull => this.UsableFreeSpace <= this.BlockSize;

        [JsonIgnore]
        public bool HasPendingWrites => this.PendingFileCount > 0;

        [JsonIgnore]
        public IEnumerable<CsdSourceFile> PendingFiles => _pendingFiles;

        [JsonIgnore]
        public long UsableFreeSpace => this.TotalSpace - SysInfo.Config.CSD.ReservedCapacityBytes - this._dataSizeOnDisk - this._pendingBytesOnDisk;
        [JsonIgnore]
        public long PendingBytes => _pendingBytes;
        [JsonIgnore]
        public long PendingBytesOnDisk => _pendingBytesOnDisk;

        [JsonIgnore]
        public long PendingFileCount => this._pendingFileCount;
        

        [JsonIgnore]
        public int DaysSinceLastVerify {
            get
            {
                if (this.Verifications.Count() <= 0)
                    return int.MaxValue;
                else
                    return (DateTime.UtcNow - this.Verifications.Max(x => x.VerificationDTM)).Days;
            }
        }

        [JsonIgnore]
        public bool LastVerifySuccess {
            get
            {
                if (this.Verifications.Count() == 0)
                    return false;
                else
                {
                    CsdVerificationResult lastResult = this.Verifications.OrderBy(x => x.VerificationDTM).LastOrDefault();

                    if (lastResult == null)
                        return false;
                    
                    return lastResult.CsdValid;
                }
            }
        }
        
        
        #endregion JSON hidden public properties
        
        public CsdDetail()
        { }

        public CsdDetail(int csdNumber, int blockSize, long totalSpace)
        {
            this.BlockSize = blockSize;
            this.TotalSpace = totalSpace;
            this.RegisterDtmUtc = DateTime.UtcNow;
            this.CsdNumber = csdNumber;
        }

        public void AddFile(CsdSourceFile file)
        {
            if (file.Copied)
            {
                this.Files.Add(file);

                _totalFiles++;
                _dataSize += file.Size;
                _dataSizeOnDisk += HelpersNew.RoundToNextMultiple(file.Size, this.BlockSize);
            }   
            else
            {
                this._pendingFiles.Add(file);

                this._pendingFileCount++;
                this._pendingBytes += file.Size;
                this._pendingBytesOnDisk += HelpersNew.RoundToNextMultiple(file.Size, this.BlockSize);
            }
        }

        public void AddWriteDate()
            => this.WriteDtmUtc.Add(DateTime.UtcNow);

        public void SortPendingFiles()
        {
            this._pendingFiles = this._pendingFiles.OrderBy(x => x.RelativePath).ToList();
        }

        public void RecordVerification(DateTime verifyDtm, bool csdValid)
        {
            if (this.Verifications == null)
                this.Verifications = new List<CsdVerificationResult>();

            this.Verifications.Add(new CsdVerificationResult()
            {
                VerificationDTM = verifyDtm,
                CsdValid = csdValid
            });
        }

        public void SyncStats(bool includePending = false)
        {
            this._totalFiles = this.Files.Count();
            this._dataSize = this.Files.Sum(x => x.Size);
            this._dataSizeOnDisk = this.Files.Sum(x => HelpersNew.RoundToNextMultiple(x.Size, this.BlockSize));
            this.BytesCopied = this.DataSize;

            if (includePending)
            {
                this._pendingFileCount = this._pendingFiles.Count();
                this._pendingBytes = this._pendingFiles.Sum(x => x.Size);
                this._pendingBytesOnDisk = this._pendingFiles.Sum(x => HelpersNew.RoundToNextMultiple(x.Size, this.BlockSize));
            }
        }

        public void MarkFileCopied(CsdSourceFile file)
        {
            if (_pendingFiles.Contains(file))
            {
                _pendingFiles.Remove(file);

                _pendingFileCount--;
                _pendingBytes -= file.Size;
                _pendingBytesOnDisk -= HelpersNew.RoundToNextMultiple(file.Size, this.BlockSize);
            }

            if (!this.Files.Contains(file))
            {
                this.Files.Add(file);

                _totalFiles++;
                _dataSize += file.Size;
                _dataSizeOnDisk += HelpersNew.RoundToNextMultiple(file.Size, this.BlockSize);
            }
        }

        /// <summary>
        ///     Create a clone of this object but only include files that have been successfully copied
        /// </summary>
        public CsdDetail TakeSnapshot(bool includePending = false)
        {
            CsdDetail newCopy = new CsdDetail()
            {
                RegisterDtmUtc = this.RegisterDtmUtc,
                CsdNumber = this.CsdNumber,
                TotalSpace = this.TotalSpace,
                BlockSize = this.BlockSize,
                BytesCopied = this.BytesCopied,
                DriveInfo = this.DriveInfo,
                Verifications = this.Verifications
            };

            newCopy.WriteDtmUtc = this.WriteDtmUtc.ToList();
            newCopy.Files = this.Files.ToList();

            if (includePending)
                newCopy._pendingFiles = this._pendingFiles.ToList();

            newCopy.SyncStats(includePending);
             
            return newCopy;
        }

        private static void SaveCsdDetailToJson(string jsonFilePath, CsdDetail csd)
        {
            

        }

        public static void SaveDetailToIndex(CsdDetail csd)
        {
            string destDir = SysInfo.Directories.JSON;
            string jsonFilePath = PathUtils.CleanPathCombine(destDir, $"csd_{csd.CsdNumber.ToString("000")}.json");

            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            SaveCsdDetailToJson(jsonFilePath, csd);
        }

        public void SavetoCsd(string driveLetter)
            => this.SaveToJson(driveLetter, "info.json");

        public void SaveToIndex()
            => this.SaveToJson();

        public void SaveToJson(string destinationDir = null, string fileName = null)
        {
            if (String.IsNullOrWhiteSpace(destinationDir))
                destinationDir = SysInfo.Directories.JSON;

            if (String.IsNullOrWhiteSpace(fileName))
                fileName = $"csd_{this.CsdNumber.ToString("000")}.json";

            string jsonFilePath = Path.Combine(destinationDir, fileName);

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(this.TakeSnapshot(), new Newtonsoft.Json.JsonSerializerSettings() {
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                Formatting = Newtonsoft.Json.Formatting.Indented
            });
            
            File.WriteAllText(jsonFilePath, json, Encoding.UTF8);
        }
    }
}