using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TutorialFontPatch
{
    /// <summary>
    /// Injects Noto Sans JP into the Tutorial windows on domain reload and window focus changes
    /// to fix missing Japanese characters on macOS.
    /// </summary>
    [InitializeOnLoad]
    static class TutorialFontModifier
    {
        const string FontPath = "Assets/Editor/Fonts/NotoSansJP-Regular.ttf";

        static readonly string[] TargetWindowTypeNames =
        {
            "Unity.Tutorials.Editor.TutorialWindow",
            "Unity.Tutorials.Editor.OverviewWindow",
        };

        static Font _font;

        static TutorialFontModifier()
        {
            EditorWindow.windowFocusChanged += OnWindowFocusChanged;

            // Double delayCall to wait for tutorial UI to be fully constructed after domain reload
            EditorApplication.delayCall += () =>
                EditorApplication.delayCall += ApplyToAllOpenWindows;
        }

        static void OnWindowFocusChanged()
            => EditorApplication.delayCall += () => CheckAndApplyFont(EditorWindow.focusedWindow);

        static void ApplyToAllOpenWindows()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
                CheckAndApplyFont(window);
        }

        static bool IsTargetWindow(EditorWindow window)
        {
            if (window == null) return false;
            var fullName = window.GetType().FullName;
            foreach (var name in TargetWindowTypeNames)
                if (fullName == name) return true;
            return false;
        }

        static void CheckAndApplyFont(EditorWindow window)
        {
            if (!IsTargetWindow(window)) return;
            ApplyFont(window);
        }

        static void ApplyFont(EditorWindow window)
        {
            if (_font == null)
                _font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);

            if (_font == null)
            {
                Debug.LogWarning($"[TutorialFontModifier] Font not found at: {FontPath}");
                return;
            }

            var root = window.rootVisualElement;
            if (root == null) return;

            // unityFontDefinition is an inherited UIToolkit property, so setting it on the root
            // cascades automatically to all descendant Label/TextElement nodes.
            // Explicit font rules in child elements (e.g. RobotoMono for code samples) still
            // take precedence over the inherited value via normal CSS cascade rules.
            root.style.unityFontDefinition = FontDefinition.FromFont(_font);
        }
    }
}
