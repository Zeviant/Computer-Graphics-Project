using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private Transform pos;

    private void Awake()
    {
        if (pos == null)
        {
            Debug.LogWarning(name + " has no spawn position assigned. Using checkpoint transform instead.");
            pos = transform;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (CheckpointManager.Instance == null)
        {
            Debug.LogWarning("No CheckpointManager found in scene.");
            return;
        }

        CheckpointManager.Instance.SetCheckpoint(pos);
    }
}