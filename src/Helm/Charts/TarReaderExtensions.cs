namespace Helm.Charts
{
    using System.IO;
    using Google.Protobuf;
    using Google.Protobuf.WellKnownTypes;
    using Hapi.Chart;
    using SharpCompress.Readers.Tar;
    using System.Linq;
    using System.Collections.Generic;

    internal static class TarReaderExtensions
    {
        public static Metadata ReadAsMetadata(this TarReader reader)
        {
            var deserializer = ChartPackage.DeserializerBuilder.Build();

            using (Stream entryStream = reader.OpenEntryStream())
            {
                using (TextReader textReader = new StreamReader(entryStream))
                {
                    return deserializer.Deserialize<Hapi.Chart.Metadata>(textReader);
                }
            }
        }

        public static Template ReadAsTemplate(this TarReader reader, string name)
        {
            using (Stream entryStream = reader.OpenEntryStream())
            {
                var template = new Hapi.Chart.Template()
                {
                    Data = ByteString.FromStream(entryStream),
                    Name = name
                };

                return template;
            }
        }

        public static Config ReadAsValues(this TarReader reader)
        {
            using (Stream entryStream = reader.OpenEntryStream())
            using (StreamReader entryReader = new StreamReader(entryStream))
            {
                var values = new Hapi.Chart.Config()
                {
                    Raw = entryReader.ReadToEnd()
                };

                return values;
            }
        }

        public static Requirement ReadAsRequirement(this TarReader reader)
        {
            var deserializer = ChartPackage.DeserializerBuilder.Build();

            using (Stream entryStream = reader.OpenEntryStream())
            using (StreamReader entryReader = new StreamReader(entryStream))
            {
                var result = deserializer.Deserialize<Requirement>(entryReader);

                return result;
            }
        }

        public static Any ReadAsFile(this TarReader reader, string typeUrl) {

            using (Stream entryStream = reader.OpenEntryStream())
            {
                var file = new Any()
                {
                    TypeUrl = typeUrl,
                    Value = ByteString.FromStream(entryStream)
                };

                return file;
            }
        }
    }
}
