using UnityEngine;

public class Chest : MonoBehaviour
{
    public string Color; // Color del cofre
    public Animator chestAnimator; // Animator del cofre
    public AudioClip openSound; // Clip de sonido
    private AudioSource audioSource; // Componente AudioSource
    public ParticleSystem particles; // Sistema de partículas

    public bool isOpened = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>(); // Obtener AudioSource
    }

    public void Open()
    {
        if (isOpened) return; // Evita que se vuelva a abrir
        isOpened = true;

        chestAnimator.SetTrigger("Open");
        PlaySound();
        TriggerParticles();
    }

    private void PlaySound()
    {
        audioSource.PlayOneShot(openSound); // Reproducir el sonido
    }

    private void TriggerParticles()
    {
        particles.Play(); // Reproducir partículas
    }

    public void SyncOpenEffects()
    {
        Open(); // Reutilizamos Open(), que ya evita duplicados
    }
}