using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Archiver.Classes.Tape;
using Archiver.Utilities.Shared;
using ICSharpCode.SharpZipLib.Tar;
using Newtonsoft.Json;

namespace Archiver.Utilities.Tape
{
    public class TapeProcessor
    {
        private TapeSourceInfo _sourceInfo;
        private TapeDetail _tapeDetail;
        private TapeStatus _status;

        public TapeProcessor(TapeSourceInfo sourceInfo)
        {
            Initialize(sourceInfo);
        }

        private void Initialize(TapeSourceInfo sourceInfo)
        {
            _sourceInfo = sourceInfo;
            _tapeDetail = new TapeDetail()
            {
                ID = sourceInfo.ID,
                Name = sourceInfo.Name,
                Description = sourceInfo.Description,
                SourceInfo = sourceInfo,
                WriteDTM = DateTime.UtcNow
            };
        }

        public void ProcessTape()
        {
            _status = new TapeStatus(_tapeDetail);
            _status.StartTimer();

            Thread rewindThread = new Thread(RewindTape);
            rewindThread.Start();

            // give the rewind a bit to start and mark the status
            Thread.Sleep(500);

            IndexAndCountFiles();
            SizeFiles();

            if (rewindThread.IsAlive)
            {
                lock (_status)
                {
                    _status.WriteStatus("Rewinding tape");
                }

                rewindThread.Join();
            }

            _status.ShowBeforeSummary(TapeUtils.GetTapeInfo());

            if (CheckDestinationSize())
            {
                WriteTapeSummary();
                WriteTapeJsonSummary();
                ArchiveFiles();

                _status.ShowAfterSummary(TapeUtils.GetTapeInfo());

                WriteTapeJsonToIndex();
                RewindTape();

                if (Config.TapeAutoEject)
                    EjectTape();
            }

            lock (_status)
            {
                _status.WriteStatus("Complete!");
            }

            _status.Dispose();
        }

        private void RewindTape()
        {
            lock (_status)
            {
                _status.WriteStatus("Rewinding tape");
            }

            using (TapeOperator tape = new TapeOperator(Config.TapeDrive, false))
            {
                tape.RewindTape();
            }
        }

        private void EjectTape()
        {
            lock (_status)
            {
                _status.WriteStatus("Ejecting tape");
            }

            using (TapeOperator tape = new TapeOperator(Config.TapeDrive, false))
            {
                tape.EjectTape();
            }
        }
        
        private void IndexAndCountFiles()
        {
            lock (_status)
            {
                _status.WriteStatus("Counting source files");
            }

            FileScanner scanner = new FileScanner(_tapeDetail);

            scanner.OnProgressChanged += (newFiles, excludedFiles) => {
                _status.FileScanned(newFiles, excludedFiles);
            };

            scanner.OnComplete += () => {
                _status.FileScanned(_tapeDetail.FileCount, _tapeDetail.ExcludedFileCount, true);
            };

            Thread scanThread = new Thread(scanner.ScanFiles);
            scanThread.Start();
            scanThread.Join();
        }

        private void SizeFiles()
        {
            lock (_status)
            {
                _status.WriteStatus("Calculating source file sizes");
            }

            FileSizer sizer = new FileSizer(_tapeDetail);

            sizer.OnProgressChanged += (currentFile) => {
                _status.FileSized(currentFile);
            };

            sizer.OnComplete += () => {
                _status.FileSized(_tapeDetail.FileCount, true);
            };

            Thread sizeThread = new Thread(sizer.SizeFiles);
            sizeThread.Start();
            sizeThread.Join();
        }

        private bool CheckDestinationSize()
        {
            TapeInfo info = TapeUtils.GetTapeInfo();

            // if the archive is smaller than the tape, we know we are 
            // good so we just return true
            if (info.MediaInfo.Capacity > _tapeDetail.TotalArchiveBytes)
                return true;

            double ratioRequired = (double)_tapeDetail.TotalArchiveBytes / (double)info.MediaInfo.Capacity;

            if (ratioRequired > 1.0)
                return _status.ShowTapeWarning(ratioRequired);

            return false;
        }

