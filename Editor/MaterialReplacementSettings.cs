using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Anosion.MaterialReplacer
{
    public class MaterialReplacementSettings
    {
        public AvatarMaterialConfiguration AvatarMaterialConfig { get; }
        public Dictionary<Material, Material> ReplacementMap { get; }
        public Dictionary<AvatarMaterialConfiguration.MaterialLocation, bool> SelectedMeshLocations { get; }

        public MaterialReplacementSettings(AvatarMaterialConfiguration avatarMaterialConfig)
        {
            AvatarMaterialConfig = avatarMaterialConfig;
            ReplacementMap = avatarMaterialConfig.Materials.Keys
                .ToDictionary(material => material, _ => (Material)null);
            SelectedMeshLocations = avatarMaterialConfig.Materials
                .SelectMany(material => material.Value)
                .ToDictionary(location => location, _ => true);
        }
    }
}