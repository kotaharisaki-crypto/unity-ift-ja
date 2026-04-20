using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TutorialFontPatch
{
    /// <summary>
    /// Adds a menu item to reset all tutorial completion states.
    /// Uses reflection to call the internal MarkAllTutorialsUncompleted() on TutorialWindow.
    /// </summary>
    static class TutorialProgressReset
    {
        const string TargetWindowTypeName = "Unity.Tutorials.Editor.TutorialWindow";

        [MenuItem("Tutorials/進捗をリセット", priority = 200)]
        static void ResetProgress()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "チュートリアル進捗のリセット",
                "すべてのチュートリアルの完了状態をリセットします。\nよろしいですか？",
                "リセットする",
                "キャンセル");

            if (!confirmed)
                return;

            // Find TutorialWindow type across all loaded assemblies
            Type windowType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(TargetWindowTypeName))
                .FirstOrDefault(t => t != null);

            if (windowType == null)
            {
                Debug.LogError("[TutorialProgressReset] TutorialWindow が見つかりませんでした。");
                return;
            }

            // Get or create the TutorialWindow instance via its public static Instance property
            var instanceProp = windowType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
            var window = instanceProp?.GetValue(null);

            if (window == null)
            {
                Debug.LogError("[TutorialProgressReset] TutorialWindow のインスタンスを取得できませんでした。");
                return;
            }

            // Call internal MarkAllTutorialsUncompleted() which:
            //   1. Sets CompletedByUser = false on every Tutorial asset with ProgressTrackingEnabled
            //   2. Broadcasts TutorialsCompletionStatusUpdatedEvent to refresh the overview UI
            var method = windowType.GetMethod(
                "MarkAllTutorialsUncompleted",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (method == null)
            {
                Debug.LogError("[TutorialProgressReset] MarkAllTutorialsUncompleted メソッドが見つかりませんでした。パッケージのバージョンが変わった可能性があります。");
                return;
            }

            method.Invoke(window, null);
            Debug.Log("[TutorialProgressReset] すべてのチュートリアルの進捗をリセットしました。");
        }
    }
}
