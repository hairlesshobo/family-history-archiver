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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FoxHollow.Archiver.Shared;
using FoxHollow.Archiver.Shared.Classes;
using FoxHollow.Archiver.Shared.Classes.CSD;
using FoxHollow.Archiver.Shared.Utilities;

namespace FoxHollow.Archiver.CLI.Utilities.CSD
{
    public static class Processing
    {
        private static int _updateFrequencyMs = 1000;

        public static void IndexAndCountFiles(CsdScanStats stats)
        {
            Console.WriteLine();

            FileScanner scanner = new FileScanner(stats);

            scanner.OnProgressChanged += (newFiles, existingFiles, excludedFiles, filesPerSecond) => {
                // TODO: Add deleted count here
                Status.FileScanned(newFiles, existingFiles, excludedFiles, 0, 0, filesPerSecond);
            };

            scanner.OnComplete += () => {
                Status.FileScanned(
                    stats.NewFileCount, 
                    stats.ExistingFileCount, 
                    stats.ExcludedFileCount, 
                    stats.DeletedFileCount, 
                    stats.ModifiedFileCount,
                    0, 
                    true
                );
            };

            Thread scanThread = new Thread(scanner.ScanFiles);
            scanThread.Start();
            scanThread.Join();
        }

        public static void SizeFiles(CsdScanStats stats)
        {
            Console.WriteLine();

            FileSizer sizer = new FileSizer(stats);

            sizer.OnProgressChanged += (currentFile, totalSize, filesPerSecond) => {
                Status.FileSized(currentFile, totalSize, filesPerSecond);
            };

            sizer.OnComplete += () => {
                Status.FileSized(stats.NewFileCount, stats.TotalSizePending, 0, true);
            };

            Thread sizeThread = new Thread(sizer.SizeFiles);
            sizeThread.Start();
            sizeThread.Join();
        }

        public static bool VerifyFreeSpace(CsdScanStats stats)
        {
            long requiredBytes = stats.NewFileEntries.Sum(x => x.Size);
            long freeSpace = stats.DestinationCsds.Sum(x => x.FreeSpace);
            bool sufficientSpace = false;

            if (freeSpace > requiredBytes)
                sufficientSpace = true;

            Status.WriteVerifyFreeSpace(requiredBytes, freeSpace, sufficientSpace);

            return sufficientSpace;
        }

        public static void DistributeFiles(CsdScanStats stats)
        {
            FileDistributor distributor = new FileDistributor(stats);

            distributor.OnProgressChanged += (currentFile, csdCount, filesPerSecond) => {
                Status.FileDistributed(currentFile, csdCount, filesPerSecond);
            };

            distributor.OnComplete += () => {
                int csdCount = stats.DestinationCsds.Where(x => x.HasPendingWrites == true).Count();
                Status.FileDistributed(stats.NewFileCount, csdCount, 0, true);
            };

            Thread distributeThread = new Thread(distributor.DistributeFiles);
            distributeThread.Start();
            distributeThread.Join();
        }

