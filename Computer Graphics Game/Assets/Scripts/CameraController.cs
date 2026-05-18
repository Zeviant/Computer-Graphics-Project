using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Vector3 localOrbitOffset;
    [SerializeField] [Range(0.0f, 1.0f)] private float smoothPosition = 0.5f;
    [SerializeField] [Range(0.0f, 1.0f)] private float smoothRotation = 0.5f;
    [SerializeField] private LayerMask cameraCollisionMask;
    [SerializeField] private float collisionNormalOffset;

    [Header("Input")]
    [SerializeField] private float lookSpeed = 10.0f;

    [Header("vvv Don't touch")]
    [SerializeField] private Vector3 currentOrigin;
    [SerializeField] private Quaternion currentRotation;
    [SerializeField] private Vector3 targetOrigin;
    [SerializeField] private float targetYaw;
    [SerializeField] private float targetPitch;

    void Start()
    {
        // Input
        targetYaw = (targetYaw + lookSpeed * Input.GetAxis("Mouse X") * Time.deltaTime) % 360.0f;
        targetPitch = Mathf.Clamp(targetPitch + -1.0f * lookSpeed * Input.GetAxis("Mouse Y") * Time.deltaTime, -90.0f, 90.0f);

        // Calculate all targets
        var targetRotation = Quaternion.Euler(targetPitch, targetYaw, 0.0f);
        var targetOrigin = player.position;

        currentOrigin = targetOrigin;
        currentRotation = targetRotation;
        var currentVector = currentRotation * localOrbitOffset;
        Ray ray = new(currentOrigin, currentVector.normalized);
        if (Physics.Raycast(ray, out RaycastHit hit, currentVector.magnitude, cameraCollisionMask, QueryTriggerInteraction.Ignore))
        {
            currentVector = (hit.distance * currentVector.normalized) + collisionNormalOffset * hit.normal;
        }

        transform.position = currentOrigin + currentVector;
        transform.rotation = currentRotation;
    }

    void LateUpdate()
    {
        // Input
        targetYaw = (targetYaw + lookSpeed * Input.GetAxis("Mouse X") * Time.deltaTime) % 360.0f;
        targetPitch = Mathf.Clamp(targetPitch + -1.0f * lookSpeed * Input.GetAxis("Mouse Y") * Time.deltaTime, -90.0f, 90.0f);

        // Calculate all targets
        var targetRotation = Quaternion.Euler(targetPitch, targetYaw, 0.0f);
        var targetOrigin = player.position;
        
        // Exponential moving average (EMA)
        currentOrigin = Vector3.Lerp(currentOrigin, targetOrigin, smoothPosition);
        currentRotation = Quaternion.Slerp(currentRotation, targetRotation, smoothRotation);

        // Send ray 
        var currentVector = currentRotation * localOrbitOffset;
        Ray ray = new(currentOrigin, currentVector.normalized);
        if (Physics.Raycast(ray, out RaycastHit hit, currentVector.magnitude, cameraCollisionMask, QueryTriggerInteraction.Ignore))
        {
            currentVector = (hit.distance * currentVector.normalized) + collisionNormalOffset * hit.normal;
        }

        transform.position = currentOrigin + currentVector;
        transform.rotation = currentRotation;
    }
}
