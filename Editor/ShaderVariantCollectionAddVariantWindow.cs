using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShaderVariantsCollectionTool
{
    public class ShaderVariantCollectionAddVariantWindow : EditorWindow
    {
        private static ShaderVariantCollectionAddVariantWindow m_window;
        public static ShaderVariantCollectionAddVariantWindow Window
        {
            get
            {
                if (m_window == null)
                {
                    m_window = EditorWindow.GetWindow<ShaderVariantCollectionAddVariantWindow>("AddVariantWindow");
                    m_window.minSize = new Vector2(480, 320);
                }
                return m_window;
            }
        }

        private Shader mShader;
        private PassType mPassType;
        private ShaderVariantCollectionMapper mMapper;

        private static MethodInfo sGetShaderGlobalKeywordsMethod = null;
        private static MethodInfo sGetShaderLocalKeywordsMethod = null;

        private string[] mShaderKeywords;
        private List<string> mSelectedShaderKeywords = new List<string>();
        private int mSelectedShaderKeywordIndex = 0;

        private enum State
        {
            None,
            Success,
            Failure
        }

        private State mState;
        private string mMessage;


        private static void InitGetKeywordMethod()
        {
            if (sGetShaderGlobalKeywordsMethod == null)
                sGetShaderGlobalKeywordsMethod = typeof(ShaderUtil).GetMethod("GetShaderGlobalKeywords",
                    BindingFlags.NonPublic | BindingFlags.Static);

            if (sGetShaderLocalKeywordsMethod == null)
                sGetShaderLocalKeywordsMethod = typeof(ShaderUtil).GetMethod("GetShaderLocalKeywords",
                    BindingFlags.NonPublic | BindingFlags.Static);
        }

        public void Setup(Shader shader, PassType passType, ShaderVariantCollectionMapper mapper)
        {
            mShader = shader;
            mPassType = passType;
            mMapper = mapper;

            InitGetKeywordMethod();

            string[] globalKeywords = sGetShaderGlobalKeywordsMethod.Invoke(null, new object[] { shader }) as string[];
            string[] localKeywords = sGetShaderLocalKeywordsMethod.Invoke(null, new object[] { shader }) as string[];
            mShaderKeywords = globalKeywords.Concat(localKeywords).ToArray();

            mSelectedShaderKeywords.Clear();
            mSelectedShaderKeywordIndex = 0;
            mState = State.None;
        }

        public void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField($"Current Shader: {mShader}");
            mPassType = (PassType)EditorGUILayout.EnumPopup("PassType", mPassType);

            #region Keywords to add
            EditorGUILayout.LabelField($"Available Keywords:");

            EditorGUILayout.BeginHorizontal();
            int i = 0;
            foreach (string keyword in mShaderKeywords)
            {
                if (i != 0 && i % 4 == 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }

                if (!mSelectedShaderKeywords.Contains(keyword))
                {
                    if (GUILayout.Button(keyword))
                    {
                        mSelectedShaderKeywords.Add(keyword);
                    }
                    i++;
                }
            }
            EditorGUILayout.EndHorizontal();
            #endregion

            #region Current keywords
            EditorGUILayout.LabelField($"Selected Keywords:");

            EditorGUILayout.BeginHorizontal();
            for (int keywordIndex = 0; keywordIndex < mSelectedShaderKeywords.Count; ++keywordIndex)
            {
                if (keywordIndex != 0 && keywordIndex % 4 == 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }

                if (GUILayout.Button(mSelectedShaderKeywords[keywordIndex]))
                {
                    mSelectedShaderKeywords.RemoveAt(keywordIndex);
                }
            }
            EditorGUILayout.EndHorizontal();
            #endregion

            if (GUILayout.Button("Add Variant"))
            {
                bool newVariantSuccess = false;
                ShaderVariantCollection.ShaderVariant newVariant;
                string errorMessage = "";

                try
                {
                    newVariant = new ShaderVariantCollection.ShaderVariant(mShader, mPassType,
                            mSelectedShaderKeywords.ToArray());
                    newVariantSuccess = true;
                }
                catch (Exception e)
                {
                    newVariantSuccess = false;
                    errorMessage = e.Message;
                }

                string keywordString = string.Join(", ", mSelectedShaderKeywords);
                if (newVariantSuccess)
                {
                    // Re-initialize to ensure the variable is properly initialized
                    newVariant = new ShaderVariantCollection.ShaderVariant(mShader, mPassType,
                        mSelectedShaderKeywords.ToArray());
                    if (mMapper.HasVariant(newVariant))
                    {
                        mState = State.Failure;
                        mMessage = $"Variant <{mPassType}>[{keywordString}] already exists";
                    }
                    else
                    {
                        ShaderVariantCollectionToolsWindow.Window.UndoShaderVariantCollectionTool();
                        if (!mMapper.AddVariant(newVariant))
                        {
                            mState = State.Failure;
                            mMessage = $"Failed to add variant <{mPassType}>[{keywordString}]";
                        }
                        else
                        {
                            mState = State.Success;
                            mMessage = $"Successfully added variant <{mPassType}>[{keywordString}]";
                            ShaderVariantCollectionToolsWindow.Window.RefreshPassKeywordMap(mShader);
                            ShaderVariantCollectionToolsWindow.Window.Repaint();
                        }
                    }
                }
                else
                {
                    mState = State.Failure;
                    mMessage = $"Failed to create variant <{mPassType}>[{keywordString}], error: {errorMessage}";
                }
            }

            #region Message display
            if (mState != State.None)
            {
                Color oriColor = GUI.color;
                GUI.color = mState == State.Success ? Color.green : Color.red;
                GUI.color *= 0.5f;

                EditorGUILayout.LabelField(mMessage, EditorStyles.whiteLabel);

                GUI.color = oriColor;
            }
            #endregion

            EditorGUILayout.EndVertical();


        }
    }
}