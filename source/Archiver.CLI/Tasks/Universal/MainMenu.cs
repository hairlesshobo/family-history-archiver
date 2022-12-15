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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FoxHollow.Archiver.CLI.Utilities.Shared;
using FoxHollow.Archiver.Shared;
using FoxHollow.Archiver.Shared.Utilities;
using FoxHollow.TerminalUI;
using FoxHollow.TerminalUI.Elements;
using FoxHollow.TerminalUI.Types;
using static FoxHollow.Archiver.Shared.Utilities.Formatting;

namespace FoxHollow.Archiver.CLI.Tasks.Universal
{
    public static class MainMenu
    {
        public async static Task StartTaskAsync(CancellationTokenSource cts)
            => await ShowMenuAsync(cts);

        private async static Task ShowMenuAsync(CancellationTokenSource cts)
        {
            while (!cts.IsCancellationRequested)
            {
                Terminal.Header.UpdateHeader("Main Menu", "Archiver");
                Terminal.Clear();

                Menu menu = BuildMenu();
                menu.QuitCallback = () => {
                    cts.Cancel();
                    return Task.CompletedTask;
                };

                var results = await menu.ShowAsync(cts.Token);

                if (results == null)
                    return;

                if (results.First() == null)
                    continue;

                if ((bool)results.First() == false)
                {
                    Console.WriteLine();
                    Console.Write("Process complete, press ");
                    Formatting.WriteC(ConsoleColor.DarkYellow, "<enter>");
                    Console.Write(", ");
                    Formatting.WriteC(ConsoleColor.DarkYellow, "<esc>");
                    Console.Write(", or ");
                    Formatting.WriteC(ConsoleColor.DarkYellow, "q");
                    Console.Write(" to return to the main menu...");

                    while (true)
                    {
                        ConsoleKeyInfo key = Console.ReadKey(true);

                        if (key.Key == ConsoleKey.Q || key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Escape)
                            break;
                    }
                }
            }
        }

        private static Menu BuildMenu()
        {
            List<MenuEntry> entries = new List<MenuEntry>();

            entries.AddRange(BuildDiscMenu());
            entries.Add(new MenuEntry() { Header = true });
            entries.AddRange(BuildTapeMenu());
            entries.Add(new MenuEntry() { Header = true });
            entries.AddRange(BuildCsdMenu());
            entries.Add(new MenuEntry() { Header = true });
            entries.AddRange(BuildUniversalMenu());

            return new Menu(entries);
        }

        private static List<MenuEntry> BuildDiscMenu()
        {
            string discMenuAppend = String.Empty;

            if (AppInfo.IsOpticalDrivePresent == false)
                discMenuAppend += " (drive not detected)";
                
            return new List<MenuEntry>()
            {
                new MenuEntry() {
                    Name = "Disc Operations" + discMenuAppend,
                    Header = true
                },
                new MenuEntry() {
                    Name = "Search Disc Archive",
                    HelpText = "Search the entire disc archive for one or more files",
                    Task = Tasks.Disc.DiscSearcherTask.StartTaskAsync,
                    ForegroundColor = ConsoleColor.Green,
                    SelectedValue = true, // do not show the "press enter to return to main menu" message
                },
                //! not implemented
                new MenuEntry() {
                    Name = "Restore entire disc(s)",
                    HelpText = "Restore the entire contents of one or more discs to the local hard drive",
                    Task = NotImplementedAsync,
                    SelectedValue = true,
                    Disabled = !AppInfo.IsOpticalDrivePresent || true, // TODO: remove once implemented
                    ForegroundColor = ConsoleColor.Green
                },
                new MenuEntry() {
                    Name = "View disc summary",
                    HelpText = "View a summary of the disc currently in the drive",
                    Task = NotImplementedAsync, // Tasks.Disc.ShowDiscSummaryTask.StartTaskAsync,
                    Disabled = !AppInfo.IsOpticalDrivePresent || true, // TODO: remove once implemented
                    ForegroundColor = ConsoleColor.Blue
                },
                new MenuEntry() {
                    Name = "View Archive Summary",
                    HelpText = "View a summary of the entire disc archive",
                    Task = Tasks.Disc.DiscArchiveSummaryTask.StartTaskAsync,
                    SelectedValue = true, // do not show the "press enter to return to main menu" message
                    ForegroundColor = ConsoleColor.Blue
                },
                new MenuEntry() {
                    Name = "Verify Discs",
                    HelpText = "Verify the integrity of one or more archive discs",
                    Task = Tasks.Disc.DiscVerificationTask.StartTaskAsync,
                    Disabled = AppInfo.IsReadonlyFilesystem || !AppInfo.IsOpticalDrivePresent,
                    ForegroundColor = ConsoleColor.DarkYellow
                },
                new MenuEntry() {
                    Name = "Scan For Changes",
                    HelpText = "Scan for any files that need to be added to the disc archive",
                    Task = Tasks.Disc.DiscArchiverTask.StartScanOnlyTaskAsync,
                    ForegroundColor = ConsoleColor.DarkYellow
                },
                new MenuEntry() {
                    Name = "Scan For Renamed/Moved Files",
                    HelpText = "Scan for any files currently in index that were likely renamed or moved",
                    Task = NotImplementedAsync, // Tasks.Disc.ScanForFileRenamesTask.StartTaskAsync,
                    Disabled = true, // TODO: Remove once implemented
                    ForegroundColor = ConsoleColor.DarkYellow
                },
                new MenuEntry() {
                    Name = "Run Archive process",
                    HelpText = "Scan for new files to add to archive and generate ISO files to write to discs",
                    Task = Tasks.Disc.DiscArchiverTask.StartTaskAsync,
                    Disabled = AppInfo.IsReadonlyFilesystem || !AppInfo.IsOpticalDrivePresent,
                    ForegroundColor = ConsoleColor.Red
                }
            };
        }

