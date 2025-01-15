using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace OrchardCoreContrib.PoExtractor.Tests;

public class PluginTests
{
    private const string PluginTestFiles = nameof(PluginTestFiles);
    
    [Fact]
    public async Task ProcessPluginsBasicJsonLocalizationProcessor()
    {
        // Arrange
        using var stream = new MemoryStream();
        var plugins = new[] { Path.Join(PluginTestFiles, "BasicJsonLocalizationProcessor.csx") };
        var projectProcessors = new  List<IProjectProcessor>();
        var projectFiles = new List<string> { Path.Join(PluginTestFiles, "OrchardCoreContrib.PoExtractor.Tests.dll") };
        var localizableStrings = new LocalizableStringCollection();
        
        // Act
        await Program.ProcessPluginsAsync(plugins, projectProcessors, projectFiles);
        projectProcessors[0].Process(PluginTestFiles, Path.GetFileName(Environment.CurrentDirectory), localizableStrings);

        using (var writer = new PoWriter(stream))
        {
            writer.WriteRecord(localizableStrings.Values);
        }

        // Assert
        Assert.Single(projectProcessors);
        Assert.Single(projectFiles);
        Assert.Equal(5, localizableStrings.Values.Count());
        
        const string expectedResult = @"
        #: PluginTestFiles/i18n/en.json:0
        msgctxt ""about.title""
        msgid ""About us""
        msgstr """"

        #: PluginTestFiles/i18n/en.json:0
        msgctxt ""about.notes""
        msgid ""Title for main menu""
        msgstr """"

        #: PluginTestFiles/i18n/en.json:0
        msgctxt ""home.title""
        msgid ""Home page""
        msgstr """"

        #: PluginTestFiles/i18n/en.json:0
        msgctxt ""home.context""
        msgid ""Displayed on the main website page""
        msgstr """"

        #: PluginTestFiles/i18n/en.json:0
        msgctxt ""admin.login.title""
        msgid ""Administrator login""
        msgstr """"";
        var actualResult = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal(CleanupSpaces(expectedResult), CleanupSpaces(actualResult));
    }

    private static string CleanupSpaces(string input)
    {
        // Trim leading whitespaces.
        input = Regex.Replace(input.Trim(), @"^\s+", string.Empty, RegexOptions.Multiline);
        
        // Make the path OS-specific, so the test works on Windows as well. 
        return input.Replace('/', Path.DirectorySeparatorChar);
    }
}
