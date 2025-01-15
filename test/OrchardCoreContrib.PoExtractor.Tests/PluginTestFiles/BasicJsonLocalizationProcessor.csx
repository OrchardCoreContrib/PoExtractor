using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using OrchardCoreContrib.PoExtractor;

// This example plugin implements processing for a very simplistic subset of the i18next JSON format. It only supports
// strings and other objects, and the files must be located in i18n/{language}.json. Even though this is only meant as a
// demo, even this much can be useful in a real life scenario if paired with a backend API that generates the files for
// other languages using PO files, to centralize the localization tooling.
public class BasicJsonLocalizationProcessor : IProjectProcessor
{
    public void Process(string path, string basePath, LocalizableStringCollection strings)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentException.ThrowIfNullOrEmpty(basePath);
        ArgumentNullException.ThrowIfNull(strings);

        var jsonFilePaths = Directory.GetFiles(path, "*.json", SearchOption.AllDirectories)
            .Where(path => Path.GetFileNameWithoutExtension(path).ToUpperInvariant() is "EN" or "00" or "IV")
            .Where(path => Path.GetFileName(Path.GetDirectoryName(path))?.ToUpperInvariant() is "I18N")
            .GroupBy(Path.GetDirectoryName)
            .Select(group => group
                .OrderBy(path => Path.GetFileNameWithoutExtension(path).ToUpperInvariant() switch
                {
                    "EN" => 0,
                    "00" => 1,
                    "IV" => 2,
                    _ => 3,
                })
                .ThenBy(path => path)
                .First());

        foreach (var jsonFilePath in jsonFilePaths)
        {
            try
            {
                ProcessJson(
                    jsonFilePath,
                    strings,
                    JObject.Parse(File.ReadAllText(jsonFilePath)),
                    string.Empty);
            }
            catch
            {
                Console.WriteLine("Process failed for: {0}", path);
            }
        }
    }

    private static void ProcessJson(string path, LocalizableStringCollection strings, JsonNode json, string prefix)
    {
        if (json is JsonObject jsonObject)
        {
            foreach (var (name, value) in jsonObject)
            {
                var newPrefix = string.IsNullOrEmpty(prefix) ? name : $"{prefix}.{name}";
                ProcessJson(path, strings, value, newPrefix);
            }
            
            return;
        }

        if (json is JsonValue jsonValue)
        {
            var value = jsonValue.GetObjectValue()?.ToString();
            strings.Add(new()
            {
                Context = prefix,
                Location = new() { SourceFile = path },
                Text = value,
            });
        }
    }
}

projectProcessors.Add(new BasicJsonLocalizationProcessor());
