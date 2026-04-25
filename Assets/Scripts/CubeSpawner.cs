using UnityEngine;

public class CubeSpawner : MonoBehaviour
{
    [Header("Cube Settings")]
    public Vector3 size = Vector3.one;
    public Color color = Color.cyan;
    public bool rotate = true;
    public float rotationSpeed = 45f;

    private GameObject cube;

    void Start()
    {
        cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.localScale = size;

        // Coloca el cubo 5 unidades frente a la cámara principal
        Camera cam = Camera.main;
        if (cam != null)
            cube.transform.position = cam.transform.position + cam.transform.forward * 5f;
        else
            cube.transform.position = new Vector3(0, 0, 5);

        var renderer = cube.GetComponent<Renderer>();
        var mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        renderer.material = mat;
    }

    void Update()
    {
        if (rotate && cube != null)
            cube.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}
