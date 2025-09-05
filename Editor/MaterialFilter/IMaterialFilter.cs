using System.Collections.Generic;
using UnityEngine;

namespace ShaderVariantsCollectionTool
{
    public abstract class IMaterialFilter : ScriptableObject
    {
        //return true will save and false will strip
        public abstract bool Filter(Material material, List<IMaterialCollector> collections);
    }
}