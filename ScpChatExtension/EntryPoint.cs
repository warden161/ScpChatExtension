using HarmonyLib;
using System;
using System.Collections.Generic;
using PluginAPI.Core.Attributes;
using PluginAPI.Core;

namespace ScpChatExtension;

public class EntryPoint
{
    public const string Name = "ScpChatExtension";
    public const string Version = "1.0.0";
    public static Harmony HarmonyPatcher { get; } = new(HarmonyId);
    public static List<ReferenceHub> ProximityToggled { get; set; } = new();
    public const string HarmonyId = "com.warden161.chatextensions";

    [PluginEntryPoint(Name, Version, "Allows SCPs to toggle proximity chat using LeftAlt.", "warden161")]
    public void Load()
    {
        if (!Config.IsEnabled)
            return;

        Log.Info($"{Name} v{Version} has been loaded!");
        HarmonyPatcher.PatchAll();
    }

    [PluginConfig] public static Config Config;
}
