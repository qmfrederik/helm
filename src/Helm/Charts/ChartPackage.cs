using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using SharpCompress.Readers;
using SharpCompress.Readers.Tar;
using System;
using System.Collections.Generic;
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
        internal static readonly DeserializerBuilder DeserializerBuilder =
            new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .IgnoreUnmatchedProperties();

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
                        filename = filename.NormalizePath();
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
            return LoadFrom(this.stream);
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
                    if (!reader.Entry.IsDirectory && reader.Entry.Key.Normalize() == entryName)
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

        public static Hapi.Chart.Chart LoadFrom(Stream stream)
        {
            Hapi.Chart.Chart chart = null;

            stream.Seek(0, SeekOrigin.Begin);

            using (var gzipStream = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true))
            using (var reader = TarReader.Open(gzipStream, new ReaderOptions() { LeaveStreamOpen = true }))
            {
                chart = Proccess(reader).chart;
            }

            return chart;
        }

        private static bool CheckContext(string basePath, string path, ref string chartName)
        {
            var name = path.GetChartName(basePath);
            if (string.IsNullOrEmpty(chartName))
            {
                chartName = name;
                return true;
            }
            else
            {
                return name == chartName;
            }
        }

        private static (Hapi.Chart.Chart chart, bool proccessed) Proccess(TarReader reader, string basePath = "", bool next = true)
        {
            Hapi.Chart.Chart chart = new Hapi.Chart.Chart();
            string chartName = null;

            while (!next || reader.MoveToNextEntry())
            {
                var path = reader.Entry.Key.NormalizePath();

                if (!CheckContext(basePath, path, ref chartName))
                {
                    return (chart, proccessed: false);
                }

                if (path.IsDependency(basePath, chartName, out var newBasePath))
                {
                    var result = Proccess(reader, newBasePath, false);

                    // Just make this behave same as the implemention of "helm install"
                    // REF: helm/pkg/chartutil/requirements.go  :  func ProcessRequirementsEnabled(c *chart.Chart, v *chart.Config) error
                    // They commened as below:
                    //   If any dependency is not a part of requirements.yaml
                    //   then this should be added to chartDependencies.
                    //   However, if the dependency is already specified in requirements.yaml
                    //   we should not add it, as it would be anyways processed from requirements.yaml
                    if (!chart.Dependencies.Any(c => c.Metadata.Name == result.chart.Metadata.Name && c.Metadata.Version == result.chart.Metadata.Version))
                    {
                        chart.Dependencies.Add(result.chart);
                    }

                    next = result.proccessed;
                }
                else if (path.IsMetadata(basePath, chartName))
                {
                    chart.Metadata = reader.ReadAsMetadata();
                }
                else if (path.IsTemplate(basePath, chartName))
                {
                    chart.Templates.Add(reader.ReadAsTemplate(path.GetUrl(basePath, chartName)));
                }
                else if (path.IsValues(basePath, chartName))
                {
                    chart.Values = reader.ReadAsValues();
                }
                else if (path.IsRequirement(basePath, chartName))
                {
                    var requirements = reader.ReadAsRequirement();

                    // TODO:
                    // 1. Need to download chart
                    // 2. Use LoadFrom(stream) to load chart
                    // 3. Add loaded chart to dependency
                }
                else
                {
                    // TODO: respect .helmignore
                    chart.Files.Add(reader.ReadAsFile(path.GetUrl(basePath, chartName)));
                }

                next = true;
            }

            return (chart, proccessed: true);
        }
    }
}
