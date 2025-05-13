public interface ITimeProvider
{
    float GetTimeRemaining();
    bool IsTimeRunning();
    void StartTimerAfterDialogue(); // Agregar este método a la interfaz
}
