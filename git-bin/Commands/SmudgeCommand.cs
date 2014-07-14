using System;
using System.Collections.Generic;
using System.IO;
using GitBin.Remotes;
using System.Threading;
using System.Text;

namespace GitBin.Commands
{
    public class SmudgeCommand : ICommand
    {
        private readonly ICacheManager _cacheManager;
        private readonly IRemote _remote;

        public SmudgeCommand(
            ICacheManager cacheManager,
            IRemote remote,
            string[] args)
        {
            if (args.Length != 0)
                throw new ArgumentException();

            _cacheManager = cacheManager;
            _remote = remote;
        }

        public void Execute()
        {
            var stdin = Console.OpenStandardInput();
            var textReader = new StreamReader(stdin);
            var yaml = textReader.ReadToEnd();

            var document = GitBinDocument.FromYaml(yaml);
            GitBinConsole.Write("Smudging {0}:", document.Filename);

            //var comparableFilename = document.Filename.ToLower();
            //if (comparableFilename.Contains("golem_ice.tga"))
            //{
            //    for (int i = 0; i < 100000; i++)
            //    {
            //        System.Threading.Thread.Sleep(1000);
            //    }
            //}

            DownloadMissingFiles(document.ChunkHashes);

            OutputReassembledChunks(document.ChunkHashes);
        }

        private void DownloadMissingFiles(IEnumerable<string> chunkHashes)
        {
            var filesToDownload = _cacheManager.GetFilenamesNotInCache(chunkHashes);

            if (filesToDownload.Length == 0)
            {
                GitBinConsole.WriteNoPrefix(" All chunks already present in cache");
            }
            else
            {
                if (filesToDownload.Length == 1)
                {
                    GitBinConsole.WriteNoPrefix(" Downloading 1 chunk: ");
                }
                else
                {
                    GitBinConsole.WriteNoPrefix(" Downloading {0} chunks: ", filesToDownload.Length);
                }

                AsyncFileProcessor.ProcessFiles(filesToDownload, DownloadFile);
            }

            GitBinConsole.WriteLine();
        }

        private void DownloadFile(string[] filesToDownload, int indexToDownload)
        {
            var filename = filesToDownload[indexToDownload];
            var fullPath = _cacheManager.GetPathForFile(filename);

            _remote.DownloadFile(fullPath, filename);
        }

        private void OutputReassembledChunks(IEnumerable<string> chunkHashes)
        {
            var stdout = Console.OpenStandardOutput();

            List<string> failedHashes = null;

            foreach (var chunkHash in chunkHashes)
            {
                byte[] chunkData = chunkData = _cacheManager.ReadFileFromCache(chunkHash);

                var hashForFileInCache = CleanCommand.GetHashForChunk(chunkData, chunkData.Length);

                if (string.Compare(chunkHash, hashForFileInCache, true) != 0)
                {
                    if (failedHashes == null)
                    {
                        failedHashes = new List<string>();
                    }
                    failedHashes.Add(chunkHash);
                    try
                    {
                        File.Delete(_cacheManager.GetPathForFile(chunkHash));
                    }
                    catch
                    { }
                }

                if (failedHashes == null)
                {
                    stdout.Write(chunkData, 0, chunkData.Length);
                }
            }

            if (failedHashes != null)
            {
                StringBuilder sb = new StringBuilder();

                foreach (var item in failedHashes)
                {
                    sb.AppendLine(item);
                }
                throw new Exception("Hash check failed for: " + sb.ToString());
            }

            stdout.Flush();
        }
    }
}