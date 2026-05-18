using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Renderer[] playerRenderers;
    [SerializeField] private Vector3 localOrbitOffset;
    [SerializeField] [Range(0.0f, 1.0f)] private float smoothPosition = 0.5f;
    [SerializeField] [Range(0.0f, 1.0f)] private float smoothRotation = 0.5f;
    [SerializeField] private LayerMask cameraCollisionMask;
    [SerializeField] private float collisionNormalOffset;
    [SerializeField] private float fadeDistance;
    [SerializeField] private float fadeSpeed;

    [Header("Input")]
    [SerializeField] private float lookSpeed = 10.0f;

    [Header("vvv Don't touch")]
    [SerializeField] private Vector3 currentOrigin;
    [SerializeField] private Quaternion currentRotation;
    [SerializeField] private float currentPlayerAlpha;
    [SerializeField] private Vector3 targetOrigin;
    [SerializeField] private float targetYaw;
    [SerializeField] private float targetPitch;

    (Vector3, Quaternion) GetTarget() {
        var targetOrigin = player.position;
        var targetRotation = Quaternion.Euler(targetPitch, targetYaw, 0.0f);
        return (targetOrigin, targetRotation);
    }

    void UpdateTransform() {
        var currentVector = currentRotation * localOrbitOffset;
        Ray ray = new(currentOrigin, currentVector.normalized);
        if (Physics.Raycast(ray, out RaycastHit hit, currentVector.magnitude, cameraCollisionMask, QueryTriggerInteraction.Ignore))
        {
            currentVector = (hit.distance * currentVector.normalized) + collisionNormalOffset * hit.normal;
        }

        transform.position = currentOrigin + currentVector;
        transform.rotation = currentRotation;
    }

    void Start()
    {
        var (targetOrigin, targetRotation) = GetTarget();
        currentOrigin = targetOrigin;
        currentRotation = targetRotation;
        UpdateTransform();
    }

    void LateUpdate()
    {
        // Mouselook
        targetYaw = (targetYaw + lookSpeed * Input.GetAxis("Mouse X") * Time.deltaTime) % 360.0f;
        targetPitch = Mathf.Clamp(targetPitch + -1.0f * lookSpeed * Input.GetAxis("Mouse Y") * Time.deltaTime, -90.0f, 90.0f);

        var (targetOrigin, targetRotation) = GetTarget();       
        // Exponential moving average (EMA)
        currentOrigin = Vector3.Lerp(currentOrigin, targetOrigin, smoothPosition);
        currentRotation = Quaternion.Slerp(currentRotation, targetRotation, smoothRotation);
        UpdateTransform();

        // Make capsule transparent if close
        var distanceToPlayer = Vector3.Distance(transform.position, player.position);
        var targetAlpha = (distanceToPlayer < fadeDistance) ? 0.0f : 1.0f;
        currentPlayerAlpha = Mathf.MoveTowards(currentPlayerAlpha, targetAlpha, fadeSpeed * Time.deltaTime);

        foreach (var r in playerRenderers) {
            var currentColor = r.material.color;
            currentColor.a = currentPlayerAlpha;
            r.material.color = currentColor;
        }
    }
}
