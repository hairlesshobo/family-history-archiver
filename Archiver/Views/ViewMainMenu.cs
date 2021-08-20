// using System;
// using System.Collections.Generic;
// using Archiver.Classes.Views;
// using Archiver.Shared;
// using Archiver.Shared.Utilities;
// using Archiver.Utilities.Shared;
// using Archiver.ViewComponents;
// using Terminal.Gui;

// namespace Archiver.Views
// {
//     public class ViewMainMenu
//     {
//         private List<int> _menuEntryIndex = new List<int>();
//         HyperlinkInfo _actionLink = null;
//         volatile bool _quit = false;

//         private void MenuItemSelected(HyperlinkInfo link)
//         {
//             if (link.DropFromGui)
//             {
//                 _actionLink = link;
//                 Application.Current.Running = false;
//             }
//             else if (link.Action != null)
//                 link.Action();
//         }

//         public bool Show()
//         {
//             // reset the state handling
//             _menuEntryIndex.Clear();
//             _quit = false;
//             _actionLink = null;


//             Application.Init();
//             GuiGlobals.Colors.InitColorSchemes();

//             Toplevel top = Application.Top;
//             Window mainWindow = BuildMainWindow(top);

//             // ScrollView sv = new ScrollView ()
//             // {
//             //     X = 0,
//             //     Y = 0,
//             //     Height = Dim.Fill(),
//             //     Width = Dim.Fill(),
//             //     ContentSize = new Size (25, 30),
// 			//     ShowVerticalScrollIndicator = true,
// 			//     ShowHorizontalScrollIndicator = false,
                
//             // };

//             // mainWindow.Add(sv);

//             BuildDiscMenu(mainWindow);
//             BuildTapeMenu(mainWindow);
//             BuildCsdMenu(mainWindow);
//             BuildUniversalMenu(mainWindow);

//             StatusBarComponent.Add(top, () => { _quit = true; }, true);

//             Application.Run();

//             HandlePostRunAction();

//             return _quit;
//         }

//         private void AddLabel(View view, string text, int marginAbove = 0)
//         {
//             int newY = GetNextLocation(marginAbove);
//             _menuEntryIndex.Add(newY);

//             Label newLabel = new Label($"----- {text} -----") 
//             {
//                 X = 0,
//                 Y = newY
//             };
//             view.Add(newLabel);
//         }

//         private void AddHyperlink(View view, HyperlinkInfo info, int marginAbove = 0)
//         {
//             int newY = GetNextLocation(marginAbove);
//             _menuEntryIndex.Add(newY);

//             Hyperlink newHyperlink = new Hyperlink(info)
//             {
//                 X = 4,
//                 Y = newY,
//                 ActionHandler = MenuItemSelected
//             };
//             view.Add(newHyperlink);
//         }

//         private int GetNextLocation(int marginAbove = 0)
//         {
//             int count = _menuEntryIndex.Count;

//             if (count == 0)
//                 return 1 + marginAbove;
//             else
//                 return _menuEntryIndex[count-1] + 1 + marginAbove;
//         }

//         private void HandlePostRunAction()
//         {
//             if (_actionLink != null)
//             {
//                 if (_actionLink.DropFromGui)
//                 {
//                     Application.Shutdown();
//                     Console.Clear();
//                 }
//                 else
//                     Application.RequestStop();

//                 if (_actionLink.Action != null)
//                     _actionLink.Action();

//                 if (_actionLink.DropFromGui && _actionLink.PauseAfterOperation)
//                 {
//                     Console.WriteLine();
//                     Console.Write("Process complete, press ");
//                     Formatting.WriteC(ConsoleColor.DarkYellow, "<enter>");
//                     Console.Write(", ");
//                     Formatting.WriteC(ConsoleColor.DarkYellow, "<esc>");
//                     Console.Write(", or ");
//                     Formatting.WriteC(ConsoleColor.DarkYellow, "q");
//                     Console.Write(" to return to the main menu...");

//                     while (true)
//                     {
//                         ConsoleKeyInfo key = Console.ReadKey(true);

//                         if (key.Key == ConsoleKey.Q || key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Escape)
//                             break;
//                     }
//                 }
//             }
//         }

//         private Window BuildMainWindow(Toplevel top)
//         {
//             Window mainWindow = new Window("Archiver - Main Menu")
//             {
//                 X = 0,
//                 Y = 0,
//                 Width = Dim.Fill(),
//                 Height = Dim.Fill(1),
//                 CanFocus = false,
//                 // ColorScheme = GuiGlobals.Colors.GlobalScheme
//             };

