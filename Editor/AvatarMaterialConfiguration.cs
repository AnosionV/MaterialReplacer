using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using UnityEngine;

namespace Anosion.MaterialReplacer
{
    public class AvatarMaterialConfiguration
    {
        private readonly Dictionary<Material, List<MaterialLocation>> materials;

        public GameObject Avatar { get; }
        private ImmutableDictionary<Material, ImmutableArray<MaterialLocation>> Materials => materials.ToImmutableDictionary(entry => entry.Key, entry => ImmutableArray.ToImmutableArray(entry.Value));
        public AvatarMaterialConfiguration(GameObject avatar, Dictionary<GameObject, List<Material>> objectMaterialData)
        {
            Avatar = avatar;
            materials = new Dictionary<Material, List<MaterialLocation>>();

            foreach (var meshEntry in objectMaterialData)
            {
                GameObject mesh = meshEntry.Key;
                List<Material> materials = meshEntry.Value;

                foreach (var (material, slotIndex) in materials.Select((mat, index) => (mat, index)))
                {
                    AddMaterialLocation(material, mesh, slotIndex);
                }
            }
        }

        private void AddMaterialLocation(Material material, GameObject mesh, int slotIndex)
        {
            if (!materials.ContainsKey(material))
            {
                materials[material] = new List<MaterialLocation>();
            }

            materials[material].Add(new MaterialLocation(mesh, slotIndex));
        }

        public AvatarMaterialConfiguration Map(Dictionary<Material, Material> replacementMap)
        {
            AvatarMaterialConfiguration transformedConfig = new AvatarMaterialConfiguration(Avatar, new Dictionary<GameObject, List<Material>>());

            foreach (var materialEntry in materials)
            {
                Material originalMaterial = materialEntry.Key;
                Material newMaterial = replacementMap.ContainsKey(originalMaterial)
                    ? replacementMap[originalMaterial]
                    : originalMaterial;

                foreach (var location in materialEntry.Value)
                {
                    transformedConfig.AddMaterialLocation(newMaterial, location.Mesh, location.SlotIndex);
                }
            }

            return transformedConfig;
        }

        public static Dictionary<GameObject, List<Material>> ExtractMaterialData(GameObject avatar)
        {
            Dictionary<GameObject, List<Material>> materialData = new Dictionary<GameObject, List<Material>>();

            Renderer[] renderers = avatar.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                materialData[renderer.gameObject] = renderer.sharedMaterials.ToList();
            }

            return materialData;
        }

        public static void Applymaterials(AvatarMaterialConfiguration config)
        {
            foreach (var materialEntry in config.materials)
            {
                Material material = materialEntry.Key;

                foreach (var location in materialEntry.Value)
                {
                    location.ApplyMaterial(material);
                }
            }
        }

        public class MaterialLocation
        {
            public GameObject Mesh { get; }
            public int SlotIndex { get; }

            public MaterialLocation(GameObject mesh, int slotIndex)
            {
                Mesh = mesh;
                SlotIndex = slotIndex;
            }

            public void ApplyMaterial(Material material)
            {
                Renderer renderer = Mesh.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material[] materials = renderer.sharedMaterials;
                    materials[SlotIndex] = material;
                    renderer.sharedMaterials = materials;
                }
            }
        }
    }
}
