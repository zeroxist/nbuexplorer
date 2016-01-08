using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace NbuExplorer
{
    public class MessageExporter
    {
        public static void ExportForWindowsPhone(string fileName, List<DataSetNbuExplorer.MessageRow> rows)
        {
            /* format sample:
BEGIN:VMSG
VERSION: 1.1
BEGIN:VCARD
TEL:+420777123456
END:VCARD
BEGIN:VBODY
X-BOX:SENDBOX
X-READ:READ
X-SIMID:1
X-LOCKED:UNLOCKED
X-TYPE:SMS
Date:2016/01/05 19:03:14
Subject;ENCODING=QUOTED-PRINTABLE;CHARSET=UTF-8:Ahojda. J=C3=A1 sed=C3=ADm ve vlaku. Mam d=C4=9Blat nakup? T.
END:VBODY
END:VMSG
            */

            System.Text.Encoding enc = System.Text.Encoding.UTF8;
            using (StreamWriter sw = new StreamWriter(fileName, false, enc))
            {
                foreach (var mr in rows)
                {
                    if (mr.IsmessagetextNull())
                        continue;

                    sw.WriteLine("BEGIN:VMSG");
                    sw.WriteLine("VERSION: 1.1");
                    sw.WriteLine("BEGIN:VCARD");
                    sw.WriteLine(string.Format("TEL:{0}", mr.number));
                    sw.WriteLine("END:VCARD");
                    sw.WriteLine("BEGIN:VBODY");
                    if (mr.box == "O")
                    {
                        sw.WriteLine("X-BOX:SENDBOX");
                    }
                    else
                    {
                        sw.WriteLine("X-BOX:INBOX");
                    }
                    sw.WriteLine("X-READ:READ");
                    sw.WriteLine("X-SIMID:1");
                    sw.WriteLine("X-LOCKED:UNLOCKED");
                    sw.WriteLine("X-TYPE:SMS");

                    if (!mr.IstimeNull()) // TODO: check 
                    {
                        sw.WriteLine(string.Format("Date:{0:yyyy}/{0:MM}/{0:dd} {0:hh}:{0:mm}:{0:ss}", mr.time));
                    }

                    const int lineLimit = 76;
                    bool encNeeded = mr.messagetext.Length > lineLimit;
                    string encodedText = EncodingUtils.EncodeQuotedPrintable(mr.messagetext, enc, ref encNeeded, lineLimit).ToString();

                    sw.Write("Subject");
                    if (encNeeded) sw.Write(";ENCODING=QUOTED-PRINTABLE;CHARSET=UTF-8");
                    sw.Write(":");
                    sw.WriteLine(encodedText);

                    sw.WriteLine("END:VBODY");
                    sw.WriteLine("END:VMSG");
                }
            }
        }

        public static void ExportForAndroid(string fileName, List<DataSetNbuExplorer.MessageRow> rows)
        {
            var culture = CultureInfo.GetCultureInfo("en-US");
            XmlWriterSettings sett = new XmlWriterSettings
            {
                IndentChars = "  ",
                Indent = true
            };
            using (XmlWriter xw = XmlWriter.Create(fileName, sett))
            {
                xw.WriteProcessingInstruction("xml", "version='1.0' encoding='UTF-8' standalone='yes' ");
                xw.WriteProcessingInstruction("xml-stylesheet", "type=\"text/xsl\" href=\"sms.xsl\"");
                xw.WriteStartElement("smses");
                xw.WriteAttributeString("count", rows.Count.ToString());
                foreach (var mr in rows)
                {
                    xw.WriteStartElement("sms");
                    xw.WriteAttributeString("protocol", "0");
                    xw.WriteAttributeString("address", string.IsNullOrEmpty(mr.number) ? "unknown" : mr.number);
                    xw.WriteAttributeString("date", mr.SbrTime.ToString());
                    xw.WriteAttributeString("type", mr.SbrType.ToString());
                    xw.WriteAttributeString("subject", "null");
                    xw.WriteAttributeString("body", XmlHelper.CleanStringForXml(mr.messagetext));
                    xw.WriteAttributeString("toa", "null");
                    xw.WriteAttributeString("sc_toa", "null");
                    xw.WriteAttributeString("service_center", "null");
                    xw.WriteAttributeString("read", "1");
                    xw.WriteAttributeString("status", "-1");
                    xw.WriteAttributeString("locked", "0");
                    xw.WriteAttributeString("readable_date", mr.IstimeNull() ? "" : mr.time.ToString("MMM d, yyyy h:mm:ss tt", culture));
                    xw.WriteAttributeString("contact_name", mr.name);
                    xw.WriteEndElement();
                }
                xw.Close();
            }
        }

        public static void ExportTextFile(string fileName, bool formatCsv, List<DataSetNbuExplorer.MessageRow> msgsToExport)
        {
            System.Text.Encoding enc = formatCsv ? System.Text.Encoding.Default : System.Text.Encoding.UTF8;
            using (StreamWriter sw = new StreamWriter(fileName, false, enc))
            {
                foreach (var row in msgsToExport)
                {
                    writeMessageInTextFormat(sw, row, formatCsv);
                }
                sw.Close();
            }
        }

        private static void writeMessageInTextFormat(StreamWriter sw, DataSetNbuExplorer.MessageRow mr, bool formatCsv)
        {
            string msgdirection = "";
            switch (mr.box)
            {
                case "I": msgdirection = "from"; break;
                case "O": msgdirection = "to"; break;
            }

            if (formatCsv)
            {
                sw.WriteLine(string.Format("{0};{1};{2};\"{3}\";\"{4}\"",
                    mr.IstimeNull() ? "" : mr.time.ToString(),
                    msgdirection,
                    mr.number,
                    (mr.name == mr.number) ? "" : mr.name.Replace("\"", "\"\""),
                    mr.messagetext.Replace("\"", "\"\"")
                    ));
            }
            else
            {
                if (!mr.IstimeNull()) sw.Write(mr.time.ToString() + " ");
                sw.Write(string.Format("{0} {1}", msgdirection, mr.number).TrimStart());
                if (mr.name != mr.number) sw.Write(" (" + mr.name + ")");
                if (!mr.IsnumberNull()) sw.WriteLine(":");
                sw.WriteLine(mr.messagetext.TrimEnd());
                sw.WriteLine();
            }
        }
    }
}