        private static List<MenuEntry> BuildTapeMenu()
        {
            string tapeMenuAppend = String.Empty;

            if (AppInfo.IsTapeDrivePresent == false)
                tapeMenuAppend += " (drive not detected)";
                
            return new List<MenuEntry>()
            {
                new MenuEntry() {
                    Name = "Tape Operations" + tapeMenuAppend,
                    Header = true
                },
                new MenuEntry() {
                    Name = "Search Tape Archive",
                    HelpText = "Search the entire tape archive for one or more files",
                    Task = Tasks.Tape.TapeSearcherTask.StartTaskAsync,
                    ForegroundColor = ConsoleColor.Green,
                    SelectedValue = true, // do not show the "press enter to return to main menu" message
                },
                //! not implemented
                new MenuEntry() {
                    Name = "Restore entire tape (to tar file)",
                    HelpText = "Restore the contents of a single tape to a tar file on the local hard drive",
                    Task = NotImplementedAsync, // Tasks.Tape.RestoreToTarTask.StartTaskAsync,
                    Disabled = !AppInfo.IsTapeDrivePresent || true, // TODO: remove once implemented
                    ForegroundColor = ConsoleColor.Green
                },
                //! not implemented
                new MenuEntry() {
                    Name = "Restore entire tape (to original file structure)",
                    HelpText = "Restore the contents of a single tape to a directory on the local hard drive",
                    Task = NotImplementedAsync, // Tasks.Taepe.RestoreToFilesTask.StartTaskAsync,
                    Disabled = !AppInfo.IsTapeDrivePresent || true, // TODO: remove once implemented
                    ForegroundColor = ConsoleColor.Green
                },
                new MenuEntry() {
                    Name = "View Tape Summary",
                    HelpText = "View a summary of the tape currently in the drive",
                    Task = Tasks.Tape.ShowTapeSummaryTask.StartTaskAsync,
                    Disabled = !AppInfo.IsTapeDrivePresent,
                    SelectedValue = true, // do not show the "press enter to return to main menu" message
                    ForegroundColor = ConsoleColor.Blue
                },
                new MenuEntry() {
                    Name = "View Archive Summary",
                    HelpText = "View a summary of the entire tape archive",
                    Task = Tasks.Tape.ShowArchiveSummaryTask.StartTaskAsync,
                    SelectedValue = true, // do not show the "press enter to return to main menu" message
                    ForegroundColor = ConsoleColor.Blue
                },
                new MenuEntry() {
                    Name = "Verify Tape",
                    HelpText = "Verify the integrity of archive tape currently in the drive",
                    Task = Tasks.Tape.TapeVerificationTask.StartTaskAsync,
                    Disabled = AppInfo.IsReadonlyFilesystem || !AppInfo.IsTapeDrivePresent,
                    SelectedValue = true, // do not show the "press enter to return to main menu" message
                    ForegroundColor = ConsoleColor.DarkYellow
                },
                new MenuEntry() {
                    Name = "Run tape archive",
                    HelpText = "Select and write a tape with the newest files currently available",
                    Task = Tasks.Tape.ArchiverTask.StartTaskAsync,
                    Disabled = AppInfo.IsReadonlyFilesystem || !AppInfo.IsTapeDrivePresent || true, // TODO: Remove once converted
                    ForegroundColor = ConsoleColor.Red
                }
            };
        }

