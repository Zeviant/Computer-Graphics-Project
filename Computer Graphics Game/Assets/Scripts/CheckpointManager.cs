using System.Collections;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance;

    [Header("Respawn")]
    [SerializeField] private Transform defaultSpawnPoint;
    [SerializeField] private float respawnDelay = 1.0f;

    [Header("Disintegration Particles")]
    [SerializeField] private ParticleSystem disintegrateParticles;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip deathPoofSound;
    [SerializeField] private float deathPoofVolume = 1.0f;

    private Vector3 currentSpawnPosition;
    private Quaternion currentSpawnRotation;

    private bool isRespawning = false;

    private void Awake()
    {
        Instance = this;

        if (defaultSpawnPoint != null)
        {
            currentSpawnPosition = defaultSpawnPoint.position;
            currentSpawnRotation = defaultSpawnPoint.rotation;
        }
    }

    public void SetCheckpoint(Transform checkpoint)
    {
        currentSpawnPosition = checkpoint.position;
        currentSpawnRotation = checkpoint.rotation;
    }

    public void Respawn(GameObject player)
    {
        if (isRespawning)
            return;

        StartCoroutine(RespawnRoutine(player));
    }

    private IEnumerator RespawnRoutine(GameObject player)
    {
        isRespawning = true;

        CharacterController controller = player.GetComponent<CharacterController>();
        PlayerController playerController = player.GetComponent<PlayerController>();
        Renderer[] renderers = player.GetComponentsInChildren<Renderer>();

        if (playerController != null)
            playerController.enabled = false;

        // Disintegrate at current position
        PlayPoofSound();
        PlayParticles(player.transform.position);

        // Hide player while waiting to respawn
        SetRenderersEnabled(renderers, false);

        yield return new WaitForSeconds(respawnDelay);

        // Teleport player to checkpoint
        if (controller != null)
            controller.enabled = false;

        player.transform.SetPositionAndRotation(currentSpawnPosition, currentSpawnRotation);

        if (playerController != null)
            playerController.ResetVelocity();

        if (controller != null)
            controller.enabled = true;

        // Reappear at checkpoint
        PlayParticles(player.transform.position);
        SetRenderersEnabled(renderers, true);

        if (playerController != null)
            playerController.enabled = true;

        isRespawning = false;
    }

    private void PlayParticles(Vector3 position)
    {
        if (disintegrateParticles == null)
            return;

        ParticleSystem particles = Instantiate(
            disintegrateParticles,
            position,
            Quaternion.identity
        );

        particles.Play();

        Destroy(particles.gameObject, 2f);
    }

    private void PlayPoofSound()
    {
        if (audioSource == null || deathPoofSound == null)
            return;

        audioSource.PlayOneShot(deathPoofSound, deathPoofVolume);
    }

    private void SetRenderersEnabled(Renderer[] renderers, bool enabled)
    {
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = enabled;
        }
    }
}