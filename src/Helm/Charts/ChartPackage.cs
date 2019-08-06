using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using SharpCompress.Readers;
using SharpCompress.Readers.Tar;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Helm.Charts
{
    public class ChartPackage
    {
        private readonly Stream stream;
        private readonly IDeserializer deserializer;
        private readonly Collection<string> entryNames = new Collection<string>();
        private readonly string directoryName;

        private Hapi.Chart.Metadata metadata;
        private Hapi.Chart.Config values;

        private ChartPackage(Stream stream)
        {
            this.deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .IgnoreUnmatchedProperties()
                .Build();

            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));

            if (!this.stream.CanSeek || !this.stream.CanRead)
            {
                throw new ArgumentOutOfRangeException(nameof(stream), "The stream must be seakable and readable");
            }

            using (var gzipStream = new GZipStream(this.stream, CompressionMode.Decompress, leaveOpen: true))
            using (var reader = TarReader.Open(gzipStream, new ReaderOptions() { LeaveStreamOpen = true }))
            {
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        var filename = reader.Entry.Key;
                        filename = Normalize(filename);
                        this.entryNames.Add(filename);
                    }
                }
            }

            if (this.entryNames.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stream), "The Helm chart appears to be an empty archive.");
            }

            if (this.entryNames[0].StartsWith("/"))
            {
                this.directoryName = "/" + this.entryNames[0].Substring(1).Split('/')[0];
            }
            else
            {
                this.directoryName = this.entryNames[0].Split('/')[0];
            }

            if (this.entryNames.Any(e => !e.StartsWith(this.directoryName + "/")))
            {
                throw new ArgumentOutOfRangeException(nameof(stream), "The Helm chart appears to be invalid; all files must be within the same top level folder");
            }

            if (!this.entryNames.Contains(this.directoryName + "/Chart.yaml"))
            {
                throw new ArgumentOutOfRangeException(nameof(stream), "The Helm chart appears to be invalid; the Chart.yaml file is missing");
            }
        }

        public Hapi.Chart.Metadata Metadata
        {
            get
            {
                if (this.metadata == null)
                {
                    this.metadata = this.Read<Hapi.Chart.Metadata>("Chart.yaml");
                }

                return this.metadata;
            }
        }

        public Hapi.Chart.Config Values
        {
            get
            {
                if (this.values == null)
                {
                    this.values = this.Read<Hapi.Chart.Config>("values.yaml");
                }

                return this.values;
            }
        }

        public static ChartPackage Open(Stream stream)
        {
            return new ChartPackage(stream);
        }

        public Hapi.Chart.Chart Serialize()
        {
            var chart = new Hapi.Chart.Chart();
            chart.Metadata = this.Metadata;

            this.stream.Seek(0, SeekOrigin.Begin);

            using (var gzipStream = new GZipStream(this.stream, CompressionMode.Decompress, leaveOpen: true))
            using (var reader = TarReader.Open(gzipStream, new ReaderOptions() { LeaveStreamOpen = true }))
            {
                while (reader.MoveToNextEntry())
                {
                    if (this.IsTemplate(reader.Entry.Key))
                    {
                        using (Stream entryStream = reader.OpenEntryStream())
                        {
                            var template = new Hapi.Chart.Template()
                            {
                                Data = ByteString.FromStream(entryStream),
                                Name = Normalize(reader.Entry.Key).Substring(this.directoryName.Length + 1)
                            };

                            chart.Templates.Add(template);
                        }
                    }
                    else if (this.IsValues(reader.Entry.Key))
                    {
                        using (Stream entryStream = reader.OpenEntryStream())
                        using (StreamReader entryReader = new StreamReader(entryStream))
                        {
                            chart.Values = new Hapi.Chart.Config()
                            {
                                Raw = entryReader.ReadToEnd()
                            };
                        }
                    }
                    else if (!this.IsTemplate(reader.Entry.Key)
                        && !this.IsMetadata(reader.Entry.Key)
                        && !this.IsValues(reader.Entry.Key))
                    {
                        // TODO: respect .helmignore
                        using (Stream entryStream = reader.OpenEntryStream())
                        {
                            chart.Files.Add(new Any()
                            {
                                TypeUrl = Normalize(reader.Entry.Key).Substring(this.directoryName.Length + 1),
                                Value = ByteString.FromStream(entryStream)
                            });
                        }
                    }
                }
            }

            // Dependencies are currently not supported
            // chart.Dependencies.Add();
            return chart;
        }

        private static string Normalize(string path)
        {
            return path.Replace('\\', '/');
        }

        private bool IsTemplate(string entryName)
        {
            return Normalize(entryName).StartsWith(this.directoryName + "/templates/");
        }

        private bool IsMetadata(string entryName)
        {
            return Normalize(entryName) == this.directoryName + "/Chart.yaml";
        }

        private bool IsValues(string entryName)
        {
            return Normalize(entryName) == this.directoryName + "/values.yaml";
        }

        private T Read<T>(string filename)
        {
            var entryName = this.directoryName + "/" + filename;

            this.stream.Seek(0, SeekOrigin.Begin);

            using (var gzipStream = new GZipStream(this.stream, CompressionMode.Decompress, leaveOpen: true))
            using (var reader = TarReader.Open(gzipStream, new ReaderOptions() { LeaveStreamOpen = true }))
            {
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory && Normalize(reader.Entry.Key) == entryName)
                    {
                        using (Stream entryStream = reader.OpenEntryStream())
                        using (TextReader textReader = new StreamReader(entryStream))
                        {
                            return this.deserializer.Deserialize<T>(textReader);
                        }
                    }
                }
            }

            throw new InvalidOperationException($"The helm chart does not contain a file named {filename}");
        }
    }
}
