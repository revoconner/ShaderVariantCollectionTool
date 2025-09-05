using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ShaderVariantsCollectionTool;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class ShaderVariantCollectionMaterialVariantConverter
{
    private Dictionary<Material, List<IMaterialCollector>> mMaterials = new Dictionary<Material, List<IMaterialCollector>>();

    private Dictionary<Shader, string[]> mCachedKeywords = new Dictionary<Shader, string[]>();

    private Dictionary<Shader, List<string>> mInsertedVariants = new Dictionary<Shader, List<string>>();

    private List<ShaderVariantCollection.ShaderVariant> mVariants = new List<ShaderVariantCollection.ShaderVariant>();

    private static MethodInfo _GetShaderGlobalKeywordsMethod = null;
    private static MethodInfo _GetShaderLocalKeywordsMethod = null;

    // Store manual keyword combinations to be processed
    private List<string[]> mManualKeywordCombinations = new List<string[]>();

    private static MethodInfo GetShaderGlobalKeywordsMethod
    {
        get
        {
            if (_GetShaderGlobalKeywordsMethod == null)
                _GetShaderGlobalKeywordsMethod = typeof(ShaderUtil).GetMethod("GetShaderGlobalKeywords",
                    BindingFlags.NonPublic | BindingFlags.Static);

            return _GetShaderGlobalKeywordsMethod;
        }
    }

    private static MethodInfo GetShaderLocalKeywordsMethod
    {
        get
        {
            if (_GetShaderLocalKeywordsMethod == null)
                _GetShaderLocalKeywordsMethod = typeof(ShaderUtil).GetMethod("GetShaderLocalKeywords",
                    BindingFlags.NonPublic | BindingFlags.Static);

            return _GetShaderLocalKeywordsMethod;
        }
    }

    private static string[] GetShaderKeywords(Shader shader)
    {
        string[] globalKeywords = GetShaderGlobalKeywordsMethod.Invoke(null, new object[] { shader }) as string[];
        string[] localKeywords = GetShaderLocalKeywordsMethod.Invoke(null, new object[] { shader }) as string[];

        return globalKeywords.Concat(localKeywords).ToArray();
    }

    public bool CollectMaterial(IEnumerable<IMaterialCollector> collectors)
    {
        mMaterials.Clear();
        List<Material> materials = new List<Material>();

        bool success = false;

        try
        {
            int collectorIndex = 0;
            int collectorListCount = collectors.Count();
            foreach (IMaterialCollector collector in collectors)
            {
                EditorUtility.DisplayProgressBar("Material collection", $"Processing {collectorIndex + 1}/{collectorListCount} - {collector.name}",
                    (float)collectorIndex / (float)collectorListCount);
                materials.Clear();
                collector.AddMaterialBuildDependency(materials);
                foreach (Material material in materials)
                {
                    if (!mMaterials.TryGetValue(material, out List<IMaterialCollector> fromCollect))
                    {
                        fromCollect = new List<IMaterialCollector>();
                        mMaterials.Add(material, fromCollect);
                    }

                    fromCollect.Add(collector);
                }

                collectorIndex++;
            }

            Debug.Log($"[Material Collection] Collected {mMaterials.Count} materials");

            success = true;
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        return success;
    }

    public bool FilterMaterial(IEnumerable<IMaterialFilter> materialFilters)
    {
        var deleteList = mMaterials
            .Where(kv => materialFilters.Any(mf => !mf.Filter(kv.Key, kv.Value)))
            .Select(kv => kv.Key);

        foreach (var deleteMat in deleteList)
            mMaterials.Remove(deleteMat);

        return true;
    }

    public bool FilterMaterial(IEnumerable<IVariantFilter> variantFilters)
    {
        mVariants.RemoveAll(variant => variantFilters.Any(vf => !vf.Filter(variant)));
        return true;
    }

    private bool IsKeywordBelongToShader(Shader shader, string keyword)
    {
        if (!mCachedKeywords.TryGetValue(shader, out string[] keywords))
        {
            keywords = GetShaderKeywords(shader);
            mCachedKeywords.Add(shader, keywords);
        }

        return keywords.Contains(keyword);
    }

    private bool AddVariant(ShaderVariantCollection.ShaderVariant variant)
    {
        int findIndex = mVariants.FindIndex(v =>
        {
            return v.shader == variant.shader
                   && v.passType == variant.passType
                   && v.keywords.SequenceEqual(variant.keywords);
        });

        if (findIndex < 0)
        {
            mVariants.Add(variant);
        }

        return findIndex < 0;
    }

    private bool IsValidVariant(Shader shader, PassType passType, string[] keywords)
    {
        try
        {
            ShaderVariantCollection.ShaderVariant testVariant
                = new ShaderVariantCollection.ShaderVariant(shader, passType, keywords);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private readonly string[] mSingleKeywords = new string[1];
    private bool IsKeywordInPass(Shader shader, PassType passType, string keyword)
    {
        mSingleKeywords[0] = keyword;
        return IsValidVariant(shader, passType, mSingleKeywords);
    }

    private readonly string[] mEmptyKeywords = new string[0];
    private bool IsPassExist(Shader shader, PassType passType)
    {
        return IsValidVariant(shader, passType, mEmptyKeywords);
    }


    private List<string> mTempInsertKeywords = new List<string>();
    private List<string> mTempSortedKeywords = new List<string>();

    private void AddVariantFromKeywords(Shader shader, List<string> validKeywords)
    {
        if (validKeywords.Count == 0)
        {
            for (PassType passType = (PassType)Enum.GetValues(typeof(PassType)).Cast<int>().Min();
                 passType <= (PassType)Enum.GetValues(typeof(PassType)).Cast<int>().Max();
                 ++passType)
            {
                if (!IsPassExist(shader, passType))
                    continue;

                AddVariant(new ShaderVariantCollection.ShaderVariant(shader, passType, mEmptyKeywords));
            }

            return;
        }


        foreach (string keyword in validKeywords)
        {
            for (PassType passType = (PassType)Enum.GetValues(typeof(PassType)).Cast<int>().Min();
                 passType <= (PassType)Enum.GetValues(typeof(PassType)).Cast<int>().Max();
                 ++passType)
            {
                if (!IsKeywordInPass(shader, passType, keyword))
                    continue;

                mTempInsertKeywords.Clear();
                mTempInsertKeywords.Add(keyword);

                foreach (string currentKeyword in validKeywords)
                {
                    if (currentKeyword == keyword)
                        continue;

                    mTempInsertKeywords.Add(currentKeyword);

                    mTempSortedKeywords.Clear();
                    mTempSortedKeywords.AddRange(mTempInsertKeywords);
                    mTempSortedKeywords.Sort();

                    if (!IsValidVariant(shader, passType, mTempSortedKeywords.ToArray()))
                        mTempInsertKeywords.RemoveAt(mTempInsertKeywords.Count - 1);
                }

                mTempInsertKeywords.Sort();
                AddVariant(new ShaderVariantCollection.ShaderVariant(shader, passType, mTempInsertKeywords.ToArray()));
            }
        }
    }

    // Add method to store manual keyword combinations
    public void AddManualKeywordCombinations(List<string[]> combinations)
    {
        mManualKeywordCombinations.Clear();
        mManualKeywordCombinations.AddRange(combinations);
    }

    // Process manual keyword combinations for each shader
    private void ProcessManualKeywordCombinations()
    {
        if (mManualKeywordCombinations.Count == 0)
            return;

        int processedCount = 0;
        int addedVariants = 0;

        // Get all unique shaders from materials
        HashSet<Shader> uniqueShaders = new HashSet<Shader>();
        foreach (var mat in mMaterials.Keys)
        {
            if (mat.shader != null)
                uniqueShaders.Add(mat.shader);
        }

        // For each shader, try to add each manual keyword combination
        foreach (Shader shader in uniqueShaders)
        {
            foreach (string[] keywordCombination in mManualKeywordCombinations)
            {
                processedCount++;

                // First check if all keywords in the combination are valid for this shader
                bool allKeywordsValid = true;
                foreach (string keyword in keywordCombination)
                {
                    if (!IsKeywordBelongToShader(shader, keyword))
                    {
                        allKeywordsValid = false;
                        break;
                    }
                }

                if (!allKeywordsValid)
                    continue;

                // Try to add this combination to all possible passes
                for (PassType passType = (PassType)Enum.GetValues(typeof(PassType)).Cast<int>().Min();
                     passType <= (PassType)Enum.GetValues(typeof(PassType)).Cast<int>().Max();
                     ++passType)
                {
                    // Sort the keywords for consistency
                    string[] sortedKeywords = keywordCombination.OrderBy(k => k).ToArray();

                    // Check if this variant is valid
                    if (IsValidVariant(shader, passType, sortedKeywords))
                    {
                        if (AddVariant(new ShaderVariantCollection.ShaderVariant(shader, passType, sortedKeywords)))
                        {
                            addedVariants++;
                        }
                    }
                }
            }
        }

        if (processedCount > 0)
        {
            Debug.Log($"[Manual Keywords] Processed {processedCount} combinations, added {addedVariants} valid variants");
        }
    }

    public bool CollectVariant()
    {
        mCachedKeywords.Clear();
        mInsertedVariants.Clear();
        mVariants.Clear();

        List<string> validKeywords = new List<string>();

        int materialIndex = 0;

        bool success = false;

        try
        {
            // First process material-based variants
            foreach (Material material in mMaterials.Keys)
            {
                EditorUtility.DisplayProgressBar("Variant Collection - Material to Variant Conversion",
                    $"Processing material {materialIndex}/{mMaterials.Keys.Count} - {material.name}",
                    (float)materialIndex / (float)mMaterials.Keys.Count);

                Shader shader = material.shader;

                validKeywords.Clear();
                validKeywords.AddRange(material.shaderKeywords.Distinct());
                validKeywords.RemoveAll(keyword => !IsKeywordBelongToShader(shader, keyword));
                validKeywords.Sort();

                string keywordsKey = string.Join(" ", validKeywords);
                if (mInsertedVariants.TryGetValue(shader, out List<string> collectedVariant))
                {
                    if (collectedVariant.Contains(keywordsKey))
                        continue;
                }
                else
                {
                    collectedVariant = new List<string>();
                    mInsertedVariants.Add(shader, collectedVariant);
                }

                AddVariantFromKeywords(shader, validKeywords);
                collectedVariant.Add(keywordsKey);
                materialIndex++;
            }

            // Then process manual keyword combinations
            ProcessManualKeywordCombinations();

            success = true;
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"[Material Variant Conversion] Error processing material {materialIndex} {mMaterials.ElementAt(materialIndex).Key.name}\nError message: {e.Message}\nStackTrace: {e.StackTrace}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log($"[Material Variant Conversion] Added {mVariants.Count} total variants");
        return success;
    }

    public IEnumerable<Material> GetMaterials()
    {
        return mMaterials.Keys;
    }

    public int GetVariantCount()
    {
        return mVariants.Count;
    }

    public IEnumerable<IMaterialCollector> GetMaterialFrom(Material material)
    {
        if (mMaterials.TryGetValue(material, out var fromList))
        {
            return fromList;
        }

        return new IMaterialCollector[0];
    }

    public void WriteToShaderVariantCollectionFile(ShaderVariantCollection collection)
    {
        foreach (var variant in mVariants)
        {
            collection.Add(variant);
        }
    }

    public void GetKeywordFromMaterial(string keyword, IList<Material> matList, Shader shader = null)
    {
        foreach (Material mat in GetMaterials())
        {
            if (shader != null && mat.shader != shader)
                continue;

            if (mat.shaderKeywords.Contains(keyword) && !matList.Contains(mat))
                matList.Add(mat);
        }
    }
}