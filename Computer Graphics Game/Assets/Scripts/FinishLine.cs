using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class FinishLine : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    [Header("Camera")]
    [SerializeField] private CameraController cameraController;
    [SerializeField] private Camera finishCamera;
    [SerializeField] private Transform finishCameraView;
    [SerializeField] private float cameraMoveDuration = 0.35f;

    [Header("Finish Particles")]
    [SerializeField] private ParticleSystem finishParticlesPrefab;
    [SerializeField] private Transform particleSpawnPoint1;
    [SerializeField] private Transform particleSpawnPoint2;
    [SerializeField] private Transform particleSpawnPoint3;
    [SerializeField] private float particleDestroyDelay = 5f;

    [Header("Finish Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip finishSound;
    [SerializeField] private float finishSoundVolume = 1f;

    [Header("Delayed Finish Action")]
    [SerializeField] private float delayedActionTime = 3f;
    [SerializeField] private UnityEvent onDelayedFinishAction;

    [Header("Settings")]
    [SerializeField] private bool triggerOnlyOnce = true;

    private bool hasFinished = false;
    private Coroutine cameraMoveCoroutine;
    private Coroutine delayedActionCoroutine;

    private void Start()
    {
        if (cameraController == null)
            cameraController = FindFirstObjectByType<CameraController>();

        if (finishCamera == null)
            finishCamera = Camera.main;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasFinished && triggerOnlyOnce)
            return;

        if (!other.CompareTag(playerTag))
            return;

        PlayerController player = other.GetComponent<PlayerController>();

        if (player == null)
            player = other.GetComponentInParent<PlayerController>();

        if (player == null)
            return;

        hasFinished = true;

        LockAndStopPlayer(player);
        PlayFinishSound();
        StartFinishCameraMove();
        SpawnFinishParticles();
        StartDelayedAction();
    }

    private void LockAndStopPlayer(PlayerController player)
    {
        player.InputLocked = true;
        player.ResetVelocity();
    }

    private void PlayFinishSound()
    {
        if (audioSource == null || finishSound == null)
            return;

        audioSource.PlayOneShot(finishSound, finishSoundVolume);
    }

    private void StartFinishCameraMove()
    {
        if (cameraController != null)
            cameraController.enabled = false;

        if (finishCamera == null)
            return;

        if (finishCameraView == null)
            return;

        if (cameraMoveCoroutine != null)
            StopCoroutine(cameraMoveCoroutine);

        cameraMoveCoroutine = StartCoroutine(MoveCameraToFinishView());
    }

    private IEnumerator MoveCameraToFinishView()
    {
        Transform cameraTransform = finishCamera.transform;

        Vector3 startPosition = cameraTransform.position;
        Quaternion startRotation = cameraTransform.rotation;

        Vector3 targetPosition = finishCameraView.position;
        Quaternion targetRotation = finishCameraView.rotation;

        float elapsed = 0f;

        while (elapsed < cameraMoveDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / cameraMoveDuration;
            t = Mathf.Clamp01(t);

            float smoothT = t * t * (3f - 2f * t);

            cameraTransform.position = Vector3.Lerp(startPosition, targetPosition, smoothT);
            cameraTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, smoothT);

            yield return null;
        }

        cameraTransform.position = targetPosition;
        cameraTransform.rotation = targetRotation;
    }

    private void SpawnFinishParticles()
    {
        SpawnParticlesAt(particleSpawnPoint1);
        SpawnParticlesAt(particleSpawnPoint2);
        SpawnParticlesAt(particleSpawnPoint3);
    }

    private void SpawnParticlesAt(Transform spawnPoint)
    {
        if (finishParticlesPrefab == null)
            return;

        if (spawnPoint == null)
            return;

        ParticleSystem particles = Instantiate(
            finishParticlesPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        particles.Play();

        Destroy(particles.gameObject, particleDestroyDelay);
    }

    private void StartDelayedAction()
    {
        if (delayedActionCoroutine != null)
            StopCoroutine(delayedActionCoroutine);

        delayedActionCoroutine = StartCoroutine(DelayedActionRoutine());
    }

    private IEnumerator DelayedActionRoutine()
    {
        yield return new WaitForSeconds(delayedActionTime);

        OnDelayedFinishAction();
        onDelayedFinishAction?.Invoke();
    }

    private void OnDelayedFinishAction()
    {
        // palbo
    }
}