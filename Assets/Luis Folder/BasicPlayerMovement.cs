using UnityEngine;
using UnityEngine.InputSystem;
using Controls = CharacterController.InputSystem_Actions;

/// <summary>
/// Movimiento básico con gravedad (caminar con WASD relativo a la cámara).
/// - "Movement Locked" en el Inspector = personaje estático, sólo se mueve la cámara (DiverCamera).
/// - Tecla de toggle (por defecto L) para activar/desactivar el bloqueo en Play.
/// Usar junto con DiverCamera en la Main Camera para la cámara 360°.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BasicPlayerMovement : MonoBehaviour
{
    [Header("Bloqueo")]
    [Tooltip("Si está activo, WASD no mueve al personaje: sólo se mueve la cámara.")]
    public bool movementLocked = true;
    [SerializeField] Key toggleLockKey = Key.L;

    [Header("Movimiento")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float sprintMultiplier = 1.6f;
    [SerializeField] float turnSpeed = 10f;
    [SerializeField] bool rotateTowardsMovement = true;

    [Header("Referencias")]
    [SerializeField] Transform cameraTransform; // vacío = Camera.main

    Rigidbody rb;
    Controls controls;
    Vector2 moveInput;
    bool sprinting;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        controls = new Controls();
    }

    void OnEnable()  => controls.Player.Enable();
    void OnDisable() => controls.Player.Disable();
    void OnDestroy() => controls.Dispose();

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current[toggleLockKey].wasPressedThisFrame)
        {
            movementLocked = !movementLocked;
            Debug.Log($"Movimiento {(movementLocked ? "BLOQUEADO (sólo cámara)" : "LIBRE")}");
        }

        moveInput = movementLocked ? Vector2.zero : controls.Player.Move.ReadValue<Vector2>();
        sprinting = controls.Player.Sprint.IsPressed();
    }

    void FixedUpdate()
    {
        Vector3 dir = GetMoveDirection();
        float speed = moveSpeed * (sprinting ? sprintMultiplier : 1f);

        // Sólo tocamos X/Z; Y la maneja la gravedad
        Vector3 vel = rb.linearVelocity;
        rb.linearVelocity = new Vector3(dir.x * speed, vel.y, dir.z * speed);

        if (rotateTowardsMovement && dir.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(dir);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, target, turnSpeed * Time.fixedDeltaTime));
        }
    }

    Vector3 GetMoveDirection()
    {
        Vector3 forward = Vector3.forward, right = Vector3.right;
        if (cameraTransform != null)
        {
            forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        }
        return Vector3.ClampMagnitude(forward * moveInput.y + right * moveInput.x, 1f);
    }
}
