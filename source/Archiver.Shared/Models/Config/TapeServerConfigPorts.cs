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
using FoxHollow.FHM.Shared.Interfaces;
using FoxHollow.FHM.Shared.Models;

namespace FoxHollow.Archiver.Shared.Models.Config
{
    public class TapeServerConfigPorts : IValidatableConfig
    {
        /// <summary>
        ///     UDP port used to send UDP broadcasts on. This is used so that the archive app can automatically
        ///     find the tape server on the network
        /// </summary>
        public int Broadcast { get; set; } = 34091; 

        /// <summary>
        ///     TCP port used to control the tape server
        /// </summary>
        public int Control { get; set; } = 34092; 

        /// <summary>
        ///     TCP port used to stream data to/from the tape
        /// </summary>
        public int Stream { get; set; } = 34093;

        public List<ValidationError> Validate(string prefix = null)
        {
            List<ValidationError> results = new List<ValidationError>();

            if (Broadcast <= 0)
                results.AddValidationError(prefix, nameof(Broadcast), "Port must be greater than 0");

            if (Control <= 0)
                results.AddValidationError(prefix, nameof(Control), "Port must be greater than 0");

            if (Stream <= 0)
                results.AddValidationError(prefix, nameof(Stream), "Port must be greater than 0");

            return results;
        }
    }
}