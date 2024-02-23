using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OrchardCoreContrib.PoExtractor
{
    /// <summary>
    /// Writes <see cref="LocalizableString"/> objects in the <see href="https://www.gnu.org/software/gettext/manual/html_node/PO-Files.html">Portable Object format</see> to a stream
    /// </summary>
    public class PoWriter : IDisposable
    {
        public const string PortaleObjectTemplateExtension = ".pot";

        private readonly TextWriter _writer;

        /// <summary>
        /// Creates a new instance of the <see cref="PoWriter"/>, that writes records to the file
        /// </summary>
        /// <param name="path">the path to the file</param>
        /// <remarks>This function creates a new file or overwrites the existing file, if it already exists</remarks>
        public PoWriter(string path)
        {
            _writer = new StreamWriter(File.Create(path));
        }

        /// <summary>
        /// Creates a new instance of the <see cref="PoWriter"/>, that writes records to the stream
        /// </summary>
        /// <param name="stream"></param>
        public PoWriter(Stream stream)
        {
            _writer = new StreamWriter(stream);
        }

        /// <summary>
        /// Writes a <see cref="LocalizableString"/> object to the output
        /// </summary>
        /// <param name="record">the object to write</param>
        public void WriteRecord(LocalizableString record)
        {
            foreach (var location in record.Locations)
            {
                _writer.WriteLine($"#: {location.SourceFile}:{location.SourceFileLine}");

                if (!string.IsNullOrEmpty(location.Comment))
                {
                    _writer.WriteLine($"#. {location.Comment}");
                }
            }

            if (!string.IsNullOrEmpty(record.Context))
            {
                _writer.WriteLine($"msgctxt \"{Escape(record.Context)}\"");
            }

            _writer.WriteLine($"msgid \"{Escape(record.Text)}\"");

            if (string.IsNullOrEmpty(record.TextPlural))
            {
                _writer.WriteLine($"msgstr \"\"");
            }
            else
            {
                _writer.WriteLine($"msgid_plural \"{Escape(record.TextPlural)}\"");
                _writer.WriteLine($"msgstr[0] \"\"");
            }

            _writer.WriteLine();
        }

        /// <summary>
        /// Writes a collection of <see cref="LocalizableString"/> objects to the output
        /// </summary>
        /// <param name="records">the collection to write</param>
        public void WriteRecord(IEnumerable<LocalizableString> records)
        {
            foreach (var record in records)
            {
                WriteRecord(record);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _writer.Close();
                _writer.Dispose();
            }
        }

        private static string Escape(string text)
        {
            var sb = new StringBuilder(text);
            sb.Replace("\\", "\\\\"); // \ -> \\
            sb.Replace("\"", "\\\""); // " -> \"
            sb.Replace("\r\n", "\\r\\n");
            sb.Replace("\n", "\\n");

            return sb.ToString();
        }
    }
}
