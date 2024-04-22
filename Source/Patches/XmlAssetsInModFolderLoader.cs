using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Verse;

namespace MoreDefLanguages.Patches;

[HarmonyPatch(typeof(DirectXmlLoader), nameof(DirectXmlLoader.XmlAssetsInModFolder))]
public class XmlAssetsInModFolderLoader
{
    public static void Postfix(ref LoadableXmlAsset[] __result, ModContentPack mod,
        string folderPath,
        List<string> foldersToLoadDebug = null)
    {
        List<string> modFoldersToLoad = foldersToLoadDebug ?? mod.foldersToLoadDescendingOrder;
        Dictionary<string, FileInfo> dictionary = new Dictionary<string, FileInfo>();
        
        modFoldersToLoad.ForEach(modPath =>
        {
            var defsFolder = new DirectoryInfo(Path.Combine(modPath, folderPath));
            if (!defsFolder.Exists) return;

            foreach (var supportedExtension in LoaderStore.Loaders.Keys.ToList())
            {
                foreach (var file in defsFolder.GetFiles($"*{supportedExtension}", SearchOption.AllDirectories))
                {
                    var key = file.FullName.Substring(modPath.Length + 1);
                    if (!dictionary.ContainsKey(key))
                        dictionary.Add(key, file);
                }
            }
        });

        if (dictionary.Count == 0) return;

        var assets = new List<LoadableXmlAsset>().Concat(__result).ToList();

        var fileList = dictionary.Values.ToList();
        GenThreading.ParallelFor(0, fileList.Count, i =>
        {
            var fileInfo = fileList[i];

            if (!LoaderStore.Loaders.ContainsKey(fileInfo.Extension)) return;

            var document = LoaderStore.Loaders[fileInfo.Extension].Parse(fileInfo.FullName);
            if (document == null) return;

            assets.Add(new LoadableXmlAsset(fileInfo.Name, fileInfo.Directory.FullName,
                document.OuterXml)
            {
                mod = mod
            });
        });

        __result = assets.ToArray();
    }
}