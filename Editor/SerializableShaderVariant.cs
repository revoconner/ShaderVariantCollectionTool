using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShaderVariantsCollectionTool
{
    [Serializable]
    public struct SerializableShaderVariant
    {
        public Shader shader;
        public PassType passType;
        public string[] keywords;

        public SerializableShaderVariant(ShaderVariantCollection.ShaderVariant variant)
        {
            shader = variant.shader;
            passType = variant.passType;
            keywords = variant.keywords;
        }

        public ShaderVariantCollection.ShaderVariant Deserialize()
        {
            //The reason for this initialization is that if the variant is invalid, there will be no error
            return new ShaderVariantCollection.ShaderVariant()
            {
                shader = shader,
                passType = passType,
                keywords = keywords
            };
        }

        public bool IsValid()
        {
            try
            {
                new ShaderVariantCollection.ShaderVariant(shader, passType, keywords);;
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}
