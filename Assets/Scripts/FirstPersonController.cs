using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3.5f;
    public float gravity   = -12f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public float maxLookAngle     = 80f;

    [Header("Camera Bob")]
    public float bobFrequency = 2.2f;
    public float bobAmplitude = 0.06f;

    private CharacterController _cc;
    private Camera              _cam;
    private float               _verticalRotation;
    private float               _verticalVelocity;
    private float               _bobTimer;
    private Vector3             _camDefaultPos;

    void Start()
    {
        _cc  = GetComponent<CharacterController>();
        _cam = GetComponentInChildren<Camera>();
        _camDefaultPos = _cam.transform.localPosition;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    void Update()
    {
        Look();
        Move();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
    }

    void Look()
    {
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(0f, mx, 0f);

        _verticalRotation = Mathf.Clamp(_verticalRotation - my, -maxLookAngle, maxLookAngle);
        _cam.transform.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);
    }

    void Move()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 dir = transform.right * h + transform.forward * v;
        bool moving = dir.magnitude > 0.1f;

        if (_cc.isGrounded)
            _verticalVelocity = -0.5f;
        else
            _verticalVelocity += gravity * Time.deltaTime;

        dir.y = _verticalVelocity;
        _cc.Move(dir * walkSpeed * Time.deltaTime);

        // Camera bob
        if (moving && _cc.isGrounded)
        {
            _bobTimer += Time.deltaTime * bobFrequency;
            float bobY = Mathf.Sin(_bobTimer * Mathf.PI * 2f) * bobAmplitude;
            _cam.transform.localPosition = _camDefaultPos + new Vector3(0f, bobY, 0f);
        }
        else
        {
            _bobTimer = 0f;
            _cam.transform.localPosition = Vector3.Lerp(
                _cam.transform.localPosition, _camDefaultPos, Time.deltaTime * 8f);
        }
    }
}
