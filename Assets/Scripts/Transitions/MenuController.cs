using PlayFab.MultiplayerModels;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    //Volver al men� principal
    public void MainMenuGame()
    {
        SceneTransition.Instance.LoadLevelMainMenu();

        PlayFabController.Instance.RemoveConnectionString();
        PlayFabController.Instance.RemoveSharedGroupId();

        PlayFabController.Instance.LeaveLobby();
    }

    public void MainMenu()
    {
        SceneManager.LoadScene(0);
    }

    //ir al men� de partidas guardadas
    public void Continue()
    {
        SceneManager.LoadScene(1);
    }
    //Ir al menu de opciones
    public void Settings()
    {
        SceneManager.LoadScene(5);
    }
    //Ver los creditos
    public void Credits()
    {
        SceneManager.LoadScene(4);
    }
    //Personalizar tu avatar
    public void Avatar()
    {
        SceneManager.LoadScene(3);
    }
    //Ir al men� de amigos
    public void Social()
    {
        SceneManager.LoadScene(6);
    }

    //Salir del puzzle
    public void ExitLevel()
    {
        SceneTransition.Instance.LoadLevelGame();

        Unpause();
    }
    //Men� de pausa
    public void Pause()
    {
        Time.timeScale = 0f;
    }
    
    //Cerrar men� de pausa
    public void Unpause()
    {
        Time.timeScale = 1f;
    }

}
