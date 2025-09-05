using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ShaderVariantsCollectionTool
{
    //Used to collect all packaging scenarios dependencies
    public class MaterialCollection_SceneDependency : IMaterialCollector
    {
        //Is it possible to collect only scenes that are enabled in EditorBuildSettings
        public bool collectOnlyEnable = true;
        public override void AddMaterialBuildDependency(IList<Material> buildDependencyList)
        {
            var sceneDependencyMaterials = EditorBuildSettings.scenes
                .Where(scene => !collectOnlyEnable || scene.enabled)
                .SelectMany(scene => AssetDatabase.GetDependencies(scene.path))
                .Where(dependencyAsset => dependencyAsset.EndsWith(".mat"))
                .Distinct()
                .Select(matPath => AssetDatabase.LoadAssetAtPath<Material>(matPath));

            foreach (Material m in sceneDependencyMaterials)
                buildDependencyList.Add(m);
        }
    }
}