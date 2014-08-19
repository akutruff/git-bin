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
                        
            const int numberOfBytesInMebibyte = 1024 * 1024;
            
            var chunkBuffer = new byte[numberOfBytesInMebibyte];
            int numberOfBytesRead;
            int currentByteInChunk = 0;

            Stream stdout = null;

            bool isPassThrough = false; 

            do
            {
                numberOfBytesRead = stdin.Read(chunkBuffer, currentByteInChunk, chunkBuffer.Length - currentByteInChunk);

                currentByteInChunk += numberOfBytesRead;

                if ((currentByteInChunk == chunkBuffer.Length || numberOfBytesRead == 0) && currentByteInChunk > 0)
                {
                    if (!isPassThrough)
                    {
                        if (TryParseYamlFromBufferAndAssembleFileFromCache(chunkBuffer, currentByteInChunk))
                        {
                            return;
                        }
                        isPassThrough = true;
                    }

                    if (stdout == null)
                    {
                        stdout = Console.OpenStandardOutput();
                    }

                    stdout.Write(chunkBuffer, 0, currentByteInChunk);
                    currentByteInChunk = 0;
                }
            } while (numberOfBytesRead > 0);
            
            if (stdout != null)
            {
                GitBinConsole.Write("SKIPPING Smudge");
                stdout.Flush();
            }
        }

        private bool TryParseYamlFromBufferAndAssembleFileFromCache(byte[] chunkBuffer, int totalBytesInChunk)
        {
            var textReader = new StreamReader(new MemoryStream(chunkBuffer, 0, totalBytesInChunk));

            GitBinDocument document = null;
            try
            {
                var yaml = textReader.ReadToEnd();

                document = GitBinDocument.FromYaml(yaml);
                GitBinConsole.Write("Smudging {0}:", document.Filename);
            }
            catch
            {
                return false;
            }

            if (document != null)
            {
                DownloadMissingFiles(document.ChunkHashes);

                OutputReassembledChunks(document.ChunkHashes);
            }

            return true;
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