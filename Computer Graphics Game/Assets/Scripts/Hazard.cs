using UnityEngine;
public class Hazard : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {

        if (other.tag == "Player")
        {
            CheckpointManager.Instance.Respawn(other.gameObject);
        }
    }
}
