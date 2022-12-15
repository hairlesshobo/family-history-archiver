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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FoxHollow.Archiver.CLI.Utilities.Disc;
using FoxHollow.Archiver.CLI.Utilities.Shared;
using FoxHollow.Archiver.Shared.Classes.Disc;
using FoxHollow.Archiver.Shared.Operations.Disc;
using FoxHollow.Archiver.Shared.Utilities;
using FoxHollow.FHM.Shared.Models;
using FoxHollow.TerminalUI;
using FoxHollow.TerminalUI.Elements;
using FoxHollow.TerminalUI.Types;

namespace FoxHollow.Archiver.CLI.Tasks.Disc
{
    internal static class DiscVerificationTask
    {
        // TODO: Show results after process is complete
        // TODO: Allow verification even if last date is < required verification period
        internal static async Task StartTaskAsync(CancellationToken cToken)
        {
            // todo: use cToken here
            Terminal.Header.UpdateLeft("Verify Discs > Initializing...");
            Terminal.Clear();

            List<DiscDetail> allDiscs = await Helpers.ReadDiscIndexAsync(cToken);

            Terminal.Header.UpdateLeft("Select Drive");
            OpticalDrive selectedDrive = await DiscTasks.SelectCdromDriveAsync();

            if (selectedDrive == null)
                return;

            Terminal.Header.UpdateLeft("Verify Discs");
            bool? verifyAll = await AskVerifyAllDiscsAsync();

            if (verifyAll == null)
                return;

            List<DiscDetail> discsToVerify = null;
            
            if (verifyAll.Value == true)
                discsToVerify = allDiscs;
            else
                discsToVerify = await AskDiskToVerifyAsync(allDiscs);

            if (discsToVerify == null)
                return;

            discsToVerify.Sort((x, y) => x.DiscName.CompareTo(y.DiscName));

            await StartVerificationAsync(selectedDrive, discsToVerify);
        }

        private static async Task StartVerificationAsync(OpticalDrive selectedDrive, List<DiscDetail> discsToVerify)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            DiscVerifier verifier = new DiscVerifier(selectedDrive, discsToVerify);

            Terminal.StatusBar.ShowItems(
                new StatusBarItem(
                    "Cancel",
                    (key) => {
                        cts.Cancel();
                        return Task.CompletedTask;
                    },
                    Key.MakeKey(ConsoleKey.C, ConsoleModifiers.Control)
                )
            );

            Terminal.Clear();

            KeyValueText kvtStatus = new KeyValueText("Status", null, -16);
            Terminal.NextLine();

            KeyValueText kvtDiscs = new KeyValueText("Discs Verified", null, -16);
            Terminal.NextLine();
            Terminal.NextLine();

            KeyValueText kvtElapsedTime = new KeyValueText("Elapsed Time", null, -16);
            Terminal.NextLine();

            KeyValueText kvtDiscName = new KeyValueText("Disc Name", null, -16);
            Terminal.NextLine();

            KeyValueText kvtVerified = new KeyValueText("Verified", Formatting.GetFriendlySize(0), -16);
            Terminal.NextLine();

            KeyValueText kvtCurrentRate = new KeyValueText("Current Rate", Formatting.GetFriendlyTransferRate(0), -16);
            Terminal.NextLine();

            KeyValueText kvtAvgRate = new KeyValueText("Average Rate", Formatting.GetFriendlyTransferRate(0), -16);
            Terminal.NextLine();
            Terminal.NextLine();

            ProgressBar progressBar = new ProgressBar();
            Terminal.NextLine();
            Terminal.NextLine();

            Text pendingLabel = new Text("Discs Pending Verification...", foregroundColor: ConsoleColor.Magenta);
            Terminal.NextLine();

            List<DataTableColumn> columns = new List<DataTableColumn>()
            {
                new DataTableColumn("DiscName", "Disc Name", 12),
                new DataTableColumn(null, "Status", 20)
                {
                    Format = (value) => 
                    {
                        DiscDetail detail = (DiscDetail)value;
                        int lastVerifyDays = detail.DaysSinceLastVerify;

                        if (lastVerifyDays > 7)
                            return "Pending";
                        else
                        {
                            if (detail.LastVerifySuccess == true)
                                return "Verification Passed";
                            else
                                return "Verification FAILED";
                        }
                        
                    }
                },
                new DataTableColumn("DaysSinceLastVerify", "Last Verify", 13)
                {
                    Format = (value) => $"{value} day(s)"
                },
                new DataTableColumn("DataSize", "Data Size", 12)
                {
                    Format = (value) => Formatting.GetFriendlySize((long)value)
                }
            };

