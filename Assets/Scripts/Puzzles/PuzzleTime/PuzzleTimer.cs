using UnityEngine;
using System.Collections;

public class PuzzleTimer : MonoBehaviour
{
    [Header("Referencia al administrador de gameobjects del apartado puzzles")]
    [SerializeField] private PuzzlesUIManager puzzlesUIManager;

    [SerializeField] private float delayBeforePanel = 4f;

    private bool isTimerStarted = false;
    private bool hasTimedOut = false;

    private ITimeProvider timeProvider;
    public void SetTimeProvider(ITimeProvider provider)
    {
        timeProvider = provider;
    }
    private void Start()
    {
        puzzlesUIManager.TimerText.gameObject.SetActive(false);
        puzzlesUIManager.TimeOutText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (timeProvider == null || !timeProvider.IsTimeRunning())
            return;

        float timeRemaining = timeProvider.GetTimeRemaining();

        if (timeRemaining > 0)
        {
            if (!isTimerStarted)
            {
                isTimerStarted = true;
                puzzlesUIManager.TimerText.gameObject.SetActive(true);
                puzzlesUIManager.TimeOutText.gameObject.SetActive(false);
            }

            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            puzzlesUIManager.TimerText.text = $"{minutes:00}:{seconds:00}";
        }
        else if (isTimerStarted && !hasTimedOut)
        {
            hasTimedOut = true;
            OnTimeOutHandler();
        }
    }

    private void OnTimeOutHandler()
    {
        puzzlesUIManager.PauseButton.gameObject.SetActive(false);
        puzzlesUIManager.TimeOutText.text = GetTimeOutMessage();
        puzzlesUIManager.TimeOutText.gameObject.SetActive(true);

        StartCoroutine(ShowRetryPanelAfterDelay());
    }

    private IEnumerator ShowRetryPanelAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforePanel);

        puzzlesUIManager.TimeOutText.gameObject.SetActive(false);
        puzzlesUIManager.TimerText.gameObject.SetActive(false);

        if (puzzlesUIManager.RetryPanel != null)
        {
            puzzlesUIManager.RetryPanel.SetActive(true);

            if (!string.IsNullOrEmpty(PlayFabController.Instance.sharedGroupId))
                PlayFabController.Instance.CheckAllPlayersVotedRetry();
            else
                puzzlesUIManager.RetryVoteText.text = "";
        }
    }

    private string GetTimeOutMessage()
    {
        int currentLanguage = PlayerPrefs.GetInt("LocaleKey", 0);

        switch (currentLanguage)
        {
            case 1:
                return "¡Se acabó el tiempo!";
            case 0:
            default:
                return "Time's up!";
        }
    }
}
