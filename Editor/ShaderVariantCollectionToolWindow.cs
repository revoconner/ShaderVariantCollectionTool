using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace ShaderVariantsCollectionTool
{

    public class ShaderVariantCollectionToolWindow : EditorWindow
    {
        private enum FeatureViewState
        {
            None,
            ShaderVariantIndex,
            CollectionTool
        }

        private static Vector2 cMinWindowSize = new Vector2(1200, 600);
        private static ShaderVariantCollectionToolWindow mwindow;
        public static ShaderVariantCollectionToolWindow Window
        {
            get
            {
                if (mwindow == null)
                {
                    mwindow = EditorWindow.GetWindow<ShaderVariantCollectionToolWindow>("ShaderVariantCollectionTool");
                    mwindow.minSize = cMinWindowSize;
                }
                return mwindow;
            }
        }

        private ShaderVariantCollection mCollectionFile;
        [SerializeField]
        private ShaderVariantCollectionMapper mCollectionMapper;

        private ShaderVariantCollectionMapper collectionMapper
        {
            get
            {
                if (mCollectionMapper == null || mCollectionMapper.mCollection != mCollectionFile)
                {
                    mCollectionMapper = new ShaderVariantCollectionMapper(mCollectionFile);
                    if (mShaderViewSelectedShader != null)
                        CollectPassKeywordMap(collectionMapper.GetShaderVariants(mShaderViewSelectedShader));
                }

                return mCollectionMapper;
            }
        }

        private ShaderVariantCollectionToolConfig mConfig;

        private FeatureViewState mCurrentFeatureState;
        private Vector2 mFeatureViewScrollViewPos = Vector2.zero;
        private Vector2 mWorkViewScrollViewPos = Vector2.zero;

        // Panel resizing variables
        private float mLeftPanelWidth = 400f;
        private bool mResizing = false;

        private void ResetFeatureView()
        {
            mCurrentFeatureState = FeatureViewState.None;
        }

        #region ShaderVariantIndex

        private Shader mWillInsertShader;
        [SerializeField]
        private Shader mShaderViewSelectedShader;
        private string mFilterShaderName = "";
        [SerializeField]
        private List<Shader> mFilterShaders = new List<Shader>();

        [Serializable]
        private class CachePassData
        {
            public PassType passType;
            public List<SerializableShaderVariant> variants;
            public bool toggleValue;
        }

        [SerializeField] private string mVariantFilterString = "";
        [SerializeField] private string[] mVariantFilterArray = null;
        private bool mVariantFilterAllMatch = false;

        [SerializeField]
        private List<CachePassData> mPassVariantCacheData = new List<CachePassData>();

        private void ResetShaderView()
        {
            mShaderViewSelectedShader = null;
            mFeatureViewScrollViewPos = Vector2.zero;
            mWorkViewScrollViewPos = Vector2.zero;
            mPassVariantCacheData.Clear();
        }

        private void CollectPassKeywordMap(IEnumerable<UnityEngine.ShaderVariantCollection.ShaderVariant> variants)
        {
            mPassVariantCacheData.Clear();

            foreach (var variant in variants)
            {
                int findRes = mPassVariantCacheData.FindIndex(data => data.passType == variant.passType);
                CachePassData pass;
                if (findRes < 0)
                {
                    pass = new CachePassData()
                    {
                        passType = variant.passType,
                        variants = new List<SerializableShaderVariant>(),
                        toggleValue = false
                    };
                    mPassVariantCacheData.Add(pass);
                }
                else
                {
                    pass = mPassVariantCacheData[findRes];
                }

                pass.variants.Add(new SerializableShaderVariant(variant));
            }
        }

        public void RefreshPassKeywordMap(Shader currentShader)
        {
            if (currentShader != mShaderViewSelectedShader)
                return;

            Dictionary<PassType, bool> toggleData = new Dictionary<PassType, bool>();
            foreach (CachePassData data in mPassVariantCacheData)
            {
                toggleData.Add(data.passType, data.toggleValue);
            }

            CollectPassKeywordMap(collectionMapper.GetShaderVariants(currentShader));

            foreach (CachePassData data in mPassVariantCacheData)
            {
                if (toggleData.TryGetValue(data.passType, out bool toggleValue))
                    data.toggleValue = toggleValue;
            }
        }
        #endregion

        #region CollectionTool

        private enum CollectionViewState
        {
            CollectorList,
            VariantFilter,
            MaterialFrom,
            ManualKeywords
        }

        private int mSelectedInterfaceImplIndex;
        private CollectionViewState mCollectionViewState = CollectionViewState.CollectorList;
        private bool mOverrideFile = false;

        private ShaderVariantCollectionMaterialVariantConverter mConverter =
            new ShaderVariantCollectionMaterialVariantConverter();

        private Material mTestMaterial = null;

        private Shader mTestShader = null;
        private string mTestKeyword = "";
        private List<Material> mKeywordFromMaterialList = new List<Material>();
        private Vector2 mTestKeywordScrollViewPos = Vector2.zero;

        // Manual keyword combinations
        [Serializable]
        private class ManualKeywordCombination
        {
            public string keywords = "";
            public bool enabled = true;
        }

        [SerializeField]
        private List<ManualKeywordCombination> mManualKeywordCombinations = new List<ManualKeywordCombination>();
        private Vector2 mManualKeywordsScrollPos = Vector2.zero;

        #endregion

        private GUIStyle mBlackStyle, mItemStyle, mButtonStyle, mHighlightedButtonStyle, mSpecialButtonStyle, mLeftAlignedButtonStyle;

        private static int cBorderWidth = 10;
        private static int cLeftTopHeight = 130;
        private static int cLeftMiddleHeight = 100;
        private static int cMiddleWidth = 5;
        private static int cButtonHeight = 28;

        [MenuItem("Tools/ShaderVariantCollectionTool/OpenWindow", priority = 200)]
        public static void OpenWindow()
        {
            Window.Show();
        }

        public void OnGUI()
        {
            // Ensure styles are set up first
            SetupStyle();

            EditorGUILayout.Space(cBorderWidth);
            GUILayout.BeginHorizontal(GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

            #region Left Section
            EditorGUILayout.BeginVertical(GUILayout.Width(mLeftPanelWidth));

            #region Top Left Section - File Selection
            EditorGUILayout.BeginVertical(mBlackStyle, GUILayout.MinHeight(cLeftTopHeight));

            EditorGUILayout.LabelField("Variant Collection File:");

            Color oriColor = GUI.color;
            if (mCollectionFile == null)
                GUI.color = Color.red;
            ShaderVariantCollection newCollectionFile = EditorGUILayout.ObjectField(mCollectionFile, typeof(ShaderVariantCollection), false) as ShaderVariantCollection;
            GUI.color = oriColor;

            if (newCollectionFile != mCollectionFile)
            {
                SaveObject(mCollectionFile);
                mCollectionFile = newCollectionFile;
                ResetShaderView();
            }

            // Add Create New Collection button
            if (GUILayout.Button("Create New Collection", mButtonStyle, GUILayout.Height(cButtonHeight)))
            {
                CreateNewCollection();
            }

            EditorGUILayout.LabelField("Tool Configuration File:");

            var newConfig = EditorGUILayout.ObjectField(mConfig, typeof(ShaderVariantCollectionToolConfig), false) as ShaderVariantCollectionToolConfig;
            if (newConfig != mConfig)
            {
                SaveObject(mConfig);
                mConfig = newConfig;
            }

            // Add Create New Config button
            if (GUILayout.Button("Create New Config", mButtonStyle, GUILayout.Height(cButtonHeight)))
            {
                CreateNewConfig();
            }

            EditorGUILayout.EndVertical();
            #endregion

            EditorGUILayout.Space(cBorderWidth);

            #region Middle Left Section - Feature Selection
            EditorGUILayout.BeginVertical(mBlackStyle, GUILayout.MinHeight(cLeftMiddleHeight));
            EditorGUILayout.LabelField("Functions");
            if (mCollectionFile != null)
            {
                if (GUILayout.Button(new GUIContent("Variant File Enhanced UI and Browser", "Quick browse variant collection file content"), mButtonStyle, GUILayout.ExpandWidth(true), GUILayout.Height(cButtonHeight)))
                {
                    mCurrentFeatureState = FeatureViewState.ShaderVariantIndex;
                    ResetShaderView();
                }

                if (GUILayout.Button(new GUIContent("Automated Variant Collection", "Automatically collect shader build variants"), mButtonStyle, GUILayout.ExpandWidth(true), GUILayout.Height(cButtonHeight)))
                {
                    mCurrentFeatureState = FeatureViewState.CollectionTool;
                    mFeatureViewScrollViewPos = Vector2.zero;
                    mWorkViewScrollViewPos = Vector2.zero;
                    mSelectedInterfaceImplIndex = 0;
                    mCollectionViewState = CollectionViewState.CollectorList;
                }
            }
            EditorGUILayout.EndVertical();
            #endregion

            EditorGUILayout.Space(cBorderWidth);

            #region Bottom Left Section - Secondary Options
            EditorGUILayout.BeginVertical(mBlackStyle, GUILayout.MinHeight(position.height - cLeftTopHeight - cLeftMiddleHeight - 4 * cBorderWidth));

            if (mCollectionFile != null)
            {
                if (mCurrentFeatureState == FeatureViewState.ShaderVariantIndex)
                {
                    EditorGUILayout.LabelField("Shader View");

                    EditorGUILayout.BeginHorizontal();
                    mWillInsertShader = EditorGUILayout.ObjectField(mWillInsertShader, typeof(Shader)) as Shader;
                    if (GUILayout.Button("Add", mButtonStyle, GUILayout.Height(cButtonHeight)))
                    {
                        if (mWillInsertShader != null && mCollectionFile != null)
                        {
                            try
                            {
                                if (!collectionMapper.HasShader(mWillInsertShader))
                                {
                                    UndoShaderVariantCollectionTool();
                                    collectionMapper.AddShader(mWillInsertShader);
                                    if (mFilterShaderName != "" &&
                                        mWillInsertShader.name.IndexOf(mFilterShaderName, StringComparison.OrdinalIgnoreCase) >= 0 &&
                                        !mFilterShaders.Contains(mWillInsertShader))
                                    {
                                        mFilterShaders.Add(mWillInsertShader);
                                    }
                                }
                                else
                                    ShowNotification(new GUIContent($"Shader:{mWillInsertShader} already exists in the current variant collection file"));
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogError($"Error adding shader: {e.Message}");
                                ShowNotification(new GUIContent($"Error adding shader: {e.Message}"));
                            }
                        }
                        else
                        {
                            ShowNotification(new GUIContent("Please select a shader and ensure collection file is loaded"));
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    #region Filter Name
                    string prevFilterShaderName = mFilterShaderName;
                    mFilterShaderName = EditorGUILayout.TextField("Filter", mFilterShaderName);
                    if (mFilterShaderName == "")
                    {
                        mFilterShaders.Clear();
                    }
                    else if (prevFilterShaderName != mFilterShaderName)
                    {
                        FilterShader();
                    }
                    #endregion

                    if (collectionMapper.shaders.Count > 0 && GUILayout.Button(new GUIContent("Clear", "Clear the variant collection file"), mButtonStyle, GUILayout.Width(mLeftPanelWidth), GUILayout.Height(cButtonHeight)))
                    {
                        if (EditorUtility.DisplayDialog("Confirm", "Are you sure you want to clear the file?", "Yes", "No"))
                        {
                            UndoShaderVariantCollectionTool();
                            mCollectionFile.Clear();
                            collectionMapper.Refresh();
                            mShaderViewSelectedShader = null;
                            mPassVariantCacheData.Clear();
                            mFilterShaders.Clear();
                        }
                    }

                    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                    mFeatureViewScrollViewPos = EditorGUILayout.BeginScrollView(mFeatureViewScrollViewPos);

                    IEnumerable<Shader> displayList =
                        (mFilterShaderName == "" ? (collectionMapper.shaders as IEnumerable<Shader>) : mFilterShaders);

                    Shader removeShader = null;

                    Color oriGUIColor = GUI.color;
                    foreach (var shader in displayList)
                    {
                        EditorGUILayout.BeginHorizontal(GUILayout.Width(mLeftPanelWidth));

                        if (shader == mShaderViewSelectedShader)
                            GUI.color = Color.green;

                        if (GUILayout.Button(new GUIContent(shader.name, shader.name),
                                GUILayout.Width(mLeftPanelWidth - 30)))
                        {
                            if (mShaderViewSelectedShader == shader)
                            {
                                Selection.activeObject = shader;
                                EditorGUIUtility.PingObject(shader);
                            }

                            mShaderViewSelectedShader = shader;
                            CollectPassKeywordMap(collectionMapper.GetShaderVariants(shader));
                        }
                        GUI.color = oriGUIColor;

                        if (GUILayout.Button("-", GUILayout.Width(20)))
                        {
                            removeShader = shader;
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    if (removeShader != null)
                    {
                        UndoShaderVariantCollectionTool();
                        collectionMapper.RemoveShader(removeShader);
                        mFilterShaders.Remove(removeShader);

                        if (removeShader == mShaderViewSelectedShader)
                        {
                            mShaderViewSelectedShader = null;
                            mPassVariantCacheData.Clear();
                        }
                    }

                    EditorGUILayout.EndScrollView();
                }

                if (mCurrentFeatureState == FeatureViewState.CollectionTool)
                {
                    EditorGUILayout.LabelField("Collection View");

                    // Main workflow buttons in new order - left aligned
                    if (GUILayout.Button(new GUIContent("[1] Set Materials for Variant Collection", "Choose what materials or paths are used to collect valid shader variants"), mLeftAlignedButtonStyle, GUILayout.ExpandWidth(true), GUILayout.Height(cButtonHeight)))
                    {
                        mCollectionViewState = CollectionViewState.CollectorList;
                        mSelectedInterfaceImplIndex = 0;
                    }

                    // Store original color
                    Color originalColor = GUI.color;
                    GUI.color = new Color(1f, 0.7f, 0.4f, 1f); // Pale orange
                    if (GUILayout.Button(new GUIContent("[Express] One-Click Collect and Write", "One-Click Collect Valid Variants and Write to the Collection"), mSpecialButtonStyle, GUILayout.ExpandWidth(true), GUILayout.Height(cButtonHeight)))
                    {
                        if (EditorUtility.DisplayDialog("Confirm", "Start one-click variant collection?", "Yes", "No") && ConfirmOverride())
                        {
                            var _ = CollectMaterial()
                                     && FilterMaterial()
                                     && ProcessManualKeywordCombinations()
                                     && mConverter.CollectVariant()
                                     && FilterVariant()
                                     && WriteCollectedVariantToFile();
                        }
                    }
                    // Restore original color
                    GUI.color = originalColor;

                    int materialCount = mConverter.GetMaterials().Count();
                    string materialCountStr = materialCount > 0 ? $"({materialCount})" : "";
                    if (GUILayout.Button(new GUIContent("[2] Collect all Materials" + materialCountStr, "Collect files using active collectors"), mLeftAlignedButtonStyle, GUILayout.ExpandWidth(true), GUILayout.Height(cButtonHeight)))
                    {
                        var _ = CollectMaterial() && FilterMaterial();
                    }

                    GUIStyle manualKeywordsButtonStyle = mCollectionViewState == CollectionViewState.ManualKeywords ? mHighlightedButtonStyle : mLeftAlignedButtonStyle;
                    if (GUILayout.Button(new GUIContent("[3] Add Manual Keyword Combinations (optional)", "Add runtime keyword combinations manually"), manualKeywordsButtonStyle, GUILayout.ExpandWidth(true), GUILayout.Height(cButtonHeight)))
                    {
                        mCollectionViewState = CollectionViewState.ManualKeywords;
                    }

                    int variantCount = mConverter.GetVariantCount();
                    string variantCountStr = variantCount > 0 ? $"({variantCount})" : "";
                    if (GUILayout.Button(new GUIContent("[4] Filter out Valid Shader Variants" + variantCountStr, "Convert collected material keywords to valid variants"), mLeftAlignedButtonStyle, GUILayout.ExpandWidth(true), GUILayout.Height(cButtonHeight)))
                    {
                        var _ = ProcessManualKeywordCombinations() && mConverter.CollectVariant() && FilterVariant();
                    }

                    if (GUILayout.Button(new GUIContent("[5] Write to Shader Variant Collection File", "Write collected variants to variant collection file"), mLeftAlignedButtonStyle, GUILayout.ExpandWidth(true), GUILayout.Height(cButtonHeight)))
                    {
                        if (EditorUtility.DisplayDialog("Confirm", "Confirm writing to variant collection file?", "Yes", "No") && ConfirmOverride())
                        {
                            WriteCollectedVariantToFile();
                        }
                    }

                    EditorGUILayout.BeginHorizontal();
                    mOverrideFile = EditorGUILayout.Toggle(mOverrideFile, GUILayout.Width(20));
                    EditorGUILayout.LabelField("Override Variant Collection File", GUILayout.ExpandWidth(true));
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(20);
                    EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);

                    // These buttons remain center-aligned as requested
                    GUIStyle variantFilterButtonStyle = mCollectionViewState == CollectionViewState.VariantFilter ? mHighlightedButtonStyle : mButtonStyle;
                    if (GUILayout.Button(new GUIContent("Variant Filter", "Filter collected variants by conditions"), variantFilterButtonStyle, GUILayout.ExpandWidth(true), GUILayout.Height(cButtonHeight)))
                    {
                        mCollectionViewState = CollectionViewState.VariantFilter;
                        mSelectedInterfaceImplIndex = 0;
                    }

                    GUIStyle materialFromButtonStyle = mCollectionViewState == CollectionViewState.MaterialFrom ? mHighlightedButtonStyle : mButtonStyle;
                    if (GUILayout.Button(new GUIContent("Material Source Check", "Find which collector collected this material"), materialFromButtonStyle, GUILayout.ExpandWidth(true), GUILayout.Height(cButtonHeight)))
                    {
                        mCollectionViewState = CollectionViewState.MaterialFrom;
                    }
                }
            }

            EditorGUILayout.EndVertical();
            #endregion

            EditorGUILayout.EndVertical();
            #endregion

            #region Resizable Separator
            Rect separatorRect = GUILayoutUtility.GetRect(cMiddleWidth, position.height - cBorderWidth * 2, GUILayout.Width(cMiddleWidth));
            EditorGUIUtility.AddCursorRect(separatorRect, MouseCursor.ResizeHorizontal);

            if (Event.current.type == EventType.MouseDown && separatorRect.Contains(Event.current.mousePosition))
            {
                mResizing = true;
            }

            if (mResizing)
            {
                mLeftPanelWidth = Event.current.mousePosition.x;
                mLeftPanelWidth = Mathf.Clamp(mLeftPanelWidth, 300f, position.width - 400f);
                Repaint();
            }

            if (Event.current.type == EventType.MouseUp)
            {
                mResizing = false;
            }

            // Draw separator line
            Color separatorColor = EditorGUIUtility.isProSkin ? new Color(0.4f, 0.4f, 0.4f) : new Color(0.6f, 0.6f, 0.6f);
            EditorGUI.DrawRect(new Rect(separatorRect.x + separatorRect.width / 2f - 1f, separatorRect.y, 2f, separatorRect.height), separatorColor);
            #endregion

            #region Right Section
            float rightWidth = position.width - mLeftPanelWidth - cMiddleWidth - 10;
            EditorGUILayout.BeginVertical(mBlackStyle, GUILayout.MinWidth(rightWidth), GUILayout.MinHeight(position.height - cBorderWidth * 2));

            if (mCollectionFile != null)
            {
                #region Variant Browse
                if (mCurrentFeatureState == FeatureViewState.ShaderVariantIndex && mShaderViewSelectedShader != null)
                {
                    if (GUILayout.Button("+", mButtonStyle, GUILayout.Height(cButtonHeight)))
                    {
                        OpenAddVariantWindow();
                    }

                    EditorGUILayout.BeginHorizontal();
                    string newVariantFilterString =
                        EditorGUILayout.TextField(new GUIContent("Keyword Filter", "Separate multiple keywords with spaces"), mVariantFilterString);
                    if (newVariantFilterString != mVariantFilterString)
                    {
                        mVariantFilterString = newVariantFilterString;
                        if (newVariantFilterString == "<no keywords>")
                            mVariantFilterArray = new string[0];
                        else if (string.IsNullOrWhiteSpace(newVariantFilterString))
                            mVariantFilterArray = null;
                        else
                            mVariantFilterArray = newVariantFilterString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    }

                    if (mVariantFilterString != "<no keywords>")
                        mVariantFilterAllMatch = EditorGUILayout.Toggle("Exact Match", mVariantFilterAllMatch);
                    EditorGUILayout.EndHorizontal();

                    if (mPassVariantCacheData.Count == 0)
                    {
                        EditorGUILayout.LabelField("No variants collected for current shader");
                    }

                    int keyowrdWidth = (int)rightWidth - 20;
                    int minusWidth = 20;

                    bool removeVariant = false;
                    ShaderVariantCollection.ShaderVariant removedVariant = default;

                    mWorkViewScrollViewPos = EditorGUILayout.BeginScrollView(mWorkViewScrollViewPos);
                    foreach (CachePassData cacheData in mPassVariantCacheData)
                    {
                        cacheData.toggleValue = EditorGUILayout.Foldout(cacheData.toggleValue, $"{cacheData.passType.ToString()}({cacheData.variants.Count})");

                        if (cacheData.toggleValue)
                        {
                            foreach (SerializableShaderVariant variant in cacheData.variants)
                            {
                                if (mVariantFilterArray != null)
                                {
                                    bool needSkipDisplay = false;
                                    foreach (var needVariant in mVariantFilterArray)
                                    {
                                        if (!variant.keywords.Contains(needVariant))
                                        {
                                            needSkipDisplay = true;
                                            break;
                                        }
                                    }

                                    needSkipDisplay |= (mVariantFilterAllMatch &&
                                                        variant.keywords.Length != mVariantFilterArray.Length);
                                    needSkipDisplay |= mVariantFilterArray.Length == 0 && variant.keywords.Length != 0;

                                    if (needSkipDisplay)
                                        continue;
                                }

                                EditorGUILayout.BeginHorizontal(GUILayout.Width(rightWidth));
                                if (variant.keywords.Length == 0)
                                    EditorGUILayout.LabelField("<no keywords>", GUILayout.Width(keyowrdWidth));
                                else
                                    EditorGUILayout.LabelField(string.Join(", ", variant.keywords), GUILayout.Width(keyowrdWidth));

                                if (GUILayout.Button("-", GUILayout.Width(minusWidth)))
                                {
                                    removeVariant = true;
                                    removedVariant = variant.Deserialize();
                                }
                                EditorGUILayout.EndHorizontal();
                            }

                            if (GUILayout.Button("+", mButtonStyle, GUILayout.Height(cButtonHeight)))
                            {
                                OpenAddVariantWindow(cacheData.passType);
                            }
                        }
                    }
                    EditorGUILayout.EndScrollView();

                    if (removeVariant)
                    {
                        UndoShaderVariantCollectionTool();
                        collectionMapper.RemoveVariant(removedVariant);
                        RefreshPassKeywordMap(mShaderViewSelectedShader);
                    }

                }
                #endregion

                #region Project Collection Tool

                if (mCurrentFeatureState == FeatureViewState.CollectionTool)
                {

                    #region Material Collector List
                    if (mCollectionViewState == CollectionViewState.CollectorList)
                    {
                        DrawInterfaceList<IMaterialCollector>("Material Collector", rightWidth);
                    }
                    #endregion
                    #region Variant Filter
                    else if (mCollectionViewState == CollectionViewState.VariantFilter)
                    {
                        DrawInterfaceList<IVariantFilter>("Variant Filter", rightWidth);
                    }
                    #endregion
                    #region Material Source
                    else if (mCollectionViewState == CollectionViewState.MaterialFrom)
                    {
                        mTestMaterial = EditorGUILayout.ObjectField("Material to Check", mTestMaterial, typeof(Material)) as Material;

                        if (mConverter.GetMaterials().Count() == 0)
                        {
                            EditorGUILayout.LabelField("No materials collected. Please collect materials or check collection code");
                        }
                        else if (mTestMaterial != null)
                        {
                            var materialFrom = mConverter.GetMaterialFrom(mTestMaterial);
                            if (materialFrom.Count() == 0)
                                EditorGUILayout.LabelField("Current material was not collected");
                            else
                            {
                                EditorGUILayout.LabelField("From Collectors:");
                                foreach (IMaterialCollector collector in materialFrom)
                                {
                                    if (GUILayout.Button(collector.name, mButtonStyle, GUILayout.Height(cButtonHeight)))
                                    {
                                        Selection.activeObject = collector;
                                        EditorGUIUtility.PingObject(collector);
                                    }
                                }
                            }
                        }
                    }
                    #endregion
                    #region Manual Keywords
                    else if (mCollectionViewState == CollectionViewState.ManualKeywords)
                    {
                        EditorGUILayout.LabelField("Manual Keyword Combinations", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField("Add runtime keyword combinations that aren't active in materials.");
                        EditorGUILayout.LabelField("Each line is a separate combination. Separate keywords with spaces.");
                        EditorGUILayout.Space();

                        if (GUILayout.Button("Add New Combination", mButtonStyle, GUILayout.Height(cButtonHeight)))
                        {
                            mManualKeywordCombinations.Add(new ManualKeywordCombination());
                        }

                        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                        mManualKeywordsScrollPos = EditorGUILayout.BeginScrollView(mManualKeywordsScrollPos);

                        ManualKeywordCombination toRemove = null;

                        for (int i = 0; i < mManualKeywordCombinations.Count; i++)
                        {
                            EditorGUILayout.BeginHorizontal();

                            mManualKeywordCombinations[i].enabled = EditorGUILayout.Toggle(mManualKeywordCombinations[i].enabled, GUILayout.Width(20));

                            EditorGUILayout.LabelField($"Combination {i + 1}:", GUILayout.Width(100));

                            mManualKeywordCombinations[i].keywords = EditorGUILayout.TextField(mManualKeywordCombinations[i].keywords, GUILayout.ExpandWidth(true));

                            if (GUILayout.Button("Remove", mButtonStyle, GUILayout.Width(60), GUILayout.Height(cButtonHeight)))
                            {
                                toRemove = mManualKeywordCombinations[i];
                            }

                            EditorGUILayout.EndHorizontal();
                        }

                        if (toRemove != null)
                        {
                            mManualKeywordCombinations.Remove(toRemove);
                        }

                        EditorGUILayout.EndScrollView();

                        if (mManualKeywordCombinations.Count > 0)
                        {
                            EditorGUILayout.Space();
                            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                            if (GUILayout.Button("Clear All Combinations", mButtonStyle, GUILayout.Height(cButtonHeight)))
                            {
                                if (EditorUtility.DisplayDialog("Confirm", "Clear all manual keyword combinations?", "Yes", "No"))
                                {
                                    mManualKeywordCombinations.Clear();
                                }
                            }
                        }
                    }
                    #endregion
                }
                #endregion
            }

            EditorGUILayout.EndVertical();
            #endregion

            GUILayout.EndHorizontal();
        }

        private Dictionary<Type, Type[]> mCachedImplements = new Dictionary<Type, Type[]>();
        private Type[] GetInterfaceImplements<T>() where T : ScriptableObject
        {
            if (!mCachedImplements.TryGetValue(typeof(T), out var impls))
            {
                impls = AppDomain.CurrentDomain.GetAssemblies().SelectMany(
                    assembly => assembly.GetTypes().Where(type =>
                        type.IsSubclassOf(typeof(T)))).ToArray();
                mCachedImplements.Add(typeof(T), impls);
            }

            return impls;
        }

        private void DrawInterfaceList<T>(string objectName, float uiWidth) where T : ScriptableObject
        {
            Type[] implements = GetInterfaceImplements<T>();
            if (implements.Length == 0)
            {
                EditorGUILayout.LabelField($"No implementation class of {typeof(T).Name} was found");
            }
            else
            {
                #region Add Object
                EditorGUILayout.BeginHorizontal();
                mSelectedInterfaceImplIndex = EditorGUILayout.Popup(mSelectedInterfaceImplIndex, implements.Select(i => i.Name).ToArray(), GUILayout.Width(uiWidth * 0.7f));
                if (GUILayout.Button($"Add {objectName}", mButtonStyle, GUILayout.Width(uiWidth * 0.3f), GUILayout.Height(cButtonHeight)))
                {
                    T newObject = CreateInstance(implements[mSelectedInterfaceImplIndex]) as T;
                    newObject.name = implements[mSelectedInterfaceImplIndex].Name;
                    Undo.RegisterCreatedObjectUndo(newObject, "Create SCV tool impl object");
                    Undo.RecordObject(mConfig, "Impl object list insert");
                    mConfig.AddToggleObject(new ToggleObject()
                    {
                        obj = newObject,
                        use = true
                    });
                    AssetDatabase.AddObjectToAsset(newObject, mConfig);
                    SaveObject(mConfig);
                }
                EditorGUILayout.EndHorizontal();
                #endregion

                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                var objectList = mConfig.GetToggleObjectList(typeof(T));

                #region Object List
                mWorkViewScrollViewPos = EditorGUILayout.BeginScrollView(mWorkViewScrollViewPos);
                for (int i = 0; i < objectList.Count; ++i)
                {
                    ToggleObject toggleObject = objectList[i];

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Delete", mButtonStyle, GUILayout.Height(cButtonHeight)))
                    {
                        Undo.RecordObject(mConfig, "Impl object remove");
                        mConfig.RemoveToggleObject(toggleObject);
                        AssetDatabase.RemoveObjectFromAsset(toggleObject.obj);
                        SaveObject(mConfig);
                    }

                    toggleObject.use = EditorGUILayout.ToggleLeft("Use", toggleObject.use);

                    EditorGUI.BeginChangeCheck();
                    string newName = EditorGUILayout.TextField($"{objectName} name", toggleObject.obj.name);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(toggleObject.obj, "Change object name");
                        toggleObject.obj.name = newName;
                    }

                    if (i != 0 && GUILayout.Button("Up", mButtonStyle, GUILayout.Width(40), GUILayout.Height(cButtonHeight)))
                    {
                        Undo.RecordObject(mConfig, "Swap object list");
                        (objectList[i - 1], objectList[i]) = (objectList[i], objectList[i - 1]);
                    }
                    if (i != objectList.Count - 1 && GUILayout.Button("Down", mButtonStyle, GUILayout.Width(50), GUILayout.Height(cButtonHeight)))
                    {
                        Undo.RecordObject(mConfig, "Swap object list");
                        (objectList[i], objectList[i + 1]) = (objectList[i + 1], objectList[i]);
                    }

                    EditorGUILayout.EndHorizontal();

                    Editor editor = GetEditor(toggleObject.obj as ScriptableObject);
                    editor.OnInspectorGUI();

                    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                }
                EditorGUILayout.EndScrollView();
                #endregion
            }
        }

        private bool CollectMaterial()
        {
            return mConverter.CollectMaterial(mConfig.GetToggleObjectList(typeof(IMaterialCollector))
                .Where(co => co.use)
                .Select(co => co.obj as IMaterialCollector));
        }

        private bool FilterMaterial()
        {
            var materialFilters = mConfig.GetToggleObjectList(typeof(IMaterialFilter))
                .Where(mf => mf.use)
                .Select(mf => mf.obj as IMaterialFilter);

            return mConverter.FilterMaterial(materialFilters);
        }

        private bool FilterVariant()
        {
            var variantFilters = mConfig.GetToggleObjectList(typeof(IVariantFilter))
                .Where(vf => vf.use)
                .Select(vf => vf.obj as IVariantFilter);

            return mConverter.FilterMaterial(variantFilters);
        }

        private bool ProcessManualKeywordCombinations()
        {
            var enabledCombinations = mManualKeywordCombinations
                .Where(c => c.enabled && !string.IsNullOrWhiteSpace(c.keywords))
                .Select(c => c.keywords.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                .Where(keywords => keywords.Length > 0)
                .ToList();

            if (enabledCombinations.Count > 0)
            {
                Debug.Log($"[Manual Keywords] Processing {enabledCombinations.Count} manual keyword combinations");
                mConverter.AddManualKeywordCombinations(enabledCombinations);
            }

            return true;
        }

        private void OpenAddVariantWindow(PassType passType = PassType.Normal)
        {
            var window = ShaderVariantCollectionAddVariantWindow.Window;

            window.Setup(mShaderViewSelectedShader, passType, collectionMapper);
            window.Show();
            ShowNotification(new GUIContent("Add variant window has been opened. If not found, check if window is covered by current window."));
        }

        private bool ConfirmOverride()
        {
            if (!mOverrideFile)
                return true;

            return EditorUtility.DisplayDialog("Override Confirmation", "Confirm overriding existing collection file content?", "Confirm", "Cancel");
        }

        private void FilterShader()
        {
            mFilterShaders.Clear();

            if (mFilterShaderName != "")
            {
                foreach (var shader in collectionMapper.shaders)
                {
                    if (shader.name.IndexOf(mFilterShaderName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        mFilterShaders.Add(shader);
                    }
                }
            }
        }

        internal void UndoShaderVariantCollectionTool()
        {
            collectionMapper.SetSerializeFlag(true);
            Undo.RecordObject(mCollectionFile, "Change SVC tool");
            Undo.RegisterCompleteObjectUndo(collectionMapper, "Change SVC tool");
            Undo.RegisterCompleteObjectUndo(this, "Change SVC tool");
            collectionMapper.SetSerializeFlag(false);
        }

        private bool WriteCollectedVariantToFile()
        {
            try
            {
                UndoShaderVariantCollectionTool();

                if (mOverrideFile)
                {
                    mCollectionFile.Clear();
                }

                mConverter.WriteToShaderVariantCollectionFile(mCollectionFile);
                collectionMapper.Refresh();

                FilterShader();

                return true;
            }
            catch
            {
                return false;
            }
        }

        private Dictionary<Type, Type> mCachedEditorTypes = new Dictionary<Type, Type>();
        private Type ImplementGetEditor(Type type)
        {
            if (!mCachedEditorTypes.TryGetValue(type, out var editor))
            {
                editor = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes()
                        .Where(t => t.IsSubclassOf(typeof(ShaderVariantCollectionToolEditor))))
                    .FirstOrDefault(t => !t.IsAbstract
                                         && t.IsDefined(typeof(ShaderVariantCollectionToolEditorAttribute), false)
                                         && ((ShaderVariantCollectionToolEditorAttribute)t.GetCustomAttributes(
                                             typeof(ShaderVariantCollectionToolEditorAttribute), false)[0]).componentType ==
                                         type);

                editor = editor ?? typeof(ToolDefaultEditor);
                mCachedEditorTypes.Add(type, editor);
            }

            return editor;
        }

        private Dictionary<ScriptableObject, Editor> mCachedEditor = new Dictionary<ScriptableObject, Editor>();
        private Editor GetEditor(ScriptableObject obj)
        {
            if (!mCachedEditor.TryGetValue(obj, out var editor))
            {
                editor = Editor.CreateEditor(obj, ImplementGetEditor(obj.GetType()));
                mCachedEditor.Add(obj, editor);
            }

            return editor;
        }

        private void SaveObject(Object obj)
        {
            if (obj != null)
            {
                EditorUtility.SetDirty(obj);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void SetupStyle()
        {
            if (mBlackStyle == null)
            {
                Color backColor = EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.18f) : new Color(0.7f, 0.7f, 0.7f);
                Texture2D _blackTexture = MakeTex(4, 4, backColor);
                _blackTexture.hideFlags = HideFlags.DontSave;
                mBlackStyle = new GUIStyle();
                mBlackStyle.normal.background = _blackTexture;
                mBlackStyle.padding = new RectOffset(5, 5, 5, 5);
            }

            if (mItemStyle == null)
            {
                Color itemColor = EditorGUIUtility.isProSkin ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.9f, 0.9f, 0.9f);
                Texture2D _itemColorTexture = MakeTex(4, 4, itemColor);
                _itemColorTexture.hideFlags = HideFlags.DontSave;
                mItemStyle = new GUIStyle();
                mItemStyle.normal.background = _itemColorTexture;
            }

            if (mButtonStyle == null)
            {
                mButtonStyle = new GUIStyle(GUI.skin.button);
                mButtonStyle.alignment = TextAnchor.MiddleCenter;
                mButtonStyle.padding = new RectOffset(10, 10, 5, 5);
            }

            if (mLeftAlignedButtonStyle == null)
            {
                mLeftAlignedButtonStyle = new GUIStyle(GUI.skin.button);
                mLeftAlignedButtonStyle.alignment = TextAnchor.MiddleLeft;
                mLeftAlignedButtonStyle.padding = new RectOffset(10, 10, 5, 5);
            }

            if (mHighlightedButtonStyle == null)
            {
                mHighlightedButtonStyle = new GUIStyle(GUI.skin.button);
                mHighlightedButtonStyle.alignment = TextAnchor.MiddleCenter;
                mHighlightedButtonStyle.padding = new RectOffset(10, 10, 5, 5);

                Color cyanColor = new Color(0f, 0.8f, 0.8f, 0.4f);
                Texture2D highlightTexture = MakeTex(4, 4, cyanColor);
                highlightTexture.hideFlags = HideFlags.DontSave;
                mHighlightedButtonStyle.normal.background = highlightTexture;
                mHighlightedButtonStyle.hover.background = highlightTexture;
                mHighlightedButtonStyle.active.background = highlightTexture;
                mHighlightedButtonStyle.focused.background = highlightTexture;
            }

            if (mSpecialButtonStyle == null)
            {
                mSpecialButtonStyle = new GUIStyle(GUI.skin.button);
                mSpecialButtonStyle.alignment = TextAnchor.MiddleCenter;
                mSpecialButtonStyle.padding = new RectOffset(10, 10, 5, 5);
                mSpecialButtonStyle.fontStyle = FontStyle.Bold;
            }
        }

        private void CreateNewConfig()
        {
            // Ensure the Settings folder exists
            string settingsPath = "Assets/Settings";
            if (!AssetDatabase.IsValidFolder(settingsPath))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }

            // Find the next available config name
            int configNumber = 1;
            string configName;
            string configPath;

            do
            {
                configName = $"SVCTconfig{configNumber}";
                configPath = $"{settingsPath}/{configName}.asset";
                configNumber++;
            } while (AssetDatabase.LoadAssetAtPath<ShaderVariantCollectionToolConfig>(configPath) != null);

            // Create and save the new config
            ShaderVariantCollectionToolConfig newConfig = ScriptableObject.CreateInstance<ShaderVariantCollectionToolConfig>();
            AssetDatabase.CreateAsset(newConfig, configPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Set it as the current config
            SaveObject(mConfig);
            mConfig = newConfig;

            Debug.Log($"Created new config file: {configPath}");
            ShowNotification(new GUIContent($"Created {configName} in Assets/Settings"));

            // Ping the newly created asset in the Project window
            EditorGUIUtility.PingObject(newConfig);
        }

        private void CreateNewCollection()
        {
            // Ensure the Settings folder exists
            string settingsPath = "Assets/Settings";
            if (!AssetDatabase.IsValidFolder(settingsPath))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }

            // Find the next available collection name
            int collectionNumber = 1;
            string collectionName;
            string collectionPath;

            do
            {
                collectionName = $"SVCTcollectionFile{collectionNumber}";
                collectionPath = $"{settingsPath}/{collectionName}.shadervariants";
                collectionNumber++;
            } while (AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(collectionPath) != null);

            // Create and save the new collection
            ShaderVariantCollection newCollection = new ShaderVariantCollection();
            AssetDatabase.CreateAsset(newCollection, collectionPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Set it as the current collection
            SaveObject(mCollectionFile);
            mCollectionFile = newCollection;
            ResetShaderView();

            Debug.Log($"Created new collection file: {collectionPath}");
            ShowNotification(new GUIContent($"Created {collectionName} in Assets/Settings"));

            // Ping the newly created asset in the Project window
            EditorGUIUtility.PingObject(newCollection);
        }

        private void CreateIfDefaultConfigIsNull()
        {
            if (mConfig != null) return;

            // First try to find any existing config in Assets/Settings
            string settingsPath = "Assets/Settings";
            if (AssetDatabase.IsValidFolder(settingsPath))
            {
                string[] configGuids = AssetDatabase.FindAssets("t:ShaderVariantCollectionToolConfig", new string[] { settingsPath });
                if (configGuids.Length > 0)
                {
                    mConfig = AssetDatabase.LoadAssetAtPath<ShaderVariantCollectionToolConfig>(
                        AssetDatabase.GUIDToAssetPath(configGuids[0]));
                    return;
                }
            }

            // If no config found in Settings, try the old location (package folder)
            MonoScript ms = MonoScript.FromScriptableObject(CreateInstance<ShaderVariantCollectionToolConfig>());
            string scriptFilePath = AssetDatabase.GetAssetPath(ms);
            string scriptDirectoryPath = System.IO.Path.GetDirectoryName(scriptFilePath);
            string[] findResultGUID = AssetDatabase.FindAssets("t:ShaderVariantCollectionToolConfig", new string[] { scriptDirectoryPath });

            if (findResultGUID.Length > 0)
            {
                mConfig = AssetDatabase.LoadAssetAtPath<ShaderVariantCollectionToolConfig>(
                    AssetDatabase.GUIDToAssetPath(findResultGUID[0]));
            }
            // Don't auto-create a config when installed as package
        }

        public void Awake()
        {
            CreateIfDefaultConfigIsNull();
        }

        public void OnDisable()
        {
            if (ShaderVariantCollectionAddVariantWindow.Window != null)
                ShaderVariantCollectionAddVariantWindow.Window.Close();
            SaveObject(mConfig);
        }
    }
}
