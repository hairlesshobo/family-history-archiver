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
using FoxHollow.FHM.Shared.Interfaces;
using Newtonsoft.Json;

namespace FoxHollow.Archiver.Shared.Classes.Tape
{
        public class TapeDetail : TapeSummary, IMediaDetail
    {
        public List<TapeVerificationResult> Verifications { get; set; }
        public string Hash { get; set; } = null;
        public double CompressionRatio
        {
            get
            {
                if (this.ArchiveBytesOnTape == 0)
                    return 0;
                else
                    return ((double)this.TotalArchiveBytes / (double)this.ArchiveBytesOnTape);

            }
        }
        public TapeSourceInfo SourceInfo { get; set; }
        public long ArchiveBytesOnTape { get; set; }
        public int FilesCopied 
        { 
            get
            {
                return this.FlattenFiles().Where(x => x.Copied == true).Count();
            }
        }
        

        [JsonIgnore]
        public int DaysSinceLastVerify {
            get
            {
                if (this.Verifications.Count() <= 0)
                    return int.MaxValue;
                else
                    return (DateTime.UtcNow - this.Verifications.Max(x => x.VerificationDTM)).Days;
            }
        }

        [JsonIgnore]
        public bool LastVerifySuccess {
            get
            {
                if (this.Verifications.Count() == 0)
                    return false;
                else
                {
                    TapeVerificationResult lastResult = this.Verifications.OrderBy(x => x.VerificationDTM).LastOrDefault();

                    if (lastResult == null)
                        return false;
                    
                    return lastResult.TapeValid;
                }
            }
        }

        public TapeDetail() : base()
        {
            this.Files = new List<TapeSourceFile>();
            this.Verifications = new List<TapeVerificationResult>();
        }

        public TapeSummary GetSummary()
            => (TapeSummary)this;

        public void SaveToIndex()
            => SaveToJson();

        public void SaveToJson(string destinationDir = null, string fileName = null)
        {
            if (destinationDir == null)
                destinationDir = AppInfo.Directories.JSON;

            if (fileName == null)
                fileName = $"tape_{this.ID.ToString("000")}.json";

            
            if (!Directory.Exists(destinationDir))
                Directory.CreateDirectory(destinationDir);

            string jsonFilePath = Path.Combine(destinationDir, fileName);

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(this, new Newtonsoft.Json.JsonSerializerSettings() {
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                Formatting = Newtonsoft.Json.Formatting.Indented
            });

            // Write the json data needed for future runs of this app
            File.WriteAllText(jsonFilePath, json, Encoding.UTF8);
        }
    }
}