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
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate CheckpointManager found. Destroying duplicate: " + name);
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (defaultSpawnPoint != null)
        {
            currentSpawnPosition = defaultSpawnPoint.position;
            currentSpawnRotation = defaultSpawnPoint.rotation;

            Debug.Log("Default spawn set to: " + currentSpawnPosition);
        }
        else
        {
            currentSpawnPosition = Vector3.zero;
            currentSpawnRotation = Quaternion.identity;

            Debug.LogWarning("CheckpointManager has no default spawn point assigned. Using world origin.");
        }
    }

    public void SetCheckpoint(Transform checkpoint)
    {
        if (checkpoint == null)
        {
            Debug.LogWarning("Tried to set checkpoint, but checkpoint Transform was null.");
            return;
        }

        currentSpawnPosition = checkpoint.position;
        currentSpawnRotation = checkpoint.rotation;

        Debug.Log("Checkpoint set to: " + checkpoint.name + " at " + currentSpawnPosition);
    }

    public void Respawn(GameObject player)
    {
        if (player == null)
        {
            Debug.LogWarning("Respawn called, but player was null.");
            return;
        }

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

        PlayPoofSound();
        PlayParticles(player.transform.position);

        SetRenderersEnabled(renderers, false);

        yield return new WaitForSeconds(respawnDelay);

        if (controller != null)
            controller.enabled = false;

        Debug.Log("Respawning player to: " + currentSpawnPosition);

        player.transform.SetPositionAndRotation(currentSpawnPosition, currentSpawnRotation);

        if (playerController != null)
            playerController.ResetVelocity();

        if (controller != null)
            controller.enabled = true;

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
            if (renderer != null)
                renderer.enabled = enabled;
        }
    }
}