using SovokXmltv.Sovok;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace SovokXmltv
{
    public class XmlTvWriter : IDisposable
    {
        private const string ICONS_PREFIX_HOST = "http://sovok.tv";
        private static readonly DateTime UNIX_START = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        private XmlWriter _writer;

        public XmlTvWriter(Stream output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            _writer = XmlWriter.Create(output, new XmlWriterSettings { Async = true });
        }

        public async Task Write(SettingsApiResponse settings, ChannelsListApiResponse channels, Epg3ApiResponse epg)
        {
            var timezone = settings.Settings.Timezone.Split(':')[0];
            _writer.WriteDocType("tv", "xmltv.dtd", "SYSTEM", null);

            writeStartElement("tv");
            writeAttribute("generator-info-name", "SovokXmltv");

            foreach (var channel in channels.Channels)
            {
                writeStartElement("channel");
                writeAttribute("id", channel.Id);

                writeStartElement("display-name");
                writeAttribute("lang", "ru");
                _writer.WriteValue(channel.Name);
                _writer.WriteEndElement(); //display-name

                writeStartElement("icon");
                writeAttribute("src", ICONS_PREFIX_HOST + channel.Icon);
                _writer.WriteEndElement(); // icon

                _writer.WriteEndElement(); //channel
            }

            await _writer.FlushAsync();

            foreach (var channel in epg.Channels)
            {
                for (int i = 0; i < channel.Programs.Length; i++)
                {
                    var programm = channel.Programs[i];
                    var nextProgram = i == channel.Programs.Length - 1 ? null : channel.Programs[i + 1];

                    writeStartElement("programme");
                    writeAttribute("channel", channel.Id);
                    if (programm.ProgramStartDateTime != 0)
                        writeAttribute("start", toDateTime(programm.ProgramStartDateTime, timezone));
                    if (programm.ProgramEndDateTime != 0)
                        writeAttribute("stop", toDateTime(programm.ProgramEndDateTime, timezone));
                    else if (nextProgram != null && nextProgram.ProgramStartDateTime != 0)
                        writeAttribute("stop", toDateTime(nextProgram.ProgramStartDateTime, timezone));

                    writeStartElement("title");
                    writeAttribute("lang", "ru");
                    _writer.WriteValue(programm.ProgramName);
                    _writer.WriteEndElement(); //title

                    if (!String.IsNullOrEmpty(programm.Description))
                    {
                        writeStartElement("desc");
                        writeAttribute("lang", "ru");
                        _writer.WriteValue(programm.Description);
                        _writer.WriteEndElement(); //desc
                    }

                    _writer.WriteEndElement(); //programme
                }
                await _writer.FlushAsync();
            }

            _writer.WriteEndElement(); //tv
            await _writer.FlushAsync();
        }

        public void Dispose() => _writer.Dispose();

        private void writeStartElement(string name) => _writer.WriteStartElement(name, null);

        private void writeAttribute(string name, object value) => _writer.WriteAttributeString(name, null, value is string ? (string)value : value.ToString());

        private string toDateTime(double unixTime, string timezone) => UNIX_START.AddSeconds(unixTime).ToString($"yyyyMMddHHmmss {timezone}00");
    }
}
