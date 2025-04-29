using UnityEngine;

public class Chest : MonoBehaviour
{
    public string Color; // Color del cofre
    [SerializeField] private Animator chestAnimator; // Animator del cofre
    [SerializeField] private AudioClip openSound; // Clip de sonido
    [SerializeField] private AudioClip rejectSound; // Clip de sonido para la llave incorrecta
    [SerializeField] private AudioSource audioSource; // Componente AudioSource
    [SerializeField] private ParticleSystem particles; // Sistema de partículas

    public bool isOpened = false; // Estado de si el cofre está abierto

    void Start()
    {
        audioSource = GetComponent<AudioSource>(); // Obtener AudioSource
    }

    public void Open()
    {
        if (isOpened) return; // Evita que se vuelva a abrir
        isOpened = true;

        chestAnimator.SetTrigger("Open"); // Activar animación de apertura
        PlaySound(openSound); // Reproducir sonido de apertura
        TriggerParticles(); // Activar partículas

        ChestManager.Instance.OnChestOpened();
    }
    public void RejectKey()
    {
        if (isOpened) return; // No hacer nada si ya está abierto

        // Llamar al método de agitación del objeto
        StartShake(); // Inicia la sacudida

        Debug.Log("Llave incorrecta usada en cofre de color: " + Color);

        // Reproducir sonido de error al usar llave incorrecta
        PlaySound(rejectSound);
    }
    public void StartShake()
    {
        // Asegúrate de que tienes un script ObjectShake en el mismo GameObject o asigna el script de agitación
        ChestShake shakeScript = GetComponent<ChestShake>();

        if (shakeScript != null)
        {
            shakeScript.StartShake(); // Inicia la sacudida del cofre
        }
        else
        {
            Debug.LogWarning("El script ObjectShake no está asignado.");
        }
    }
    private void PlaySound(AudioClip sound)
    {
        audioSource.PlayOneShot(sound); // Reproducir el sonido pasado como parámetro
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