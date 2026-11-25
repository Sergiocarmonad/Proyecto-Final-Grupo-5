using UnityEngine;
using UnityEngine.InputSystem;

// Player controller sencillo: WASD para moverse + cámara 360° que orbita al jugador.
// "Can Move" desactivado = el personaje queda estático y sólo gira la cámara.
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public bool canMove = false;
    public float moveSpeed = 5f;

    [Header("Cámara")]
    public Transform cameraTransform;
    public float cameraDistance = 5f;
    public float cameraHeight = 1.5f;
    public float lookSensitivity = 0.15f;

    Rigidbody rb;
    InputAction moveAction;
    InputAction lookAction;
    float yaw;
    float pitch = 15f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Acciones del InputSystem_Actions (asset project-wide, ya viene activado)
        moveAction = InputSystem.actions.FindAction("Move");
        lookAction = InputSystem.actions.FindAction("Look");

        if (cameraTransform == null) cameraTransform = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
    }

    void FixedUpdate()
    {
        Vector2 input = canMove ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        // Adelante/derecha según la cámara, sin componente vertical
        Vector3 forward = cameraTransform.forward; forward.y = 0; forward.Normalize();
        Vector3 right = cameraTransform.right;     right.y = 0;   right.Normalize();
        Vector3 dir = forward * input.y + right * input.x;

        rb.linearVelocity = new Vector3(dir.x * moveSpeed, rb.linearVelocity.y, dir.z * moveSpeed);
    }

    void LateUpdate()
    {
        Vector2 look = lookAction.ReadValue<Vector2>();
        yaw += look.x * lookSensitivity;
        pitch -= look.y * lookSensitivity;
        pitch = Mathf.Clamp(pitch, -80f, 80f);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 target = transform.position + Vector3.up * cameraHeight;

        cameraTransform.position = target - rotation * Vector3.forward * cameraDistance;
        cameraTransform.rotation = rotation;
    }
}