        public static void CopyFiles(string driveLetter, CsdDetail csd, Stopwatch masterSw, Action indexSaveCallback)
        {
            csd.AddWriteDate();

            long totalFilesToCopy = csd.PendingFileCount;
            long totalSizeToCopy = csd.PendingBytes;
            long bytesCopiedSinceLastupdate = 0;
            long bytesCopied = 0;
            int currentFile = 1;
            double averageTransferRate = 0.0;
            long sampleCount = 0;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            long lastSample = sw.ElapsedMilliseconds;
            long lastIndexSave = sw.ElapsedMilliseconds;

            while (csd.PendingFileCount > 0)
            {
                CsdSourceFile file = csd.PendingFiles.First();

                // check if we need to save the index
                if ((sw.ElapsedMilliseconds - lastIndexSave) > (SysInfo.Config.CSD.AutoSaveInterval * 1000))
                {
                    indexSaveCallback();

                    lastIndexSave = sw.ElapsedMilliseconds;
                }

                CustomFileCopier copier = file.ActivateCopy(driveLetter);

                copier.OnProgressChanged += (progress) => {
                    bytesCopied += progress.BytesCopiedSinceLastupdate;
                    bytesCopiedSinceLastupdate += progress.BytesCopiedSinceLastupdate;
                    file.DestinationCsd.BytesCopied += progress.BytesCopiedSinceLastupdate;

                    if ((sw.ElapsedMilliseconds - lastSample) > _updateFrequencyMs)
                    {
                        sampleCount++;

                        double timeSinceLastUpdate = (double)(sw.ElapsedMilliseconds - lastSample) / 1000.0;
                        double instantTransferRate = (double)bytesCopiedSinceLastupdate / timeSinceLastUpdate;

                        if (sampleCount == 1)
                            averageTransferRate = instantTransferRate;
                        else
                            averageTransferRate = averageTransferRate + (instantTransferRate - averageTransferRate) / sampleCount;

                        Status.WriteCsdCopyLine(csd, driveLetter, masterSw.Elapsed, currentFile, totalFilesToCopy, bytesCopied, totalSizeToCopy, instantTransferRate, averageTransferRate);

                        bytesCopiedSinceLastupdate = 0;
                        lastSample = sw.ElapsedMilliseconds;
                    }
                };

                copier.OnComplete += (progress) => {
                    file.Hash = copier.MD5_Hash;
                };

                Thread copyThread = new Thread(() => copier.Copy());
                copyThread.Start();
                copyThread.Join();

                file.Copied = true;
                file.ArchiveTimeUtc = DateTime.UtcNow;

                currentFile++;
            }

            sw.Stop();

            indexSaveCallback();
        }

        public static void GenerateHashFile(string driveLetter, CsdDetail csd, Stopwatch masterSw)
        {
            Status.WriteHashListFile(csd, masterSw.Elapsed, 0.0);

            string destinationFile = $"{driveLetter}/hashlist.txt";

            using (FileStream fs = File.Open(destinationFile, FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine($"{"MD5 Hash".PadRight(32)}   File");

                    Stopwatch stopwatch = Stopwatch.StartNew();
                    int currentLine = 1;
                    long lastSample = stopwatch.ElapsedMilliseconds;

                    foreach (CsdSourceFile file in csd.Files.Where(x => x.Size > 0 && x.Hash != null).OrderBy(x => x.RelativePath))
                    {
                        sw.WriteLine($"{file.Hash.PadRight(32)}   {file.RelativePath}");

                        if ((stopwatch.ElapsedMilliseconds - lastSample) > _updateFrequencyMs)
                        {
                            double currentPercent = ((double)currentLine / (double)csd.TotalFiles) * 100.0;
                            Status.WriteHashListFile(csd, masterSw.Elapsed, currentPercent);

                            lastSample = stopwatch.ElapsedMilliseconds;
                        }

                        currentLine++;
                    }

                    sw.Flush();
                }
            }

            Status.WriteHashListFile(csd, masterSw.Elapsed, 100.0);
        }

        public static void SaveJsonData(string driveLetter, CsdDetail csd, Stopwatch masterSw)
        {
            Status.WriteJsonLine(csd, masterSw.Elapsed);

            csd.SaveToIndex();
        }

