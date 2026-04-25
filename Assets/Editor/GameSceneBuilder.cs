using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public class GameSceneBuilder
{
    public static void Execute()
    {
        // ── Ensure folders ──────────────────────────────────────────────
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        // ── Create & open empty scene ───────────────────────────────────
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Render settings ─────────────────────────────────────────────
        RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.01f, 0.005f, 0.02f);
        RenderSettings.fog          = true;
        RenderSettings.fogColor     = new Color(0.01f, 0.005f, 0.02f);
        RenderSettings.fogMode      = FogMode.Linear;
        RenderSettings.fogStartDistance = 8f;
        RenderSettings.fogEndDistance   = 22f;

        // ── Materials ───────────────────────────────────────────────────
        Material floorMat   = MakeMat("CasinoFloor",   new Color(0.06f, 0.02f, 0.09f), 0.1f, 0.3f);
        Material wallMat    = MakeMat("CasinoWall",    new Color(0.04f, 0.02f, 0.06f), 0.1f, 0.2f);
        Material ceilMat    = MakeMat("CasinoCeiling", new Color(0.02f, 0.01f, 0.04f), 0.0f, 0.1f);
        Material slotMat    = MakeMat("SlotMachine",   new Color(0.12f, 0.07f, 0.02f), 0.6f, 0.5f);
        Material screenMat  = MakeMat("SlotScreen",    new Color(0.0f,  0.6f,  0.1f),  0.0f, 0.8f);
        SetEmission(screenMat, new Color(0f, 1f, 0.2f) * 1.5f);
        Material lightStripMat = MakeMat("LightStrip", new Color(0.9f, 0.85f, 0.7f), 0.0f, 1.0f);
        SetEmission(lightStripMat, new Color(0.8f, 0.7f, 0.5f) * 1.8f);

        SaveMat(floorMat,      "Assets/Materials/CasinoFloor.mat");
        SaveMat(wallMat,       "Assets/Materials/CasinoWall.mat");
        SaveMat(ceilMat,       "Assets/Materials/CasinoCeiling.mat");
        SaveMat(slotMat,       "Assets/Materials/SlotMachine.mat");
        SaveMat(screenMat,     "Assets/Materials/SlotScreen.mat");
        SaveMat(lightStripMat, "Assets/Materials/LightStrip.mat");

        // ── Room geometry ────────────────────────────────────────────────
        const float W = 18f, H = 4f, D = 34f;

        Box("Floor",      new Vector3(0,  -0.5f,      0), new Vector3(W,   1f,   D),   floorMat);
        Box("Ceiling",    new Vector3(0,   H + 0.5f,  0), new Vector3(W,   1f,   D),   ceilMat);
        Box("WallLeft",   new Vector3(-W/2f - 0.25f, H/2f, 0), new Vector3(0.5f, H+1f, D), wallMat);
        Box("WallRight",  new Vector3( W/2f + 0.25f, H/2f, 0), new Vector3(0.5f, H+1f, D), wallMat);
        Box("WallBack",   new Vector3(0, H/2f, -D/2f - 0.25f), new Vector3(W+1f, H+1f, 0.5f), wallMat);
        Box("WallFront",  new Vector3(0, H/2f,  D/2f + 0.25f), new Vector3(W+1f, H+1f, 0.5f), wallMat);

        // ── Slot machines ────────────────────────────────────────────────
        float[] machineZ = { -14f, -10f, -6f, -2f, 2f, 6f, 10f, 14f };
        foreach (float z in machineZ)
        {
            SlotMachine("SlotL" + z, new Vector3(-5.5f, 0f, z), slotMat, screenMat, facingRight: true);
            SlotMachine("SlotR" + z, new Vector3( 5.5f, 0f, z), slotMat, screenMat, facingRight: false);
        }

        // ── Neon wall lights ─────────────────────────────────────────────
        Color[] neons = {
            new Color(1f, 0.08f, 0.08f),
            new Color(0.95f, 0.72f, 0.0f),
            new Color(0.55f, 0.08f, 1f),
        };
        float[] neonZ = { -12f, -6f, 0f, 6f, 12f };
        for (int i = 0; i < neonZ.Length; i++)
        {
            NeonLight("NeonL" + i, new Vector3(-W/2f + 0.4f, 2.8f, neonZ[i]), neons[i % 3], 2.5f, 7f);
            NeonLight("NeonR" + i, new Vector3( W/2f - 0.4f, 2.8f, neonZ[i]), neons[(i+1) % 3], 2.5f, 7f);
        }

        // ── Ceiling fluorescent strips ───────────────────────────────────
        float[] ceilZ = { -12f, -6f, 0f, 6f, 12f };
        float[] ceilX = { -4f, 4f };
        foreach (float z in ceilZ)
            foreach (float x in ceilX)
                FluorescentStrip("Strip_" + x + "_" + z, new Vector3(x, H - 0.05f, z), lightStripMat);

        // ── Player ───────────────────────────────────────────────────────
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0f, 1f, 2f);

        var cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.center = Vector3.zero;
        cc.radius = 0.35f;
        cc.stepOffset = 0.3f;
        cc.slopeLimit = 45f;

        player.AddComponent<FirstPersonController>();

        var camGO = new GameObject("MainCamera");
        camGO.tag = "MainCamera";
        camGO.transform.SetParent(player.transform);
        camGO.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        camGO.transform.localRotation = Quaternion.identity;

        var cam = camGO.AddComponent<Camera>();
        cam.nearClipPlane = 0.08f;
        cam.farClipPlane  = 40f;
        cam.fieldOfView   = 75f;
        camGO.AddComponent<AudioListener>();

        // ── Save scene ───────────────────────────────────────────────────
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/GameScene.unity");

        // ── Build Settings ───────────────────────────────────────────────
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity",  true),
            new EditorBuildSettingsScene("Assets/Scenes/GameScene.unity", true),
        };

        AssetDatabase.SaveAssets();
        Debug.Log("[GameSceneBuilder] GameScene construida y guardada con éxito.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    static Material MakeMat(string name, Color color, float metallic, float smoothness)
    {
        var m = new Material(Shader.Find("Standard"));
        m.name = name;
        m.color = color;
        m.SetFloat("_Metallic",    metallic);
        m.SetFloat("_Glossiness",  smoothness);
        return m;
    }

    static void SetEmission(Material m, Color emission)
    {
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", emission);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
    }

    static void SaveMat(Material m, string path)
    {
        AssetDatabase.CreateAsset(m, path);
    }

    static GameObject Box(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position   = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static void SlotMachine(string name, Vector3 base_, Material body, Material screen, bool facingRight)
    {
        float side = facingRight ? 1f : -1f;

        // Body
        var bodyGO = Box(name + "_Body", base_ + new Vector3(0f, 0.9f, 0f),
                         new Vector3(1.1f, 1.8f, 0.85f), body);

        // Top
        Box(name + "_Top", base_ + new Vector3(0f, 1.95f, 0f),
            new Vector3(1.2f, 0.15f, 0.95f), body);

        // Screen (emissive)
        Box(name + "_Screen", base_ + new Vector3(side * 0.58f, 1.15f, 0f),
            new Vector3(0.06f, 0.65f, 0.55f), screen);

        // Small glow light at screen
        var lgo = new GameObject(name + "_ScreenLight");
        lgo.transform.position = base_ + new Vector3(side * 0.8f, 1.15f, 0f);
        var l = lgo.AddComponent<Light>();
        l.type      = LightType.Point;
        l.color     = new Color(0f, 0.9f, 0.25f);
        l.intensity = 0.5f;
        l.range     = 2.5f;
    }

    static void NeonLight(string name, Vector3 pos, Color color, float intensity, float range)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var l = go.AddComponent<Light>();
        l.type      = LightType.Point;
        l.color     = color;
        l.intensity = intensity;
        l.range     = range;

        var f = go.AddComponent<LightFlicker>();
        f.normalIntensity = intensity;
        f.minStableTime   = 0.8f;
        f.maxStableTime   = 5f;
    }

    static void FluorescentStrip(string name, Vector3 pos, Material mat)
    {
        // Visual strip
        var strip = Box(name + "_Geo", pos, new Vector3(1.4f, 0.06f, 0.18f), mat);
        Object.DestroyImmediate(strip.GetComponent<BoxCollider>());

        // Light
        var lgo = new GameObject(name + "_Light");
        lgo.transform.position = pos - new Vector3(0f, 0.12f, 0f);
        var l = lgo.AddComponent<Light>();
        l.type      = LightType.Point;
        l.color     = new Color(0.75f, 0.65f, 0.55f);
        l.intensity = 1.4f;
        l.range     = 9f;

        var f = lgo.AddComponent<LightFlicker>();
        f.normalIntensity = 1.4f;
        f.minStableTime   = 2f;
        f.maxStableTime   = 9f;
        f.maxFlickers     = 2;
    }
}
