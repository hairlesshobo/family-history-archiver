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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FoxHollow.Archiver.Shared.Models.Config;
using FoxHollow.Archiver.Shared;
using FoxHollow.Archiver.Shared.Utilities;
using FoxHollow.FHM.Shared;
using FoxHollow.FHM.Shared.Classes;
using FoxHollow.FHM.Shared.Exceptions;
using FoxHollow.FHM.Shared.Models;
using FoxHollow.FHM.Shared.Utilities;
// using FoxHollow.LibSMB2Sharp;
using FoxHollow.TerminalUI;
using FoxHollow.TerminalUI.Elements;
using FoxHollow.TerminalUI.Types;

namespace FoxHollow.Archiver.TestCLI
{
    class Program
    {
        static void Main()
        {
            Utils.RequireSupportedOS();
            AppInfo.InitPlatform();

            try
            {
                Terminal.Initialize("", "TestCLI");

                // var detail = Windows.WMI.GetCdromDetail("A:");

                // Console.WriteLine(JsonSerializer.Serialize(detail, new JsonSerializerOptions()
                // {
                //     IgnoreNullValues = false,
                //     IncludeFields = true,
                //     WriteIndented = true
                // }));


                //% slax bootloader, known good MD5: 3c78799690d95bd975e352020fc2acb8 linux dd OK, linux archiver OK, windows dd ??, windows archiver OK
                //% archive 0001   , known good MD5: d8f3a48ab0205c2debe1aa55bc0bb6ea linux dd OK, linux archiver OK, windows dd ??, windows archiver OK
                //% archive 0012   , known good MD5: 0ee70125b6a67db3487116844b6c861b linux dd OK, linux archiver OK, windows dd ??, windows archiver OK

                string knownHash = "0fbd7bf76639134005460f9321ae1d25";

                CancellationTokenSource cts = new CancellationTokenSource();

                Terminal.InitStatusBar(
                    new StatusBarItem(
                        "Cancel",
                        (key) => {
                            cts.Cancel();
                            Terminal.Stop();

                            return Task.CompletedTask;
                        },
                        Key.MakeKey(ConsoleKey.C, ConsoleModifiers.Control)
                    )
                );
                
                //! archive 0012
                //! actual raw size: 24,935,661,568 
                //! windows IOCTL_STORAGE_READ_CAPACITY reported length: 24,935,661,568
                //! WMI reported size: 24,935,110,656
                //! windows explorer reported size: 24,935,110,656

                // var kvtElapsedTime = new KeyValueText("Elapsed Time", null, -16);
                // Terminal.NextLine();

                // var kvtVerified = new KeyValueText("Verified", Formatting.GetFriendlySize(0), -16);
                // Terminal.NextLine();

                // var kvtCurrentRate = new KeyValueText("Current Rate", Formatting.GetFriendlyTransferRate(0), -16);
                // Terminal.NextLine();

                // var kvtAvgRate = new KeyValueText("Average Rate", Formatting.GetFriendlyTransferRate(0), -16);
                // Terminal.NextLine();
                // Terminal.NextLine();

                // var progressBar = new ProgressBar();
                // Terminal.NextLine();
                // Terminal.NextLine();

                // kvtElapsedTime.Show();
                // kvtVerified.Show();
                // kvtCurrentRate.Show();
                // kvtAvgRate.Show();
                // progressBar.Show();

                // Stopwatch sw = Stopwatch.StartNew();

                Task terminalTask = Terminal.StartAsync();

                Task mainTask = Task.Run(async () => 
                {
                    await Task.Delay(2000);
                //     //smb://den;admin@nas.cz.foxhollow.cc/Studio/Video/Final Discs/EVA Basketball DVDs/2007/07-11-20 -- Episcopal.iso
                //     using (Smb2Context smb2 = new Smb2Context(connectionString: "smb://den;admin@nas.cz.foxhollow.cc/Movies", password: "Lh2Oog2y"))
                //     {
                //         Smb2Share share = smb2.OpenShare();

                //         Smb2FileEntry entry = share.GetFile("/HD/Guardians of the Galaxy/Guardians of the Galaxy.mkv");
                        
                //         using (Stream reader = entry.OpenReader())
                //         {
                //             Md5StreamGenerator generator = new Md5StreamGenerator(reader, (int)smb2.MaxReadSize);
                //             generator.OnProgressChanged += (progress) =>
                //             {
                //                 kvtVerified.UpdateValue($"{Formatting.GetFriendlySize(progress.TotalBytesProcessed)} / {Formatting.GetFriendlySize(progress.TotalBytes)}");
                //                 kvtAvgRate.UpdateValue(Formatting.GetFriendlyTransferRate(progress.AverageRate));
                //                 kvtCurrentRate.UpdateValue(Formatting.GetFriendlyTransferRate(progress.InstantRate));

                //                 progressBar.UpdateProgress(progress.PercentCompleted);
                //                 kvtElapsedTime.UpdateValue(sw.Elapsed.ToString());
                //             };

                //             string hash = await generator.GenerateAsync(cts.Token);

                //             sw.Stop();

                //             bool discValid = (knownHash.ToLower() == hash.ToLower());
                //         }
                //     }
                });


                Task.WaitAll(mainTask, terminalTask);
                // Terminal.WaitForStop();
            }
            catch (Exception e)
            {
                Console.CursorLeft = 0;
                Console.CursorTop = Console.CursorTop + 5;
                Formatting.WriteLineC(ConsoleColor.Red, "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                Console.WriteLine();
                Formatting.WriteLineC(ConsoleColor.Red, $"Unhandled exception occurred: {e.Message}");
                Console.WriteLine();
                if (e.InnerException != null)
                {
                    Formatting.WriteLineC(ConsoleColor.Red, $"Inner Exception: {e.InnerException.ToString()}");
                    Console.WriteLine();
                }
                Formatting.WriteLineC(ConsoleColor.Red, e.StackTrace);
                Console.WriteLine();
                Formatting.WriteLineC(ConsoleColor.Red, "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                Console.WriteLine();
                AppInfo.WriteSystemInfo();
                Console.WriteLine();
                Console.WriteLine();
                Console.Write("Press ");
                Formatting.WriteC(ConsoleColor.DarkYellow, "<any key>");
                Console.WriteLine(" to terminate application");

                Console.ReadKey(true);

                return;
            }
        }
        
        static void MainOld()
        {
            Utils.RequireSupportedOS();
            AppInfo.InitPlatform();

            Formatting.WriteLineC(ConsoleColor.Green, $"Archive TestCLI component starting up. (PID: {SysInfo.PID})");
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            AppInfo.WriteSystemInfo(true);

            if (AppInfo.ConfigErrors.Count > 0)
            {
                Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
                Formatting.WriteLineC(ConsoleColor.Red, $"{AppInfo.ConfigErrors.Count} Configuration ERROR(s) Found!");
                Console.WriteLine();

                int fieldWidth = AppInfo.ConfigErrors.Max(x => x.Field.Length)+2;

                foreach (ValidationError error in AppInfo.ConfigErrors)
                {
                    Formatting.WriteC(ConsoleColor.Cyan, "Field: ");
                    Console.WriteLine(error.Field.PadRight(fieldWidth));
                    Formatting.WriteC(ConsoleColor.Red, "Error: ");
                    Console.WriteLine(error.Error);
                    Console.WriteLine();
                }

                Formatting.WriteLineC(ConsoleColor.Red, "Application terminating due to error!");
                Console.ReadLine();
                Environment.Exit(1);
            }

            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Console.WriteLine();

            // TODO: Test this with disc in the drive
            //var drives = DriveInfo.GetDrives().Where(x => x.DriveType == DriveType.CDRom);


            // Console.WriteLine(DiskUtils.LinuxGetFileSize("/home/flip/cv_debug.log"));

            // Console.WriteLine($"sr0: {OpticalDriveUtils.GenerateDiscMD5("sr0")}");
            // Console.WriteLine($"sr0: {OpticalDriveUtils.WindowsGenerateDiscMD5("A:")}");

            List<OpticalDrive> drives = OpticalDriveUtils.GetDrives();

            Console.WriteLine(JsonSerializer.Serialize(drives, new JsonSerializerOptions() { WriteIndented = true }));
            // Console.WriteLine(OpticalDriveUtils.LinuxGetMountPoint("sr0"));

            if (SysInfo.OSType == OSType.Windows)
            {
                Console.WriteLine();
                Console.WriteLine("Press any key to close the window...");
                Console.ReadKey();
            }

        // private static void MakeMD5()
        // {
        //     //% slax bootloader, known good MD5: 3c78799690d95bd975e352020fc2acb8 linux dd OK, linux archiver OK, windows dd ??, windows archiver ??
        //     //% archive 0001   , known good MD5: d8f3a48ab0205c2debe1aa55bc0bb6ea linux dd OK, linux archiver OK, windows dd ??, windows archiver ??

        //     using (LinuxNativeStreamReader reader = new LinuxNativeStreamReader(LinuxNativeStreamReader.StreamSourceType.Disk, "/dev/sr0"))
        //     {
        //         Md5StreamGenerator generator = new Md5StreamGenerator(reader);
        //         generator.OnProgressChanged += (progress) =>
        //         {
        //             Console.WriteLine($"{progress.PercentCopied}%");
        //         };

        //         generator.OnComplete += (hash) =>
        //         {
        //             Console.WriteLine(hash);
        //         };

        //         generator.GenerateAsync();

        //         // Console.WriteLine(md5hash);
        //     }

        }

        
    }
}
