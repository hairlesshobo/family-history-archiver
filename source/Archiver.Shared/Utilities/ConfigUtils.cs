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
using System.Text;
using FoxHollow.FHM.Shared.Models;
using FoxHollow.Archiver.Shared.Models.Config;
using Microsoft.Extensions.Configuration;

namespace FoxHollow.Archiver.Shared.Utilities
{
    public static partial class ConfigUtils
    {
        public static string GetConfigDirectory()
        {
            // we set the directory to where the exe is
            string dir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            
            string configDir = Path.Join(dir, "config/");

            // lets check the current dir and the parent dir for a config directory

            if (Directory.Exists(configDir))
                dir = configDir;
            else
            {
                configDir = Path.Join(dir, "../", "config/");

                if (Directory.Exists(configDir))
                    dir = configDir;
            }

            return dir;
        }

        public static IConfiguration ReadConfigFile()
        {
            string configDir = GetConfigDirectory();

            // we set the directory to where the exe is
            Directory.SetCurrentDirectory(configDir);
            
            IConfiguration _config = new ConfigurationBuilder()
                .SetBasePath(configDir)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            return _config;
        }

        public static ArchiverConfig ReadConfig(out List<ValidationError> validationErrors)
        {
            IConfiguration _config = ReadConfigFile();

            ArchiverConfig config = new ArchiverConfig();
            _config.Bind(config);

            validationErrors = config.Validate();
            
            return config;
        }

        public static string BuildValidationPrefix(params string[] values)
            => String.Join('.', values.Where(x => !String.IsNullOrWhiteSpace(x)));
    }
}