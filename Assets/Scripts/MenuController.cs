using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Управление кнопками главного меню.
/// Повесить на любой объект в сцене Menu, затем привязать методы к OnClick кнопок.
/// </summary>
public class MenuController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip clickSound;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void OnStartClicked()
    {
        if (clickSound != null)
            StartCoroutine(PlayClickAndExecute(() => SceneManager.LoadScene("MainScene")));
        else
            SceneManager.LoadScene("MainScene");
    }

    public void OnExitClicked()
    {
        if (clickSound != null)
            StartCoroutine(PlayClickAndExecute(() => QuitGame()));
        else
            QuitGame();
    }

    private void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private IEnumerator PlayClickAndExecute(System.Action action)
    {
        audioSource.PlayOneShot(clickSound);
        yield return new WaitForSeconds(clickSound.length);
        action?.Invoke();
    }
}
