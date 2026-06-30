using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Управление меню выхода: показывает/скрывает BackgroundExit по кнопке ButtonExitMenu.
/// </summary>
public class ExitMenuController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject backgroundExit;
    [SerializeField] private Button buttonExitMenu;
    [SerializeField] private Button buttonContinue;
    [SerializeField] private Button buttonExit;

    [Header("Pause References")]
    [SerializeField] private TimerController timerController;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private NeighborController neighborController;

    void Start()
    {
        if (backgroundExit != null)
            backgroundExit.SetActive(false);

        if (buttonExitMenu != null)
            buttonExitMenu.onClick.AddListener(OnExitMenuClicked);

        if (buttonContinue != null)
            buttonContinue.onClick.AddListener(OnContinueClicked);

        if (buttonExit != null)
            buttonExit.onClick.AddListener(OnExitClicked);
    }

    private void OnExitMenuClicked()
    {
        if (backgroundExit != null)
            backgroundExit.SetActive(true);

        // Поставить игру на паузу
        if (timerController != null)
            timerController.Pause();

        if (playerController != null)
            playerController.enabled = false;

        if (neighborController != null)
            neighborController.enabled = false;

        Time.timeScale = 0f;
    }

    private void OnContinueClicked()
    {
        // Выход из игры
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void OnExitClicked()
    {
        // Возврат на сцену (скрываем меню)
        if (backgroundExit != null)
            backgroundExit.SetActive(false);

        // Продолжить игру
        Time.timeScale = 1f;

        if (timerController != null)
            timerController.Play();

        if (playerController != null)
            playerController.enabled = true;

        if (neighborController != null)
            neighborController.enabled = true;
    }
}
