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
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using FoxHollow.Archiver.Shared;
using FoxHollow.Archiver.Shared.Models.Config;
using FoxHollow.Archiver.Shared.Utilities;
using FoxHollow.FHM.Shared.Models;
using FoxHollow.FHM.Shared.TapeDrivers;

namespace FoxHollow.Archiver.TapeServer
{
    class Program
    {
        [DllImport("libc.so.6", EntryPoint = "getpid")]
        private static extern int GetPid();

        static void Main()
        {
            Utils.RequireSupportedOS();
            AppInfo.InitPlatform();
            

            int pid = GetPid();
            
            Formatting.WriteLineC(ConsoleColor.Green, $"Archive TapeServer component starting up. (PID: {pid})");
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
                Environment.Exit(1);
            }

            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Console.WriteLine();
            // ITapeDrive tapeDrive = TapeServerHelpers.GetTapeDrive(config);

            // NetworkServer server = new NetworkServer(config, tapeDrive);



            // uint blockSize = 512; //512 * 512;
            // int maxBlocks = 512;
            // int currentBlock = 0;
            // string device = "/home/flip/archive/test.txt"; // AppInfo.TapeDrive;
            // string text = String.Empty;

            // using (NativeLinuxTapeDriver tapeDrive = new NativeLinuxTapeDriver(device, blockSize))
            // {
            //     tapeDrive.Open();
            //     byte[] buffer = new byte[blockSize];
            //     bool endOfData = false;

            //     do
            //     {
            //         endOfData = tapeDrive.Read(buffer);

            //         int strlen = Array.IndexOf(buffer, (byte)0);

            //         if (!endOfData)
            //         {
            //             if (strlen > 0)
            //                 text += Encoding.UTF8.GetString(buffer, 0, strlen);
            //             else
            //                 text += Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            //         }

            //         currentBlock++;
            //     }
            //     while (!endOfData && currentBlock <= maxBlocks);

            //     Console.WriteLine(text);
            // }
            

            // server.StartControlServer();
        }
    }
}
