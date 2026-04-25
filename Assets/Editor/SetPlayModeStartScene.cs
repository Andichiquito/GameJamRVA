using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class SetPlayModeStartScene
{
    static SetPlayModeStartScene()
    {
        Execute();
    }

    [MenuItem("Casino/Fix Play Mode Start Scene")]
    public static void Execute()
    {
        var mainMenu = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/MainMenu.unity");

        if (mainMenu == null)
        {
            UnityEngine.Debug.LogError("[PlayModeStartScene] No se encontró Assets/Scenes/MainMenu.unity");
            return;
        }

        EditorSceneManager.playModeStartScene = mainMenu;
        UnityEngine.Debug.Log("[PlayModeStartScene] Play Mode iniciará desde MainMenu.");
    }
}