//             top.Add(mainWindow);

//             return mainWindow;
//         }

//         private void BuildDiscMenu(View view)
//         {
//             AddLabel(view, "Disc Operations");

//             AddHyperlink(view, new HyperlinkInfo() 
//             {
//                 Text = "Search Disc Archive", 
//                 Color = GuiGlobals.Colors.Green,
//                 Action = Operations.Disc.DiscSearcher.StartOperation,
//                 DropFromGui = true
//             });
            

//             //! not implemented
//             AddHyperlink(view, new HyperlinkInfo() 
//             {
//                 Text = "Restore entire disc(s)", 
//                 Disabled = !SysInfo.IsOpticalDrivePresent || true, // remove once implemented
//                 Color = GuiGlobals.Colors.Green,
//                 Action = NotImplemented,
//             });

//             AddHyperlink(view, new HyperlinkInfo() 
//             {
//                 Text = "View Archive Summary", 
//                 Color = GuiGlobals.Colors.Blue,
//                 Action = Operations.Disc.DiscSummary.StartOperation,
//                 DropFromGui = true
//             });
            
//             AddHyperlink(view, new HyperlinkInfo() 
//             {
//                 Text = "Verify Discs", 
//                 Color = GuiGlobals.Colors.Yellow,
//                 Action = Operations.Disc.DiscVerification.StartOperation,
//                 Disabled = SysInfo.IsReadonlyFilesystem || !SysInfo.IsOpticalDrivePresent,
//                 DropFromGui = true,
//             });
            
//             AddHyperlink(view, new HyperlinkInfo() 
//             {
//                 Text = "Scan For Changes", 
//                 Color = GuiGlobals.Colors.Yellow,
//                 Action = Operations.Disc.DiscArchiver.StartScanOnly,
//                 DropFromGui = true
//             });
            
//             AddHyperlink(view, new HyperlinkInfo() 
//             {
//                 Text = "Scan For Renamed/Moved Files", 
//                 Color = GuiGlobals.Colors.Yellow,
//                 Action = Operations.Disc.ScanForFileRenames.StartOperation,
//                 DropFromGui = true
//             });
            
//             AddHyperlink(view, new HyperlinkInfo() 
//             {
//                 Text = "Run Archive process", 
//                 Color = GuiGlobals.Colors.Red,
//                 Action = Operations.Disc.DiscArchiver.StartOperation,
//                 Disabled = SysInfo.IsReadonlyFilesystem || !SysInfo.IsOpticalDrivePresent,
//                 DropFromGui = true
//             });
//         }

//         private void BuildTapeMenu(View view)
//         {
//             AddLabel(view, "Tape Operations", 1);

//             AddHyperlink(view, new HyperlinkInfo() 
//             {
//                 Text = "Search Tape Archive",
//                 Action = Operations.Tape.TapeSearcher.StartOperation,
//                 Color = GuiGlobals.Colors.Green,
//                 DropFromGui = true
//             });

//             //! not implemented
//             AddHyperlink(view, new HyperlinkInfo() 
//             {
//                 Text = "Restore entire tape (to tar file)",
//                 Action = Operations.Tape.RestoreTapeToTar.StartOperation,
//                 Disabled = !SysInfo.IsTapeDrivePresent || true, // remove once implemented
//                 Color = GuiGlobals.Colors.Green,
//                 DropFromGui = true
//             });

//             //! not implemented
//             AddHyperlink(view, new HyperlinkInfo() 
//             {
//                 Text = "Restore entire tape (to original file structure)",
//                 Action = NotImplemented,
//                 Disabled = !SysInfo.IsTapeDrivePresent || true, // remove once implemented
//                 Color = GuiGlobals.Colors.Green,
//                 DropFromGui = true
//             });

//             AddHyperlink(view, new HyperlinkInfo() 
//             {
//                 Text = "Read Tape Summary",
//                 Action = Operations.Tape.ShowTapeSummary.StartOperation,
//                 Disabled = !SysInfo.IsTapeDrivePresent,
//                 Color = GuiGlobals.Colors.Blue,
//                 DropFromGui = true
//             });

//             AddHyperlink(view, new HyperlinkInfo() 
//             {
//                 Text = "View Archive Summary",
//                 Action = Operations.Tape.TapeArchiveSummary.StartOperation,
//                 // SelectedValue = true, // do not show the "press enter to return to main menu" message
//                 Color = GuiGlobals.Colors.Blue,
//                 DropFromGui = true,
//                 PauseAfterOperation = false
//             });

