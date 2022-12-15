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
using FoxHollow.Archiver.Shared.Classes.Tape;
using FoxHollow.Archiver.Shared;
using ICSharpCode.SharpZipLib.Tar;
using Newtonsoft.Json;
using FoxHollow.FHM.Shared.TapeDrivers;
using FoxHollow.FHM.Shared.Native;
using FoxHollow.TerminalUI.Elements;
using FoxHollow.TerminalUI.Types;

namespace FoxHollow.Archiver.CLI.Utilities.Tape
{
    public static class TapeUtils
    {
        
        public static TapeDetail GetTapeDetail(int id)
        {
            List<TapeDetail> tapes = new List<TapeDetail>();

            if (Directory.Exists(AppInfo.Directories.JSON))
            {
                string[] jsonFiles = Directory.GetFiles(AppInfo.Directories.JSON, $"tape_{id.ToString("000")}.json");
                int totalFiles = jsonFiles.Length;
                
                if (totalFiles == 1)
                {
                    Console.CursorLeft = 0;
                    Console.Write("Reading tape index file... ");

                    TapeDetail tapeDetail = JsonConvert.DeserializeObject<TapeDetail>(File.ReadAllText(jsonFiles[0]));
                    tapeDetail.FlattenFiles().ToList().ForEach(x => x.Tape = tapeDetail);

                    Console.WriteLine("done");

                    return tapeDetail;
                }
                else
                    return null;
            }

            return null;
        }

        public static void RewindTape()
        {
            using (NativeWindowsTapeDriver tape = new NativeWindowsTapeDriver(AppInfo.TapeDrive, false))
            {
                tape.RewindTape();
            }
        }

        public static TapeSourceInfo SelectTape()
        {
            TapeSourceInfo tape = default(TapeSourceInfo);

            string searchPath = Path.Join(Directory.GetCurrentDirectory(), "config", "tapes");

            string[] files = Directory.GetFiles(searchPath, "*.json");

            List<MenuEntry> entries = new List<MenuEntry>();
            List<TapeSourceInfo> sources = new List<TapeSourceInfo>();

            foreach (string file in files)
            {
                sources.Add(JsonConvert.DeserializeObject<TapeSourceInfo>(File.ReadAllText(file)));
            }

            foreach (TapeSourceInfo source in sources.OrderBy(x => x.ID))
            {
                entries.Add(new MenuEntry() {
                    Name = $"{source.ID.ToString().PadLeft(3)}: {source.Name}",
                    SelectedValue = source
                });
            }

            Menu menu = new Menu(entries);
            // menu.MenuLabel = "Select tape...";
            // TODO: Fix
            // menu.OnCancel += Operations.MainMenu.StartOperation;
            
            List<TapeSourceInfo> selectedItems = new List<TapeSourceInfo>();
            // TODO: fix
            // menu.Show(true);

            if (selectedItems != null && selectedItems.Count() >= 1)
                return selectedItems.First();

            return tape;
        }

        public static bool TapeHasJsonRecord()
        {
            using (NativeWindowsTapeDriver tape = new NativeWindowsTapeDriver(AppInfo.TapeDrive))
            {
                // lets test if the second file record is the start of a tar, or a json file. if
                // a json file, then we know that the tape is a new style with the tar located
                // at file 3 instead of 2
                tape.SetTapeFilePosition(1);

                byte[] buffer = new byte[tape.BlockSize];

                // we just want to read one block.. because we only even need the first 2 bytes to determine if the file is a json record
                tape.Read(buffer);

                char firstChar = Encoding.UTF8.GetChars(buffer, 0, 1)[0];

                if (firstChar == '{')
                    return true;
                else
                    return false;
            }
        }

        // TODO: make async
        public static string ReadTxtSummaryFromTape()
        {
            using (NativeWindowsTapeDriver tape = new NativeWindowsTapeDriver(AppInfo.TapeDrive))
            {
                byte[] buffer = new byte[tape.BlockSize];

                // seek the tape to the beginning of the file marker
                tape.SetTapeFilePosition(0);

                string summary = String.Empty;
                bool endOfData = false;

                do
                {
                    endOfData = tape.Read(buffer);

                    int strlen = Array.IndexOf(buffer, (byte)0);

                    if (!endOfData)
                    {
                        if (strlen > 0)
                            summary += Encoding.UTF8.GetString(buffer, 0, strlen);
                        else
                            summary += Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                    }
                }
                while (!endOfData);

                return summary;
            }
        }

