using System.Collections.Generic;
using MoreDefLanguages.Loaders;

namespace MoreDefLanguages;

public class LoaderStore
{
    private static Dictionary<string, ILoader> _loaders = new()
    {
        { ".csv", new BetterCSVLoader() },
        { ".json", new JSONLoader() },
        { ".md", new MarkdownLoader() },
        { ".yaml", new YamlLoader() },
        { ".yml", new YamlLoader() }
    };

    public static Dictionary<string, ILoader> Loaders { get; } = _loaders;

    public static void AddLoader(string extension, ILoader loader)
    {
        if (!extension.EndsWith(".")) extension = $".{extension}";
        
        _loaders.Add(extension, loader);
    }
}