        public static void GenerateIndexFiles(string driveLetter, CsdDetail csd, Stopwatch masterSw)
        {
            Status.WriteCsdIndex(csd, masterSw.Elapsed, 0.0);

            if (!Directory.Exists(SysInfo.Directories.Index))
                Directory.CreateDirectory(SysInfo.Directories.Index);

            string txtIndexPath = PathUtils.CleanPathCombine(SysInfo.Directories.Index, "index.txt");

            bool createMasterIndex = !File.Exists(txtIndexPath);      
            string headerLine = $"CSD   {"Archive Date (UTC)".PadRight(19)}   {"Create Date (UTC)".PadRight(19)}   {"Modify Date (UTC)".PadRight(19)}   {"Size".PadLeft(12)}   Path";      

            using (FileStream masterIndexFS = File.Open(txtIndexPath, FileMode.Append, FileAccess.Write))
            {
                using (StreamWriter masterIndex = new StreamWriter(masterIndexFS))
                {
                    // if we are creating the file for the firs time, write header line
                    if (createMasterIndex)
                        masterIndex.WriteLine(headerLine);

                    string csdIndexTxtPath = PathUtils.CleanPathCombine(driveLetter, "index.txt");

                    using (FileStream csdIndexFS = File.Open(csdIndexTxtPath, FileMode.Create, FileAccess.Write))
                    {
                        using (StreamWriter csdIndex = new StreamWriter(csdIndexFS))
                        {
                            csdIndex.WriteLine(headerLine);

                            Stopwatch sw = Stopwatch.StartNew();
                            int currentLine = 1;
                            long lastSample = sw.ElapsedMilliseconds;

                            // Write the human readable index
                            foreach (CsdSourceFile file in csd.Files.OrderBy(x => x.RelativePath))
                            {
                                string line = "";
                                line += csd.CsdNumber.ToString("000");
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

                                csdIndex.WriteLine(line);
                                masterIndex.WriteLine(line);

                                if ((sw.ElapsedMilliseconds - lastSample) > _updateFrequencyMs)
                                {
                                    double currentPercent = ((double)currentLine / (double)csd.TotalFiles) * 100.0;
                                    Status.WriteCsdIndex(csd, masterSw.Elapsed, currentPercent);

                                    lastSample = sw.ElapsedMilliseconds;
                                }

                                currentLine++;
                            }
                        }
                    }
                }
            }

            Status.WriteCsdIndex(csd, masterSw.Elapsed, 100.0);
        }

        private static void WriteCsdSummary(string driveLetter, CsdDetail csd, Stopwatch masterSw)
        {
            Status.WriteJsonLine(csd, masterSw.Elapsed);

            csd.SavetoCsd(driveLetter);

            Status.WriteJsonLine(csd, masterSw.Elapsed);
        }

        private static string DetectDriveLetter(CsdDetail csd, Stopwatch masterSw, long totalFileCount, long totalBytes)
        {
            while (1 == 1)
            {
                Status.WriteAttachCsdLine(csd, masterSw.Elapsed, totalFileCount, totalBytes);

                string driveLetter = CsdUtils.SelectDrive(csd, true);

                if (driveLetter == null)
                    Thread.Sleep(1000);
                else
                    return driveLetter;
            }
        }

        public static void ProcessCsdDrives(CsdScanStats stats)
        {
            Status.InitCsdDriveLines();

            foreach (CsdDetail csd in stats.DestinationCsds.Where(x => x.HasPendingWrites == true).OrderBy(x => x.CsdNumber))
            {
                long totalFiles = csd.PendingFileCount;
                long totalBytes = csd.PendingBytes;

                Stopwatch masterSw = new Stopwatch();
                masterSw.Start();

                string driveLetter = DetectDriveLetter(csd, masterSw, totalFiles, totalBytes);

                if (driveLetter == null)
                {
                    Status.ProcessComplete();

                    Formatting.WriteC(ConsoleColor.Yellow, "WARNING: ");
                    Console.WriteLine("Process canceled prematurely, not all data was archived!");
                    return;
                }

                masterSw.Restart();

                CopyFiles(driveLetter, csd, masterSw, () => {
                    CsdDetail snapshot = csd.TakeSnapshot();

                    GenerateIndexFiles(driveLetter, snapshot, masterSw);
                    GenerateHashFile(driveLetter, snapshot, masterSw);
                    WriteCsdSummary(driveLetter, snapshot, masterSw);
                    SaveJsonData(driveLetter, snapshot, masterSw);
                });

                masterSw.Stop();
                Status.WriteCsdComplete(csd, masterSw.Elapsed, totalFiles, totalBytes);
            }

            Status.ProcessComplete();

            Formatting.WriteLineC(ConsoleColor.Green, "Process complete!");
        }
    }
}