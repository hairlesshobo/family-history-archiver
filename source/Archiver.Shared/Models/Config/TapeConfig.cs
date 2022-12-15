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
using System.Text.RegularExpressions;
using FoxHollow.FHM.Shared;
using FoxHollow.FHM.Shared.Interfaces;
using FoxHollow.FHM.Shared.Models;
using FoxHollow.FHM.Shared.Utilities;

namespace FoxHollow.Archiver.Shared.Models.Config
{
    public class TapeConfig : IValidatableConfig
    {
        /// <Summary>
        /// Driver to use to access the tape drive. Options: 
        ///   - auto   [Automatically detects a local tape drive, if present. If not, scans the network for a remote tape server
        ///   - native [Uses the tape drive physically attached to the local system. Currently only works on windows]
        ///   - remote [USes a tape drive attached to a remote system that has been build as a tape server]
        /// </Summary>
        public string Driver { get; set; } = "auto";

        /// <Summary>
        /// Tape drive to use with the system. Options
        ///   - auto         [Automatically detect a local tape drive or remote tape server]
        ///   - \\\\.\\Tape0 [Use the first tape drive attached to the local Windows system]
        ///   - /dev/mt0     [Use the tape drive named mt0 on the local Linux system]
        ///   - ip|hostname  [Connect to the remote tape server specified by IP address or hostname]
        /// </Summary>
        public string Drive { get; set; } = "auto";
        // "Drive": "\\\\.\\Tape0",

        /// <Summary>
        ///     Automatically eject tape after writing?
        /// </Summary>
        
        public bool AutoEject { get; set; } = true;

        /// <Summary>
        /// Tape block size for the text records at the beginning of the tape
        ///
        /// 64KB is a pretty standard value to use here
        /// </Summary>
        public uint TextBlockSize { get; set; } = 65536;

        /// <Summary>
        /// the blocking factor to use when creating the tar
        ///
        /// the tar block is 512 (cannot be changed), so the below number is multiplied
        /// by 512 to generate the final block size on tape.
        ///
        /// for example if 256 is specified below:
        ///     512 * 256 = 128KB blocks
        /// </Summary>
        public ushort BlockingFactor { get; set; } = 512;

        /// <Summary>
        /// the number of blocks to store in memory as a write buffer
        /// 
        /// this app creates the tar and stores it in memory, and then writes the tape from
        /// memory. this is done to help ensure that the tape will not constantly stop and 
        /// rewind if the source data is coming in slower than the tape can write it.
        ///
        /// this number is multiplied by the final block size calculated from above
        ///
        /// for example, if 256 is specified for "BlockingFactor" above and 8192 is specified
        /// here:
        ///    512 * 256 = 128KB blocks
        ///    128KB * 8192 = 1GB memory buffer
        ///
        /// 512 blocking factor:
        ///    256KB * 16384 = 4 gb buffer
        ///    256KB * 32768 = 8 gb buffer
        /// </Summary>
        public uint MemoryBufferBlockCount { get; set; } = 4096; // 256KB * 4096 = 1 gb buffer

        /// <Summary>
        /// Required buffer fill to start writing
        ///
        /// Percentage of buffer blocks that must be full before writing will start.
        /// ideally this should be >= 95%
        /// </Summary>
        public ushort MemoryBufferMinFill { get; set; } = 95;

        public List<ValidationError> Validate(string prefix = null)
        {
            this.Drive = TapeUtilsNew.CleanTapeDrivePath(this.Drive);

            List<ValidationError> results = new List<ValidationError>();

            bool drivePathValid = false;

            // Check for Linux
            if (!drivePathValid && SysInfo.OSType == OSType.Linux && Regex.Match(this.Drive, @"/dev/nst\d").Success)
                drivePathValid = true;

            // check for Windows
            if (!drivePathValid && SysInfo.OSType == OSType.Windows && Regex.Match(this.Drive, @"\\\\\.\\Tape\d").Success)
                drivePathValid = true;

            if (!drivePathValid)
            {
                if (SysInfo.OSType == OSType.Linux)
                    results.AddValidationError(prefix, nameof(Drive), $"Invalid drive provided: `{this.Drive}`. On Linux, the path must be either `auto` or in the form of `/dev/nstX`");
                if (SysInfo.OSType == OSType.Windows)
                    results.AddValidationError(prefix, nameof(Drive), @$"Invalid drive provided: `{this.Drive}`. On Windows, the path must be either `auto` or in the form of `\\.\TapeX`");
            }

            // TODO: Add handling for ip/hostname drive paths

            return results;
        }
    }
}