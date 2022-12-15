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
using System.Reflection;
using FoxHollow.Archiver.Shared.Models.Config;
using FoxHollow.Archiver.Shared.Utilities;
using FoxHollow.FHM.Shared;
using FoxHollow.FHM.Shared.Models;
using FoxHollow.FHM.Shared.Utilities;

namespace FoxHollow.Archiver.Shared
{
    /// <summary>
    ///     Indicates the mode that the application is currently running in
    /// </summary>
    public enum AppInfoMode
    {
        Unknown, 
        ArchiverCLI, 
        Reserved,  // reserved for ArchiverGUI 
        TapeServer, 
        TestCLI
    }

    /// <summary>
    ///     Global class for providing useful system-related and config information
    /// </summary>
    public static class AppInfo
    {
        private static bool _didInit = false;
        private static ArchiverConfig _config = null;
        private static AppInfoMode _mode = AppInfoMode.Unknown;
        private static bool _isOpticalDrivePresent = false;
        private static bool _isReadonlyFilesystem = true;
        private static bool _isTapeDrivePresent = true;
        private static SystemDirectories _directories = null;
        private static List<ValidationError> _configErrors;


        /// <summary>
        ///     Config that was loaded and parsed from appsettings.json
        /// </summary>
        public static ArchiverConfig Config => _config;
        /// <summary>
        ///     Configured directories for the application
        /// </summary>
        public static SystemDirectories Directories => _directories;
        /// <summary>
        ///     A list containing any errors that were discovered during loading of the config
        /// </summary>
        public static List<ValidationError> ConfigErrors => _configErrors;

        /// <summary>
        ///     Mode that the application is currently runnning as
        /// </summary>
        public static AppInfoMode Mode => _mode;

        /// <summary>
        ///     Flag indicating whether one or more optical drives were detected
        /// </summary>
        public static bool IsOpticalDrivePresent => _isOpticalDrivePresent;

        /// <summary>
        ///     Flag indiciating whether the application is currently running from a readonly filesystem 
        ///     (such as a cdrom)
        /// </summary>
        public static bool IsReadonlyFilesystem => _isReadonlyFilesystem;
        
        /// <summary>
        ///     Flag indicating whether one or more tape drives were detected on the local system
        /// </summary>
        public static bool IsTapeDrivePresent => _isTapeDrivePresent;

        /// <summary>
        ///     Path to the configured tape drive
        /// </summary>
        public static string TapeDrive => _mode == AppInfoMode.ArchiverCLI ? Config.Tape.Drive : Config.TapeServer.Drive;
        
        
        /// <summary>
        ///     Static constructor
        /// </summary>
        static AppInfo()
        {
            _mode = GetMode();
        }

        /// <summary>
        ///     Global initialization which must be called as one of the very first steps
        ///     of the application startup. This is responsible for populating the system 
        ///     directory list, detecting local drives, and reading the app config file
        /// </summary>
        public static void InitPlatform()
        {
            if (!_didInit)
            {
                _config = ConfigUtils.ReadConfig(out _configErrors);

                _directories = new SystemDirectories();
                _directories.Bin = PathUtils.CleanPath(AppContext.BaseDirectory);
                _directories.Index = PathUtils.ResolveRelativePath(Path.Combine(_directories.Bin, "../"));
                _directories.JSON = PathUtils.CleanPathCombine(_directories.Index, "json");
                _directories.ISO = PathUtils.CleanPathCombine(_directories.Index, "../iso");
                _directories.DiscStaging = (
                      !String.IsNullOrWhiteSpace(Config.Disc.StagingDir) 
                    ? PathUtils.ResolveRelativePath(Path.Combine(_directories.Index, Config.Disc.StagingDir)) 
                    : PathUtils.CleanPath(Path.Combine(Path.GetTempPath(), Path.GetTempFileName()))
                );

                _isOpticalDrivePresent = OpticalDriveUtils.GetDriveNames().Any();
                _isReadonlyFilesystem = TestForReadonlyFs();
                _isTapeDrivePresent = TapeUtilsNew.IsTapeDrivePresent();
            }
        }

        public static void WriteSystemInfo(bool writeConfig = false, bool color = false)
        {
            PrintHeader(color, "System Information:");
            PrintField(color, 11, "OS Platform", $"{SysInfo.OSType.ToString()} ({SysInfo.Architecture.ToString()})");
            PrintField(color, 11, "Description", SysInfo.Description);
            PrintField(color, 11, "Identifier", SysInfo.Identifier);

            if (writeConfig && Mode == AppInfoMode.TapeServer)
            {
                Console.WriteLine();
                PrintHeader(color, "Configuration:");
                PrintField(color, 12, "Tape Drive", Config.TapeServer.Drive);
            }
        }

        private static void PrintHeader(bool color, string value)
        {
            if (color)
                Formatting.WriteLineC(ConsoleColor.Magenta, value);
            else
                Console.WriteLine(value);
        }

        private static void PrintField(bool color, int width, string fieldName, string value)
        {
            if (color == true)
            {
                Formatting.WriteC(ConsoleColor.Cyan, $"  {fieldName.PadLeft(width)}: ");
                Console.WriteLine(value);
            }
            else
                Console.WriteLine($"  {fieldName.PadLeft(width)}: {value}");
        }

        /// <summary>
        ///     Get the mode that the application is currently running as
        /// </summary>
        /// <returns>Currently running mode</returns>
        private static AppInfoMode GetMode()
        {
            string assemblyName = Assembly.GetEntryAssembly().GetName().Name;

            switch (assemblyName)
            {
                case "Archiver":
                    return AppInfoMode.ArchiverCLI;

                case "TapeServer":
                    return AppInfoMode.TapeServer;

                case "TestCLI":
                    return AppInfoMode.TestCLI;
                
                default:
                    return AppInfoMode.Unknown;
            }
        }

        /// <summary>
        ///     Test if the app is running on a readonly filesystem
        /// </summary>
        /// <returns>true if on readonly filesystem, false otherwise</returns>
        private static bool TestForReadonlyFs()
        {
            string testFile = Path.Combine(Directories.Bin, "__accesstest.tmp");
            bool canWrite = true;

            if (File.Exists(testFile))
            {
                try
                {
                    File.Delete(testFile);
                }
                catch
                {
                    canWrite = false;
                }
            }

            try
            {
                File.WriteAllText(testFile, String.Empty);
                File.Delete(testFile);
            }
            catch
            {
                canWrite = false;
            }

            return !canWrite;
        }

        /// <summary>
        ///     Class that describes the system directories
        /// </summary>
        public class SystemDirectories
        {
            /// <summary>
            ///     Full path to the directory of the executable that is currently running
            /// </summary>
            public string Bin { get; internal protected set; }

            /// <summary>
            ///     Full path to the archive index directory
            /// </summary>
            public string Index { get; internal protected set; }

            /// <summary>
            ///     Full path to the archive json index directory
            /// </summary>
            public string JSON { get; internal protected set; }

            /// <summary>
            ///     Full path to the staging directory to use when archiving to disc
            /// </summary>
            public string DiscStaging { get; internal protected set; }

            /// <summary>
            ///     Full path to the directory where ISO files will be created
            /// </summary>
            public string ISO { get; internal protected set; }
        }
    }
}