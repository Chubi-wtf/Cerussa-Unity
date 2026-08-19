using UnityEngine;


[RequireComponent(typeof(Animator), typeof(CharacterController))]
public sealed class AnimatorDriver : MonoBehaviour
{
    [SerializeField] private HealthScript_ health; 
    [SerializeField] private float speedSmoothTime = .08f;
    [SerializeField] private float speedNormalizer = 1f;

    private Animator animator;
    private CharacterController controller;
    private float currentSpeedParam;
    private float speedVelocity;

    private static readonly int SpeedPlayerAnimHash = Animator.StringToHash("SpeedPlayerAnim");
    private static readonly int HasDiedHash = Animator.StringToHash("HasDied");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();

        if (health == null) health = GetComponent<HealthScript_>();
        if (health != null) health.OnDied += HandleDied;
    }

    private void OnDestroy()
    {
        if (health != null) health.OnDied -= HandleDied;
    }

    private void Update()
    {
        if (health != null && health.IsDead) return; 

        Vector3 horizontalVelocity = controller.velocity;
        horizontalVelocity.y = 0f;

        float targetSpeed = horizontalVelocity.magnitude / Mathf.Max(speedNormalizer, .0001f);
        currentSpeedParam = Mathf.SmoothDamp(currentSpeedParam, targetSpeed, ref speedVelocity, speedSmoothTime);

        animator.SetFloat(SpeedPlayerAnimHash, currentSpeedParam);
    }

    private void HandleDied()
    {
        animator.SetFloat(SpeedPlayerAnimHash, 0f);
        animator.SetBool(HasDiedHash, true);
    }
}