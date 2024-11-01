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
        public ImmutableDictionary<Material, ImmutableArray<MaterialLocation>> Materials =>
            materials.ToImmutableDictionary(entry => entry.Key, entry => ImmutableArray.CreateRange(entry.Value));

        public AvatarMaterialConfiguration(GameObject avatar, Dictionary<GameObject, List<Material>> objectMaterialData)
        {
            Avatar = avatar;
            materials = objectMaterialData
                .SelectMany(meshEntry => meshEntry.Value.Select((material, slotIndex) => (Mesh: meshEntry.Key, Material: material, SlotIndex: slotIndex)))
                .Where(entry => entry.Material != null && entry.Mesh != null)
                .GroupBy(entry => entry.Material)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(entry => new MaterialLocation(entry.Mesh, entry.SlotIndex)).ToList()
                );
        }

        private AvatarMaterialConfiguration(GameObject avatar, Dictionary<Material, List<MaterialLocation>> materials)
        {
            Avatar = avatar;
            this.materials = materials;
        }

        public AvatarMaterialConfiguration TransformMaterials(Dictionary<Material, Material> replacementMap, Dictionary<MaterialLocation, bool> selectedMeshLocations)
        {
            bool isMeshSelected(MaterialLocation location) => selectedMeshLocations.TryGetValue(location, out var isSelected) && isSelected;

            var newMaterials = materials
                .SelectMany(materialEntry => materialEntry.Value.Select(matLocation => (
                    Material: isMeshSelected(matLocation) && replacementMap.TryGetValue(materialEntry.Key, out var replacement) && replacement != null
                        ? replacement
                        : materialEntry.Key,
                    matLocation.Mesh,
                    matLocation.SlotIndex
                )))
                .GroupBy(entry => entry.Material)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(entry => new MaterialLocation(entry.Mesh, entry.SlotIndex)).ToList()
                );

            return new AvatarMaterialConfiguration(Avatar, newMaterials);
        }

        public bool HasDifferences(Dictionary<GameObject, List<Material>> objectMaterialData)
        {
            Dictionary<GameObject, List<Material>> thisObjectMaterialData = materials
                .SelectMany(entry => entry.Value.Select(location => (location.Mesh, Material: entry.Key, location.SlotIndex)))
                .GroupBy(config => config.Mesh)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderBy(config => config.SlotIndex)
                        .Select(config => config.Material)
                        .ToList()
                );

            return !thisObjectMaterialData.Keys.ToHashSet().SetEquals(objectMaterialData.Keys) ||
                   thisObjectMaterialData.Any(kvp => kvp.Value.Count != objectMaterialData[kvp.Key].Count || !kvp.Value.SequenceEqual(objectMaterialData[kvp.Key]));
        }

        public static Dictionary<GameObject, List<Material>> ExtractMaterialData(GameObject avatar)
        {
            return avatar.GetComponentsInChildren<Renderer>()
                .Where(renderer => renderer != null)
                .ToDictionary(renderer => renderer.gameObject, renderer => renderer.sharedMaterials.ToList());
        }

        public static void ApplyMaterials(AvatarMaterialConfiguration config)
        {
            foreach (var materialEntry in config.materials)
            {
                Material material = materialEntry.Key;

                foreach (var location in materialEntry.Value)
                {
                    if (material != null && location != null)
                    {
                        location.ApplyMaterial(material);
                    }
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
                if (material == null || ReferenceEquals(Mesh, null))
                {
                    return;
                }

                if (Mesh.TryGetComponent<Renderer>(out var renderer))
                {
                    Material[] materials = renderer.sharedMaterials;
                    if (SlotIndex >= 0 && SlotIndex < materials.Length)
                    {
                        materials[SlotIndex] = material;
                        renderer.sharedMaterials = materials;
                    }
                }
            }
        }
    }
}
