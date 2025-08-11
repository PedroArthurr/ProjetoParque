#if UNITY_EDITOR

using UnityEditor;

public static class ApiUpdaterShortcut
{
    [MenuItem("Tools/API/Run API Updater")]
    private static void Run()
    { EditorApplication.ExecuteMenuItem("Assets/Run API Updater"); }
}

#endif