//             AddHyperlink(view, new HyperlinkInfo() 
//             {
//                 Text = "Verify Tape",
//                 Action = Operations.Tape.TapeVerification.StartOperation,
//                 Disabled = SysInfo.IsReadonlyFilesystem || !SysInfo.IsTapeDrivePresent,
//                 Color = GuiGlobals.Colors.Yellow,
//                 DropFromGui = true
//             });

//             AddHyperlink(view, new HyperlinkInfo() 
//             {
//                 Text = "Run tape archive",
//                 Action = Operations.Tape.TapeArchiver.StartOperation,
//                 Disabled = SysInfo.IsReadonlyFilesystem || !SysInfo.IsTapeDrivePresent,
//                 Color = GuiGlobals.Colors.Red,
//                 DropFromGui = true
//             });

//         }

//         private void BuildCsdMenu(View view)
//         {
//             AddLabel(view, "Cold Storage Disk (HDD) Operations", 1);


//             AddHyperlink(view, new HyperlinkInfo() {
//                 Text = "Register CSD Drive",
//                 Action = Operations.CSD.RegisterDrive.StartOperation,
//                 Color = GuiGlobals.Colors.Green,
//                 DropFromGui = true
//             });

//             //! not implemented
//             AddHyperlink(view, new HyperlinkInfo() {
//                 Text = "Restore entire CSD Drive",
//                 Action = NotImplemented,
//                 //Disabled = true, // remove once implemented
//                 Color = GuiGlobals.Colors.Green,
//                 DropFromGui = true
//             });

//             //! not implemented
//             AddHyperlink(view, new HyperlinkInfo() {
//                 Text = "Read CSD Drive Summary",
//                 Action = NotImplemented,         
//                 // Action = ShowTapeSummary.StartOperation,
//                 //Disabled = true, // remove once implemented
//                 // SelectedValue = true, // do not show the "press enter to return to main menu" message
//                 Color = GuiGlobals.Colors.Blue,
//                 DropFromGui = true
//             });

//             AddHyperlink(view, new HyperlinkInfo() {
//                 Text = "View CSD Archive Summary",
//                 Action = Operations.CSD.ArchiveSummary.StartOperation,
//                 // SelectedValue = true, // do not show the "press enter to return to main menu" message
//                 Color = GuiGlobals.Colors.Blue,
//                 DropFromGui = true
//             });

//             //! not implemented
//             AddHyperlink(view, new HyperlinkInfo() {
//                 Text = "Verify CSD Drive",
//                 Action = NotImplemented,
//                 // Action = TapeVerification.StartOperation,
//                 //Disabled = SysInfo.IsReadonlyFilesystem || true, // remove once implemented
//                 Color = GuiGlobals.Colors.Yellow,
//                 DropFromGui = true
//             });

//             AddHyperlink(view, new HyperlinkInfo() {
//                 Text = "Clean CSD Drive - Remove files not in index",
//                 Action = Operations.CSD.Cleaner.StartOperation,
//                 // Action = TapeVerification.StartOperation,
//                 //Disabled = SysInfo.IsReadonlyFilesystem, // remove once implemented
//                 Color = GuiGlobals.Colors.Yellow,
//                 DropFromGui = true
//             });

//             AddHyperlink(view, new HyperlinkInfo() {
//                 Text = "Run CSD Archive Process",
//                 Action = Operations.CSD.Archiver.StartOperation,
//                 //Disabled = SysInfo.IsReadonlyFilesystem,
//                 Color = GuiGlobals.Colors.Red,
//                 DropFromGui = true
//             });

//         }

//         private void BuildUniversalMenu(View view)
//         {
//             AddLabel(view, "Universal Operations", 1);

//             AddHyperlink(view, new HyperlinkInfo() {
//                 Text = "Copy Tools to Local Disk",
//                 Action = NotImplemented,
//                 //Disabled = !SysInfo.IsReadonlyFilesystem,
//                 DropFromGui = true
//             });

//             AddHyperlink(view, new HyperlinkInfo() {
//                 Text = "Create Index ISO",
//                 Action = Helpers.CreateIndexIso,
//                 //Disabled = SysInfo.IsReadonlyFilesystem,
//                 DropFromGui = true
//             });

//             AddHyperlink(view, new HyperlinkInfo() {
//                 Text = "Exit",
//                 Action = () => 
//                 {
//                     _quit = true;
//                     Application.RequestStop();
//                 }
//             });
//         }

//         public static void NotImplemented()
//             => MessageBox.ErrorQuery(40, 10, "Error", "This operation has not yet been implemented.", "Ok");
//     }
// }