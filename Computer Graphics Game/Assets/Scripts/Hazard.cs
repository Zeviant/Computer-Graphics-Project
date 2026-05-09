using UnityEngine;
public class Hazard : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        CheckpointManager.Instance.Respawn(other.gameObject);
    }
}
