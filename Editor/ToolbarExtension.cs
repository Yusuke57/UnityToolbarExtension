using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.UIElements;

namespace Yusuke57.UnityToolbarExtension.Editor
{
    [InitializeOnLoad]
    public static class ToolbarExtension
    {
        private const string ToolbarZoneLeftAlignName = "ToolbarZoneLeftAlign";
        private const string ToolbarZoneRightAlignName = "ToolbarZoneRightAlign";
        private const string ToolbarExtensionLeftContainerName = "ToolbarExtensionLeftContainer";
        private const string ToolbarExtensionRightContainerName = "ToolbarExtensionRightContainer";
        private const string ToolbarExtensionLeftAlignName = "LeftAlign";
        private const string ToolbarExtensionRightAlignName = "RightAlign";

        static ToolbarExtension()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            var toolbar = GetToolbar();
            if (toolbar == null)
            {
                return;
            }

            var toolbarZoneLeftAlign = toolbar.Q(ToolbarZoneLeftAlignName);
            var toolbarZoneRightAlign = toolbar.Q(ToolbarZoneRightAlignName);
            if (toolbarZoneLeftAlign == null || toolbarZoneRightAlign == null)
            {
                return;
            }

            // Retinaディスプレイから外部ディスプレイにウィンドウを移動した際などにリセットされてしまうため、
            // 描画済みかどうかを毎フレーム確認し、描画されていなかったら描画するようにしておく
            var leftContainer = toolbarZoneLeftAlign.Q(ToolbarExtensionLeftContainerName);
            var rightContainer = toolbarZoneRightAlign.Q(ToolbarExtensionRightContainerName);
            if (leftContainer != null && rightContainer != null)
            {
                // 描画済みなので終了
                return;
            }

            if (leftContainer == null)
            {
                leftContainer = CreateContainerElement();
                leftContainer.name = ToolbarExtensionLeftContainerName;
                toolbarZoneLeftAlign.Insert(toolbarZoneLeftAlign.childCount, leftContainer);
            }

            if (rightContainer == null)
            {
                rightContainer = CreateContainerElement();
                rightContainer.name = ToolbarExtensionRightContainerName;
                toolbarZoneRightAlign.Insert(toolbarZoneRightAlign.childCount, rightContainer);
            }

            DrawElements(leftContainer.Q(ToolbarExtensionLeftAlignName), leftContainer.Q(ToolbarExtensionRightAlignName),
                rightContainer.Q(ToolbarExtensionLeftAlignName), rightContainer.Q(ToolbarExtensionRightAlignName));
        }
        
        private static VisualElement GetToolbar()
        {
            var toolbarType = Type.GetType("UnityEditor.Toolbar,UnityEditor")!;

            var getField = toolbarType.GetField("get", BindingFlags.Static | BindingFlags.Public);
            var getValue = getField?.GetValue(null);

            var windowBackendProperty = toolbarType.GetProperty("windowBackend", BindingFlags.Instance | BindingFlags.NonPublic);
            var windowBackendValue = windowBackendProperty?.GetValue(getValue);

            var iWindowBackendType = Type.GetType("UnityEditor.IWindowBackend,UnityEditor")!;

            var visualTreeProperty = iWindowBackendType.GetProperty("visualTree", BindingFlags.Instance | BindingFlags.Public);
            var visualTreeValue = visualTreeProperty?.GetValue(windowBackendValue);

            return visualTreeValue as VisualElement;
        }

        private static VisualElement CreateContainerElement()
        {
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.FlexStart;
            root.style.flexGrow = 1;
            
            var leftAlign = new VisualElement();
            leftAlign.name = ToolbarExtensionLeftAlignName;
            leftAlign.style.flexDirection = FlexDirection.Row;
            leftAlign.style.alignItems = Align.Center;
            leftAlign.style.justifyContent = Justify.FlexStart;
            leftAlign.style.flexGrow = 1;
            root.Add(leftAlign);
            
            var flexSpacer = new VisualElement();
            flexSpacer.style.flexGrow = 1;
            root.Add(flexSpacer);
            
            var rightAlign = new VisualElement();
            rightAlign.name = ToolbarExtensionRightAlignName;
            rightAlign.style.flexDirection = FlexDirection.Row;
            rightAlign.style.alignItems = Align.Center;
            rightAlign.style.justifyContent = Justify.FlexEnd;
            rightAlign.style.flexGrow = 1;
            root.Add(rightAlign);
            
            return root;
        }

