using UnityEditor;
using UnityEngine;
using ARRoomTransformer.Editor;

public static class AutoSetupRunner
{
    public static void RunAll()
    {
        Debug.Log("Starting Auto Setup...");
        ARSceneSetupWizard.CreateFullARScene();
        BackroomsAssetCreator.CreateOnlyPrefabs();
        BackroomsAssetCreator.CreateOnlyCatalog();
        Debug.Log("Auto Setup Completed!");
    }
}
