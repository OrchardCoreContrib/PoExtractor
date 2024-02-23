using Xunit;

namespace OrchardCoreContrib.PoExtractor.Tests
{
    public class PoWriterTests
    {
        private readonly MemoryStream _stream = new();

        [Fact]
        public void WriteRecord_WritesSingularLocalizableString()
        {
            // Arrange
            var localizableString = new LocalizableString
            {
                Text = "Computer"
            };
            
            // Act
            using (var writer = new PoWriter(_stream))
            {
                writer.WriteRecord(localizableString);
            }

            // Assert
            var result = ReadPoStream();
            Assert.Equal($"msgid \"Computer\"", result[0]);
            Assert.Equal($"msgstr \"\"", result[1]);
        }

        [Fact]
        public void WriteRecord_Escapes()
        {
            // Arrange
            var localizableString = new LocalizableString
            {
                Text = "Computer \r\n"
            };

            // Act
            using (var writer = new PoWriter(_stream))
            {
                writer.WriteRecord(localizableString);
            }

            // Assert
            var result = ReadPoStream();
            Assert.Equal($"msgid \"Computer \\r\\n\"", result[0]);
            Assert.Equal($"msgstr \"\"", result[1]);
        }

        [Fact]
        public void WriteRecord_ShouldNotEscape_CarriageReturn()
        {
            // Arrange
            var localizableString = new LocalizableString
            {
                Text = "Orchard\rCore"
            };

            // Act
            using (var writer = new PoWriter(_stream))
            {
                writer.WriteRecord(localizableString);
            }

            // Assert
            var result = ReadPoStream();
            Assert.Equal("msgid \"Orchard\rCore\"", result[0]);
            Assert.Equal("msgstr \"\"", result[1]);
        }

        [Fact]
        public void WriteRecord_WritesPluralLocalizableString()
        {
            // Arrange
            var localizableString = new LocalizableString()
            {
                Text = "Computer",
                TextPlural = "Computers"
            };

            // Act
            using (var writer = new PoWriter(_stream))
            {
                writer.WriteRecord(localizableString);
            }

            // Assert
            var result = ReadPoStream();
            Assert.Equal($"msgid \"Computer\"", result[0]);
            Assert.Equal($"msgid_plural \"Computers\"", result[1]);
            Assert.Equal($"msgstr[0] \"\"", result[2]);
        }

        [Fact]
        public void WriteRecord_WritesContext()
        {
            // Arrange
            var localizableString = new LocalizableString()
            {
                Text = "Computer",
                Context = "CONTEXT"
            };

            // Act
            using (var writer = new PoWriter(_stream))
            {
                writer.WriteRecord(localizableString);
            }

            // Assert
            var result = ReadPoStream();
            Assert.Equal($"msgctxt \"CONTEXT\"", result[0]);
            Assert.Equal($"msgid \"Computer\"", result[1]);
            Assert.Equal($"msgstr \"\"", result[2]);
        }

        [Fact]
        public void WriteRecord_WritesLocations()
        {
            // Arrange
            var localizableString = new LocalizableString()
            {
                Text = "Computer",
            };
            
            localizableString.Locations.Add(new LocalizableStringLocation
            {
                SourceFile = "File.cs",
                SourceFileLine = 1,
                Comment = "Comment 1"
            });
            
            localizableString.Locations.Add(new LocalizableStringLocation
            {
                SourceFile = "File.cs",
                SourceFileLine = 2,
                Comment = "Comment 2"
            });

            // Act
            using (var writer = new PoWriter(_stream))
            {
                writer.WriteRecord(localizableString);
            }

            // Assert
            var result = ReadPoStream();
            Assert.Equal($"#: File.cs:1", result[0]);
            Assert.Equal($"#. Comment 1", result[1]);
            Assert.Equal($"#: File.cs:2", result[2]);
            Assert.Equal($"#. Comment 2", result[3]);
            Assert.Equal($"msgid \"Computer\"", result[4]);
            Assert.Equal($"msgstr \"\"", result[5]);
        }

        private string[] ReadPoStream()
        {
            using (var reader = new StreamReader(new MemoryStream(_stream.ToArray())))
            {
                return reader
                    .ReadToEnd()
                    .Split(Environment.NewLine);
            }
        }
    }
}