            DataTable dataTable = new DataTable((IList)verifier.PendingDiscs, columns, rows: 6, showHeader: true);

            NotificationBox box = new NotificationBox(5, 0);
            box.SetTextJustify(0, TextJustify.Center);
            box.SetTextJustify(1, TextJustify.Center);

            Action hideProgress = () => {
                kvtElapsedTime.Hide();
                kvtDiscName.Hide();
                kvtVerified.Hide();
                kvtCurrentRate.Hide();
                kvtAvgRate.Hide();
                progressBar.Hide();
            };

            verifier.OnStatusChanged += (status, text) => kvtStatus.UpdateValue(text); 
            verifier.OnDiscVerificationStarted += (disc) => {
                kvtDiscName.UpdateValue(disc.DiscName);
                kvtDiscName.Show();
                kvtElapsedTime.Show();
                kvtVerified.Show();
                kvtCurrentRate.Show();
                kvtAvgRate.Show();
                progressBar.Show();

                kvtDiscs.UpdateValue($"{verifier.CompletedDiscCount}/{verifier.DiscCount}");
            };

            verifier.OnDiscVerificationProgressChanged += (disc, progress, sw) => {
                kvtVerified.UpdateValue($"{Formatting.GetFriendlySize(progress.TotalBytesProcessed)} / {Formatting.GetFriendlySize(progress.TotalBytes)}");
                kvtAvgRate.UpdateValue(Formatting.GetFriendlyTransferRate(progress.AverageRate));
                kvtCurrentRate.UpdateValue(Formatting.GetFriendlyTransferRate(progress.InstantRate));

                progressBar.UpdateProgress(progress.PercentCompleted);
                kvtElapsedTime.UpdateValue(sw.Elapsed.ToString());
            };

            verifier.OnDiscVerificationComplete += async (disc, success) => {
                hideProgress();

                dataTable.SetDataStore((IList)verifier.PendingDiscs);
                dataTable.Redraw();

                if (success)
                {
                    box.SetBorderColors(ConsoleColor.Green, null);
                    box.SetLineColor(0, ConsoleColor.Green);
                    box.UpdateText(0, "Verification Successful");
                    box.UpdateText(1, $"Disc `{disc.DiscName}` was successfully verified!");   
                }
                else
                {
                    box.SetBorderColors(ConsoleColor.Red, null);
                    box.SetLineColor(0, ConsoleColor.Red);
                    box.UpdateText(0, "Verification FAILED");
                    box.UpdateText(1, $"Disc `{disc.DiscName}` failed verification!");
                }

                box.Show();

                await Task.Delay(5000);

                box.Hide();
            };

            kvtStatus.Show();
            kvtDiscs.Show();
            pendingLabel.Show();
            dataTable.Show();

            hideProgress();

            await verifier.StartVerificationAsync(cts.Token);
        }

        private static async Task<Nullable<bool>> AskVerifyAllDiscsAsync()
        {
            Terminal.Clear();
            var entries = new List<MenuEntry>()
            {
                new MenuEntry() {
                    Name = "All Discs",
                    SelectedValue = true
                },
                new MenuEntry() {
                    Name = "Single Disc",
                    SelectedValue = false
                }
            };

            Menu menu = new Menu(entries, enableCancel: true); 
            List<bool> result = await menu.ShowAsync<bool>(); // was true

            if (result == null)
                return null;

            return result[0];
        }

        private static async Task<List<DiscDetail>> AskDiskToVerifyAsync(List<DiscDetail> allDiscs)
        {
            List<MenuEntry> entries = new List<MenuEntry>();

            foreach (DiscDetail disc in allDiscs.Where(x => x.NewDisc == false).OrderBy(x => x.DiscNumber))
            {
                entries.Add(new MenuEntry()
                {
                    // Name = $"{disc.DiscName} `R|`N `BDate Archived:`N {disc.ArchiveDTM.ToString("MM-dd-yyyy")} `R|`N `BData Size:`N {Formatting.GetFriendlySize(disc.DataSize).PadLeft(10)}",
                    Name = $"{disc.DiscName} | Date Archived: {disc.ArchiveDTM.ToString("MM-dd-yyyy")} | Data Size: {Formatting.GetFriendlySize(disc.DataSize).PadLeft(10)}",
                    SelectedValue = disc
                });
            }

            Menu menu = new Menu(entries, multiSelect: true, enableCancel: true);
            menu.LeftPad = 0;
            Terminal.Clear();
            List<DiscDetail> discsToVerify = await menu.ShowAsync<DiscDetail>(); // was true

            return discsToVerify;
        }
    }
}