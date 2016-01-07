using System.Text;

namespace NbuExplorer
{
    public static class EncodingUtils
    {
        public static StringBuilder EncodeQuotedPrintable(string text, Encoding encoding, ref bool encNeeded, int lineLimit = int.MaxValue)
        {
            int lineCounter = 0;

            StringBuilder tmp = new StringBuilder();
            foreach (char c in text)
            {
                if (c < ' ' || c == '=' || c == ';' || c > '~')
                {
                    encNeeded = true;
                    foreach (byte b in encoding.GetBytes(new[] { c }))
                    {
                        BreakLineIfNeeded(ref lineCounter, lineLimit, 3, tmp);
                        tmp.Append("=");
                        tmp.Append(((int)b).ToString("X").PadLeft(2, '0'));
                    }
                }
                else
                {
                    BreakLineIfNeeded(ref lineCounter, lineLimit, 1, tmp);
                    tmp.Append(c);
                }
            }
            return tmp;
        }

        public static void BreakLineIfNeeded(ref int counter, int limit, int increment, StringBuilder tmp)
        {
            if (counter + increment > limit)
            {
                tmp.AppendLine("=");
                counter = 0;
            }
            counter += increment;
        }
    }
}
