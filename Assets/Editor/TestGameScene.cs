using UnityEditor;
using UnityEditor.SceneManagement;

public class TestGameScene
{
    public static void Execute()
    {
        // Limpia el start scene para poder probar GameScene directo
        EditorSceneManager.playModeStartScene = null;
        UnityEngine.Debug.Log("[TestGameScene] Start scene limpiada. Abre GameScene y presiona Play.");
    }

    public static void Restore()
    {
        // Restaura MainMenu como start scene
        var mainMenu = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/MainMenu.unity");
        EditorSceneManager.playModeStartScene = mainMenu;
        UnityEngine.Debug.Log("[TestGameScene] Start scene restaurada a MainMenu.");
    }
}
