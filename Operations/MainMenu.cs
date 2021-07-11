using System;
using System.Collections.Generic;
using System.Linq;
using Archiver.Classes.Disc;
using Archiver.Classes.Tape;
using Archiver.Operations.CSD;
using Archiver.Operations.Disc;
using Archiver.Utilities;
using Archiver.Utilities.Shared;
using Archiver.Utilities.Tape;

namespace Archiver.Operations
{
    public static class MainMenu
    {
        private static bool _initialized = false;
        private static CliMenu<bool> _menu; 

        public static void StartOperation()
        {
            // CSD.RegisterDrive.StartOperation();

            // Console.ReadLine();

            // return;

            Initialize();

            while (1 == 1)
            {
                bool result = _menu.Show(true).First();

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

        private static void Initialize()
        {
            if (_initialized == false)
            {
                _initialized = true;

                string tapeMenuAppend = String.Empty;

                if (Config.TapeDrivePresent == false)
                    tapeMenuAppend += " (drive not detected)";

                string discMenuAppend = String.Empty;

                if (Config.OpticalDrivePresent == false)
                    discMenuAppend += " (drive not detected)";

                _menu = new CliMenu<bool>(new List<CliMenuEntry<bool>>()
                {
                    new CliMenuEntry<bool>() {
                        Name = "Disc Operations" + discMenuAppend,
                        Header = true
                    },
                    new CliMenuEntry<bool>() {
                        Name = "Search Disc Archive",
                        Action = DiscSearcher.StartOperation,
                        ForegroundColor = ConsoleColor.Green,
                        SelectedValue = true, // do not show the "press enter to return to main menu" message
                    },
                    //! not implemented
                    new CliMenuEntry<bool>() {
                        Name = "Restore entire disc(s)",
                        Action = NotImplemented,
                        Disabled = !Config.OpticalDrivePresent || true, // remove once implemented
                        ForegroundColor = ConsoleColor.Green
                    },
                    new CliMenuEntry<bool>() {
                        Name = "View Archive Summary",
                        Action = Disc.DiscSummary.StartOperation,
                        SelectedValue = true, // do not show the "press enter to return to main menu" message
                        ForegroundColor = ConsoleColor.Blue
                    },                   
                    new CliMenuEntry<bool>() {
                        Name = "Verify Discs",
                        Action = DiscVerification.StartOperation,
                        Disabled = Config.ReadOnlyFilesystem || !Config.OpticalDrivePresent,
                        ForegroundColor = ConsoleColor.DarkYellow
                    },
                    new CliMenuEntry<bool>() {
                        Name = "Scan For Changes",
                        Action = DiscArchiver.StartScanOnly,
                        ForegroundColor = ConsoleColor.DarkYellow
                    },
                    new CliMenuEntry<bool>() {
                        Name = "Scan For Renamed/Moved Files",
                        Action = ScanForFileRenames.StartOperation,
                        ForegroundColor = ConsoleColor.DarkYellow
                    },
                    new CliMenuEntry<bool>() {
                        Name = "Run Archive process",
                        Action = DiscArchiver.StartOperation,
                        Disabled = Config.ReadOnlyFilesystem || !Config.OpticalDrivePresent,
                        ForegroundColor = ConsoleColor.Red
                    },
                    

                    new CliMenuEntry<bool>() {
                        Header = true
                    },
                    new CliMenuEntry<bool>() {
                        Name = "Tape Operations" + tapeMenuAppend,
                        Header = true
                    },
                    new CliMenuEntry<bool>() {
                        Name = "Search Tape Archive",
                        Action = TapeSearcher.StartOperation,
                        ForegroundColor = ConsoleColor.Green,
                        SelectedValue = true, // do not show the "press enter to return to main menu" message
                    },
                    //! not implemented
                    new CliMenuEntry<bool>() {
                        Name = "Restore entire tape",
                        Action = NotImplemented,
                        Disabled = !Config.TapeDrivePresent || true, // remove once implemented
                        ForegroundColor = ConsoleColor.Green
                    },
                    new CliMenuEntry<bool>() {
                        Name = "Read Tape Summary",
                        Action = ShowTapeSummary.StartOperation,
                        Disabled = !Config.TapeDrivePresent,
                        SelectedValue = true, // do not show the "press enter to return to main menu" message
                        ForegroundColor = ConsoleColor.Blue
                    },
                    new CliMenuEntry<bool>() {
                        Name = "View Archive Summary",
                        Action = TapeArchiveSummary.StartOperation,
                        SelectedValue = true, // do not show the "press enter to return to main menu" message
                        ForegroundColor = ConsoleColor.Blue
                    },
                    new CliMenuEntry<bool>() {
                        Name = "Verify Tape",
                        Action = TapeVerification.StartOperation,
                        Disabled = Config.ReadOnlyFilesystem || !Config.TapeDrivePresent,
                        ForegroundColor = ConsoleColor.DarkYellow
                    },
                    new CliMenuEntry<bool>() {
                        Name = "Run tape archive",
                        Action = TapeArchiver.StartOperation,
                        Disabled = Config.ReadOnlyFilesystem || !Config.TapeDrivePresent,
                        ForegroundColor = ConsoleColor.Red
                    },

                    new CliMenuEntry<bool>() {
                        Header = true
                    },
                    new CliMenuEntry<bool>() {
                        Name = "Cold Storage Disk (HDD) Operations",
                        Header = true
                    },
                    new CliMenuEntry<bool>() {
                        Name = "Register CSD Drive",
                        Action = CSD.RegisterDrive.StartOperation,
                        ForegroundColor = ConsoleColor.Green
                    },
                    //! not implemented
                    new CliMenuEntry<bool>() {
                        Name = "Restore entire CSD Drive",
                        Action = NotImplemented,
                        Disabled = true, // remove once implemented
                        ForegroundColor = ConsoleColor.Green
                    },
                    //! not implemented
                    new CliMenuEntry<bool>() {
                        Name = "Read CSD Drive Summary",
                        Action = NotImplemented,         
                        // Action = ShowTapeSummary.StartOperation,
                        Disabled = true, // remove once implemented
                        SelectedValue = true, // do not show the "press enter to return to main menu" message
                        ForegroundColor = ConsoleColor.Blue
                    },
                    new CliMenuEntry<bool>() {
                        Name = "View CSD Archive Summary",
                        Action = CSD.ArchiveSummary.StartOperation,
                        SelectedValue = true, // do not show the "press enter to return to main menu" message
                        ForegroundColor = ConsoleColor.Blue
                    },
                    //! not implemented
                    new CliMenuEntry<bool>() {
                        Name = "Verify CSD Drive",
                        Action = NotImplemented,
                        // Action = TapeVerification.StartOperation,
                        Disabled = Config.ReadOnlyFilesystem || true, // remove once implemented
                        ForegroundColor = ConsoleColor.DarkYellow
                    },
                    new CliMenuEntry<bool>() {
                        Name = "Run CSD Archive Process",
                        Action = CSD.Archiver.StartOperation,
                        Disabled = Config.ReadOnlyFilesystem,
                        ForegroundColor = ConsoleColor.Red
                    },

                    new CliMenuEntry<bool>() {
                        Header = true
                    },
                    new CliMenuEntry<bool>() {
                        Name = "Universal Operations",
                        Header = true
                    },
                    new CliMenuEntry<bool>() {
                        Name = "Copy Tools to Local Disk",
                        Action = NotImplemented,
                        Disabled = !Config.ReadOnlyFilesystem
                    },
                    new CliMenuEntry<bool>() {
                        Name = "Create Index ISO",
                        Action = Helpers.CreateIndexIso,
                        Disabled = Config.ReadOnlyFilesystem
                    },
                    new CliMenuEntry<bool>() {
                        Name = "Exit",
                        Action = () => Environment.Exit(0)
                    }
                });

                _menu.MenuLabel = "Archiver, main menu...";
                _menu.OnCancel += () =>
                {
                    Environment.Exit(0);
                };
            }
        }

        private static void NotImplemented()
        {
            Formatting.WriteLineC(ConsoleColor.Red, "This operation has not yet been implemented.");
        }
    }
}