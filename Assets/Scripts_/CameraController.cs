using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public sealed class CameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 initialOffset = new Vector3(0f, 8.48f, -13.46f);
    [SerializeField] private float lookHeight = 1.2f;
    [SerializeField] private float followSmoothTime = .12f;

    [Header("Orbit controls")]
    [SerializeField] private float orbitSpeed = 100f;
    [SerializeField] private float zoomSpeed = .012f;
    [SerializeField] private float minDistance = 5f;
    [SerializeField] private float maxDistance = 24f;

    private Camera cameraComponent;
    private Vector3 followVelocity;
    private Vector3 impactOffset;
    private float yaw;
    private float pitch;
    private float distance;
    private float fovKick;
    private float dashFovKick;
    private float dashTilt;
    private float baseFov;

    private void Awake()
    {
        cameraComponent = GetComponent<Camera>();
        baseFov = cameraComponent.fieldOfView;

        distance = initialOffset.magnitude;
        if (distance > .01f)
        {
            pitch = Mathf.Asin(initialOffset.y / distance) * Mathf.Rad2Deg;
            yaw = Mathf.Atan2(-initialOffset.x, -initialOffset.z) * Mathf.Rad2Deg;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        ReadOrbitInput();
        impactOffset = Vector3.MoveTowards(impactOffset, Vector3.zero, 7f * Time.deltaTime);
        fovKick = Mathf.MoveTowards(fovKick, 0f, 12f * Time.deltaTime);

        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 cameraOffset = orbitRotation * Vector3.back * distance;
        Vector3 desiredPosition = target.position + cameraOffset + impactOffset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity, followSmoothTime);

        Vector3 lookDirection = (target.position + Vector3.up * lookHeight - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up) * Quaternion.Euler(0f, 0f, -dashTilt);

        float targetFov = baseFov + fovKick + dashFovKick;
        cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView, targetFov, 14f * Time.deltaTime);
    }

    public void AddFovKick(float amount) => fovKick = Mathf.Max(fovKick, amount);

    public void AddPositionImpact(Vector3 amount) => impactOffset += amount;

    public void SetDashFeedback(float fovAmount, float tiltAmount)
    {
        dashFovKick = fovAmount;
        dashTilt = tiltAmount;
    }

    private void ReadOrbitInput()
    {
        Keyboard keyboard = Keyboard.current;
        float orbitInput = 0f;
        if (keyboard != null)
        {
            orbitInput += keyboard.eKey.isPressed ? 1f : 0f;
            orbitInput -= keyboard.qKey.isPressed ? 1f : 0f;
        }

        yaw += orbitInput * orbitSpeed * Time.deltaTime;

        if (Mouse.current == null) return;

        distance = Mathf.Clamp(
            distance - Mouse.current.scroll.ReadValue().y * zoomSpeed,
            minDistance,
            maxDistance);
    }
}
