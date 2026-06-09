using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Editor
{
[InitializeOnLoad]
internal static class LevelSceneReferenceRepairer
{
    private const string LevelScenePath = "Assets/Scenes/LevelScene.unity";

    static LevelSceneReferenceRepairer()
    {
        EditorApplication.delayCall += RepairActiveLevelScene;
    }

    [MenuItem("Tools/DreamGames/Repair Level Scene References")]
    private static void RepairActiveLevelSceneMenu()
    {
        Scene scene = SceneManager.GetActiveScene();
        bool changed = RepairScene(scene);
        Debug.Log(changed
            ? "[LevelSceneReferenceRepairer] Scene references repaired."
            : "[LevelSceneReferenceRepairer] No missing scene references found.");
    }

    private static void RepairActiveLevelScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded || scene.path != LevelScenePath)
            return;

        if (RepairScene(scene))
            Debug.Log("[LevelSceneReferenceRepairer] Repaired missing LevelScene references.");
    }

    private static bool RepairScene(Scene scene)
    {
        if (!scene.isLoaded)
            return false;

        bool changed = false;

        GameManager gameManager = FindComponentInScene<GameManager>(scene);
        if (gameManager != null)
        {
            BoardSetupController boardSetup = gameManager.GetComponent<BoardSetupController>();
            if (boardSetup == null)
            {
                boardSetup = Undo.AddComponent<BoardSetupController>(gameManager.gameObject);
                changed = true;
            }

            changed |= SetObjectReference(gameManager, "_boardSetup", boardSetup);
            changed |= SetObjectReference(gameManager, "_levelButton", FindGameObjectByName(scene, "LevelButton"));
            changed |= ClearMissingObjectReference(gameManager, "_levelJson");

            changed |= SetObjectReference(boardSetup, "_gridBackgroundRenderer",
                FindComponentOnNamedObject<SpriteRenderer>(scene, "grid_background"));
            changed |= SetObjectReference(boardSetup, "_camera", FindComponentInScene<Camera>(scene));
            changed |= SetFloat(boardSetup, "_backgroundPaddingX", 0.1f);
            changed |= SetFloat(boardSetup, "_backgroundPaddingY", 0.1f);
            changed |= SetFloat(boardSetup, "_cameraPadding", 0.15f);
            changed |= SetFloat(boardSetup, "_verticalCameraPadding", 1.1f);
            changed |= SetFloat(boardSetup, "_targetBoardViewportWidth", 0.97f);
            changed |= SetFloat(boardSetup, "_cameraOffsetY", 2.0f);
        }

        UIManager uiManager = FindComponentInScene<UIManager>(scene);
        if (uiManager != null)
        {
            changed |= SetObjectReference(uiManager, "_gameHUD", FindGameObjectByName(scene, "top"));
            changed |= SetObjectReference(uiManager, "_winPanel", FindGameObjectByName(scene, "winPanel"));
            changed |= SetObjectReference(uiManager, "_losePanel", FindGameObjectByName(scene, "lossPanel"));
            changed |= SetObjectReference(uiManager, "_loseTryAgainButton",
                FindComponentOnNamedObject<Button>(scene, "tryAgainButton"));
            changed |= SetObjectReference(uiManager, "_loseCloseButton",
                FindComponentOnNamedObject<Button>(scene, "closeButton"));
        }

        if (changed)
            EditorSceneManager.MarkSceneDirty(scene);

        return changed;
    }

    private static bool SetObjectReference(Object owner, string propertyName, Object value)
    {
        if (owner == null || value == null)
            return false;

        SerializedObject serializedObject = new SerializedObject(owner);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || property.objectReferenceValue == value)
            return false;

        Undo.RecordObject(owner, "Repair Scene References");
        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(owner);
        return true;
    }

    private static bool ClearMissingObjectReference(Object owner, string propertyName)
    {
        if (owner == null)
            return false;

        SerializedObject serializedObject = new SerializedObject(owner);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || property.objectReferenceValue != null || property.objectReferenceInstanceIDValue == 0)
            return false;

        Undo.RecordObject(owner, "Clear Missing Scene Reference");
        property.objectReferenceValue = null;
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(owner);
        return true;
    }

    private static bool SetFloat(Object owner, string propertyName, float value)
    {
        if (owner == null)
            return false;

        SerializedObject serializedObject = new SerializedObject(owner);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || Mathf.Approximately(property.floatValue, value))
            return false;

        Undo.RecordObject(owner, "Repair Scene References");
        property.floatValue = value;
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(owner);
        return true;
    }

    private static T FindComponentInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }

        return null;
    }

    private static T FindComponentOnNamedObject<T>(Scene scene, string objectName) where T : Component
    {
        GameObject gameObject = FindGameObjectByName(scene, objectName);
        return gameObject != null ? gameObject.GetComponent<T>() : null;
    }

    private static GameObject FindGameObjectByName(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindTransformByName(root.transform, objectName);
            if (found != null)
                return found.gameObject;
        }

        return null;
    }

    private static Transform FindTransformByName(Transform root, string objectName)
    {
        if (root.name == objectName)
            return root;

        foreach (Transform child in root)
        {
            Transform found = FindTransformByName(child, objectName);
            if (found != null)
                return found;
        }

        return null;
    }
}
}
