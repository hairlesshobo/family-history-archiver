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
using FoxHollow.Archiver.Shared.Interfaces;
using FoxHollow.Archiver.Shared.Utilities;
using FoxHollow.FHM.Shared.Utilities;

namespace FoxHollow.Archiver.Shared.Models.Config
{
    public class DiscConfig : IValidatableConfig
    {
        public long CapacityLimit { get; set; } = 24928845824;

        public long DVDCapacityLimit { get; set; } = 4566155264;

        public string[] SourcePaths { get; set; }

        public string[] ExcludePaths { get; set; }
            
        public string[] ExcludeFiles { get; set; }
        
        public string StagingDir { get; set; } = "../";

        public List<ValidationError> Validate(string prefix = null)
        {
            Array.Sort(SourcePaths);
            ExcludePaths = PathUtils.CleanExcludePaths(ExcludePaths);
            
            List<ValidationError> results = new List<ValidationError>();

            return results;
        }
    }
}