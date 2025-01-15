using OrchardCore.Modules;
using OrchardCoreContrib.PoExtractor.DotNet;
using OrchardCoreContrib.PoExtractor.DotNet.CS;
using OrchardCoreContrib.PoExtractor.DotNet.VB;
using OrchardCoreContrib.PoExtractor.Liquid;
using OrchardCoreContrib.PoExtractor.Razor;

namespace OrchardCoreContrib.PoExtractor;

public class Program
{
    private static readonly string _defaultLanguage = Language.CSharp;
    private static readonly string _defaultTemplateEngine = TemplateEngine.Both;

    public static async Task Main(string[] args)
    {
        if (args.Length < 2 || args.Length > 10 || args.Length % 2 == 1)
        {
            ShowHelp();

            return;
        }

        var inputPath = new DirectoryInfo(args[0]).FullName;
        var outputPath = new DirectoryInfo(args[1]).FullName;

        if (!Directory.Exists(inputPath))
        {
            ShowHelp();

            return;
        }

        var options = GetCliOptions(args);

        if (options.Language == null || options.TemplateEngine == null)
        {
            ShowHelp();

            return;
        }

        var projectFiles = new List<string>();
        var projectProcessors = new List<IProjectProcessor>();

        if (options.Language == Language.CSharp)
        {
            projectProcessors.Add(new CSharpProjectProcessor());

            projectFiles.AddRange(Directory
                .EnumerateFiles(inputPath, $"*{ProjectExtension.CS}", SearchOption.AllDirectories)
                .OrderBy(f => f));
        }
        else
        {
            projectProcessors.Add(new VisualBasicProjectProcessor());

            projectFiles.AddRange(Directory
                .EnumerateFiles(inputPath, $"*{ProjectExtension.VB}", SearchOption.AllDirectories)
                .OrderBy(f => f));
        }

        if (options.TemplateEngine == TemplateEngine.Both)
        {
            projectProcessors.Add(new RazorProjectProcessor());
            projectProcessors.Add(new LiquidProjectProcessor());
        }
        else if (options.TemplateEngine == TemplateEngine.Razor)
        {
            projectProcessors.Add(new RazorProjectProcessor());
        }
        else if (options.TemplateEngine == TemplateEngine.Liquid)
        {
            projectProcessors.Add(new LiquidProjectProcessor());
        }

        if (options.Plugins.Count > 0)
        {
            await ProcessPluginsAsync(options.Plugins, projectProcessors, projectFiles);
        }

        var isSingleFileOutput = !string.IsNullOrEmpty(options.SingleOutputFile);
        var localizableStrings = new LocalizableStringCollection();
        foreach (var projectFile in projectFiles)
        {
            var projectPath = Path.GetDirectoryName(projectFile);
            var projectBasePath = Path.GetDirectoryName(projectPath) + Path.DirectorySeparatorChar;
            var projectRelativePath = projectPath[projectBasePath.Length..];
            var rootedProject = projectPath == inputPath 
                ? projectPath : projectPath[(projectPath.IndexOf(inputPath) + inputPath.Length + 1)..];
            if (IgnoredProject.ToList().Any(p => rootedProject.StartsWith(p)))
            {
                continue;
            }

            foreach (var projectProcessor in projectProcessors)
            {
                projectProcessor.Process(projectPath, projectBasePath, localizableStrings);
            }

            if (!isSingleFileOutput)
            {
                if (localizableStrings.Values.Any())
                {
                    var potPath = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(projectFile) + PoWriter.PortaleObjectTemplateExtension);

                    Directory.CreateDirectory(Path.GetDirectoryName(potPath));

                    using var potFile = new PoWriter(potPath);
                    potFile.WriteRecord(localizableStrings.Values);
                }

                Console.WriteLine($"{Path.GetFileName(projectPath)}: Found {localizableStrings.Values.Count()} strings.");
                localizableStrings.Clear();
            }
        }

