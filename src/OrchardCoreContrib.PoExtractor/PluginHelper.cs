using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace OrchardCoreContrib.PoExtractor;

public static class PluginHelper
{
    public static async Task ProcessPluginsAsync(
        IList<string> plugins,
        List<IProjectProcessor> projectProcessors,
        List<string> projectFiles,
        IEnumerable<Assembly> assemblies)
    {
        var sharedOptions = ScriptOptions.Default.AddReferences(assemblies);

        foreach (var plugin in plugins)
        {
            string code;
            ScriptOptions options;

            if (plugin.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                code = await new HttpClient().GetStringAsync(plugin);
                options = sharedOptions.WithFilePath(Path.Join(
                    Environment.CurrentDirectory,
                    Path.GetFileName(new Uri(plugin).AbsolutePath)));
            }
            else
            {
                code = await File.ReadAllTextAsync(plugin);
                options = sharedOptions.WithFilePath(Path.GetFullPath(plugin));
            }

            await CSharpScript.EvaluateAsync(code, options, new PluginContext(projectProcessors, projectFiles));
        }
    }

    public record PluginContext(List<IProjectProcessor> projectProcessors, List<string> projectFiles);
}
