using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TutorialFontPatch
{
    /// <summary>
    /// Adds a menu item to reset all tutorial completion states,
    /// both locally (SessionState) and on the Genesis server (Unity account).
    /// </summary>
    static class TutorialProgressReset
    {
        const string TargetWindowTypeName  = "Unity.Tutorials.Editor.TutorialWindow";
        const string TutorialTypeName      = "Unity.Tutorials.Editor.Tutorial";
        const string GenesisHelperTypeName = "Unity.Tutorials.Editor.GenesisHelper";

        [MenuItem("Tutorials/進捗をリセット", priority = 200)]
        static void ResetProgress()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "チュートリアル進捗のリセット",
                "すべてのチュートリアルの完了状態をリセットします。\n" +
                "Unity アカウントのサーバー上の進捗も同時にリセットされます。\n\n" +
                "よろしいですか？",
                "リセットする",
                "キャンセル");

            if (!confirmed)
                return;

            ResetLocalProgress();
            ResetServerProgress();
        }

        // ---------------------------------------------------------------
        // Local reset: clears SessionState via MarkAllTutorialsUncompleted
        // and refreshes the overview UI immediately.
        // ---------------------------------------------------------------
        static void ResetLocalProgress()
        {
            Type windowType = FindType(TargetWindowTypeName);
            if (windowType == null)
            {
                Debug.LogError("[TutorialProgressReset] TutorialWindow が見つかりませんでした。");
                return;
            }

            var instanceProp = windowType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
            var window = instanceProp?.GetValue(null);
            if (window == null) return;

            var method = windowType.GetMethod(
                "MarkAllTutorialsUncompleted",
                BindingFlags.Instance | BindingFlags.NonPublic);

            method?.Invoke(window, null);
        }

        // ---------------------------------------------------------------
        // Server reset: POST status="Started" for each tracked tutorial.
        // The framework treats only status=="Finished" as completed
        // (TableOfContentModel.UpdateLocalCompletionStatusOfAllTutorials),
        // so resetting to "Started" effectively marks them as incomplete.
        // Requires the user to be signed in to their Unity account.
        // ---------------------------------------------------------------
        static void ResetServerProgress()
        {
            Type tutorialType  = FindType(TutorialTypeName);
            Type genesisType   = FindType(GenesisHelperTypeName);

            if (tutorialType == null || genesisType == null)
            {
                Debug.LogError("[TutorialProgressReset] Tutorial または GenesisHelper 型が見つかりませんでした。");
                return;
            }

            var progressEnabledProp  = tutorialType.GetProperty("ProgressTrackingEnabled", BindingFlags.Instance | BindingFlags.Public);
            var lessonIdProp         = tutorialType.GetProperty("LessonId",                 BindingFlags.Instance | BindingFlags.Public);
            // LogTutorialStatusUpdate(string lessonId, string lessonStatus) is public static
            var updateStatusMethod   = genesisType.GetMethod("LogTutorialStatusUpdate",     BindingFlags.Static  | BindingFlags.Public);

            if (progressEnabledProp == null || lessonIdProp == null || updateStatusMethod == null)
            {
                Debug.LogError("[TutorialProgressReset] 必要なプロパティ／メソッドが見つかりませんでした。パッケージバージョンが変わった可能性があります。");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Tutorial");
            int resetCount = 0;

            foreach (string guid in guids)
            {
                string path     = AssetDatabase.GUIDToAssetPath(guid);
                var    tutorial = AssetDatabase.LoadAssetAtPath(path, tutorialType);
                if (tutorial == null) continue;

                bool progressEnabled = (bool)progressEnabledProp.GetValue(tutorial);
                if (!progressEnabled) continue;

                string lessonId = (string)lessonIdProp.GetValue(tutorial);
                if (string.IsNullOrWhiteSpace(lessonId)) continue;

                // "Started" は "Finished" ではないので、次回起動時に未完了として読み込まれる
                updateStatusMethod.Invoke(null, new object[] { lessonId, "Started" });
                resetCount++;
            }

            Debug.Log($"[TutorialProgressReset] {resetCount} 件のチュートリアルのサーバー進捗をリセットしました。" +
                      "（Unity アカウントにログインしていない場合は反映されません）");
        }

        static Type FindType(string typeName)
            => AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(typeName))
                .FirstOrDefault(t => t != null);
    }
}
