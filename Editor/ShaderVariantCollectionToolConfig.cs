using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ShaderVariantsCollectionTool
{
    [System.Serializable]
    public class ToggleObject
    {
        [SerializeReference]
        public ScriptableObject obj;
        public bool use;
    }

    //Solve nested list serialization problem
    [System.Serializable]
    public struct ListWrapper
    {
        public List<ToggleObject> list;
        public ListWrapper(List<ToggleObject> list) => this.list = list;
    }

    [System.Serializable]
    public class ManualKeywordCombination
    {
        public string keywords = "";
        public bool enabled = true;
    }

    [System.Serializable]
    public class KeywordFilter
    {
        public string keywords = "";
        public bool enabled = true;
        public bool includeAllCombinations = false;
    }

    [CreateAssetMenu(menuName = "ShaderVariantCollectionTools/Create Config")]
    public class ShaderVariantCollectionToolConfig : ScriptableObject, ISerializationCallbackReceiver
    {
        private Dictionary<System.Type, List<ToggleObject>> mToggleObjects = new Dictionary<Type, List<ToggleObject>>();

        [SerializeField] private List<string> mKeys = new List<string>();
        [SerializeField] private List<ListWrapper> mValues = new List<ListWrapper>();

        // Manual keyword combinations storage
        [SerializeField] private List<ManualKeywordCombination> mManualKeywordCombinations = new List<ManualKeywordCombination>();

        // Keyword filters storage
        [SerializeField] private List<KeywordFilter> mKeywordFilters = new List<KeywordFilter>();

        public void OnBeforeSerialize()
        {
            mKeys.Clear();
            mValues.Clear();

            foreach (var kvp in mToggleObjects)
            {
                mKeys.Add(kvp.Key.AssemblyQualifiedName);
                mValues.Add(new ListWrapper(kvp.Value));
            }
        }
        public void OnAfterDeserialize()
        {
            mToggleObjects.Clear();
            for (int i = 0; i < mKeys.Count; ++i)
            {
                System.Type type = System.Type.GetType(mKeys[i]);
                if (type != null)
                {
                    mToggleObjects.Add(type, mValues[i].list);
                }
            }
        }

        public List<ToggleObject> GetToggleObjectList(System.Type type)
        {
            if (!mToggleObjects.TryGetValue(type, out var list))
            {
                list = new List<ToggleObject>();
                mToggleObjects.Add(type, list);
            }

            return list;
        }

        public void AddToggleObject(ToggleObject obj)
        {
            GetToggleObjectList(obj.obj.GetType().BaseType).Add(obj);
        }

        public void RemoveToggleObject(ToggleObject obj)
        {
            GetToggleObjectList(obj.obj.GetType().BaseType).Remove(obj);
        }

        // Manual keyword combinations methods
        public List<ManualKeywordCombination> GetManualKeywordCombinations()
        {
            return mManualKeywordCombinations;
        }

        public void AddManualKeywordCombination(ManualKeywordCombination combination)
        {
            mManualKeywordCombinations.Add(combination);
            EditorUtility.SetDirty(this);
        }

        public void RemoveManualKeywordCombination(ManualKeywordCombination combination)
        {
            mManualKeywordCombinations.Remove(combination);
            EditorUtility.SetDirty(this);
        }

        public void ClearManualKeywordCombinations()
        {
            mManualKeywordCombinations.Clear();
            EditorUtility.SetDirty(this);
        }

        // Keyword filters methods
        public List<KeywordFilter> GetKeywordFilters()
        {
            return mKeywordFilters;
        }

        public void AddKeywordFilter(KeywordFilter filter)
        {
            mKeywordFilters.Add(filter);
            EditorUtility.SetDirty(this);
        }

        public void RemoveKeywordFilter(KeywordFilter filter)
        {
            mKeywordFilters.Remove(filter);
            EditorUtility.SetDirty(this);
        }

        public void ClearKeywordFilters()
        {
            mKeywordFilters.Clear();
            EditorUtility.SetDirty(this);
        }
    }

    public class ToolDefaultEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }

    public abstract class ShaderVariantCollectionToolEditor : Editor
    {
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ShaderVariantCollectionToolEditorAttribute : Attribute
    {
        public readonly Type componentType;

        public ShaderVariantCollectionToolEditorAttribute(Type componentType)
        {
            this.componentType = componentType;
        }
    }
}