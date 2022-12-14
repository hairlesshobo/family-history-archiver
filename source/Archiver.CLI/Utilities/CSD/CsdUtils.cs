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
using System.Management;
using System.Threading;
using FoxHollow.Archiver.Shared.Classes.CSD;
using FoxHollow.Archiver.Shared.Utilities;
using FoxHollow.TerminalUI.Elements;
using FoxHollow.TerminalUI.Types;

namespace FoxHollow.Archiver.CLI.Utilities.CSD
{
    public static class CsdUtils
    {
        public enum CsdDriveType
        {
            Initialized,
            Uninitialized
        }

        public static CsdDetail ReadCsdIndex(string driveLetter)
        {
            string infoFilePath = PathUtils.CleanPathCombine(driveLetter, "info.json");

            if (!File.Exists(infoFilePath))
                throw new DriveNotFoundException($"Unable to find index file at the following location: {infoFilePath}");

            string jsonContent = File.ReadAllText(infoFilePath);

            return Newtonsoft.Json.JsonConvert.DeserializeObject<CsdDetail>(jsonContent);
        }


        public static int GetBlockSize(string DriveLetter)
        {
            DriveLetter = DriveLetter.Trim('/');
            DriveLetter = DriveLetter.Trim('\\');
            
            if (!DriveLetter.EndsWith(':'))
                DriveLetter += ':';

            DriveLetter += "\\\\";

            DriveLetter = DriveLetter.ToUpper();

            var query = new WqlObjectQuery($"SELECT Name, Label, BlockSize FROM Win32_Volume WHERE FileSystem='NTFS' AND Name='{DriveLetter}'");
            using (var searcher = new ManagementObjectSearcher(query))
            {
                var objects = searcher.Get().OfType<ManagementObject>();


                string resultStr = objects.Select(o => o.Properties["BlockSize"].Value.ToString())
                                    .FirstOrDefault();

                if (resultStr != null)
                    return Int32.Parse(resultStr);
            }

            throw new DriveNotFoundException($"Could not find drive {DriveLetter}");
        }

        public static string SelectDrive(CsdDetail csd, bool quiet)
            => SelectDrive(CsdDriveType.Initialized, csd, quiet);

        public static string SelectDrive(CsdDriveType driveType = CsdDriveType.Initialized)
            => SelectDrive(driveType, null, false);

        private static string SelectDrive(CsdDriveType driveType, CsdDetail csd, bool quiet)
        {
            // if a CSD was provided, we MUST find only initialized drives
            if (csd != null)
                driveType = CsdDriveType.Initialized;

            IEnumerable<DriveInfo> driveQuery = DriveInfo.GetDrives()
                .Where(x => x.DriveType == DriveType.Fixed || x.DriveType == DriveType.Removable);

            if (driveType == CsdDriveType.Initialized)
            {
                if (csd != null)
                    driveQuery = driveQuery.Where(x => x.VolumeLabel == csd.CsdName);
                else
                    driveQuery = driveQuery.Where(x => x.VolumeLabel.StartsWith("CSD") && Int32.TryParse(x.VolumeLabel.Substring(3), out int _));
                    
            }
            else
                driveQuery = driveQuery.Where(x => x.VolumeLabel == "CSD___");
                
            List<DriveInfo> drives = driveQuery.ToList();

            if (drives.Count == 0) 
            {
                if (!quiet)
                {
                    Formatting.WriteC(ConsoleColor.Red, "ERROR:");
                    Console.WriteLine("No attached CSD drives were detected on this system!");
                }
                return null;
            }

            string selectedDrive = drives[0].Name.TrimEnd('\\');
            
            if (drives.Count == 1)
            {
                if (!quiet)
                {
                    Formatting.WriteLineC(ConsoleColor.DarkGreen, $"Automatically selected Drive {selectedDrive}");
                    Thread.Sleep(2000);
                    Console.Clear();
                }

                return selectedDrive;
            }

            // if we are looking for a single CSD but we found multiple attached drives with the name we
            // are looking for, we need to error out.
            // TODO: If in the future it becomes an issue to silently return, throw an error here instead
            if (csd != null && drives.Count > 1)
                return null;

            List<MenuEntry> entries = new List<MenuEntry>();

            foreach (DriveInfo drive in drives)
            {
                string driveLetter = drive.Name.TrimEnd('\\');

                entries.Add(new MenuEntry()
                {
                    Name = $"{driveLetter} (Volume name: {drive.VolumeLabel} | Format: {drive.DriveFormat})",
                    // Task = () => {
                    //     selectedDrive = driveLetter;
                    // }
                });
            }

            Menu menu = new Menu(entries);
            // menu.MenuLabel = "Select drive...";
            // TODO: fix
            // menu.OnCancel += Operations.MainMenu.StartOperation;

            menu.ShowAsync().RunSynchronously(); // was true

            return selectedDrive;
        }
    }
}