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
using FoxHollow.Archiver.Shared.Exceptions;
using FoxHollow.Archiver.Shared.Models;

namespace FoxHollow.Archiver.Shared.Utilities
{
    // TODO: move to a separate library
    public static partial class PathUtils
    {
        public static string GetDrivePath(string driveName)
        {
            if (SysInfo.OSType == OSType.Windows)
                return Windows.GetDrivePath(driveName);
            else if (SysInfo.OSType == OSType.Linux)
                return Linux.GetDrivePath(driveName);

            throw new UnsupportedOperatingSystemException();
        }
        

        /// <summary>
        ///     Combine, clean and resolve provided path
        /// </summary>
        /// <param name="paths">Parts of the path to combine</param>
        /// <returns>Clean, combined, and resolved path</returns>
        public static string CleanPathCombine(params string[] paths)
            => PathUtils.ResolveRelativePath(CombinePaths(paths));

        public static string CombinePaths(params string[] parts)
        {
            List<string> newParts = new List<string>();

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];

                if (String.IsNullOrWhiteSpace(part))
                    continue;

                if (i > 0 && part.StartsWith('/'))
                    part = part.TrimStart('/');

                part = part.TrimEnd('/');

                newParts.Add(part);
            }

            return String.Join("/", newParts);
        }
        

        /// <summary>
        ///     Clean that path (convert it to linux style paths with / )
        /// </summary>
        /// <param name="inPath">Path to clean</param>
        /// <returns>Clean path</returns>
        public static string CleanPath(string inPath)
            => inPath.Replace(@"\", "/").TrimEnd('/');

        /// <summary>
        ///     Dirty the path (convert it to windows-style \)
        /// </summary>
        /// <param name="inPath">Path to make dirty</param>
        /// <returns>Dirty path</returns>
        public static string DirtyPath(string inPath)
            => inPath.Replace("/", @"\");

        /// <summary>
        ///     Get the relative portion of a file path (in this context, that means to
        ///     get the file path without the leading drive letter or unc path)
        /// 
        /// Examples:
        ///     /mnt/drive/path     ==>   /mnt/drive/path
        ///     D:/path/to/dir      ==>   /path/to/dir
        ///     //server/path/dir   ==>   /path/dir
        /// </summary>
        /// <param name="inPath">Path to process</param>
        /// <returns></returns>
        public static string GetRelativePath(string inPath)
        {
            // TODO: add handling for paths with protocol
            
            // if the path is a UNC path, we strip the server 
            if (inPath.StartsWith("//"))
            {
                inPath = inPath.TrimStart('/');
                return inPath.Substring(inPath.IndexOf('/'));
            }
                
            // strip the drive letter
            return inPath.Split(':')[1];
        }

        /// <summary>
        ///     Resolve the provided relative path into an absolute path.
        ///     Note: Preceeding drive letter or protocol is supported.
        /// 
        /// Examples:
        ///     D:/path/to/../directory          ==>   D:/path/directory
        ///     \\server\some/folder/./another   ==>   //server/some/folder/another
        /// </summary>
        /// <param name="inPath">Path that is to be resolved</param>
        /// <returns>Resolved path</returns>
        public static string ResolveRelativePath(string inPath)
        {
            // Start by performing basic cleaning on the path (convert \ to  / and trim trailing /)
            inPath = CleanPath(inPath);

            // if path starts with ./ or ../, prepend the path our executable is in
            if (inPath.StartsWith("./") || inPath.StartsWith("../"))
                inPath = $"{SysInfo.Directories.Bin}/{inPath}";

            // a bare filename is provided with no leading ./, ../, or / is provided..
            // we prepend current directory.. but only if there is also no
            // ':' present, which would indicate a protocol was provided
            if (!inPath.StartsWith('/') && inPath.IndexOf(':') < 0)
                inPath = $"{SysInfo.Directories.Bin}/{inPath}";

            // if there is a protocol or drive provided, store it for later
            string protocolOrDrive = String.Empty;

            if (inPath.IndexOf(':') >= 0)
                protocolOrDrive = inPath.Substring(0, inPath.IndexOf(':')+1);

            // strip out the drive letter or protocol, if provided
            string pathWithoutProtocol = inPath.Substring(protocolOrDrive.Length);

            // if our path stars with a double // (such as a unc path or a protocol path)
            // we need to save that for later when we reconstruct the path
            bool doublePrefix = pathWithoutProtocol.StartsWith("//");

            // clean up any double slashes present (in case /a/path//to/something was provided, for example)
            pathWithoutProtocol = pathWithoutProtocol.Replace("//", "/");

            // split the path into parts
            List<string> pathParts = pathWithoutProtocol.TrimStart('/').Split('/').ToList();

            // we need to use a while, because if we change the object while looping with
            // a foreach, an exception will be thrown by the framework
            int index = 0;
            while (true)
            {
                if (index >= pathParts.Count)
                    break;
                
                // get the current part
                string part = pathParts[index];

                // if the current part is ".." (parent directory indicator), we need to remove it
                // and the directory part thast occurs before the ".."
                if (part == ".." && index > 0)
                {
                    pathParts.RemoveAt(index);
                    pathParts.RemoveAt(index-1);

                    // since we removed two enties, we need to back up one before we continue processing
                    index --;
                }

                // if the current part is "." we can just remove it because it is not needed
                else if (part == ".")
                    pathParts.RemoveAt(index);

                // nothing to do, so we move on to the next part to check
                else
                    index++;
            }

            // reconstruct the path and return it
            return protocolOrDrive + (doublePrefix ? "//" : "/") + String.Join("/", pathParts);
        }

        /// <summary>
        ///     Get the file name portion of the provided path
        /// </summary>
        /// <param name="fullPath">full path</param>
        /// <returns>file name only</returns>
        public static string GetFileName(string fullPath)
        {
            fullPath = CleanPath(fullPath);

            return fullPath.Substring(fullPath.LastIndexOf('/')+1);
        }

        
        public static string[] CleanExcludePaths(string[] excludePaths)
        {
            List<string> validatedPaths = new List<string>();
            
            foreach (string excPath in excludePaths)
            {
                string cleanExcPath = CleanPath(excPath);

                if (File.Exists(cleanExcPath) || Directory.Exists(cleanExcPath))
                    validatedPaths.Add(cleanExcPath);
            }

            return validatedPaths.ToArray();
        }
    }
}