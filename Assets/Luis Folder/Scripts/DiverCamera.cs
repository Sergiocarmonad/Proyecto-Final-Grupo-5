using UnityEngine;
using Controls = CharacterController.InputSystem_Actions;

/// <summary>
/// Cámara orbital en tercera persona. Ponla en la Main Camera y arrastra el buzo a "target".
/// Mouse / stick derecho = orbitar. Esc libera el cursor, clic lo vuelve a bloquear.
/// </summary>
public class DiverCamera : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float distance = 5f;
    [SerializeField] float height = 1f;            // punto de enfoque sobre el pivote del buzo
    [SerializeField] float sensitivity = 0.15f;    // grados por pixel de mouse
    [SerializeField] float minPitch = -85f;   // mirar casi totalmente arriba/abajo (360° en horizontal ya es libre)
    [SerializeField] float maxPitch = 85f;

    [Header("Colisión (evita atravesar rocas/corales)")]
    [SerializeField] LayerMask collisionMask = ~0;
    [SerializeField] float collisionRadius = 0.25f;

    Controls controls;
    float yaw;
    float pitch = 15f;

    void Awake()
    {
        controls = new Controls();
        yaw = transform.eulerAngles.y;
    }

    void OnEnable()  => controls.Player.Enable();
    void OnDisable() => controls.Player.Disable();
    void OnDestroy() => controls.Dispose();

    void Start() => LockCursor(true);

    void LateUpdate()
    {
        if (target == null) return;

        // Cursor: Esc lo suelta, clic lo bloquea
        if (UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame == true) LockCursor(false);
        if (UnityEngine.InputSystem.Mouse.current?.leftButton.wasPressedThisFrame == true) LockCursor(true);

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 look = controls.Player.Look.ReadValue<Vector2>();
            yaw += look.x * sensitivity;
            pitch -= look.y * sensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = target.position + Vector3.up * height;
        Vector3 back = rotation * Vector3.back;

        // Si hay algo entre el buzo y la cámara, acercamos la cámara
        float dist = distance;
        if (Physics.SphereCast(pivot, collisionRadius, back, out RaycastHit hit, distance,
                               collisionMask, QueryTriggerInteraction.Ignore)
            && hit.transform.root != target.root)
        {
            dist = hit.distance;
        }

        transform.SetPositionAndRotation(pivot + back * dist, rotation);
    }

    static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