        private static void DrawElements(VisualElement leftSideLeftAlignRoot, VisualElement leftSideRightAlignRoot,
            VisualElement rightSideLeftAlignRoot, VisualElement rightSideRightAlignRoot)
        {
            var elementRegisters = GetTypesImplementingInterface<IToolbarElementRegister>();
            foreach (var registerType in elementRegisters)
            {
                if (Activator.CreateInstance(registerType) is not IToolbarElementRegister register)
                {
                    continue;
                }

                var element = register.CreateElement();
                if (element == null)
                {
                    continue;
                }

                var root = register.LayoutType switch
                {
                    ToolbarElementLayoutType.LeftSideLeftAlign => leftSideLeftAlignRoot,
                    ToolbarElementLayoutType.LeftSideRightAlign => leftSideRightAlignRoot,
                    ToolbarElementLayoutType.RightSideLeftAlign => rightSideLeftAlignRoot,
                    ToolbarElementLayoutType.RightSideRightAlign => rightSideRightAlignRoot,
                    _ => throw new ArgumentOutOfRangeException()
                };

                root.Add(element);
            }
        }
        
        /// <summary>
        /// 特定のインターフェースを実装したすべての型を取得
        /// </summary>
        private static IEnumerable<Type> GetTypesImplementingInterface<TInterface>()
        {
            var interfaceType = typeof(TInterface);
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch (ReflectionTypeLoadException e)
                    {
                        return e.Types.Where(t => t != null);
                    }
                })
                .Where(t => t != null && interfaceType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);
        }

/*
        private static Button CreateReloadSceneButton()
        {
            var button = new EditorToolbarButton(ReloadScene)
            {
                name = "ReloadScene",
                icon = (Texture2D) EditorGUIUtility.IconContent("d_preAudioAutoPlayOff").image
            };

            button.SetEnabled(EditorApplication.isPlaying);

            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode) button.SetEnabled(true);
                if (state == PlayModeStateChange.EnteredEditMode) button.SetEnabled(false);
            };

            return button;
        }

        private static void ReloadScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        
        private static VisualElement CreatePrefabListButton()
        {
            var button = new EditorToolbarButton(OpenPrefabListMenu)
            {
                name = "PrefabListMenu",
                icon = (Texture2D) EditorGUIUtility.IconContent("d_Prefab On Icon").image
            };
            return button;
        }
        
        // Prefabリストメニューを表示
        private static void OpenPrefabListMenu()
        {
            var menu = new GenericMenu();
            var prefabHistory = _prefabHistoryHandler.PrefabHistory;
            foreach (var prefabPath in prefabHistory)
            {
                var prefabAssetName = System.IO.Path.GetFileNameWithoutExtension(prefabPath);
                menu.AddItem(new GUIContent(prefabAssetName), false, () =>
                {
                    PrefabStageUtility.OpenPrefab(prefabPath);
                });
            }

            menu.ShowAsContext();
        }

        private static VisualElement CreateSceneListButton()
        {
            var button = new EditorToolbarButton(OpenSceneListMenu)
            {
                name = "SceneListMenu",
                icon = (Texture2D) EditorGUIUtility.IconContent("d_UnityLogo").image
            };
            return button;
        }
        
        // Sceneリストメニューを表示
        private static void OpenSceneListMenu()
        {
            var menu = new GenericMenu();
            var scenes = EditorBuildSettings.scenes;
            foreach (var scene in scenes)
            {
                var sceneAssetPath = scene.path;
                var sceneAssetName = System.IO.Path.GetFileNameWithoutExtension(sceneAssetPath);
                menu.AddItem(new GUIContent(sceneAssetName), false, () =>
                {
                    EditorSceneManager.SaveOpenScenes();
                    EditorSceneManager.OpenScene(sceneAssetPath);
                });
            }

            menu.ShowAsContext();
        }

        private static VisualElement CreateTimeScaleSlider()
        {
            var buttonContent = EditorGUIUtility.IconContent("d_UnityEditor.AnimationWindow");
            var container = new IMGUIContainer(() =>
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.BeginVertical();
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(buttonContent, EditorStyles.iconButton))
                        {
                            Time.timeScale = 1f;
                        }
                        GUILayout.FlexibleSpace();
                    }
                    EditorGUILayout.EndVertical();

                    Time.timeScale = EditorGUILayout.Slider(Time.timeScale, 0f, 10f);
                }
                EditorGUILayout.EndHorizontal();
            });
            container.style.width = 140;
            container.style.marginLeft = 40;

            return container;
        }
        */
    }
}