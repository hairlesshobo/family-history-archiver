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
using System.Net;
using System.Text.RegularExpressions;
using FoxHollow.Archiver.Shared.Utilities;
using FoxHollow.FHM.Shared;
using FoxHollow.FHM.Shared.Interfaces;
using FoxHollow.FHM.Shared.Models;
using FoxHollow.FHM.Shared.Utilities;

namespace FoxHollow.Archiver.Shared.Models.Config
{
    public class TapeServerConfig : IValidatableConfig
    {
        /// <Summary>
        ///     Tape drive path to use
        ///
        ///     Possible values:
        ///       auto: automatically use the first tape drive on the system
        ///       tapeX: Where X is the number of the tape drive. This will automatically map to the proper device depending on the operating system
        ///       /dev/nstX: Linux only
        ///       \\\\.\\TapeX: Windows only (you can also use //./TapeX form)
        /// </Summary>
        public string Drive { get; set; } = "auto";

        /// <Summary> 
        ///     Local IP address to listen on
        ///
        ///     In most cases, this should be set to 0.0.0.0. This should only change if you have multiple network
        ///     interfaces or plan to run the TapeServer on localhost (127.0.0.1) only
        ///
        ///     Default: 0.0.0.0
        /// </Summary>
        public string ListenAddress { get; set; } = "0.0.0.0";

        /// <Summary>
        ///     Configure ports for the tape server to communicate on
        /// </Summary>
        public TapeServerConfigPorts Ports { get; set; } 

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

            if (!IPAddress.TryParse(this.ListenAddress, out _))
                results.AddValidationError(prefix, nameof(ListenAddress), $"Invalid IP address provided: {this.ListenAddress}");

            if (this.Ports == null)
                results.AddValidationError(prefix, nameof(Ports), $"Object must be provided!");
            else
                results.AddRange(Ports.Validate(ConfigUtils.BuildValidationPrefix(prefix, nameof(this.Ports))));

            return results;
        }
    }
}