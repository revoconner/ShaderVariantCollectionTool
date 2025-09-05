using System.Collections.Generic;
using UnityEngine;

namespace ShaderVariantsCollectionTool
{
    public abstract class IMaterialCollector : ScriptableObject
    {
        public abstract void AddMaterialBuildDependency(IList<Material> buildDependencyList);
    }
}