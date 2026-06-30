using UnityEngine;
using TMPro;

/// <summary>
/// Обратный таймер: отсчитывает время от заданного значения до нуля.
/// Время настраивается в редакторе, текст выводится в ноду TextTimer.
/// </summary>
public class TimerController : MonoBehaviour
{
    [Header("Настройки таймера")]
    [SerializeField] private float totalTime = 60f;            // общее время в секундах
    [SerializeField] private bool countDown = true;             // true — обратный отсчёт, false — прямой
    [SerializeField] private bool startOnAwake = true;          // запускать автоматически при старте сцены
    [SerializeField] private bool loop = false;                 // зациклить таймер

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverMenu;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private NeighborController neighborController;

    [Header("UI")]
    [SerializeField] private TMP_Text textTimer;                // ссылка на TMP_Text для отображения
    [SerializeField] private string timeFormat = "mm\\:ss";     // формат времени (mm:ss)

    private float _currentTime;
    private bool _isRunning;

    public float CurrentTime => _currentTime;
    public bool IsRunning => _isRunning;
    public bool IsFinished => countDown ? _currentTime <= 0f : _currentTime >= totalTime;

    private void Awake()
    {
        _currentTime = countDown ? totalTime : 0f;
        _isRunning = startOnAwake;
        UpdateDisplay();
    }

    private void Start()
    {
        // Найти кнопку Retry внутри GameOverMenu и подписать на перезапуск
        if (gameOverMenu != null)
        {
            var retryButton = gameOverMenu.GetComponentInChildren<UnityEngine.UI.Button>();
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(RestartGame);
            }
        }
    }

    private void Update()
    {
        if (!_isRunning) return;

        if (countDown)
        {
            _currentTime -= Time.deltaTime;
            if (_currentTime <= 0f)
            {
                _currentTime = 0f;
                UpdateDisplay();

                if (loop)
                {
                    _currentTime = totalTime;
                }
                else
                {
                    _isRunning = false;
                    OnTimerFinished();
                    return;
                }
            }
        }
        else
        {
            _currentTime += Time.deltaTime;
            if (_currentTime >= totalTime)
            {
                _currentTime = totalTime;
                UpdateDisplay();

                if (loop)
                {
                    _currentTime = 0f;
                }
                else
                {
                    _isRunning = false;
                    OnTimerFinished();
                    return;
                }
            }
        }

        UpdateDisplay();
    }

    private void OnTimerFinished()
    {
        // Показать меню проигрыша
        if (gameOverMenu != null)
        {
            gameOverMenu.SetActive(true);
        }

        // Отключить управление игрока
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // Отключить управление соседа
        if (neighborController != null)
        {
            neighborController.enabled = false;
        }
    }

    /// <summary>Перезапустить игру (загрузить текущую сцену заново).</summary>
    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    private void UpdateDisplay()
    {
        if (textTimer == null) return;

        var display = _currentTime;
        var minutes = Mathf.FloorToInt(display / 60f);
        var seconds = Mathf.FloorToInt(display % 60f);

        textTimer.text = $"{minutes:D2}:{seconds:D2}";
    }

    /// <summary>Запустить таймер.</summary>
    public void Play()
    {
        _isRunning = true;
    }

    /// <summary>Поставить на паузу.</summary>
    public void Pause()
    {
        _isRunning = false;
    }

    /// <summary>Остановить и сбросить к начальному значению.</summary>
    public void Stop()
    {
        _isRunning = false;
        _currentTime = countDown ? totalTime : 0f;
        UpdateDisplay();
    }

    /// <summary>Переключить пауза/игра.</summary>
    public void Toggle()
    {
        if (_isRunning)
            Pause();
        else
            Play();
    }

    /// <summary>Установить время вручную (в секундах) и обновить UI.</summary>
    public void SetTime(float seconds)
    {
        _currentTime = Mathf.Clamp(seconds, 0f, totalTime);
        UpdateDisplay();
    }

    /// <summary>Перезапустить с новым totalTime (обновляет totalTime и сбрасывает таймер).</summary>
    public void RestartWithNewTime(float newTotalSeconds)
    {
        totalTime = newTotalSeconds;
        _currentTime = countDown ? totalTime : 0f;
        _isRunning = true;
        UpdateDisplay();
    }
}