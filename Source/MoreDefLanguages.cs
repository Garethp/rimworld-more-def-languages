using HarmonyLib;
using Verse;

namespace MoreDefLanguages;

public class MoreDefLanguages : Mod
{
    public MoreDefLanguages(ModContentPack content) : base(content)
    {
        new Harmony("Garethp.MoreDefLanguages.main").PatchAll();
    }
}