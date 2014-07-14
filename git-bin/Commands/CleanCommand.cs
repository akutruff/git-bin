﻿using System;
using System.Security.Cryptography;
using System.Text;

namespace GitBin.Commands
{
    public class CleanCommand : ICommand
    {
        private readonly IConfigurationProvider _configurationProvider;
        private readonly ICacheManager _cacheManager;
        private readonly string _filename;

        public CleanCommand(
            IConfigurationProvider configurationProvider,
            ICacheManager cacheManager,
            string[] args)
        {
            if (args.Length != 1)
                throw new ArgumentException();

            _configurationProvider = configurationProvider;
            _cacheManager = cacheManager;

            _filename = args[0];
        }

        public void Execute()
        {
            GitBinConsole.WriteLine("Cleaning {0}", _filename);

            //var comparableFilename = _filename.ToLower();
            //if (comparableFilename.Contains("golem_iron.tga"))
            //{
            //    for (int i = 0; i < 100000; i++)
            //    {
            //        System.Threading.Thread.Sleep(1000);
            //    }
            //}

            var document = new GitBinDocument(_filename);

            var chunkBuffer = new byte[_configurationProvider.ChunkSize];
            int numberOfBytesRead;
            int totalBytesInChunk = 0;

            var stdin = Console.OpenStandardInput();

            do
            {
                numberOfBytesRead = stdin.Read(chunkBuffer, totalBytesInChunk, chunkBuffer.Length - totalBytesInChunk);
                
                totalBytesInChunk += numberOfBytesRead;

                if ((totalBytesInChunk == chunkBuffer.Length || numberOfBytesRead == 0) && totalBytesInChunk > 0)
                {
                    var hash = GetHashForChunk(chunkBuffer, totalBytesInChunk);
                    _cacheManager.WriteFileToCache(hash, chunkBuffer, totalBytesInChunk);
                    document.RecordChunk(hash);
                    totalBytesInChunk = 0;
                }
            } while (numberOfBytesRead > 0);

            var yamlString = GitBinDocument.ToYaml(document);

            Console.Write(yamlString);
            Console.Out.Flush();
        }

        public static string GetHashForChunk(byte[] chunkBuffer, int chunkLength)
        {
            var hasher = new SHA256Managed();

            byte[] hashBytes = hasher.ComputeHash(chunkBuffer, 0, chunkLength);
            var hashString = BitConverter.ToString(hashBytes).Replace("-", String.Empty);

            return hashString;
        }
    }
}