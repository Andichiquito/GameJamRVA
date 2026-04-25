using UnityEditor;

public class FixBuildSettings
{
    [MenuItem("Casino/Fix Build Settings")]
    public static void Execute()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity",  true),
            new EditorBuildSettingsScene("Assets/Scenes/GameScene.unity", true),
        };

        AssetDatabase.SaveAssets();
        UnityEngine.Debug.Log("[FixBuildSettings] índice 0=MainMenu, índice 1=GameScene");
    }
}
