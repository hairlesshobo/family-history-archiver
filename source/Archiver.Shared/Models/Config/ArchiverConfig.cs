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
using FoxHollow.Archiver.Shared.Utilities;
using FoxHollow.FHM.Shared;
using FoxHollow.FHM.Shared.Models;
using FoxHollow.FHM.Shared.Interfaces;

namespace FoxHollow.Archiver.Shared.Models.Config
{
    public class ArchiverConfig : IValidatableConfig
    {
        public DiscConfig Disc { get; set; }
        public TapeConfig Tape { get; set; }
        public CsdConfig CSD { get; set; }
        public TapeServerConfig TapeServer { get; set; }

        /// <Summary>
        /// Path to the cdbxpmd.exe tool used to generate ISO files on Windows
        ///
        /// Note: ONLY needed on windows
        /// </Summary>
        public string CdbxpPath { get; set; } 
        
        public List<ValidationError> Validate(string prefix = null)
        {
            // TODO: In the future, find a way to specify whether loading config for archiver or tape server, this way not all field will actually be required
            
            List<ValidationError> results = new List<ValidationError>();

            results.AddRange(Disc.Validate(ConfigUtils.BuildValidationPrefix(prefix, nameof(this.Disc))));
            results.AddRange(Tape.Validate(ConfigUtils.BuildValidationPrefix(prefix, nameof(this.Tape))));
            results.AddRange(CSD.Validate(ConfigUtils.BuildValidationPrefix(prefix, nameof(this.CSD))));
            results.AddRange(TapeServer.Validate(ConfigUtils.BuildValidationPrefix(prefix, nameof(this.TapeServer))));

            if (SysInfo.OSType == OSType.Windows)
            {
                // TODO: in the future, make this a warning. Still allow the app to run, don't allow the relevant functionality to work on windows
                if (String.IsNullOrWhiteSpace(this.CdbxpPath))
                    results.AddValidationError(prefix, nameof(this.CdbxpPath), "Path to utility not provided");

                if (!File.Exists(this.CdbxpPath))
                    results.AddValidationError(prefix, nameof(this.CdbxpPath), $"The path does not exist: {this.CdbxpPath}");
            }

            return results;
        }
    }
}