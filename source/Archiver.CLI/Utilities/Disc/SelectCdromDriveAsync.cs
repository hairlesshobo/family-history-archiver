using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FoxHollow.Archiver.Shared.Models;
using FoxHollow.Archiver.Shared.Utilities;
using FoxHollow.FHM.Shared.Models;
using FoxHollow.FHM.Shared.Utilities;
using FoxHollow.TerminalUI;
using FoxHollow.TerminalUI.Elements;
using FoxHollow.TerminalUI.Types;

namespace FoxHollow.Archiver.CLI.Utilities.Disc
{
    public static partial class DiscTasks
    {
        public static async Task<OpticalDrive> SelectCdromDriveAsync()
        {
            List<OpticalDrive> drives = OpticalDriveUtils.GetDrives();

            if (drives.Count == 0)
                throw new DriveNotFoundException("No optical drives were detected on this system!");
        
            if (drives.Count == 1)
                return drives[0];

            Terminal.Clear();

            List<MenuEntry> entries = new List<MenuEntry>();

            foreach (OpticalDrive drive in drives)
            {
                MenuEntry newEntry = new MenuEntry();
                newEntry.Name = drive.Name + " (Disc Loaded: ";
                newEntry.SelectedValue = drive;

                if (drive.IsReady)
                    newEntry.Name += $"YES | Volume name: {drive.VolumeLabel} | Format: {drive.VolumeFormat})";
                else
                    newEntry.Name += "NO)";

                entries.Add(newEntry);
            }

            Menu menu = new Menu(entries, enableCancel: true);

            List<OpticalDrive> selectedDrives = await menu.ShowAsync<OpticalDrive>(); //was true

            if (selectedDrives == null)
                return null;
            
            return selectedDrives[0];
        }
    }
}