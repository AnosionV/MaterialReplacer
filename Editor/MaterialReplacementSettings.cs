using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Anosion.MaterialReplacer
{
    public class MaterialReplacementSettings(AvatarMaterialConfiguration avatarMaterialConfig, bool enable = true)
    {
        public AvatarMaterialConfiguration AvatarMaterialConfig { get; } = avatarMaterialConfig;
        public Dictionary<Material, Material> ReplacementMap { get; } = avatarMaterialConfig.Materials.Keys
                .ToDictionary(material => material, _ => (Material)null);
        public Dictionary<AvatarMaterialConfiguration.MaterialLocation, bool> SelectedMeshLocations { get; } = avatarMaterialConfig.Materials
                .SelectMany(material => material.Value)
                .ToDictionary(location => location, _ => true);
        public bool Enable { get; set; } = enable;
    }
}