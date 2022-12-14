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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using FoxHollow.Archiver.Shared.Classes;
using FoxHollow.Archiver.Shared.Exceptions;
using FoxHollow.Archiver.Shared.Models;
using FoxHollow.Archiver.Shared.Native;
using Microsoft.Win32.SafeHandles;

namespace FoxHollow.Archiver.Shared.Utilities
{
    public static partial class OpticalDriveUtils
    {
        private static class Windows
        {
            public static List<OpticalDrive> GetDrives()
            {
                return DriveInfo.GetDrives().Where(x => x.DriveType == DriveType.CDRom).Select(x =>
                {
                    string driveName = PathUtils.Windows.CleanDriveName(x.Name);

                    return new OpticalDrive()
                    {
                        Name = driveName,
                        MountPoint = driveName,
                        FullPath = PathUtils.GetDrivePath(driveName),
                        IsReady = x.IsReady,
                        VolumeLabel = (x.IsReady ? x.VolumeLabel : null),
                        VolumeFormat = (x.IsReady ? x.DriveFormat : null),
                        IsDiscLoaded = x.IsReady
                    };
                }).ToList();
            }

            public static string GetDriveLabel(string driveName)
                => DriveInfo.GetDrives()
                            .FirstOrDefault(x => PathUtils.Windows.CleanDriveName(x.Name) == PathUtils.Windows.CleanDriveName(driveName))
                           ?.VolumeLabel;

            public static WindowsRawDiscStreamReader GetDriveRawStream(OpticalDrive drive)
                => new WindowsRawDiscStreamReader(drive);

            public static string[] GetOpticalDriveNames()
                => DriveInfo.GetDrives().Select(x => PathUtils.Windows.CleanDriveName(x.Name)).ToArray();

            public static void EjectDrive(OpticalDrive drive)
            {
                using (SafeFileHandle fileHandle = Native.Windows.CreateFile(
                        drive.FullPath,
                        Native.Windows.GENERIC_READ,
                        Native.Windows.FILE_SHARE_READ | Native.Windows.FILE_SHARE_WRITE, 
                        IntPtr.Zero,
                        Native.Windows.OPEN_EXISTING,
                        0,
                        IntPtr.Zero
                    ))
                {

                    if (fileHandle.IsInvalid)
                        throw new NativeMethodException("CreateFile");

                    uint returnedBytes = 0;

                    // Eject the disk
                    Native.Windows.DeviceIoControl(
                        fileHandle,
                        Native.Windows.IOCTL_STORAGE_EJECT_MEDIA,
                        IntPtr.Zero, 0,
                        IntPtr.Zero, 0,
                        ref returnedBytes,
                        IntPtr.Zero
                        );
                }
            }
        }
    }
}
