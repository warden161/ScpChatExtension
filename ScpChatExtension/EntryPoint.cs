using Exiled.API.Features;
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace ScpChatExtension;

public class EntryPoint : Plugin<PluginConfig>
{
    public override string Author { get; } = "warden161 (original by Jesus-QC)";
    public override string Name { get; } = "ScpChatExtensions";
    public override Version RequiredExiledVersion { get; } = new(6, 0, 0);
    public override Version Version { get; } = new(0, 1, 0);

    public static Harmony HarmonyPatcher { get; private set; }
    public static List<ReferenceHub> ProximityToggled { get; set; } = new List<ReferenceHub>();
    public const string HarmonyId = "com.warden161.chatextensions";
    public static PluginConfig PluginConfig;

    public override void OnEnabled()
    {
        PluginConfig = Config;
        HarmonyPatcher = new(HarmonyId);
        HarmonyPatcher.PatchAll();
        base.OnEnabled();
    }

    public override void OnDisabled()
    {
        PluginConfig = null;
        HarmonyPatcher.UnpatchAll(HarmonyId);
        HarmonyPatcher = null;
        base.OnDisabled();
    }
}
