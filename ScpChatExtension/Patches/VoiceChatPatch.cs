﻿using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Mirror;
using NorthwoodLib.Pools;
using PlayerRoles;
using PlayerRoles.Spectating;
using UnityEngine;
using VoiceChat.Networking;

namespace ScpChatExtension.Patches;

[HarmonyPatch(typeof(VoiceTransceiver), nameof(VoiceTransceiver.ServerReceiveMessage))]
public class VoiceChatPatch
{
    private static MethodInfo GetSendMethod()
    {
        foreach (MethodInfo method in typeof(NetworkConnection).GetMethods())
        {
            if (method.Name is nameof(NetworkConnection.Send) && method.GetGenericArguments().Length != 0)
                return method.MakeGenericMethod(typeof(VoiceMessage));
        }

        return null;
    }
    
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Shared.Rent(instructions);

        Label skip = generator.DefineLabel();
        Label spectatorSkip = generator.DefineLabel();
        Label noSpectatorSkip = generator.DefineLabel();

        int index = newInstructions.FindLastIndex(x => x.opcode == OpCodes.Brfalse_S) - 1;
        
        newInstructions[index].labels.Add(skip);

        newInstructions.InsertRange(index, new List<CodeInstruction>()
        {
            // if (!EntryPoint.ProximityToggled.Contains(referenceHub)) skip;
            new (OpCodes.Call, AccessTools.PropertyGetter(typeof(EntryPoint), nameof(EntryPoint.ProximityToggled))),
            new (OpCodes.Ldarg_1),
            new (OpCodes.Ldfld, AccessTools.Field(typeof(VoiceMessage), nameof(VoiceMessage.Speaker))),
            new (OpCodes.Call, AccessTools.Method(typeof(List<ReferenceHub>), nameof(List<ReferenceHub>.Contains))),
            new (OpCodes.Brfalse_S, skip),

            // if (voiceChatChannel != VoiceChatChannel.ScpChat) skip;
            new (OpCodes.Ldloc_2),
            new (OpCodes.Ldc_I4_3),
            new (OpCodes.Ceq),
            new (OpCodes.Brfalse_S, skip),

            // if (referenceHub.roleManager.CurrentRole.Team == Team.SCPs) skip;
            new (OpCodes.Ldloc_S, 4),
            new (OpCodes.Ldfld, AccessTools.Field(typeof(ReferenceHub), nameof(ReferenceHub.roleManager))),
            new (OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlayerRoleManager), nameof(PlayerRoleManager.CurrentRole))),
            new (OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlayerRoleBase), nameof(PlayerRoleBase.Team))),
            new (OpCodes.Brfalse_S, skip), // == 0 (Team.SCPs value)

            // if (!EntryPoint.PluginConfig.AllowedRoles.Contains(msg.Speaker.roleManager.RoleTypeId)) skip;
            new (OpCodes.Ldsfld, AccessTools.Field(typeof(EntryPoint), nameof(EntryPoint.PluginConfig))),
            new (OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PluginConfig), nameof(PluginConfig.AllowedRoles))),
            new (OpCodes.Ldarg_1),
            new (OpCodes.Ldfld, AccessTools.Field(typeof(VoiceMessage), nameof(VoiceMessage.Speaker))),
            new (OpCodes.Ldfld, AccessTools.Field(typeof(ReferenceHub), nameof(ReferenceHub.roleManager))),
            new (OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlayerRoleManager), nameof(PlayerRoleManager.CurrentRole))),
            new (OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlayerRoleBase), nameof(PlayerRoleBase.RoleTypeId))),
            new (OpCodes.Callvirt, AccessTools.Method(typeof(HashSet<RoleTypeId>), nameof(HashSet<RoleTypeId>.Contains))),
            new (OpCodes.Brfalse_S, skip),

            // if (referenceHub.roleManager.CurrentRole.Team != Team.Dead) noSpectatorSkip;
            // if (!msg.Speaker.IsSpectatedBy(referenceHub)) skip;
            // else spectatorSkip;
            new (OpCodes.Ldloc_S, 4),
            new (OpCodes.Ldfld, AccessTools.Field(typeof(ReferenceHub), nameof(ReferenceHub.roleManager))),
            new (OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlayerRoleManager), nameof(PlayerRoleManager.CurrentRole))),
            new (OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlayerRoleBase), nameof(PlayerRoleBase.Team))),
            new (OpCodes.Ldc_I4_5),
            new (OpCodes.Ceq),
            new (OpCodes.Brfalse_S, noSpectatorSkip),
            new (OpCodes.Ldarg_1),
            new (OpCodes.Ldfld, AccessTools.Field(typeof(VoiceMessage), nameof(VoiceMessage.Speaker))),
            new (OpCodes.Ldloc_S, 4),
            new (OpCodes.Call, AccessTools.Method(typeof(SpectatorNetworking), nameof(SpectatorNetworking.IsSpectatedBy))),
            new (OpCodes.Brfalse_S, skip),
            new (OpCodes.Br_S, spectatorSkip),

            // if(Vector3.Distance(msg.Speaker.transform.position, referenceHub.transform.position) >= 7) skip;
            new CodeInstruction(OpCodes.Ldarg_1).WithLabels(noSpectatorSkip),
            new (OpCodes.Ldfld, AccessTools.Field(typeof(VoiceMessage), nameof(VoiceMessage.Speaker))),
            new (OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ReferenceHub), nameof(ReferenceHub.transform))),
            new (OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.position))),
            new (OpCodes.Ldloc_S, 4),
            new (OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ReferenceHub), nameof(ReferenceHub.transform))),
            new (OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.position))),
            new (OpCodes.Call, AccessTools.Method(typeof(Vector3), nameof(Vector3.Distance))),
            new (OpCodes.Ldc_R4, EntryPoint.PluginConfig.MaxProximityDistance),
            new (OpCodes.Bge_S, skip),

            // msg.Channel = VoiceChatChannel.Proximity;
            // referenceHub.connectionToClient.Send<VoiceMessage>(msg, 0);
            new CodeInstruction(OpCodes.Ldarga_S, 1).WithLabels(spectatorSkip),
            new (OpCodes.Ldc_I4_1),
            new (OpCodes.Stfld, AccessTools.Field(typeof(VoiceMessage), nameof(VoiceMessage.Channel))),
            new (OpCodes.Ldloc_S, 4),
            new (OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ReferenceHub), nameof(ReferenceHub.connectionToClient))),
            new (OpCodes.Ldarg_1),
            new (OpCodes.Ldc_I4_0),
            new (OpCodes.Callvirt, GetSendMethod())
        });
        
        foreach (CodeInstruction instruction in newInstructions)
            yield return instruction;
        
        ListPool<CodeInstruction>.Shared.Return(newInstructions);
    }
}