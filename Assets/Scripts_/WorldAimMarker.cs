using UnityEngine;
using UnityEngine.InputSystem;

public sealed class WorldAimMarker : MonoBehaviour
{
    [SerializeField] private Camera aimCamera;
    [SerializeField] private LayerMask aimMask = ~0;
    [SerializeField] private float maxAimDistance = 250f;

    private Transform markerTransform;
    private Vector3 aimPoint;
    private bool hasAimPoint;

    private void Awake()
    {
        if (aimCamera == null) aimCamera = Camera.main;
        CreateMarker();
    }

    private void Update()
    {
        hasAimPoint = TryFindAimPoint(out aimPoint);
        if (markerTransform == null) return;

        markerTransform.gameObject.SetActive(hasAimPoint);
        if (hasAimPoint)
            markerTransform.position = aimPoint + Vector3.up * .025f;
    }

    public bool TryGetAimPoint(out Vector3 point)
    {
        point = aimPoint;
        return hasAimPoint;
    }

    private bool TryFindAimPoint(out Vector3 point)
    {
        point = default;
        if (aimCamera == null || Mouse.current == null) return false;

        Ray ray = aimCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
            return true;
        }

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (!groundPlane.Raycast(ray, out float distance)) return false;
        point = ray.GetPoint(distance);
        return true;
    }

    private void CreateMarker()
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "Aim Marker";
        marker.transform.localScale = new Vector3(.65f, .02f, .65f);
        markerTransform = marker.transform;

        Collider markerCollider = marker.GetComponent<Collider>();
        if (markerCollider != null) Destroy(markerCollider);

        Renderer renderer = marker.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (renderer != null && shader != null)
            renderer.material = new Material(shader) { color = new Color(1f, .2f, .55f) };
    }
}
