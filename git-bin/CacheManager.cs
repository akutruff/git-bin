using GitBin.Remotes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GitBin
{
    public interface ICacheManager
    {
        string[] GetFilesInCacheThatAreNotOnRemote(IRemote remote);
        byte[] ReadFileFromCache(string filename);
        void WriteFileToCache(string filename, byte[] contents, int contentLength);
        void WriteFileToCache(string filename, Stream stream);
        GitBinFileInfo[] ListFiles();
        void RecordFilesInRemote(IRemote remote, IEnumerable<string> filenamesToRecord);
        void ClearCache();
        string[] GetFilenamesNotInCache(IEnumerable<string> filenamesToCheck);
        string GetPathForFile(string filename);
    }

    public class CacheManager : ICacheManager
    {
        private static readonly string remoteCacheIndexYmlFilename = "remoteGitBinIndex.yml";
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
            var gitBinFileInfos = allFiles.Where(x => x.Name != remoteCacheIndexYmlFilename).Select(fi => new GitBinFileInfo(fi.Name, fi.Length));

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
            List<string> filesNotInCache = new List<string>();
           
            foreach (var filename in filenamesToCheck.Distinct())
            {
                if (!File.Exists(GetPathForFile(filename)))
                {
                    filesNotInCache.Add(filename);
                }
            }
            
            return filesNotInCache.ToArray();
        }

        public string GetPathForFile(string filename)
        {
            return Path.Combine(_cacheDirectoryInfo.FullName, filename);
        }

        public string[] GetFilesInCacheThatAreNotOnRemote(IRemote remote)
        {
            EnsureRemoteCacheExists(remote);

            var cacheFilePath = GetPathForFile(remoteCacheIndexYmlFilename);

            var document = GitBinDocument.FromYaml(File.ReadAllText(cacheFilePath));

            var filesInCache = new HashSet<string>(ListFiles().Select(x => x.Name));

            GitBinConsole.Write("files in cache: " + filesInCache.Count);
            
            filesInCache.ExceptWith(document.ChunkHashes);

            GitBinConsole.Write("files in cache not in remote: " + filesInCache.Count());

            return filesInCache.ToArray();
        }

        private void EnsureRemoteCacheExists(IRemote remote)
        {
            var cacheFilePath = GetPathForFile(remoteCacheIndexYmlFilename);

            if (File.Exists(cacheFilePath))
            {
                //GitBinConsole.Write("found cache file");
                return;
            }
            GitBinConsole.Write("No cache file found... getting remote file list");

            var remoteFiles = remote.ListFiles();

            GitBinConsole.Write("Remote files found: " + remoteFiles.Length);

            var document = new GitBinDocument(cacheFilePath);
            foreach (var item in remoteFiles)
            {
                document.RecordChunk(item.Name);
            }

            var yml = GitBinDocument.ToYaml(document);

            File.WriteAllText(cacheFilePath, yml);
        }

        public void RecordFilesInRemote(IRemote remote, IEnumerable<string> filenamesToRecord)
        {
            var cacheFilePath = GetPathForFile(remoteCacheIndexYmlFilename);

            EnsureRemoteCacheExists(remote);

            var document = GitBinDocument.FromYaml(File.ReadAllText(cacheFilePath));

            var fileNamesHashSet = new HashSet<string>(document.ChunkHashes);

            foreach (var chunkFileName in filenamesToRecord)
            {
                if (!fileNamesHashSet.Contains(chunkFileName))
                {
                    document.RecordChunk(chunkFileName);
                }
            }

            var yml = GitBinDocument.ToYaml(document);

            File.WriteAllText(cacheFilePath, yml);
        }

    }
}