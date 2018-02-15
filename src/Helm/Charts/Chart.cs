using SharpCompress.Readers;
using SharpCompress.Readers.Tar;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Helm.Charts
{
    public class Chart
    {
        private Stream stream;
        private Stream gzipStream;
        private TarReader reader;

        private Chart(Stream stream)
        {
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
            this.gzipStream = new GZipStream(this.stream, CompressionMode.Decompress);

            this.reader = TarReader.Open(this.gzipStream);

            while (this.reader.MoveToNextEntry())
            {
                if (!this.reader.Entry.IsDirectory && this.reader.Entry.Key.EndsWith("Chart.yaml"))
                {
                    using (Stream entryStream = this.reader.OpenEntryStream())
                    using (TextReader reader = new StreamReader(entryStream))
                    {
                        var deserializer = new Deserializer(namingConvention: new CamelCaseNamingConvention(), ignoreUnmatched: true);
                        var metadata = deserializer.Deserialize<Hapi.Chart.Metadata>(reader);
                    }
                }
            }
        }

        public static Chart Open(Stream stream)
        {
            return new Chart(stream);
        }
    }
}
