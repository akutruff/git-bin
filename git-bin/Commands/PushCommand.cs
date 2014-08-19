using System;
using System.IO;
using System.Linq;
using GitBin.Remotes;

namespace GitBin.Commands
{
    public class PushCommand : ICommand
    {
        private readonly ICacheManager _cacheManager;
        private readonly IRemote _remote;

        public PushCommand(
            ICacheManager cacheManager,
            IRemote remote,
            string[] args)
        {
            if (args.Length > 0)
                throw new ArgumentException();

            _cacheManager = cacheManager;
            _remote = remote;
        }

        public void Execute()
        {
            string[] filesToUpload;
            
            if (!_cacheManager.TryGetFilesInCacheThatAreNotOnRemote(out filesToUpload))
            {
                var filesInRemote = _remote.ListFiles();
                _cacheManager.RecordFilesInRemote(filesInRemote.Select(x => x.Name));

                if (!_cacheManager.TryGetFilesInCacheThatAreNotOnRemote(out filesToUpload))
                {
                    throw new Exception("Unable to record files in remote");
                }
            }


            if (filesToUpload.Length == 0)
            {
                GitBinConsole.Write("All chunks already present on remote");
            }
            else
            {
                if (filesToUpload.Length == 1)
                {
                    GitBinConsole.Write("Uploading 1 chunk: ");
                }
                else
                {
                    GitBinConsole.Write("Uploading {0} chunks: ", filesToUpload.Length);
                }

                AsyncFileProcessor.ProcessFiles(filesToUpload,
                    (files, index) =>
                    {
                        var file = filesToUpload[index];
                        _remote.UploadFile(_cacheManager.GetPathForFile(file), file);
                    });
                
                _cacheManager.RecordFilesInRemote(filesToUpload);
            }
            Console.WriteLine();
        }
    }
}