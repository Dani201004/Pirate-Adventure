public interface ITimeProvider
{
    float GetTimeRemaining();
    bool IsTimeRunning();
    void StartTimerAfterDialogue(); // Agregar este m�todo a la interfaz
}
