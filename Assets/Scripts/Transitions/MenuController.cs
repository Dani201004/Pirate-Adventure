using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;


public class MenuController : MonoBehaviour
{
    // Variable para manejar el cooldown
    private bool isCooldownActive = false;
    private float cooldownTime = 3f; // Tiempo en segundos para el cooldown

    // Variable estática que controla si el juego está pausado
    public static bool IsPaused = false;

    // Volver al menú principal
    public void MainMenuGame()
    {
        if (isCooldownActive)
        {
            Debug.Log("Cooldown activo. Por favor, espera.");
            return; // Si el cooldown está activo, no hace nada
        }

        // Activamos el cooldown
        isCooldownActive = true;

        // Llamada para cargar el menú principal
        SceneTransition.Instance.LoadLevelMainMenu();

        // Limpiar los datos de conexión y grupo compartido
        PlayFabController.Instance.RemoveConnectionString();
        PlayFabController.Instance.RemoveSharedGroupId();

        // Dejar el lobby
        PlayFabController.Instance.LeaveLobby();

        // Desactivamos el cooldown después de realizar la operación
        StartCoroutine(ResetCooldown());
    }

    // Coroutine para resetear el cooldown
    private IEnumerator ResetCooldown()
    {
        yield return new WaitForSeconds(cooldownTime); // Esperamos el tiempo del cooldown
        isCooldownActive = false; // Desactivamos el cooldown
    }

    public void MainMenu()
    {
        SceneManager.LoadScene(0);
    }

    //ir al menú de partidas guardadas
    public void Continue()
    {
        SceneManager.LoadScene(1);
    }
    //Ir al menú de opciones
    public void Settings()
    {
        SceneManager.LoadScene(5);
    }
    //Ir al menú de control parental
    public void ParentalControl()
    {
        SceneManager.LoadScene(9);
    }
    //Ver los creditos
    public void HowToPlay()
    {
        SceneManager.LoadScene(4);
    }
    //Personalizar tu avatar
    public void Avatar()
    {
        SceneManager.LoadScene(3);
    }
    //Ir al menú de amigos
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
    //Menú de pausa
    public void Pause()
    {
        Time.timeScale = 0f;
        IsPaused = true;  // Marcar como pausado
    }

    //Cerrar menú de pausa
    public void Unpause()
    {
        Time.timeScale = 1f;
        IsPaused = false;  // Marcar como reanudado
    }

}
