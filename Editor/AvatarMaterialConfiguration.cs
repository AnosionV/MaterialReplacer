using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

namespace Anosion.MaterialReplacer
{
    public class AvatarMaterialConfiguration
    {
        public GameObject Avatar { get; }
        public Dictionary<Material, List<MaterialLocation>> Materials { get; }
        public SortedDictionary<string, SortedDictionary<Material, List<MaterialLocation>>> MaterialGroups { get; }

        public AvatarMaterialConfiguration(GameObject avatar, Dictionary<GameObject, List<Material>> objectMaterialData) : this(
            avatar,
            objectMaterialData
                .SelectMany(meshEntry => meshEntry.Value.Select((material, slotIndex) => (Mesh: meshEntry.Key, Material: material, SlotIndex: slotIndex)))
                .Where(entry => entry.Material != null && entry.Mesh != null)
                .GroupBy(entry => entry.Material)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(entry => new MaterialLocation(entry.Mesh, entry.SlotIndex)).ToList()
                ))
        { }

        private AvatarMaterialConfiguration(GameObject avatar, Dictionary<Material, List<MaterialLocation>> materials)
        {
            Avatar = avatar;
            Materials = materials;

            var materialPaths = Materials.Keys.ToDictionary(material => material, AssetDatabase.GetAssetPath);
            MaterialGroups = new SortedDictionary<string, SortedDictionary<Material, List<MaterialLocation>>>(Materials
                .GroupBy(materialEntry => Regex.Replace(Path.GetDirectoryName(materialPaths[materialEntry.Key]), "^Assets.", ""))
                .ToDictionary(
                    group => group.Key,
                    group => new SortedDictionary<Material, List<MaterialLocation>>(group.ToDictionary(pair => pair.Key, pair => pair.Value), new MaterialPathComparer(materialPaths))
                ));
        }

        public AvatarMaterialConfiguration TransformMaterials(Dictionary<Material, Material> replacementMap, Dictionary<MaterialLocation, bool> selectedMeshLocations)
        {
            bool isMeshSelected(MaterialLocation location) => selectedMeshLocations.TryGetValue(location, out var isSelected) && isSelected;

            return new AvatarMaterialConfiguration(Avatar, Materials
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
                ));
        }

        public bool HasDifferences(Dictionary<GameObject, List<Material>> objectMaterialData)
        {
            Dictionary<GameObject, List<Material>> thisObjectMaterialData = Materials
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

        public static Dictionary<GameObject, List<Material>> ExtractMaterialData(GameObject avatar) =>
            avatar.GetComponentsInChildren<Renderer>()
                .Where(renderer => renderer != null)
                .ToDictionary(renderer => renderer.gameObject, renderer => renderer.sharedMaterials.ToList());

        public static void ApplyMaterials(AvatarMaterialConfiguration config)
        {
            foreach (var (materialEntry, location) in config.Materials.SelectMany(materialEntry =>
                materialEntry.Value
                    .Where(location => materialEntry.Key != null && location != null)
                    .Select(location => (materialEntry, location))))
            {
                location.ApplyMaterial(materialEntry.Key);
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

        private class MaterialPathComparer : IComparer<Material>
        {
            private Dictionary<Material, string> PathOf { get; }

            public MaterialPathComparer(Dictionary<Material, string> pathOf)
            {
                PathOf = pathOf;
            }

            public int Compare(Material x, Material y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;

                return string.Compare(PathOf[x], PathOf[y], System.StringComparison.Ordinal);
            }
        }
    }
}
