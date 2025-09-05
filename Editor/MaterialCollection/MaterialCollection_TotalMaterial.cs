using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ShaderVariantsCollectionTool
{
    //Here is a writing example
    //All materials in the catalog will be collected
    //This is not recommended for actual projects
    public sealed class MaterialCollection_TotalMaterial : IMaterialCollector
    {
        public enum PathMode
        {
            Asset,
            String
        }
        //private readonly string[] cIncludePath = { "Assets", "Packages" };
        [Tooltip("Path selection mode, as folder assets, or as string paths")]
        public PathMode pathMode = PathMode.String;
        public string[] mIncludePath = { "Assets", "Packages" };
        public DefaultAsset[] mFolders = new DefaultAsset[0];

        public override void AddMaterialBuildDependency(IList<Material> buildDependencyList)
        {
            string[] includePath = mFolders.Select(f => AssetDatabase.GetAssetPath(f)).ToArray();
            includePath = pathMode == PathMode.Asset ? includePath : mIncludePath;
            var materialsGUID = AssetDatabase.FindAssets("t:Material", includePath);

            foreach (Material m in materialsGUID.Select(
                         guid => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid))
                     ))
            {
                buildDependencyList.Add(m);
            }
        }
    }
}