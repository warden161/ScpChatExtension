using Exiled.API.Features;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using MEC;
using PlayerRoles;
using Random = UnityEngine.Random;
using Server = Exiled.Events.Handlers.Server;

namespace ScpChatExtension;

public class EntryPoint : Plugin<PluginConfig>
{
    public override string Author { get; } = "warden161 (original by Jesus-QC)";
    public override string Name { get; } = "ScpChatExtensions";
    public override Version RequiredExiledVersion { get; } = new(7, 0, 0);
    public override Version Version { get; } = new(1, 1, 0);

    public static Harmony HarmonyPatcher { get; private set; }
    public static List<ReferenceHub> ProximityToggled { get; set; } = new List<ReferenceHub>();
    public const string HarmonyId = "com.warden161.chatextensions";
    public static PluginConfig PluginConfig;

    public override void OnEnabled()
    {
        PluginConfig = Config;
        HarmonyPatcher = new(HarmonyId);
        HarmonyPatcher.PatchAll();

        Server.RoundStarted += OnRoundStarted;

        base.OnEnabled();
    }

    public override void OnDisabled()
    {
        PluginConfig = null;
        HarmonyPatcher.UnpatchAll(HarmonyId);
        HarmonyPatcher = null;
        
        Server.RoundStarted -= OnRoundStarted;
        base.OnDisabled();
    }

    private void OnRoundStarted()
    {
        Timing.CallDelayed(Random.Range(5, 7) * 60, () =>
        {
            PluginConfig.AllowedRoles.Add(RoleTypeId.Scp939);
            var dog = Player.Get(RoleTypeId.Scp939).FirstOrDefault();
            if (dog == null) return;
            
            dog.ShowHint("\n\n\n<align=left>Du hast eine der Stimmen gemeistert. Du kannst nun <color=yellow>sprechen</color>!\nDrücke dazu die ALT-Taste!", 5);
        });
    }
}
