﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.RepresentationModel.Serialization;

namespace GitBin
{
    public class GitBinDocument
    {
        public string Filename { get; private set; }
        public List<string> ChunkHashes { get; private set; }

        public GitBinDocument()
        {
            this.ChunkHashes = new List<string>();            
        }

        public GitBinDocument(string filename): this()
        {
            this.Filename = filename;
        }

        public void RecordChunk(string hash)
        {
            this.ChunkHashes.Add(hash);
        }

        public static string ToYaml(GitBinDocument document)
        {
            var sb = new StringBuilder();
            var stringWriter = new StringWriter(sb);

            var serializer = new YamlSerializer<GitBinDocument>();
            serializer.Serialize(stringWriter, document);

            if (Environment.OSVersion.Platform == PlatformID.MacOSX ||
                Environment.OSVersion.Platform == PlatformID.Unix)
            {
                sb.Replace("\n", "\r\n");
            }

            return sb.ToString();
        }

        public static GitBinDocument FromYaml(TextReader textReader )
        {
            var yaml = textReader.ReadToEnd();
            return FromYaml(yaml);
        }

        public static GitBinDocument FromYaml(string yaml)
        {
            GitBinDocument document;
            var serializer = new YamlSerializer<GitBinDocument>();

            try
            {
                document = serializer.Deserialize(new StringReader(yaml));
            }
            catch (YamlDotNet.Core.SyntaxErrorException e)
            {
                GitBinConsole.WriteLine("Syntax error in YAML file: {0}\n\n File contents:{1}\n", e.Message, yaml);
                throw;
            }

            return document;
        }
    }
}