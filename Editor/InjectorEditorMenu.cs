using UnityEngine;
using UnityEditor;
using DumbInjector.Injectors;

public static class InjectorEditorMenu
{
    [MenuItem("GameObject/DumbInjector/Create GlobalContextInjector", false, 10)]
    static void CreateGlobalContextInjector(MenuCommand menuCommand)
    {
        var go = new GameObject("GlobalContextInjector");
        go.AddComponent<GlobalContextInjector>();

        // Ensure it gets parented correctly in hierarchy
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(go, "Create GlobalContextInjector");
        Selection.activeObject = go;
    }

    [MenuItem("GameObject/DumbInjector/Create SceneContextInjector", false, 10)]
    static void CreateSceneContextInjector(MenuCommand menuCommand)
    {
        var go = new GameObject("SceneContextInjector");
        go.AddComponent<SceneContextInjector>();

        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(go, "Create SceneContextInjector");
        Selection.activeObject = go;
    }
}