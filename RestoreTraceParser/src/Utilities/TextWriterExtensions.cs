using System.IO;

namespace RestoreTraceParser
{
    public static class TextWriterExtensions
    {
        public static void WriteCsvCell<T>(this TextWriter writer, T value)
        {
            if (value == null)
            {
                writer.Write(",");
            }

            string str = value.ToString();
            if (str.Contains(",") || str.Contains("\"") || str.Contains("\n"))
            {
                writer.Write("\"" + str.Replace("\"", "\"\"") + "\"");
            }
            else
            {
                writer.Write(str);
            }
            writer.Write(",");
        }
    }
}
