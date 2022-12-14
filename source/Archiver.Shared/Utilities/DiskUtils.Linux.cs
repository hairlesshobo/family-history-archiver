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
using FoxHollow.Archiver.Shared.Exceptions;
using FoxHollow.Archiver.Shared.Native;

namespace FoxHollow.Archiver.Shared.Utilities
{
    public static partial class DiskUtils
    {
        public static class Linux
        {
            public static ulong GetDiskSize(string driveName)
            {
                string drivePath = PathUtils.GetDrivePath(driveName);

                int handle = Native.Linux.Open(drivePath, Native.Linux.OpenType.ReadOnly);;

                try
                {
                    return GetDiskSize(handle);
                }
                finally
                {
                    if (handle > 0)
                        Native.Linux.Close(handle);
                }
            }

            public static ulong GetDiskSize(int handle)
            {
                ulong returnValue64 = 0;

                if (Native.Linux.Ioctl(handle, Native.Linux.BLKGETSIZE64, out returnValue64) < 0)
                    throw new NativeMethodException("Ioctl");

                return returnValue64;
            }

            public static long GetFileSize(string devicePath)
            {
                Native.Linux.StatResult result = new Native.Linux.StatResult();
                Native.Linux.Stat(devicePath, ref result);

                return result.Size;
            }
        }
    }
}