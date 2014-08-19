using System;
using System.IO;
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
            int currentByteInChunk = 0;

            var stdin = Console.OpenStandardInput();

            bool isBinaryData = false;

            do
            {
                numberOfBytesRead = stdin.Read(chunkBuffer, currentByteInChunk, chunkBuffer.Length - currentByteInChunk);
                
                currentByteInChunk += numberOfBytesRead;

                if ((currentByteInChunk == chunkBuffer.Length || numberOfBytesRead == 0) && currentByteInChunk > 0)
                {
                    if (!isBinaryData)
                    {
                        if(TryParseYamlFromBufferAndPassThroughIfAlreadyYaml(chunkBuffer, currentByteInChunk))
                        {
                            return;
                        }

                        isBinaryData = true;
                    }

                    var hash = GetHashForChunk(chunkBuffer, currentByteInChunk);
                    _cacheManager.WriteFileToCache(hash, chunkBuffer, currentByteInChunk);
                    document.RecordChunk(hash);
                    currentByteInChunk = 0;
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

        private bool TryParseYamlFromBufferAndPassThroughIfAlreadyYaml(byte[] chunkBuffer, int totalBytesInChunk)
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

            var stdout = Console.OpenStandardOutput();
            stdout.Write(chunkBuffer, 0, totalBytesInChunk);
            stdout.Flush();

            return true;
        }

    }
}