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
        public ImmutableDictionary<Material, ImmutableArray<MaterialLocation>> Materials => materials.ToImmutableDictionary(entry => entry.Key, entry => ImmutableArray.ToImmutableArray(entry.Value));
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

        public AvatarMaterialConfiguration Map(Dictionary<Material, Material> replacementMap, Dictionary<MaterialLocation, bool> selectedMeshLocations)
        {
            AvatarMaterialConfiguration transformedConfig = new AvatarMaterialConfiguration(Avatar, new Dictionary<GameObject, List<Material>>());

            foreach (var materialEntry in materials)
            {
                Material originalMaterial = materialEntry.Key;
                Material newMaterial = replacementMap.TryGetValue(originalMaterial, out var replacement) && replacement != null
                    ? replacement
                    : originalMaterial;

                foreach (var location in materialEntry.Value)
                {
                    Material materialToAdd = selectedMeshLocations.TryGetValue(location, out var isSelected) && !isSelected
                        ? originalMaterial
                        : newMaterial;

                    transformedConfig.AddMaterialLocation(materialToAdd, location.Mesh, location.SlotIndex);
                }
            }

            return transformedConfig;
        }

        public bool HasDifferences(Dictionary<GameObject, List<Material>> objectMaterialData)
        {
            Dictionary<GameObject, List<Material>> thisobjectMaterialData = materials
                .SelectMany(entry => entry.Value.Select(location => (location.Mesh, Material: entry.Key, location.SlotIndex)))
                .GroupBy(config => config.Mesh)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderBy(config => config.SlotIndex)
                        .Select(config => config.Material)
                        .ToList()
                );

            // 比較処理
            return !thisobjectMaterialData.Keys.ToHashSet().SetEquals(objectMaterialData.Keys) ||
                   thisobjectMaterialData.Any(kvp => kvp.Value.Count != objectMaterialData[kvp.Key].Count || !kvp.Value.SequenceEqual(objectMaterialData[kvp.Key]));
        }

        public static Dictionary<GameObject, List<Material>> ExtractMaterialData(GameObject avatar) => avatar.GetComponentsInChildren<Renderer>()
                .ToDictionary(renderer => renderer.gameObject, renderer => renderer.sharedMaterials.ToList());

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
