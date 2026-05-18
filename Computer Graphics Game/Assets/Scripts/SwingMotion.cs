using UnityEngine;

public class SwingMotion : MonoBehaviour
{
    [Header("Mode")]
    [SerializeField] private bool spinInfinitely = false;

    [Header("Swing Settings")]
    [SerializeField] private float maxAngle = 90f;
    [SerializeField] private float swingSpeed = 1.5f;
    [SerializeField] private Vector3 swingAxis = Vector3.forward;

    [Header("Spin Settings")]
    [SerializeField] private float spinSpeed = 180f;

    [Header("Start Offset")]
    [SerializeField] private float phaseOffset = 0f;

    private Quaternion startRotation;

    private void Start()
    {
        startRotation = transform.localRotation;
        swingAxis = swingAxis.normalized;
    }

    private void Update()
    {
        if (spinInfinitely)
        {
            transform.Rotate(swingAxis, spinSpeed * Time.deltaTime, Space.Self);
            return;
        }

        float angle = Mathf.Sin((Time.time + phaseOffset) * swingSpeed) * maxAngle;

        transform.localRotation = startRotation * Quaternion.AngleAxis(angle, swingAxis);
    }
}