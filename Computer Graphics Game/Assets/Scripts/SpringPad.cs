using UnityEngine;

public class SpringPad : MonoBehaviour
{
    [Header("Launch")]
    [SerializeField] private float launchForce = 18f;
    [SerializeField] private bool usePadDirection = false;
    [SerializeField] private Transform launchDirection;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip springSound;
    [SerializeField] private float springVolume = 1f;

    [Header("Effects")]
    [SerializeField] private ParticleSystem springParticles;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerController player = other.GetComponent<PlayerController>();

        if (player == null)
            return;

        Vector3 direction = GetLaunchDirection();

        player.Launch(direction, launchForce);

        PlaySound();
        PlayParticles();
    }

    private Vector3 GetLaunchDirection()
    {
        if (usePadDirection && launchDirection != null)
            return launchDirection.forward.normalized;

        return Vector3.up;
    }

    private void PlaySound()
    {
        if (audioSource == null || springSound == null)
            return;

        audioSource.PlayOneShot(springSound, springVolume);
    }

    private void PlayParticles()
    {
        if (springParticles == null)
            return;

        springParticles.Play();
    }
}