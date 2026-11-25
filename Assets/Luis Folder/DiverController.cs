using UnityEngine;
// El archivo generado InputSystem_Actions.cs vive dentro del namespace "CharacterController".
// Usamos un alias para no chocar con la clase UnityEngine.CharacterController.
using Controls = CharacterController.InputSystem_Actions;

/// <summary>
/// Controlador de buzo en tercera persona (sin gravedad, con inercia de agua).
/// WASD / stick izq  = nadar (relativo a la cámara)
/// Space / botón sur = subir        C / botón este = bajar
/// Shift             = nado rápido
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class DiverController : MonoBehaviour
{
    [Header("Nado")]
    [SerializeField] float swimSpeed = 4f;          // velocidad normal (m/s)
    [SerializeField] float sprintMultiplier = 1.8f; // multiplicador con Shift
    [SerializeField] float verticalSpeed = 3f;      // velocidad al subir/bajar
    [SerializeField] float acceleration = 3f;       // qué tan rápido alcanza la velocidad (más bajo = más "agua")
    [Tooltip("Si está activo, W nada hacia donde mira la cámara (incluyendo arriba/abajo).")]
    [SerializeField] bool swimTowardCamera = false;

    [Header("Rotación")]
    [SerializeField] float turnSpeed = 6f;  // qué tan rápido gira hacia la dirección de nado
    [SerializeField] float maxTilt = 30f;   // inclinación (grados) al subir/bajar

    [Header("Referencias")]
    [SerializeField] Transform cameraTransform; // si se deja vacío usa Camera.main

    Rigidbody rb;
    Controls controls;

    Vector2 moveInput;
    float verticalInput;
    bool sprinting;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;                                 // estamos bajo el agua
        rb.constraints = RigidbodyConstraints.FreezeRotation;  // la física no nos voltea; rotamos nosotros
        rb.interpolation = RigidbodyInterpolation.Interpolate; // movimiento suave para la cámara

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        controls = new Controls();
    }

    void OnEnable()  => controls.Player.Enable();
    void OnDisable() => controls.Player.Disable();
    void OnDestroy() => controls.Dispose();

    // 1) Leer input en Update (cada frame)
    void Update()
    {
        moveInput = controls.Player.Move.ReadValue<Vector2>();
        sprinting = controls.Player.Sprint.IsPressed();

        verticalInput = 0f;
        if (controls.Player.Jump.IsPressed())   verticalInput += 1f;
        if (controls.Player.Crouch.IsPressed()) verticalInput -= 1f;
    }

    // 2) Aplicar física en FixedUpdate
    void FixedUpdate()
    {
        Vector3 targetVelocity = GetMoveDirection() * swimSpeed * (sprinting ? sprintMultiplier : 1f);
        targetVelocity += Vector3.up * verticalInput * verticalSpeed;

        // Lerp hacia la velocidad deseada = aceleración y frenado suaves, como en el agua
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);

        RotateTowardsVelocity();
    }

    Vector3 GetMoveDirection()
    {
        if (cameraTransform == null) return new Vector3(moveInput.x, 0f, moveInput.y);

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        if (!swimTowardCamera)
        {
            // Aplanamos para que W sea siempre "horizontal"
            forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            right = Vector3.ProjectOnPlane(right, Vector3.up).normalized;
        }

        Vector3 dir = forward * moveInput.y + right * moveInput.x;
        return Vector3.ClampMagnitude(dir, 1f); // diagonal no más rápida
    }

    void RotateTowardsVelocity()
    {
        Vector3 vel = rb.linearVelocity;
        Vector3 flatVel = new Vector3(vel.x, 0f, vel.z);

        // Yaw: hacia donde nadamos (si casi no nos movemos, mantenemos el actual)
        float yaw = flatVel.sqrMagnitude > 0.05f
            ? Quaternion.LookRotation(flatVel).eulerAngles.y
            : rb.rotation.eulerAngles.y;

        // Pitch: inclinarse al subir/bajar (negativo = nariz arriba en Unity)
        float speedRef = Mathf.Max(verticalSpeed, 0.01f);
        float pitch = -Mathf.Clamp(vel.y / speedRef, -1f, 1f) * maxTilt;

        Quaternion target = Quaternion.Euler(pitch, yaw, 0f);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, target, turnSpeed * Time.fixedDeltaTime));
    }
}
