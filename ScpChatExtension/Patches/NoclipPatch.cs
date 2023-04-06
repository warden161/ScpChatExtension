using System.Collections.Generic;
using System.Reflection.Emit;
using Exiled.API.Features;
using HarmonyLib;
using NorthwoodLib.Pools;
using PlayerRoles;
using PlayerRoles.FirstPersonControl.NetworkMessages;

namespace ScpChatExtension.Patches;

[HarmonyPatch(typeof(FpcNoclipToggleMessage), nameof(FpcNoclipToggleMessage.ProcessMessage))]
public class NoclipPatch
{
    public static void SendActivateMessage(ReferenceHub hub)
    {
        var ply = Player.Get(hub);
        ply.ShowHint(EntryPoint.PluginConfig.EnableHint);
    }

    public static void SendDeactivateMessage(ReferenceHub hub)
    {
        var ply = Player.Get(hub);
        ply.ShowHint(EntryPoint.PluginConfig.DisableHint);
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Shared.Rent(instructions);

        Label add = generator.DefineLabel();
        Label skip = generator.DefineLabel();
        int index = newInstructions.FindIndex(x => x.opcode == OpCodes.Brfalse_S) - 1;

        newInstructions[index].WithLabels(skip);
        
        newInstructions.InsertRange(index, new List<CodeInstruction>()
        {
            // if (!Plugin.Config.AllowedRoles.Contains(ReferenceHub.roleManager.CurrentRole.RoleTypeId)) return;
            new (OpCodes.Ldsfld, AccessTools.Field(typeof(EntryPoint), nameof(EntryPoint.PluginConfig))),
            new (OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PluginConfig), nameof(PluginConfig.AllowedRoles))),
            new (OpCodes.Ldloc_0),
            new (OpCodes.Ldfld, AccessTools.Field(typeof(ReferenceHub), nameof(ReferenceHub.roleManager))),
            new (OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlayerRoleManager), nameof(PlayerRoleManager.CurrentRole))),
            new (OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlayerRoleBase), nameof(PlayerRoleBase.RoleTypeId))),
            new (OpCodes.Callvirt, AccessTools.Method(typeof(HashSet<RoleTypeId>), nameof(HashSet<RoleTypeId>.Contains))),
            new (OpCodes.Brfalse_S, skip),

            // if (EntryPoint.AllowedNoclip.Contains(ReferenceHub)
            new (OpCodes.Call, AccessTools.PropertyGetter(typeof(EntryPoint), nameof(EntryPoint.ProximityToggled))),
            new (OpCodes.Ldloc_0),
            new (OpCodes.Call, AccessTools.Method(typeof(List<ReferenceHub>), nameof(List<ReferenceHub>.Contains))),
            new (OpCodes.Brfalse_S, add),

            // EntryPoint.AllowedNoclip.Remove(ReferenceHub);
            new (OpCodes.Call, AccessTools.PropertyGetter(typeof(EntryPoint), nameof(EntryPoint.ProximityToggled))),
            new (OpCodes.Ldloc_0),
            new (OpCodes.Call, AccessTools.Method(typeof(NoclipPatch), nameof(NoclipPatch.SendDeactivateMessage))),
            new (OpCodes.Ldloc_0),
            new (OpCodes.Callvirt, AccessTools.Method(typeof(List<ReferenceHub>), nameof(List<ReferenceHub>.Remove))),
            new (OpCodes.Pop),
            new (OpCodes.Br, skip),

            // else EntryPoint.AllowedNoclip.Add(ReferenceHub);
            new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(EntryPoint), nameof(EntryPoint.ProximityToggled))).WithLabels(add),
            new (OpCodes.Ldloc_0),
            new (OpCodes.Call, AccessTools.Method(typeof(NoclipPatch), nameof(NoclipPatch.SendActivateMessage))),
            new (OpCodes.Ldloc_0),
            new (OpCodes.Callvirt, AccessTools.Method(typeof(List<ReferenceHub>), nameof(List<ReferenceHub>.Add)))
        });
        
        foreach (CodeInstruction instruction in newInstructions)
            yield return instruction;

        ListPool<CodeInstruction>.Shared.Return(newInstructions);
    }
}