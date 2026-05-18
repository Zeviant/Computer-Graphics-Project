using UnityEngine;

public class MoveBetweenAB : MonoBehaviour
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float speed = 4f;
    [SerializeField] private float waitTime = 0.2f;

    private Transform target;
    private float waitTimer;

    public Vector3 DeltaMovement { get; private set; }

    private Vector3 previousPosition;

    private void Start()
    {
        target = pointB;
        previousPosition = transform.position;
    }

    private void Update()
    {
        DeltaMovement = Vector3.zero;

        if (pointA == null || pointB == null)
            return;

        previousPosition = transform.position;

        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );

        DeltaMovement = transform.position - previousPosition;

        if (Vector3.Distance(transform.position, target.position) < 0.05f)
        {
            if (target == pointA)
            {
                target = pointB;
            }
            else
            {
                target = pointA;
            }

            waitTimer = waitTime;
        }
    }
}