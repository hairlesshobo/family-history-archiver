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
using System.Threading;
using System.Threading.Tasks;
using FoxHollow.Archiver.Shared.Classes.CSD;
using FoxHollow.Archiver.Shared.Classes.Disc;
using FoxHollow.Archiver.Shared.Classes.Tape;
using FoxHollow.FHM.Shared.Interfaces;

namespace FoxHollow.Archiver.Shared.Utilities
{
    public static class ArchiveHelpers
    {
        private static string[] validMediaTypes = new string[] { "disc", "tape", "csd" };
        public static Task<List<TReturn>> ReadMediaIndexAsync<TReturn>(string mediaType, CancellationToken cToken, Action<int, int> progressUpdated = null)
            where TReturn : IMediaDetail, new()
        {
            if (string.IsNullOrWhiteSpace(mediaType))
                throw new ArgumentException($"'{nameof(mediaType)}' cannot be null or whitespace.", nameof(mediaType));

            if (!validMediaTypes.Contains(mediaType))
                throw new ArgumentException($"Invalid media type specified: {mediaType}. Valid Options: {String.Join(", ", validMediaTypes)}", nameof(mediaType));

            return Task.Run(async () => 
            {
                if (!Directory.Exists(AppInfo.Directories.JSON))
                    return null;

                List<TReturn> mediaEntries = new List<TReturn>();

                string[] jsonFiles = Directory.GetFiles(AppInfo.Directories.JSON, $"{mediaType}_*.json");
                int totalFiles = jsonFiles.Length;
                
                if (totalFiles == 0)
                    return null;

                int currentFile = 0;

                foreach (string jsonFile in jsonFiles)
                {
                    if (cToken.IsCancellationRequested)
                        return null;

                    currentFile++;

                    double currentPct = (double)currentFile / (double)totalFiles;

                    if (progressUpdated != null)
                        progressUpdated(currentFile, totalFiles);

                    using (FileStream openStream = File.OpenRead(jsonFile))
                    {
                        TReturn mediaDetail = await JsonSerializer.DeserializeAsync<TReturn>(openStream);

                        DiscDetail discDetail = mediaDetail as DiscDetail;

                        if (discDetail != null)
                            discDetail.Files.ForEach(x => x.DestinationDisc = discDetail);


                        TapeDetail tapeDetail = mediaDetail as TapeDetail;

                        if (tapeDetail != null)
                            tapeDetail.FlattenFiles().ToList().ForEach(x => x.Tape = tapeDetail);



                        CsdDetail csdDetail = mediaDetail as CsdDetail;

                        if (csdDetail != null)
                        {
                            foreach (CsdSourceFile file in csdDetail.Files)
                                file.DestinationCsd = csdDetail;

                            csdDetail.SyncStats();
                        }


                        mediaEntries.Add(mediaDetail);
                    }
                }

                return mediaEntries;
            });
        }
    }
}