using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace NbuExplorer
{
    public class MessageExporter
    {
        public static void ExportForWindowsPhone(string fileName, List<DataSetNbuExplorer.MessageRow> rows)
        {
            Encoding enc = Encoding.UTF8;
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
                    sw.WriteLine("X-TYPE:SMS");

                    if (!mr.IstimeNull())
                    {
                        sw.WriteLine("Date:" + mr.time.ToString("s", CultureInfo.InvariantCulture));
                    }

                    const int lineLimit = 76;
                    bool encNeeded = false;
                    string encodedText = EncodingUtils.EncodeQuotedPrintable(mr.messagetext, enc, ref encNeeded, lineLimit).ToString();

                    sw.Write("Subject;ENCODING=QUOTED-PRINTABLE");
                    if (encNeeded) sw.Write(";CHARSET=UTF-8");
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
            Encoding enc = formatCsv ? Encoding.Default : Encoding.UTF8;
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

        internal static void ExportForWindowsPhoneXmlPlusHash(string fileName, List<DataSetNbuExplorer.MessageRow> rows)
        {
            ExportForWindowsPhoneXml(fileName, rows);
            CreateHashForWindowsPhoneXml(fileName);
        }

        private static void CreateHashForWindowsPhoneXml(string fileName)
        {
            string fileContent = File.ReadAllText(fileName, Encoding.UTF8);

            //SHA 256
            SHA256Managed crypt = new SHA256Managed();
            byte[] sha256 = crypt.ComputeHash(Encoding.UTF8.GetBytes(fileContent), 0, Encoding.UTF8.GetByteCount(fileContent));

            //base64
            string base64 = Convert.ToBase64String(sha256);

            //AES
            byte[] encryptedAes = EncryptByAes(base64);

            //base64
            string finalHash = Convert.ToBase64String(encryptedAes);

            //flush to file
            string hashFileName = Path.ChangeExtension(fileName, ".hsh");
            File.WriteAllText(hashFileName, finalHash);
        }

        private static byte[] EncryptByAes(string plainText)
        {
            using (RijndaelManaged cryptEngine = new RijndaelManaged())
            {
                cryptEngine.Key = new Guid("{D86B2FDE-C318-4DD2-8C9E-EB3F1A244DF8}").ToByteArray();
                cryptEngine.IV = new Guid("{089B6AEC-E81D-49AC-91DF-AD071418E7A3}").ToByteArray();
                cryptEngine.Mode = CipherMode.CBC;
                cryptEngine.Padding = PaddingMode.PKCS7;

                // Create a decrytor to perform the stream transform.
                using (ICryptoTransform encryptor = cryptEngine.CreateEncryptor(cryptEngine.Key, cryptEngine.IV))
                {
                    // Create the streams used for encryption.
                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                swEncrypt.Write(plainText);
                            }
                            return msEncrypt.ToArray();
                        }
                    }
                }
            }
        }

        private static void ExportForWindowsPhoneXml(string fileName, List<DataSetNbuExplorer.MessageRow> rows)
        {
            var culture = CultureInfo.InvariantCulture;
            XmlWriterSettings sett = new XmlWriterSettings
            {
                IndentChars = "  ",
                Indent = false,
                Encoding = Encoding.UTF8
            };
            using (XmlWriter xw = XmlWriter.Create(fileName, sett))
            {
                xw.WriteProcessingInstruction("xml", "version='1.0' encoding='UTF-8'");
                xw.WriteStartElement("ArrayOfMessage");
                xw.WriteAttributeString("xmlns", "xsi", null, XmlSchema.InstanceNamespace);
                xw.WriteAttributeString("xmlns", "xsd", null, XmlSchema.Namespace);

                foreach (var mr in rows)
                {
                    xw.WriteStartElement("Message");

                    bool isIncoming = mr.SbrType == 1;
                    xw.WriteStartElement("IsIncoming");
                    xw.WriteString((isIncoming).ToString(culture).ToLowerInvariant());
                    xw.WriteEndElement();

                    if (isIncoming)
                    {
                        xw.WriteStartElement("Sender");
                    }
                    else
                    {
                        xw.WriteStartElement("Recepients");
                    }
                    string theNumber = string.IsNullOrEmpty(mr.number) ? "unknown" : mr.number;
                    xw.WriteString(theNumber);
                    xw.WriteEndElement();

                    xw.WriteStartElement("IsRead");
                    xw.WriteString("true");
                    xw.WriteEndElement();


                    xw.WriteStartElement("LocalTimestamp");
                    if (!mr.IstimeNull())
                    {
                        xw.WriteString(mr.time.ToFileTime().ToString(culture));
                    }
                    xw.WriteEndElement();

                    xw.WriteStartElement("Body");
                    xw.WriteString(XmlHelper.CleanStringForXml(mr.messagetext));
                    xw.WriteEndElement();

                    xw.WriteFullEndElement();
                }
                xw.Close();
            }
        }
    }
}
