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
using System.Runtime.InteropServices;
using FoxHollow.Archiver.Shared.Exceptions;
using Microsoft.Win32.SafeHandles;

namespace FoxHollow.Archiver.Shared.Utilities
{
    public static partial class DiskUtils
    {
        public static class Windows
        {
            public static ulong GetDiskSize(string driveName)
                =>  ReadDiskCapacity(driveName).DiskLength;

            public static ulong GetDiskSize(SafeFileHandle fileHandle)
                => ReadDiskCapacity(fileHandle).DiskLength;

            public static Native.Windows.STORAGE_READ_CAPACITY ReadDiskCapacity(string driveName)
            {
                string drivePath = PathUtils.GetDrivePath(driveName);

                IntPtr ptr = IntPtr.Zero;

                try
                {
                    // Create an handle to the drive
                    using (SafeFileHandle fileHandle = Native.Windows.CreateFile(
                        drivePath,
                        Native.Windows.GENERIC_READ,
                        Native.Windows.FILE_SHARE_READ | Native.Windows.FILE_SHARE_WRITE, //0,
                        IntPtr.Zero,
                        Native.Windows.OPEN_EXISTING,
                        0,
                        IntPtr.Zero))
                    {

                        if (fileHandle.IsInvalid)
                            throw new NativeMethodException("CreateFile");

                        return ReadDiskCapacity(fileHandle);
                    }
                }
                finally
                {
                    if (ptr != IntPtr.Zero)
                        Marshal.FreeHGlobal(ptr);
                }
            }

            public static Native.Windows.DISK_GEOMETRY_EX ReadDiskGeometry(SafeFileHandle fileHandle)
            {
                IntPtr ptr = IntPtr.Zero;

                try
                {
                    uint returnedBytes = 0;

                    var geometry = new Native.Windows.DISK_GEOMETRY_EX();
                    
                    int size = Marshal.SizeOf(geometry);
                    ptr = Marshal.AllocHGlobal(size);

                    Marshal.StructureToPtr(geometry, ptr, false);

                    bool result = Native.Windows.DeviceIoControl(
                        fileHandle,
                        Native.Windows.IOCTL_CDROM_GET_DRIVE_GEOMETRY_EX,
                        IntPtr.Zero, 0, // in buffer
                        ptr, (uint)size, // out buffer
                        ref returnedBytes,
                        IntPtr.Zero
                        );

                    if (result == false)
                        throw new NativeMethodException("DeviceIoControl");

                    geometry = (Native.Windows.DISK_GEOMETRY_EX)Marshal.PtrToStructure(
                        ptr, 
                        typeof(Native.Windows.DISK_GEOMETRY_EX)
                        );

                    return geometry;
                }
                finally
                {
                    if (ptr != IntPtr.Zero)
                        Marshal.FreeHGlobal(ptr);
                }
            }

            public static Native.Windows.GET_LENGTH_INFORMATION GetLengthInformation(SafeFileHandle fileHandle)
            {
                IntPtr ptr = IntPtr.Zero;

                try
                {
                    uint returnedBytes = 0;
                    var lengthInfo = new Native.Windows.GET_LENGTH_INFORMATION();

                    int lengthInfoSize = Marshal.SizeOf(lengthInfo);
                    ptr = Marshal.AllocHGlobal(lengthInfoSize);

                    Marshal.StructureToPtr(lengthInfo, ptr, false);

                    bool result = Native.Windows.DeviceIoControl(
                        fileHandle,              // handle to device
                        Native.Windows.IOCTL_DISK_GET_LENGTH_INFO,  // dwIoControlCode
                        IntPtr.Zero, 0, // in
                        ptr, (uint)lengthInfoSize, // out
                        ref returnedBytes,     // number of bytes returned
                        IntPtr.Zero    // OVERLAPPED structure
                    );

                    lengthInfo = (Native.Windows.GET_LENGTH_INFORMATION)Marshal.PtrToStructure(
                        ptr, 
                        typeof(Native.Windows.GET_LENGTH_INFORMATION)
                        );

                    if (result == false)
                        throw new NativeMethodException("DeviceIoControl");

                    return lengthInfo;

                }
                finally
                {
                    if (ptr != IntPtr.Zero)
                        Marshal.FreeHGlobal(ptr);
                }
            }

            public static void SetAllowExtendedIO(SafeFileHandle fileHandle)
            {
                uint returnedBytes = 0;
                
                bool result = Native.Windows.DeviceIoControl(
                    fileHandle,              // handle to device
                    Native.Windows.FSCTL_ALLOW_EXTENDED_DASD_IO,  // dwIoControlCode
                    IntPtr.Zero, 0, // in
                    IntPtr.Zero, 0, // out
                    ref returnedBytes,     // number of bytes returned
                    IntPtr.Zero    // OVERLAPPED structure
                );

                if (result == false)
                    throw new NativeMethodException("DeviceIoControl");
            }

            public static Native.Windows.STORAGE_READ_CAPACITY ReadDiskCapacity(SafeFileHandle fileHandle)
            {
                IntPtr ptr = IntPtr.Zero;

                try
                {
                    uint returnedBytes = 0;

                    var storageReadCapacity = new Native.Windows.STORAGE_READ_CAPACITY();
                    
                    int size = Marshal.SizeOf(storageReadCapacity);
                    ptr = Marshal.AllocHGlobal(size);

                    Marshal.StructureToPtr(storageReadCapacity, ptr, false);

                    bool result = Native.Windows.DeviceIoControl(
                        fileHandle,
                        Native.Windows.IOCTL_STORAGE_READ_CAPACITY,
                        IntPtr.Zero, 0, // in buffer
                        ptr, (uint)size, // out buffer
                        ref returnedBytes,
                        IntPtr.Zero
                        );

                    if (result == false)
                        throw new NativeMethodException("DeviceIoControl");

                    storageReadCapacity = (Native.Windows.STORAGE_READ_CAPACITY)Marshal.PtrToStructure(
                        ptr, 
                        typeof(Native.Windows.STORAGE_READ_CAPACITY)
                        );

                    return storageReadCapacity;
                }
                finally
                {
                    if (ptr != IntPtr.Zero)
                        Marshal.FreeHGlobal(ptr);
                }
            }
        }
    }
}