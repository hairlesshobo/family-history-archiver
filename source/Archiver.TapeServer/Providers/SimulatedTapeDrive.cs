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
using System.Text.Json;
using FoxHollow.Archiver.Shared;
using FoxHollow.Archiver.Shared.Utilities;
using FoxHollow.FHM.Shared.Models;
using FoxHollow.FHM.Shared.Interfaces;
using FoxHollow.FHM.Shared.Exceptions;

namespace FoxHollow.Archiver.TapeServer.Providers
{
    public class SimulatedTapeDrive : ITapeDrive
    {
        private SimulatedTapeSpecs _tapeSpecs;

        public SimulatedTapeDrive(string driveType)
        {
            string emulationJsonPath = Path.Combine(ConfigUtils.GetConfigDirectory(), "emulation.json");
            string emulationJson = File.ReadAllText(emulationJsonPath);
            List<SimulatedTapeSpecs> driveTypes = JsonSerializer.Deserialize<List<SimulatedTapeSpecs>>(emulationJson);

            SimulatedTapeSpecs specs = driveTypes.FirstOrDefault(x => x.DriveType.Equals(driveType, StringComparison.InvariantCultureIgnoreCase));

            if (specs == null)
                throw new TapeDriveNotFoundException($"simulated://{driveType}");

            _tapeSpecs = specs;
        }

        public void EjectTape()
        {
            throw new NotImplementedException();
        }

        public long GetBlockPosition()
        {
            throw new NotImplementedException();
        }

        public TapeDriveCapabilities GetCapabilities()
        {
            throw new NotImplementedException();
        }

        public TapeDriveDetails GetDriveDetails()
        {
            throw new NotImplementedException();
        }

        public TapeCartridgeDetails GetTapeInfo()
        {
            return new TapeCartridgeDetails()
            {
                Capacity = _tapeSpecs.Capacity
            };
        }

        public void LoadTape()
        {
            throw new NotImplementedException();
        }

        public bool ReadBlock(byte[] buffer, long? startPosition)
        {
            throw new NotImplementedException();
        }

        public bool ReadBlock(byte[] buffer)
        {
            throw new NotImplementedException();
        }

        public void RewindTape()
        {
            throw new NotImplementedException();
        }

        public void SeekToBlock(long logicalBlock)
        {
            throw new NotImplementedException();
        }

        public void SeekToEndOfData()
        {
            throw new NotImplementedException();
        }

        public void SeekToFilePosition(long fileNumber)
        {
            throw new NotImplementedException();
        }

        public void SetDriveCompression(bool newStatus)
        {
            throw new NotImplementedException();
        }

        public void WriteBlock(byte[] block)
        {
            throw new NotImplementedException();
        }

        public void WriteFilemark()
        {
            throw new NotImplementedException();
        }

        public void WriteText(string text, int filePosition)
        {
            throw new NotImplementedException();
        }

        public class SimulatedTapeSpecs
        {
            public string DriveType { get; set; }
            public long Capacity { get; set; }
        }
    }
}