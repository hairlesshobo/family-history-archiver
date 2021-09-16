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
using Archiver.Shared;
using Archiver.Shared.Utilities;
using Archiver.Utilities.Shared;
using TerminalUI;
using TerminalUI.Elements;
using TerminalUI.Types;
using static Archiver.Shared.Utilities.Formatting;

namespace Archiver.Operations
{
    public static class MainMenu
    {
        private static bool _initialized = false;
        private static CliMenu<bool> _menu; 

        public async static Task StartOperationAsync(CancellationTokenSource cts)
            => await ShowMenuAsync(cts);

        private async static Task ShowMenuAsync(CancellationTokenSource cts)
        {
            BuildMenu();
            _menu.QuitCallback = () => {
                cts.Cancel();
                return Task.CompletedTask;
            };

            while (!cts.IsCancellationRequested)
            {
                Terminal.Clear();
                Terminal.InitHeader("Main Menu", "Archiver");

                var results = await _menu.ShowAsync(false, cts.Token);

                if (results == null)
                    return;

                bool result = results.First();

                if (result == false)
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

        private static void BuildMenu()
        {
            if (_initialized == false)
            {
                _initialized = true;

                List<CliMenuEntry<bool>> entries = new List<CliMenuEntry<bool>>();

                entries.AddRange(BuildDiscMenu());
                entries.Add(new CliMenuEntry<bool>() { Header = true });
                entries.AddRange(BuildTapeMenu());
                entries.Add(new CliMenuEntry<bool>() { Header = true });
                entries.AddRange(BuildCsdMenu());
                entries.Add(new CliMenuEntry<bool>() { Header = true });
                entries.AddRange(BuildUniversalMenu());

                _menu = new CliMenu<bool>(entries);
                //_menu.MenuLabel = "Archiver, main menu...";
                // _menu.OnCancel += () =>
                // {
                //     Environment.Exit(0);
                // };
            }
        }

        private static List<CliMenuEntry<bool>> BuildDiscMenu()
        {
            string discMenuAppend = String.Empty;

            if (SysInfo.IsOpticalDrivePresent == false)
                discMenuAppend += " (drive not detected)";
                
            return new List<CliMenuEntry<bool>>()
            {
                new CliMenuEntry<bool>() {
                    Name = "Disc Operations" + discMenuAppend,
                    Header = true,
                    ShortcutKey = ConsoleKey.D
                },
                new CliMenuEntry<bool>() {
                    Name = "Search Disc Archive",
                    Task = Tasks.Disc.DiscSearcherTask.StartTaskAsync,
                    ForegroundColor = ConsoleColor.Green,
                    SelectedValue = true, // do not show the "press enter to return to main menu" message
                },
                //! not implemented
                new CliMenuEntry<bool>() {
                    Name = "Restore entire disc(s)",
                    Task = NotImplementedAsync,
                    SelectedValue = true,
                    Disabled = !SysInfo.IsOpticalDrivePresent || true, // remove once implemented
                    ForegroundColor = ConsoleColor.Green
                },
                new CliMenuEntry<bool>() {
                    Name = "View Archive Summary",
                    Task = Tasks.Disc.DiscArchiveSummaryTask.StartTaskAsync,
                    SelectedValue = true, // do not show the "press enter to return to main menu" message
                    ForegroundColor = ConsoleColor.Blue
                },
                new CliMenuEntry<bool>() {
                    Name = "Verify Discs",
                    Task = Tasks.Disc.DiscVerificationTask.StartTaskAsync,
                    Disabled = SysInfo.IsReadonlyFilesystem || !SysInfo.IsOpticalDrivePresent,
                    ForegroundColor = ConsoleColor.DarkYellow
                }
                ,
                new CliMenuEntry<bool>() {
                    Name = "Scan For Changes",
                    Task = Tasks.Disc.DiscArchiverTask.StartScanOnlyAsync,
                    ForegroundColor = ConsoleColor.DarkYellow
                },
                // new CliMenuEntry<bool>() {
                //     Name = "Scan For Renamed/Moved Files",
                //     Task = Disc.ScanForFileRenames.StartOperationAsync,
                //     ForegroundColor = ConsoleColor.DarkYellow
                // },
                new CliMenuEntry<bool>() {
                    Name = "Run Archive process",
                    Task = Tasks.Disc.DiscArchiverTask.StartOperationAsync,
                    Disabled = SysInfo.IsReadonlyFilesystem || !SysInfo.IsOpticalDrivePresent,
                    ForegroundColor = ConsoleColor.Red
                }
            };
        }

        private static List<CliMenuEntry<bool>> BuildTapeMenu()
        {
            string tapeMenuAppend = String.Empty;

            if (SysInfo.IsTapeDrivePresent == false)
                tapeMenuAppend += " (drive not detected)";
                
            return new List<CliMenuEntry<bool>>()
            {
                new CliMenuEntry<bool>() {
                    Name = "Tape Operations" + tapeMenuAppend,
                    Header = true,
                    ShortcutKey = ConsoleKey.T
                },
                new CliMenuEntry<bool>() {
                    Name = "Search Tape Archive",
                    Task = Tasks.Tape.TapeSearcherTask.StartTaskAsync,
                    ForegroundColor = ConsoleColor.Green,
                    SelectedValue = true, // do not show the "press enter to return to main menu" message
                },
                //! not implemented
                new CliMenuEntry<bool>() {
                    Name = "Restore entire tape (to tar file)",
                    Task = Tape.RestoreTapeToTar.StartOperationAsync,
                    Disabled = !SysInfo.IsTapeDrivePresent || true, // remove once implemented
                    ForegroundColor = ConsoleColor.Green
                },
                //! not implemented
                new CliMenuEntry<bool>() {
                    Name = "Restore entire tape (to original file structure)",
                    Task = NotImplementedAsync,
                    Disabled = !SysInfo.IsTapeDrivePresent || true, // remove once implemented
                    ForegroundColor = ConsoleColor.Green
                },
                // new CliMenuEntry<bool>() {
                //     Name = "Read Tape Summary",
                //     Task = Tape.ShowTapeSummary.StartOperation,
                //     Disabled = !SysInfo.IsTapeDrivePresent,
                //     // SelectedValue = true, // do not show the "press enter to return to main menu" message
                //     ForegroundColor = ConsoleColor.Blue
                // },
                new CliMenuEntry<bool>() {
                    Name = "View Archive Summary",
                    Task = Tasks.Tape.TapeArchiveSummaryTask.StartTaskAsync,
                    SelectedValue = true, // do not show the "press enter to return to main menu" message
                    ForegroundColor = ConsoleColor.Blue
                },
                // new CliMenuEntry<bool>() {
                //     Name = "Verify Tape",
                //     Task = Tape.TapeVerification.StartOperation,
                //     Disabled = SysInfo.IsReadonlyFilesystem || !SysInfo.IsTapeDrivePresent,
                //     ForegroundColor = ConsoleColor.DarkYellow
                // },
                // new CliMenuEntry<bool>() {
                //     Name = "Run tape archive",
                //     Task = Tape.TapeArchiver.StartOperation,
                //     Disabled = SysInfo.IsReadonlyFilesystem || !SysInfo.IsTapeDrivePresent,
                //     ForegroundColor = ConsoleColor.Red
                // }
            };
        }

        private static List<CliMenuEntry<bool>> BuildCsdMenu()
        {
            return new List<CliMenuEntry<bool>>()
            {
                new CliMenuEntry<bool>() {
                    Name = "Cold Storage Disk (HDD) Operations",
                    Header = true,
                    ShortcutKey = ConsoleKey.C
                },
                new CliMenuEntry<bool>() {
                    Name = "Register CSD Drive",
                    Task = CSD.RegisterDrive.StartOperationAsync,
                    ForegroundColor = ConsoleColor.Green
                },
        //         //! not implemented
        //         new CliMenuEntry<bool>() {
        //             Name = "Restore entire CSD Drive",
        //             Task = NotImplemented,
        //             Disabled = true, // remove once implemented
        //             ForegroundColor = ConsoleColor.Green
        //         },
        //         //! not implemented
        //         new CliMenuEntry<bool>() {
        //             Name = "Read CSD Drive Summary",
        //             Task = NotImplemented,         
        //             // Task = ShowTapeSummary.StartOperation,
        //             Disabled = true, // remove once implemented
        //             SelectedValue = true, // do not show the "press enter to return to main menu" message
        //             ForegroundColor = ConsoleColor.Blue
        //         },
                new CliMenuEntry<bool>() {
                    Name = "View CSD Archive Summary",
                    Task = Tasks.CSD.CsdArchiveSummaryTask.StartTaskAsync,
                    SelectedValue = true, // do not show the "press enter to return to main menu" message
                    ForegroundColor = ConsoleColor.Blue
                },
        //         //! not implemented
        //         new CliMenuEntry<bool>() {
        //             Name = "Verify CSD Drive",
        //             Task = NotImplemented,
        //             // Task = TapeVerification.StartOperation,
        //             Disabled = SysInfo.IsReadonlyFilesystem || true, // remove once implemented
        //             ForegroundColor = ConsoleColor.DarkYellow
        //         },
        //         new CliMenuEntry<bool>() {
        //             Name = "Clean CSD Drive - Remove files not in index",
        //             Task = CSD.Cleaner.StartOperation,
        //             // Task = TapeVerification.StartOperation,
        //             Disabled = SysInfo.IsReadonlyFilesystem, // remove once implemented
        //             ForegroundColor = ConsoleColor.DarkYellow
        //         },
                new CliMenuEntry<bool>() {
                    Name = "Run CSD Archive Process",
                    Task = CSD.Archiver.StartOperationAsync,
                    Disabled = SysInfo.IsReadonlyFilesystem,
                    ForegroundColor = ConsoleColor.Red
                }
            };
        }

        private static List<CliMenuEntry<bool>> BuildUniversalMenu()
        {
            return new List<CliMenuEntry<bool>>()
            {
                new CliMenuEntry<bool>() {
                    Name = "Universal Operations",
                    Header = true
                },
                // new CliMenuEntry<bool>() {
                //     Name = "Copy Tools to Local Disk",
                //     Task = NotImplemented,
                //     Disabled = !SysInfo.IsReadonlyFilesystem
                // },
                // new CliMenuEntry<bool>() {
                //     Name = "Create Index ISO",
                //     Task = Helpers.CreateIndexIso,
                //     Disabled = SysInfo.IsReadonlyFilesystem
                // },
                new CliMenuEntry<bool>() {
                    Name = "Explode",
                    Task = (cToken) => throw new InvalidOperationException("meow!!!!")
                }//,
                // new CliMenuEntry<bool>() {
                //     Name = "Exit",
                //     Task = () => Environment.Exit(0)
                // }
            };
        }

        private static async Task NotImplementedAsync(CancellationToken cToken = default)
        {
            // TODO: What to do with cToken here?
            TaskCompletionSource tcs = new TaskCompletionSource();

            Terminal.InitHeader("Not Implemented", "Archiver");
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
        }
    }
}