        private void WriteTapeSummary()
        {
            lock (_status)
            {
                _status.WriteStatus("Writing summary to tape");
            }

            using (TapeOperator tape = new TapeOperator(Config.TapeDrive, (uint)Config.TapeTextBlockSize, false))
            {
                string templatePath = Path.Join(Directory.GetCurrentDirectory(), "templates", "tape_summary.txt");
                string[] lines = File.ReadAllLines(templatePath);
                string summaryOutput = String.Empty;
                string dirList = String.Join("\n", _tapeDetail.FlattenDirectories().Select(x => "  " + x.RelativePath).ToArray());

                foreach (string line in lines)
                {
                    summaryOutput += line.Replace("<%TAPE_NAME%>", _tapeDetail.Name)
                                         .Replace("<%WRITE_DATE%>", _tapeDetail.WriteDTM.ToString())
                                         .Replace("<%FILE_COUNT%>", String.Format("{0:n0}", _tapeDetail.FileCount))
                                         .Replace("<%DIR_COUNT%>", String.Format("{0:n0}", _tapeDetail.DirectoryCount))
                                         .Replace("<%SIZE_FRIENDLY%>", Shared.Formatting.GetFriendlySize(_tapeDetail.DataSizeBytes))
                                         .Replace("<%SIZE_BYTES%>", String.Format("{0:n0}", _tapeDetail.DataSizeBytes))
                                         .Replace("<%ARCHIVE_SIZE_FRIENDLY%>", Shared.Formatting.GetFriendlySize(_tapeDetail.TotalArchiveBytes))
                                         .Replace("<%ARCHIVE_SIZE_BYTES%>", String.Format("{0:n0}", _tapeDetail.TotalArchiveBytes))
                                         .Replace("<%DIRECTORY_LIST%>", dirList)
                                         + "\n";
                }

                byte[] buffer = TapeUtils.GetStringPaddedBytes(summaryOutput, tape.BlockSize);
                TapeUtils.WriteBytesToTape(tape, buffer, false);
                tape.WriteFilemark();
            }
        }

        private void WriteTapeJsonSummary()
        {
            lock (_status)
            {
                _status.WriteStatus("Writing json record to tape");
            }

            using (TapeOperator tape = new TapeOperator(Config.TapeDrive, (uint)Config.TapeTextBlockSize, false))
            {
                string json = JsonConvert.SerializeObject(_tapeDetail.GetSummary(), Newtonsoft.Json.Formatting.None);
                byte[] buffer = TapeUtils.GetStringPaddedBytes(json, tape.BlockSize);

                TapeUtils.WriteBytesToTape(tape, buffer, true);
                tape.WriteFilemark();
            }
        }

        
        
        private void ArchiveFiles()
        {
            uint blockSize = (uint)(512 * Config.TapeBlockingFactor);
            uint bufferBlockCount = (uint)Config.TapeMemoryBufferBlockCount;
            uint minBufferPercent = (uint)Config.TapeMemoryBufferMinFill;

            TapeInfo beforeInfo = TapeUtils.GetTapeInfo();
            long remainingCapacityBefore = beforeInfo.MediaInfo.Capacity - beforeInfo.MediaInfo.Remaining;

            using (TapeOperator tape = new TapeOperator(Config.TapeDrive, blockSize, false))
            {
                tape.SetDriveCompression();

                lock (_status)
                {
                    _status.WriteStatus("Allocating memory buffer");
                }

                using (TapeTarWriter writer = new TapeTarWriter(_tapeDetail, tape, blockSize, bufferBlockCount, minBufferPercent))
                {
                    lock (_status)
                    {
                        _status.WriteStatus("Writing data to tape");
                    }

                    writer.OnProgressChanged += (progress) => {
                        _status.UpdateTarWrite(progress);
                        _status.UpdateTapeWrite(progress);
                    };
                    
                    writer.Start();

                    lock (_status)
                    {
                        _status.WriteStatus("Freeing memory buffer");
                    }
                }

                // we want to pull the size BEFORE we write the filemark.. because it seems 
                // that a filemark take an entire block, which can mess with our verification and
                // compression ratio calculation
                TapeInfo afterInfo = TapeUtils.GetTapeInfo(tape);
                long remainingCapacityAfter = afterInfo.MediaInfo.Capacity - afterInfo.MediaInfo.Remaining;
                _tapeDetail.ArchiveBytesOnTape = (remainingCapacityAfter - remainingCapacityBefore);

                tape.WriteFilemark();
            }
        }

        private void WriteTapeJsonToIndex()
        {
            lock (_status)
            {
                _status.WriteStatus("Saving tape details to index");
            }

            Helpers.SaveTape(_tapeDetail);
        }
    }
}