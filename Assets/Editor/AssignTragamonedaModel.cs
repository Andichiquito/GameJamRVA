using UnityEngine;
using UnityEditor;

public class AssignTragamonedaModel : EditorWindow
{
    const string MODEL_PATH = "Assets/tragamonedas/tragamonedas.fbx";
    const string CHILD_NAME = "Model_Tragamonedas";

    [MenuItem("Tools/Assign Tragamonedas Model to All Machines")]
    static void Run()
    {
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH);
        if (modelAsset == null)
        {
            Debug.LogError($"No se encontró el modelo en: {MODEL_PATH}");
            return;
        }

        SlotMachine[] machines = Object.FindObjectsByType<SlotMachine>(FindObjectsSortMode.None);
        if (machines.Length == 0)
        {
            Debug.LogWarning("No se encontraron objetos con componente SlotMachine en la escena.");
            return;
        }

        int count = 0;
        foreach (SlotMachine sm in machines)
        {
            Transform parent = sm.transform;

            // Eliminar modelo anterior si ya existía
            Transform existing = parent.Find(CHILD_NAME);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, parent);
            instance.name = CHILD_NAME;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale    = Vector3.one;
            Undo.RegisterCreatedObjectUndo(instance, "Assign Tragamonedas Model");

            EditorUtility.SetDirty(parent.gameObject);
            count++;
        }

        Debug.Log($"Modelo asignado a {count} máquina(s). Recuerda guardar la escena (Ctrl+S).");
    }
}