        if (isSingleFileOutput)
        {
            if (localizableStrings.Values.Any())
            {
                var potPath = Path.Combine(outputPath, options.SingleOutputFile);

                Directory.CreateDirectory(Path.GetDirectoryName(potPath));

                using var potFile = new PoWriter(potPath);
                potFile.WriteRecord(localizableStrings.Values);
            }

            Console.WriteLine($"Found {localizableStrings.Values.Count()} strings.");
        }
    }

    /// <summary>
    /// A shortcut to <see cref="PluginHelper.ProcessPluginsAsync"/> that gives the script access to all of the
    /// <c>OrchardCoreContrib.PoExtractor.*</c> assemblies.
    /// </summary>
    public static Task ProcessPluginsAsync(
        IList<string> plugins,
        List<IProjectProcessor> projectProcessors,
        List<string> projectFiles) =>
        PluginHelper.ProcessPluginsAsync(plugins, projectProcessors, projectFiles, [
            typeof(IProjectProcessor).Assembly, // OrchardCoreContrib.PoExtractor.Abstractions
            typeof(ExtractingCodeWalker).Assembly, // OrchardCoreContrib.PoExtractor.DotNet
            typeof(CSharpProjectProcessor).Assembly, // OrchardCoreContrib.PoExtractor.DotNet.CS
            typeof(VisualBasicProjectProcessor).Assembly, // OrchardCoreContrib.PoExtractor.DotNet.VB
            typeof(LiquidProjectProcessor).Assembly, // OrchardCoreContrib.PoExtractor.Liquid
            typeof(RazorProjectProcessor).Assembly, // OrchardCoreContrib.PoExtractor.Razor
        ]);

    private static GetCliOptionsResult GetCliOptions(string[] args)
    {
        var result = new GetCliOptionsResult
        {
            Language = _defaultLanguage,
            TemplateEngine = _defaultTemplateEngine,
            SingleOutputFile = null,
        };

        for (int i = 4; i <= args.Length; i += 2)
        {
            var item = args[i - 1];
            switch (args[i - 2])
            {
                case "-l":
                case "--language":
                    if (item.Equals(Language.CSharp, StringComparison.CurrentCultureIgnoreCase))
                    {
                        result.Language = Language.CSharp;
                    }
                    else if (item.Equals(Language.VisualBasic, StringComparison.CurrentCultureIgnoreCase))
                    {
                        result.Language = Language.VisualBasic;
                    }
                    else
                    {
                        result.Language = null;
                    }

                    break;
                case "-t":
                case "--template":
                    if (item.Equals(TemplateEngine.Razor, StringComparison.CurrentCultureIgnoreCase))
                    {
                        result.TemplateEngine = TemplateEngine.Razor;
                    }
                    else if (item.Equals(TemplateEngine.Liquid, StringComparison.CurrentCultureIgnoreCase))
                    {
                        result.TemplateEngine = TemplateEngine.Liquid;
                    }
                    else
                    {
                        result.TemplateEngine = null;
                    }

                    break;
                case "-i":
                case "--ignore":
                    if (!string.IsNullOrEmpty(item))
                    {
                        var ignoredProjects = item.Split(',', StringSplitOptions.RemoveEmptyEntries);

                        foreach (var ignoredProject in ignoredProjects)
                        {
                            IgnoredProject.Add(ignoredProject);
                        }
                    }

                    break;
                case "--localizer":
                    if (!string.IsNullOrEmpty(item))
                    {
                        var localizerIdentifiers = item.Split(',', StringSplitOptions.RemoveEmptyEntries);

                        LocalizerAccessors.LocalizerIdentifiers = localizerIdentifiers;
                    }

                    break;
                case "-s":
                case "--single":
                    if (!string.IsNullOrEmpty(item))
                    {
                        result.SingleOutputFile = item;
                    }

                    break;
                case "-p":
                case "--plugin":
                    if (File.Exists(item) || item.StartsWithOrdinalIgnoreCase("https://"))
                    {
                        result.Plugins.Add(item);
                    }

                    break;
                default:
                    result.Language = null;
                    result.TemplateEngine = null;
                    break;
            }
        }

        return result;
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  extractpo <INPUT_PATH> <OUTPUT_PATH> [options]");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  <INPUT_PATH> The path to the input directory, all projects at the the path will be processed.");
        Console.WriteLine("  <OUTPUT_PATH> The path to a directory where POT files will be generated.");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -l, --language <C#|VB>                 Specifies the code language to extracts translatable strings from.");
        Console.WriteLine("                                         Default: C# language");
        Console.WriteLine("  -t, --template <Razor|Liquid>          Specifies the template engine to extract the translatable strings from.");
        Console.WriteLine("                                         Default: Razor & Liquid templates");
        Console.WriteLine("  -i, --ignore project1,project2         Ignores extracting PO filed from a given project(s).");
        Console.WriteLine("  --localizer localizer1,localizer2      Specifies the name of the localizer(s) that will be used during the extraction process.");
        Console.WriteLine("  -s, --single <FILE_NAME>               Specifies the single output file.");
        Console.WriteLine("  -p, --plugin <FILE_NAME_OR_HTTPS_URL>  A path or web URL with HTTPS scheme to a C# script (.csx) file which can define further");
        Console.WriteLine("                                         IProjectProcessor implementations. You can have multiple of this switch in a call.");
    }
}
