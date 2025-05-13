using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }

    [SerializeField] private Animator transitionAnim;

    private AsyncOperation operation;

    private Canvas childCanvas;

    [SerializeField] private PlayFabController playFabController;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        childCanvas = GetComponentInChildren<Canvas>();
        if (childCanvas != null)
        {
            childCanvas.gameObject.SetActive(false);
        }
    }

    // Carga el nivel usando el nombre de la partida para cargar datos en PlayFabProgressManager.
    public void LoadLevelSave(string gameName)
    {
        if (childCanvas != null)
        {
            childCanvas.gameObject.SetActive(true);
        }

        StartCoroutine(LoadSceneSave(gameName));
    }

    private IEnumerator LoadSceneSave(string gameName)
    {
        transitionAnim.SetTrigger("End");
        yield return new WaitForSeconds(1f);

        // Cargar la escena 7
        SceneManager.LoadScene(7);

        transitionAnim.SetTrigger("Start");

        yield return new WaitForSeconds(1f);

        // Cargar la escena de forma asíncrona (ej. escena 2) sin activarla inmediatamente.
        operation = SceneManager.LoadSceneAsync(2);
        operation.allowSceneActivation = false;

        yield return new WaitForSeconds(5f);

        transitionAnim.SetTrigger("End");
        yield return new WaitForSeconds(1f);

        // Esperar hasta que la escena esté casi completamente cargada.
        while (!operation.isDone)
        {
            if (operation.progress >= 0.9f)
            {
                operation.allowSceneActivation = true;
            }
            yield return null;
        }

        yield return new WaitForSeconds(1f);
        transitionAnim.SetTrigger("Start");

        yield return new WaitForSeconds(1f);
        if (childCanvas != null)
        {
            childCanvas.gameObject.SetActive(false);
        }
    }
    public void LoadLevelNewGame()
    {
        if (childCanvas != null)
        {
            childCanvas.gameObject.SetActive(true);
        }
        StartCoroutine(LoadSceneNewGame());
    }
    private IEnumerator LoadSceneNewGame()
    {
        transitionAnim.SetTrigger("End");
        yield return new WaitForSeconds(1f);

        // Cargar la escena 7
        SceneManager.LoadScene(7);

        transitionAnim.SetTrigger("Start");

        yield return new WaitForSeconds(1f);

        // Cargar la siguiente escena de manera asíncrona sin activarla aún
        operation = SceneManager.LoadSceneAsync(2);
        operation.allowSceneActivation = false;

        yield return new WaitForSeconds(7f);

        transitionAnim.SetTrigger("End");
        yield return new WaitForSeconds(1f);

        // Esperar hasta que la escena esté casi completamente cargada
        while (!operation.isDone)
        {
            if (operation.progress >= 0.9f) // Cuando la escena está lista
            {
                operation.allowSceneActivation = true;
            }
            yield return null;
        }

        yield return new WaitForSeconds(1f);
        transitionAnim.SetTrigger("Start");

        yield return new WaitForSeconds(1f);
        if (childCanvas != null)
        {
            childCanvas.gameObject.SetActive(false);
        }
    }

    public void LoadLevelMainMenu()
    {
        if (childCanvas != null)
        {
            childCanvas.gameObject.SetActive(true);
        }
        StartCoroutine(LoadSceneMainMenu());
    }
    private IEnumerator LoadSceneMainMenu()
    {
        transitionAnim.SetTrigger("End");
        yield return new WaitForSeconds(1f);

        // Cargar la escena 7
        SceneManager.LoadScene(7);

        transitionAnim.SetTrigger("Start");

        yield return new WaitForSeconds(1f);

        // Cargar la siguiente escena de manera asíncrona sin activarla aún
        operation = SceneManager.LoadSceneAsync(0);
        operation.allowSceneActivation = false;

        yield return new WaitForSeconds(3f);

        transitionAnim.SetTrigger("End");
        yield return new WaitForSeconds(1f);

        // Esperar hasta que la escena esté casi completamente cargada
        while (!operation.isDone)
        {
            if (operation.progress >= 0.9f) // Cuando la escena está lista
            {
                operation.allowSceneActivation = true;
            }
            yield return null;
        }

        yield return new WaitForSeconds(1f);
        transitionAnim.SetTrigger("Start");

        yield return new WaitForSeconds(1f);
        if (childCanvas != null)
        {
            childCanvas.gameObject.SetActive(false);
        }
    }

    public void LoadLevelJoinRoom(int sceneIndex)
    {
        if (childCanvas != null)
        {
            childCanvas.gameObject.SetActive(true);
        }
        StartCoroutine(LoadSceneJoinRoom(sceneIndex));
    }
    private IEnumerator LoadSceneJoinRoom(int sceneIndex)
    {
        transitionAnim.SetTrigger("End");
        yield return new WaitForSeconds(1f);

        // Cargar la escena 7
        SceneManager.LoadScene(7);

        transitionAnim.SetTrigger("Start");

        yield return new WaitForSeconds(1f);

        // Aquí usas el índice de la escena que recibiste como parámetro
        operation = SceneManager.LoadSceneAsync(sceneIndex);
        operation.allowSceneActivation = false;

        yield return new WaitForSeconds(3f);

        transitionAnim.SetTrigger("End");
        yield return new WaitForSeconds(1f);

        // Esperar hasta que la escena esté casi completamente cargada
        while (!operation.isDone)
        {
            if (operation.progress >= 0.9f) // Cuando la escena está lista
            {
                operation.allowSceneActivation = true;
            }
            yield return null;
        }

        yield return new WaitForSeconds(1f);
        transitionAnim.SetTrigger("Start");

        yield return new WaitForSeconds(1f);
        if (childCanvas != null)
        {
            childCanvas.gameObject.SetActive(false);
        }
    }
    public void LoadLevelGame()
    {
        if (childCanvas != null)
        {
            childCanvas.gameObject.SetActive(true);
        }
        StartCoroutine(LoadSceneGame());
    }
    private IEnumerator LoadSceneGame()
    {
        transitionAnim.SetTrigger("End");
        yield return new WaitForSeconds(1f);

        // Cargar la escena 7
        SceneManager.LoadScene(7);

        transitionAnim.SetTrigger("Start");

        yield return new WaitForSeconds(1f);

        // Cargar la siguiente escena de manera asíncrona sin activarla aún
        operation = SceneManager.LoadSceneAsync(2);
        operation.allowSceneActivation = false;

        yield return new WaitForSeconds(3f);

        transitionAnim.SetTrigger("End");
        yield return new WaitForSeconds(1f);

        // Esperar hasta que la escena esté casi completamente cargada
        while (!operation.isDone)
        {
            if (operation.progress >= 0.9f) // Cuando la escena está lista
            {
                operation.allowSceneActivation = true;
            }
            yield return null;
        }

        yield return new WaitForSeconds(1f);
        transitionAnim.SetTrigger("Start");

        yield return new WaitForSeconds(1f);
        if (childCanvas != null)
        {
            childCanvas.gameObject.SetActive(false);
        }
    }
    public void LoadLevelCorrespondingPuzzle(string sceneName)
    {
        if (childCanvas != null)
        {
            childCanvas.gameObject.SetActive(true);
        }
        StartCoroutine(LoadSceneCorrespondingPuzzle(sceneName));
    }
    private IEnumerator LoadSceneCorrespondingPuzzle(string sceneName)
    {
        transitionAnim.SetTrigger("End");
        yield return new WaitForSeconds(1f);

        // Cargar la escena 7
        SceneManager.LoadScene(7);

        transitionAnim.SetTrigger("Start");

        yield return new WaitForSeconds(1f);

        // Cargar la siguiente escena de manera asíncrona sin activarla aún
        operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        yield return new WaitForSeconds(3f);

        transitionAnim.SetTrigger("End");
        yield return new WaitForSeconds(1f);

        // Esperar hasta que la escena esté casi completamente cargada
        while (!operation.isDone)
        {
            if (operation.progress >= 0.9f) // Cuando la escena está lista
            {
                operation.allowSceneActivation = true;
            }
            yield return null;
        }

        yield return new WaitForSeconds(1f);
        transitionAnim.SetTrigger("Start");

        yield return new WaitForSeconds(1f);
        if (childCanvas != null)
        {
            childCanvas.gameObject.SetActive(false);
        }
    }
}
