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
    }
}