        private static List<MenuEntry> BuildCsdMenu()
        {
            return new List<MenuEntry>()
            {
                new MenuEntry() {
                    Name = "Cold Storage Disk (HDD) Operations",
                    Header = true
                },
                new MenuEntry() {
                    Name = "Register CSD Drive",
                    HelpText = "Register a new drive to be used for CSD storage",
                    Task = Tasks.CSD.RegisterDriveTask.StartTaskAsync,
                    ForegroundColor = ConsoleColor.Green
                },
                //! not implemented
                new MenuEntry() {
                    Name = "Restore entire CSD Drive",
                    HelpText = "Restore the contents of a CSD drive to the local hard drive",
                    Task = NotImplementedAsync,
                    Disabled = true, // TODO: remove once implemented
                    ForegroundColor = ConsoleColor.Green
                },
                //! not implemented
                new MenuEntry() {
                    Name = "View CSD Drive Summary",
                    HelpText = "View a summary of the currently connect CSD drive",
                    Task = NotImplementedAsync, // Tasks.CSD.ShowDriveSummaryTask.StartTaskAsync,         
                    Disabled = true, // TODO: remove once implemented
                    SelectedValue = true, // do not show the "press enter to return to main menu" message
                    ForegroundColor = ConsoleColor.Blue
                },
                new MenuEntry() {
                    Name = "View CSD Archive Summary",
                    HelpText = "View a summary of the entire CSD archive",
                    Task = Tasks.CSD.ShowArchiveSummaryTask.StartTaskAsync,
                    SelectedValue = true, // do not show the "press enter to return to main menu" message
                    ForegroundColor = ConsoleColor.Blue
                },
                //! not implemented
                new MenuEntry() {
                    Name = "Verify CSD Drive",
                    HelpText = "Verify the integrity of the files on the currently connected CSD drive",
                    Task = NotImplementedAsync, // Tasks.CSD.VerifyTask.StartTaskAsync,
                    Disabled = AppInfo.IsReadonlyFilesystem || true, // TODO: remove once implemented
                    ForegroundColor = ConsoleColor.DarkYellow
                },
                new MenuEntry() {
                    Name = "Clean CSD Drive",
                    HelpText = "Remove any files on the connected CSD drive that are not in the index",
                    Task = NotImplementedAsync, // Tasks.CSD.CleanerTask.StartTaskAsync,
                    Disabled = AppInfo.IsReadonlyFilesystem, // TODO: remove once implemented
                    ForegroundColor = ConsoleColor.Red
                },
                new MenuEntry() {
                    Name = "Run CSD Archive Process",
                    HelpText = "Scan for and archive new files to the CSD archive",
                    Task = Tasks.CSD.ArchiverTask.StartTaskAsync,
                    Disabled = AppInfo.IsReadonlyFilesystem,
                    ForegroundColor = ConsoleColor.Red
                }
            };
        }

        private static List<MenuEntry> BuildUniversalMenu()
        {
            return new List<MenuEntry>()
            {
                new MenuEntry() {
                    Name = "Universal Operations",
                    Header = true
                },
                //! not implemented
                new MenuEntry() {
                    Name = "Copy Tools to Local Disk",
                    HelpText = "Install a copy of the archiver tool to the local hard drive",
                    Task = NotImplementedAsync,
                    Disabled = !AppInfo.IsReadonlyFilesystem || true // TODO: Remove once implemented 
                },
                new MenuEntry() {
                    Name = "Create Index ISO",
                    HelpText = "Generate an ISO of the archiver index to burn to disc for safe storage",
                    Task = NotImplementedAsync, // Helpers.CreateIndexIso,
                    Disabled = AppInfo.IsReadonlyFilesystem || true // TODO: Remove once implemented
                }//,
                // new MenuEntry() {
                //     Name = "Explode",
                //     Task = (cToken) => throw new InvalidOperationException("meow!!!!")
                // }
            };
        }

        private static async Task NotImplementedAsync(CancellationToken cToken = default)
        {
            // TODO: What to do with cToken here?
            TaskCompletionSource tcs = new TaskCompletionSource();

            Terminal.Header.UpdateHeader("Not Implemented", "Archiver");
            Terminal.InitStatusBar(
                new StatusBarItem(
                    "Main Menu",
                    (key) => { 
                        tcs.TrySetResult();
                        return Task.CompletedTask;
                    },
                    Key.MakeKey(ConsoleKey.Q)
                )
            );

            Terminal.Clear();
            Formatting.WriteLineC(ConsoleColor.Red, "This operation has not yet been implemented.");

            await tcs.Task;

            Terminal.StatusBar.Reset();
        }
    }
}