using System;
using System.Collections.Generic;
using System.Linq;
using Archiver.Utilities.CSD;
using Archiver.Utilities.Shared;
using Newtonsoft.Json;

namespace Archiver.Classes.CSD
{


    public class CsdDetail
    {
        #region Private members

        private int _csdNumber = 0;
        private string _csdName = "CSD000";
        private List<CsdSourceFile> _files = new List<CsdSourceFile>();
        private List<CsdSourceFile> _pendingFiles = new List<CsdSourceFile>();
        private List<DateTime> _writeDtmUtc = new List<DateTime>();
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
        public IReadOnlyList<DateTime> WriteDtmUtc => (IReadOnlyList<DateTime>)_writeDtmUtc;
        public string CsdName => _csdName;
        public int TotalFiles => _totalFiles;
        public int BlockSize { get; set; }
        public long TotalSpace { get; set; }
        public long FreeSpace => this.TotalSpace - _dataSizeOnDisk;
        public long DataSize => _dataSize;

        public long DataSizeOnDisc => _dataSizeOnDisk;
        public IReadOnlyList<CsdSourceFile> Files => (IReadOnlyList<CsdSourceFile>)_files;
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
        public long UsableFreeSpace => this.TotalSpace - Config.CsdReservedCapacity - this._dataSizeOnDisk - this._pendingBytesOnDisk;
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

            CsdGlobals._destinationCsds.Add(this);
        }

        public void AddFile(CsdSourceFile file)
        {
            if (file.Copied)
            {
                this._files.Add(file);

                _totalFiles++;
                _dataSize += file.Size;
                _dataSizeOnDisk += Helpers.RoundToNextMultiple(file.Size, this.BlockSize);
            }   
            else
            {
                this._pendingFiles.Add(file);

                this._pendingFileCount++;
                this._pendingBytes += file.Size;
                this._pendingBytesOnDisk += Helpers.RoundToNextMultiple(file.Size, this.BlockSize);
            }
        }

        public void AddWriteDate()
            => this._writeDtmUtc.Add(DateTime.UtcNow);

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
            this._totalFiles = this._files.Count();
            this._dataSize = this._files.Sum(x => x.Size);
            this._dataSizeOnDisk = this._files.Sum(x => Helpers.RoundToNextMultiple(x.Size, this.BlockSize));
            this.BytesCopied = this.DataSize;

            if (includePending)
            {
                this._pendingFileCount = this._pendingFiles.Count();
                this._pendingBytes = this._pendingFiles.Sum(x => x.Size);
                this._pendingBytesOnDisk = this._pendingFiles.Sum(x => Helpers.RoundToNextMultiple(x.Size, this.BlockSize));
            }
        }

        public void MarkFileCopied(CsdSourceFile file)
        {
            if (_pendingFiles.Contains(file))
            {
                _pendingFiles.Remove(file);

                _pendingFileCount--;
                _pendingBytes -= file.Size;
                _pendingBytesOnDisk -= Helpers.RoundToNextMultiple(file.Size, this.BlockSize);
            }

            if (!_files.Contains(file))
            {
                this._files.Add(file);

                _totalFiles++;
                _dataSize += file.Size;
                _dataSizeOnDisk += Helpers.RoundToNextMultiple(file.Size, this.BlockSize);
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

            newCopy._writeDtmUtc = _writeDtmUtc;
            newCopy._files = this._files.ToList();

            if (includePending)
                newCopy._pendingFiles = this._pendingFiles.ToList();

            newCopy.SyncStats(includePending);
             
            return newCopy;
        }
    }
}