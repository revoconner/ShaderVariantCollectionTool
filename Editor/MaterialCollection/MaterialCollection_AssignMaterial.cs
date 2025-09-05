using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShaderVariantsCollectionTool
{
    //This is a specific material to collect it
    //Generally used in Debug
    public class MaterialCollection_AssignMaterial : IMaterialCollector
    {
        public Material[] materials = new Material[0];

        public override void AddMaterialBuildDependency(IList<Material> buildDependencyList)
        {
            materials.ToList().ForEach(buildDependencyList.Add);
        }
    }
}