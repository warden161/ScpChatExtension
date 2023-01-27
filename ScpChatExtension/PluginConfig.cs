using System.Collections.Generic;
using Exiled.API.Interfaces;
using PlayerRoles;

namespace ScpChatExtension
{
    public class PluginConfig : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; }

        public float MaxProximityDistance { get; set; } = 7f;

        public HashSet<RoleTypeId> AllowedRoles { get; set; } = new HashSet<RoleTypeId>()
        {
            RoleTypeId.Scp049,
            RoleTypeId.Scp096,
            RoleTypeId.Scp106,
            RoleTypeId.Scp173,
            RoleTypeId.Scp0492,
            RoleTypeId.Scp939,
        };

        public string EnableHint { get; set; } = "Proximity Chat has been enabled!";
        public string DisableHint { get; set; } = "Proximity Chat has been disabled!";
    }
}