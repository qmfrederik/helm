using Google.Protobuf;
using SharpCompress.Common;
using SharpCompress.Writers;
using SharpCompress.Writers.Tar;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Helm.Charts
{
    public class ChartPackageBuilder
    {
        private readonly Serializer serializer = new SerializerBuilder()
            .WithNamingConvention(new CamelCaseNamingConvention())
            .Build();

        public ChartPackageBuilder()
        {
        }

        public void Build(Hapi.Chart.Chart chart, Stream stream)
        {
            if (chart == null)
            {
                throw new ArgumentNullException(nameof(chart));
            }

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanWrite)
            {
                throw new ArgumentOutOfRangeException(nameof(stream), "The stream must be writeable");
            }

            if (stream.CanSeek)
            {
                // Clear out the stream.
                stream.SetLength(0);
            }

            using (var gzipStream = new GZipStream(stream, CompressionMode.Compress, leaveOpen: true))
            using (var writer = new TarWriter(gzipStream, new WriterOptions(CompressionType.None) { LeaveStreamOpen = true }))
            {
                var chartName = chart.Metadata.Name;
                this.WriteObject(writer, $"{chartName}/Chart.yaml", chart.Metadata);
                this.WriteString(writer, $"{chartName}/values.yaml", chart.Values.Raw);

                foreach (var template in chart.Templates)
                {
                    if (!template.Name.StartsWith($"templates/"))
                    {
                        throw new InvalidOperationException($"The name of the template {template.Name} must start with templates/.");
                    }

                    this.WriteByteString(writer, $"{chartName}/{template.Name}", template.Data);
                }

                foreach (var file in chart.Files)
                {
                    this.WriteByteString(writer, $"{chartName}/{file.TypeUrl}", file.Value);
                }
            }
        }

        private void WriteObject(TarWriter writer, string path, object value)
        {
            if (writer == null)
            {
                throw new ArgumentOutOfRangeException(nameof(writer));
            }

            if (path == null)
            {
                throw new ArgumentOutOfRangeException(nameof(path));
            }

            if (value == null)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            using (var stream = new MemoryStream())
            using (var textWriter = new StreamWriter(stream, Encoding.UTF8))
            {
                this.serializer.Serialize(textWriter, value);
                stream.Position = 0;
                writer.Write(path, stream);
            }
        }

        private void WriteString(TarWriter writer, string path, string value)
        {
            if (writer == null)
            {
                throw new ArgumentOutOfRangeException(nameof(writer));
            }

            if (path == null)
            {
                throw new ArgumentOutOfRangeException(nameof(path));
            }

            if (value == null)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            using (var stream = new MemoryStream())
            using (var textWriter = new StreamWriter(stream, Encoding.UTF8))
            {
                textWriter.Write(value);
                stream.Position = 0;
                writer.Write(path, stream);
            }
        }

        private void WriteByteString(TarWriter writer, string path, ByteString value)
        {
            if (writer == null)
            {
                throw new ArgumentOutOfRangeException(nameof(writer));
            }

            if (path == null)
            {
                throw new ArgumentOutOfRangeException(nameof(path));
            }

            if (value == null)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            using (var stream = new MemoryStream())
            {
                value.WriteTo(stream);
                stream.Position = 0;
                writer.Write(path, stream);
            }
        }
    }
}
