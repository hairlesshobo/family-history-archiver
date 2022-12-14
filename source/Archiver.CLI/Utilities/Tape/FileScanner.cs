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
using System.Diagnostics;
using System.IO;
using System.Linq;
using FoxHollow.Archiver.Shared.Classes.Tape;
using FoxHollow.Archiver.Shared.Utilities;

namespace FoxHollow.Archiver.CLI.Utilities.Tape
{
    public delegate void Scanner_ProgressChangedDelegate(long newFiles, long excludedFiles);
    public delegate void Scanner_CompleteDelegate();

    public class FileScanner
    {
        public event Scanner_CompleteDelegate OnComplete;
        public event Scanner_ProgressChangedDelegate OnProgressChanged;

        private const int _sampleDurationMs = 250;
        private Stopwatch _sw;
        private long _lastSample;
        private TapeDetail _tapeDetail;
        private long _newFiles = 0;

        public FileScanner(TapeDetail tapeDetail)
        {
            _tapeDetail = tapeDetail;
            _sw = new Stopwatch();

            this.OnComplete += delegate { };
            this.OnProgressChanged += delegate { };
        }

        public void ScanFiles()
        {
            _sw.Start();

            _tapeDetail.Directories = new List<TapeSourceDirectory>();

            foreach (string dirtySourcePath in _tapeDetail.SourceInfo.SourcePaths)
            {
                TapeSourceDirectory newSource = ScanRootDirectory(dirtySourcePath);

                // we only add the source if it doesn't already exist, otherwise we end up duplicating our list
                // of source files
                if (!_tapeDetail.Directories.Contains(newSource))
                    _tapeDetail.Directories.Add(newSource);
            }

            _sw.Stop();
            OnComplete();
        }

        private TapeSourceDirectory ScanRootDirectory(string sourcePath)
        {
            sourcePath = PathUtils.CleanPath(sourcePath);
            string relativePath = PathUtils.GetRelativePath(sourcePath);

            DirectoryInfo sourceDirInfo = new DirectoryInfo(sourcePath);
            List<DirectoryInfo> directoryParts = new List<DirectoryInfo>() {
                sourceDirInfo 
            };
            
            DirectoryInfo parentInfo = sourceDirInfo.Parent;
            TapeSourceDirectory existingDir = null;

            while (parentInfo != null)
            {
                existingDir = FindExistingDirectoryByAboslutePath(parentInfo.FullName);

                if (existingDir != null)
                    break;

                directoryParts.Add(parentInfo);
                parentInfo = parentInfo.Parent;
            }

            if (existingDir != null)
            {
                existingDir.Directories.Add(ScanDirectory(new TapeSourceDirectory(sourcePath)));
                return existingDir;
            }
            else
            {
                directoryParts.Reverse();

                if (directoryParts.Count() > 1)
                {
                    TapeSourceDirectory topLevelDir = new TapeSourceDirectory(directoryParts[0].FullName);
                    TapeSourceDirectory subDir = default(TapeSourceDirectory);

                    for (int i = 1; i < directoryParts.Count()-1; i++)
                    {
                        DirectoryInfo dirInfo = directoryParts[i];
                        TapeSourceDirectory newSubDir = new TapeSourceDirectory(dirInfo.FullName);

                            if (i == 1)
                            {
                                subDir = newSubDir;
                                topLevelDir.Directories.Add(subDir);
                            }
                            else
                            {
                                subDir.Directories.Add(newSubDir);
                                subDir = newSubDir;
                            }
                    }

                    if (subDir != null)
                        subDir.Directories.Add(ScanDirectory(new TapeSourceDirectory(directoryParts[directoryParts.Count()-1].FullName)));
                    else
                        topLevelDir.Directories.Add(ScanDirectory(new TapeSourceDirectory(directoryParts[directoryParts.Count()-1].FullName)));
                        
                
                    return topLevelDir;
                }
                else
                    return ScanDirectory(new TapeSourceDirectory(directoryParts[0].FullName));
            }
        }

        private TapeSourceDirectory FindExistingDirectoryByAboslutePath(string absolutePath)
        {
            string relativePath = PathUtils.GetRelativePath(PathUtils.CleanPath(absolutePath));
            return FindExistingDirectory(relativePath);
        }

        private TapeSourceDirectory FindExistingDirectory(string relativePath, int currentLevel = 1, IEnumerable<TapeSourceDirectory> searchInput = default(IEnumerable<TapeSourceDirectory>))
        {
            IEnumerable<TapeSourceDirectory> search = _tapeDetail.Directories;

            if (searchInput != null)
                search = searchInput;

            string relativePathClean = relativePath.TrimEnd('/');

            string[] relativePathParts = relativePath.Split('/');

            int levels = relativePathParts.Length-1;

            if (currentLevel < levels)
            {
                currentLevel++;
                return FindExistingDirectory(String.Join('/', relativePathParts[0..(currentLevel+1)]), currentLevel, search.SelectMany(x => x.Directories));
            }

            if (currentLevel == levels)
                return search.FirstOrDefault(x => x.RelativePath.ToLower() == relativePathClean.ToLower());

            return null;
        }

        private TapeSourceDirectory ScanDirectory(string sourcePath)
        {
            TapeSourceDirectory directory = new TapeSourceDirectory(PathUtils.CleanPath(sourcePath));
            return ScanDirectory(directory);
        }

        private TapeSourceDirectory ScanDirectory(TapeSourceDirectory directory)
        {
            if (!Directory.Exists(directory.FullPath))
                throw new DirectoryNotFoundException($"Source directory does not exist: {directory.FullPath}");

            foreach (string dir in Directory.GetDirectories(directory.FullPath))
            {
                string cleanDir = PathUtils.CleanPath(dir);

                if (!(_tapeDetail.SourceInfo.ExcludePaths.Any(x => cleanDir.ToLower().StartsWith(x.ToLower()))))
                    directory.Directories.Add(ScanDirectory(dir));
            }

            foreach (string file in Directory.GetFiles(directory.FullPath))
            {
                string cleanFile = PathUtils.CleanPath(file);

                if (_tapeDetail.SourceInfo.ExcludePaths.Any(x => cleanFile.ToLower().StartsWith(x.ToLower())))
                    _tapeDetail.ExcludedFileCount++;

                else if (_tapeDetail.SourceInfo.ExcludeFiles.Any(x => PathUtils.GetFileName(cleanFile).ToLower().EndsWith(x.ToLower())))
                    _tapeDetail.ExcludedFileCount++;

                else
                {
                    directory.Files.Add(new TapeSourceFile(cleanFile, _tapeDetail));
                    _newFiles++;
                }

                if (_sw.ElapsedMilliseconds - _lastSample > _sampleDurationMs)
                {
                    OnProgressChanged(_newFiles, _tapeDetail.ExcludedFileCount);
                    _lastSample = _sw.ElapsedMilliseconds;
                }
            }

            return directory;
        }
    }
}