        public static TapeInfo GetTapeInfo(NativeWindowsTapeDriver tape)
        {
            if (tape.TapeLoaded())
            {
                return new TapeInfo()
                {
                    DriveInfo = tape.DriveInfo,
                    TapePosition = tape.GetTapeBlockPosition(),
                    MediaInfo = tape.MediaInfo
                };
            }
            else
            {
                return new TapeInfo()
                {
                    DriveInfo = tape.DriveInfo,
                    TapePosition = -1,
                    MediaInfo = default (Windows.TapeMediaInfo)
                };
            }

        }

        public static TapeInfo GetTapeInfo()
        {
            using (NativeWindowsTapeDriver tape = new NativeWindowsTapeDriver(AppInfo.TapeDrive, false))
            {
                return GetTapeInfo(tape);
            }
        }

        public static TapeSummary ReadTapeSummaryFromTape()
        {
            bool hasJson = TapeHasJsonRecord();

            if (!hasJson)
                throw new InvalidOperationException("Tape does not have json record!");

            using (NativeWindowsTapeDriver tape = new NativeWindowsTapeDriver(AppInfo.TapeDrive, (uint)AppInfo.Config.Tape.TextBlockSize, false))
            {
                byte[] buffer = new byte[tape.BlockSize];

                // seek the tape to the beginning of the file marker
                tape.RewindTape();
                tape.SetTapeFilePosition(1);

                string json = String.Empty;
                bool endOfData = false;

                do
                {
                    endOfData = tape.Read(buffer);

                    int strlen = Array.IndexOf(buffer, (byte)0);

                    if (!endOfData)
                    {
                        if (strlen > 0)
                            json += Encoding.UTF8.GetString(buffer, 0, strlen);
                        else
                            json += Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                    }
                }
                while (!endOfData);

                return JsonConvert.DeserializeObject<TapeSummary>(json);
            }
        }

        public static void ListContentsFromTape()
        {
            bool hasJson = TapeHasJsonRecord();

            using (NativeWindowsTapeDriver tape = new NativeWindowsTapeDriver(AppInfo.TapeDrive, AppInfo.Config.Tape.BlockingFactor * 512))
            {
                byte[] buffer = new byte[tape.BlockSize];

                // seek the tape to the beginning of the file marker
                tape.SetTapeFilePosition(hasJson ? 2 : 1);

                TarArchive archive = TarArchive.CreateInputTarArchive(tape.Stream, AppInfo.Config.Tape.BlockingFactor);
                archive.ProgressMessageEvent += ShowTarProgressMessage;
                archive.ListContents();
            }
        }

        public static void ShowTarProgressMessage(TarArchive archive, TarEntry entry, string message)
        {
            if (entry.TarHeader.TypeFlag != TarHeader.LF_NORMAL && entry.TarHeader.TypeFlag != TarHeader.LF_OLDNORM) {
                Console.WriteLine("Entry type " + (char)entry.TarHeader.TypeFlag + " found!");
            }

            if (message != null)
                Console.Write(entry.Name + " " + message);
            else {
                // if (this.verbose) {
                //     string modeString = DecodeType(entry.TarHeader.TypeFlag, entry.Name.EndsWith("/")) + DecodeMode(entry.TarHeader.Mode);
                //     string userString = (string.IsNullOrEmpty(entry.UserName)) ? entry.UserId.ToString() : entry.UserName;
                //     string groupString = (string.IsNullOrEmpty(entry.GroupName)) ? entry.GroupId.ToString() : entry.GroupName;

                //     Console.WriteLine(string.Format("{0} {1}/{2} {3,8} {4:yyyy-MM-dd HH:mm:ss} {5}", modeString, userString, groupString, entry.Size, entry.ModTime.ToLocalTime(), entry.Name));
                // } else {
                    Console.WriteLine(entry.Name);
                //}
            }
        }

        public static void WriteBytesToTape(NativeWindowsTapeDriver tape, byte[] inputBuffer, bool appendToTape)
        {
            if (inputBuffer.Length % (int)tape.BlockSize != 0)
                throw new InvalidDataException("Provided input buffer must be a multiple of tape blocksize");

            //if (appendToTape)
            //    tape.SetTapeToEndOfData();

            using (MemoryStream reader = new MemoryStream(inputBuffer, 0, inputBuffer.Length, false))
            {
                int currentPosition = 0;
                byte[] buffer = new byte[tape.BlockSize];

                while (reader.Read(buffer, 0, (int)tape.BlockSize) > 0)
                {
                    currentPosition += (int)tape.BlockSize;

                    tape.Write(buffer);
                }
            }
        }

        public static bool IsTapeLoaded()
        {
            using (NativeWindowsTapeDriver tape = new NativeWindowsTapeDriver(AppInfo.TapeDrive, false))
            {
                return tape.TapeLoaded();
            }
        }
    }
}