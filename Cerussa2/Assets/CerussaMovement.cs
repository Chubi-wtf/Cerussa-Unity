using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class CerussaMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool followCamera = true;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 3.5f, -7f);
    [SerializeField] private float cameraFollowSmoothTime = .09f;
    [SerializeField] private float cameraLookHeight = 1.2f;

    [Header("Ground movement")]
    [SerializeField] private float moveSpeed = 7.5f;
    [SerializeField] private float acceleration = 40f;
    [SerializeField] private float turnSpeed = 18f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 2.25f;
    [SerializeField] private int airJumps = 1;
    [SerializeField] private float gravity = -24f;

    [Header("Dodge")]
    [SerializeField] private float dodgeSpeed = 16f;
    [SerializeField] private float dodgeDuration = .28f;
    [SerializeField] private float dodgeCooldown = .14f;

    [Header("Dash feedback")]
    [SerializeField] private Color trailColor = new Color(1f, .18f, .62f, 1f);
    [SerializeField] private float trailLifetime = .38f;
    [SerializeField] private float dashFovKick = 5f;

    [Header("Camera impact")]
    [SerializeField, Range(0f, 8f)] private float dashCameraTilt = 1.5f;
    [SerializeField] private float jumpFovKick = 1.5f;
    [SerializeField] private float landingFovKick = 3f;
    [SerializeField] private float landingMinFallSpeed = 8f;

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private Vector3 dodgeDirection;
    private float verticalVelocity;
    private float dodgeTimeRemaining;
    private float dodgeCooldownRemaining;
    private int jumpsUsed;
    private Vector3 cameraFollowVelocity;
    private TrailRenderer dashTrail;
    private Camera followCameraComponent;
    private float baseCameraFov;
    private Vector3 cameraImpactOffset;
    private float feedbackFovKick;
    private float lastVerticalVelocity;
    private bool wasGrounded;

    private void Awake()
    {
        if (GetComponent<Camera>() != null)
        {
            Debug.LogError("CerussaMovement debe ir en el objeto Player, no en la Main Camera.", this);
            enabled = false;
            return;
        }

        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // A child camera inherits the player's rotation and makes camera-relative
        // movement feed back into the player's facing direction. Keep it separate.
        if (cameraTransform != null && cameraTransform.IsChildOf(transform))
            cameraTransform.SetParent(null, true);

        if (cameraTransform != null)
        {
            followCameraComponent = cameraTransform.GetComponent<Camera>();
            if (followCameraComponent != null) baseCameraFov = followCameraComponent.fieldOfView;
        }

        CreateDashTrail();
        wasGrounded = controller.isGrounded;
    }

    private void Update()
    {
        if (cameraTransform == null) return;

        Vector2 input = ReadMoveInput();
        Vector3 desiredDirection = CameraRelativeDirection(input);

        HandleJump();
        HandleDodge(desiredDirection);
        ApplyMovement(desiredDirection, input.magnitude);
    }

    private void LateUpdate()
    {
        if (dashTrail != null)
            dashTrail.emitting = dodgeTimeRemaining > 0f;

        if (!followCamera || cameraTransform == null) return;

        cameraImpactOffset = Vector3.MoveTowards(cameraImpactOffset, Vector3.zero, 7f * Time.deltaTime);
        feedbackFovKick = Mathf.MoveTowards(feedbackFovKick, 0f, 12f * Time.deltaTime);

        Vector3 targetPosition = transform.position + cameraOffset + cameraImpactOffset;
        cameraTransform.position = Vector3.SmoothDamp(
            cameraTransform.position,
            targetPosition,
            ref cameraFollowVelocity,
            cameraFollowSmoothTime);
        Vector3 lookTarget = transform.position + Vector3.up * cameraLookHeight;
        Vector3 lookDirection = (lookTarget - cameraTransform.position).normalized;
        float dashTilt = dodgeTimeRemaining > 0f
            ? Vector3.Dot(dodgeDirection, cameraTransform.right) * dashCameraTilt
            : 0f;
        cameraTransform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up)
            * Quaternion.Euler(0f, 0f, -dashTilt);

        if (followCameraComponent != null)
        {
            float targetFov = baseCameraFov + feedbackFovKick;
            if (dodgeTimeRemaining > 0f) targetFov += dashFovKick;
            followCameraComponent.fieldOfView = Mathf.Lerp(
                followCameraComponent.fieldOfView,
                targetFov,
                14f * Time.deltaTime);
        }
    }

    private Vector2 ReadMoveInput()
    {
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        Vector2 input = gamepad != null ? gamepad.leftStick.ReadValue() : Vector2.zero;

        if (keyboard != null)
        {
            input.x += (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            input.y += (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
        }

        return Vector2.ClampMagnitude(input, 1f);
    }

    private Vector3 CameraRelativeDirection(Vector2 input)
    {
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f;
        right.y = 0f;
        return (forward.normalized * input.y + right.normalized * input.x).normalized;
    }

    private void HandleJump()
    {
        bool grounded = controller.isGrounded;
        if (grounded && !wasGrounded && lastVerticalVelocity < -landingMinFallSpeed)
        {
            feedbackFovKick = Mathf.Max(feedbackFovKick, landingFovKick);
            cameraImpactOffset += Vector3.down * .18f;
        }
        wasGrounded = grounded;

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
            jumpsUsed = 0;
        }

        bool jumpPressed = (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

        if (!jumpPressed) return;

        if (controller.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            feedbackFovKick = Mathf.Max(feedbackFovKick, jumpFovKick);
            return;
        }

        // airJumps represents extra jumps after leaving the ground.
        if (jumpsUsed < airJumps)
        {
            jumpsUsed++;
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            feedbackFovKick = Mathf.Max(feedbackFovKick, jumpFovKick);
        }
    }

    private void HandleDodge(Vector3 desiredDirection)
    {
        dodgeCooldownRemaining -= Time.deltaTime;
        bool dodgePressed = (Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);

        if (dodgePressed && dodgeTimeRemaining <= 0f && dodgeCooldownRemaining <= 0f)
        {
            dodgeDirection = desiredDirection.sqrMagnitude > .01f ? desiredDirection : transform.forward;
            dodgeTimeRemaining = dodgeDuration;
            dodgeCooldownRemaining = dodgeDuration + dodgeCooldown;
        }
    }

    private void ApplyMovement(Vector3 desiredDirection, float inputMagnitude)
    {
        if (dodgeTimeRemaining > 0f)
        {
            dodgeTimeRemaining -= Time.deltaTime;
            horizontalVelocity = dodgeDirection * dodgeSpeed;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dodgeDirection), turnSpeed * Time.deltaTime);
        }
        else
        {
            Vector3 targetVelocity = desiredDirection * (moveSpeed * inputMagnitude);
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, acceleration * Time.deltaTime);

            if (desiredDirection.sqrMagnitude > .01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(desiredDirection), turnSpeed * Time.deltaTime);
        }

        lastVerticalVelocity = verticalVelocity;
        verticalVelocity += gravity * Time.deltaTime;
        controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
    }

    private void CreateDashTrail()
    {
        GameObject trailObject = new GameObject("Dash Perfume Trail");
        trailObject.transform.SetParent(transform);
        trailObject.transform.localPosition = Vector3.up * 1.05f;

        dashTrail = trailObject.AddComponent<TrailRenderer>();
        dashTrail.time = trailLifetime;
        dashTrail.minVertexDistance = .08f;
        dashTrail.widthMultiplier = .65f;
        dashTrail.alignment = LineAlignment.View;
        dashTrail.emitting = false;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(trailColor, 0f),
                new GradientColorKey(new Color(.82f, .35f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(.8f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        dashTrail.colorGradient = gradient;

        Shader trailShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (trailShader != null)
            dashTrail.material = new Material(trailShader);
    }
}
