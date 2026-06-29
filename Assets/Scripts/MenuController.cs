using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Управление кнопками главного меню.
/// Повесить на любой объект в сцене Menu, затем привязать методы к OnClick кнопок.
/// </summary>
public class MenuController : MonoBehaviour
{
    public void OnStartClicked()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void OnExitClicked()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}