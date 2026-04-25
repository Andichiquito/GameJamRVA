using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class DiagnoseProject
{
    public static void Execute()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== DIAGNÓSTICO DEL PROYECTO ===\n");

        // ── Build Settings ──────────────────────────────────────────────
        sb.AppendLine("── BUILD SETTINGS ──");
        var scenes = EditorBuildSettings.scenes;
        if (scenes.Length == 0)
        {
            sb.AppendLine("  ¡VACÍO! No hay escenas en Build Settings.");
        }
        else
        {
            for (int i = 0; i < scenes.Length; i++)
                sb.AppendLine($"  [{i}] {scenes[i].path}  enabled={scenes[i].enabled}");
        }
        sb.AppendLine();

        // ── Archivos .unity ─────────────────────────────────────────────
        sb.AppendLine("── ARCHIVOS .unity EN ASSETS ──");
        foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var info = new System.IO.FileInfo(path);
            sb.AppendLine($"  {path}  ({info.Length / 1024} KB)");
        }
        sb.AppendLine();

        // ── MainMenu scene ──────────────────────────────────────────────
        sb.AppendLine("── MAINMENU SCENE (Assets/MainMenu.unity) ──");
        var mmScene = EditorSceneManager.OpenScene("Assets/MainMenu.unity", OpenSceneMode.Additive);
        bool hasCanvas = false, hasStartBtn = false, hasQuitBtn = false,
             hasManager = false, hasNeonFlicker = false;

        foreach (var go in mmScene.GetRootGameObjects())
        {
            sb.AppendLine($"  Root: {go.name}");
            CheckChildren(go.transform, sb, 2,
                ref hasCanvas, ref hasStartBtn, ref hasQuitBtn,
                ref hasManager, ref hasNeonFlicker);
        }
        sb.AppendLine($"  Canvas: {hasCanvas}  StartBtn: {hasStartBtn}  QuitBtn: {hasQuitBtn}");
        sb.AppendLine($"  MainMenuManager: {hasManager}  NeonFlicker: {hasNeonFlicker}");
        EditorSceneManager.CloseScene(mmScene, true);
        sb.AppendLine();

        // ── GameScene ───────────────────────────────────────────────────
        sb.AppendLine("── GAMESCENE (Assets/Scenes/GameScene.unity) ──");
        var gsScene = EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Additive);
        bool hasPlayer = false, hasCC = false, hasFPC = false,
             hasCam = false, hasFloor = false, hasSlots = false, hasLights = false;

        int slotCount = 0, lightCount = 0;
        foreach (var go in gsScene.GetRootGameObjects())
        {
            if (go.name == "Player")       { hasPlayer = true; }
            if (go.name == "Floor")        { hasFloor  = true; }
            if (go.name.StartsWith("Slot")) slotCount++;
            if (go.name.StartsWith("Neon") || go.name.StartsWith("Strip")) lightCount++;

            if (go.GetComponent<CharacterController>() != null) hasCC  = true;
            if (go.GetComponent<FirstPersonController>() != null) hasFPC = true;

            // Check children for camera
            foreach (Transform child in go.transform)
                if (child.GetComponent<Camera>() != null) hasCam = true;
        }
        hasSlots  = slotCount > 0;
        hasLights = lightCount > 0;

        sb.AppendLine($"  Player GO: {hasPlayer}  CharacterController: {hasCC}  FirstPersonController: {hasFPC}");
        sb.AppendLine($"  Camera (hijo): {hasCam}  Floor: {hasFloor}");
        sb.AppendLine($"  Slot machines root GOs: {slotCount}  Neon/Strip lights: {lightCount}");
        EditorSceneManager.CloseScene(gsScene, true);
        sb.AppendLine();

        // ── Botón wiring ────────────────────────────────────────────────
        sb.AppendLine("── SCRIPTS EN ASSETS/SCRIPTS ──");
        foreach (var guid in AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/Scripts" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            sb.AppendLine($"  {System.IO.Path.GetFileName(path)}");
        }

        Debug.Log(sb.ToString());
    }

    static void CheckChildren(Transform t, System.Text.StringBuilder sb, int indent,
        ref bool hasCanvas, ref bool hasStartBtn, ref bool hasQuitBtn,
        ref bool hasManager, ref bool hasNeonFlicker)
    {
        string pad = new string(' ', indent);
        foreach (Transform child in t)
        {
            var comps = string.Join(", ", System.Array.ConvertAll(
                child.GetComponents<Component>(),
                c => c.GetType().Name));
            sb.AppendLine($"{pad}- {child.name}  [{comps}]");

            if (child.GetComponent<Canvas>()          != null) hasCanvas      = true;
            if (child.name == "StartButton")                   hasStartBtn    = true;
            if (child.name == "QuitButton")                    hasQuitBtn     = true;
            if (child.GetComponent<MainMenuManager>() != null) hasManager     = true;
            if (child.GetComponent<NeonFlicker>()     != null) hasNeonFlicker = true;

            CheckChildren(child, sb, indent + 2, ref hasCanvas, ref hasStartBtn,
                          ref hasQuitBtn, ref hasManager, ref hasNeonFlicker);
        }
    }
}
