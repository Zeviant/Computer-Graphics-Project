using UnityEngine;

public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            CheckpointManager.Instance.Respawn(other.gameObject);
        }
    }
}