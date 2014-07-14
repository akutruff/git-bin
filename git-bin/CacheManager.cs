﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GitBin
{
    public interface ICacheManager
    {
        byte[] ReadFileFromCache(string filename);
        void WriteFileToCache(string filename, byte[] contents, int contentLength);
        void WriteFileToCache(string filename, Stream stream);
        GitBinFileInfo[] ListFiles();
        void ClearCache();
        string[] GetFilenamesNotInCache(IEnumerable<string> filenamesToCheck);
        string GetPathForFile(string filename);
    }

    public class CacheManager : ICacheManager
    {
        private readonly DirectoryInfo _cacheDirectoryInfo;

        public CacheManager(IConfigurationProvider configurationProvider)
        {
            _cacheDirectoryInfo = Directory.CreateDirectory(configurationProvider.CacheDirectory);
        }

        public byte[] ReadFileFromCache(string filename)
        {
            var path = GetPathForFile(filename);

            if (!File.Exists(path))
                throw new ಠ_ಠ("Tried to read file from cache that does not exist. [" + path + ']');

            return File.ReadAllBytes(path);
        }

        public void WriteFileToCache(string filename, byte[] contents, int contentLength)
        {
            var path = GetPathForFile(filename);
            
            if (File.Exists(path) && new FileInfo(path).Length == contentLength)
                return;

            var filestream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, contentLength, FileOptions.WriteThrough);
            filestream.Write(contents, 0, contentLength);
            filestream.Close();
        }

        public void WriteFileToCache(string filename, Stream stream)
        {
            var path = GetPathForFile(filename);
            
            if (File.Exists(path))
                    return;

            var buffer = new byte[8192];
            
            var fileStream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, buffer.Length, FileOptions.WriteThrough);

            int bytesRead;
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                fileStream.Write(buffer, 0, bytesRead);
            }

            fileStream.Close();
            stream.Dispose();
        }

        public GitBinFileInfo[] ListFiles()
        {
            var allFiles = _cacheDirectoryInfo.GetFiles();
            var gitBinFileInfos = allFiles.Select(fi => new GitBinFileInfo(fi.Name, fi.Length));

            return gitBinFileInfos.ToArray();
        }

        public void ClearCache()
        {
            foreach (var file in ListFiles())
            {
                File.Delete(GetPathForFile(file.Name));
            }
        }

        public string[] GetFilenamesNotInCache(IEnumerable<string> filenamesToCheck)
        {
            var filenamesInCache = ListFiles().Select(fi => fi.Name);
            var filenamesNotInCache = filenamesToCheck.Distinct().Except(filenamesInCache);

            return filenamesNotInCache.ToArray();
        }

        public string GetPathForFile(string filename)
        {
            return Path.Combine(_cacheDirectoryInfo.FullName, filename);
        }
    }
}