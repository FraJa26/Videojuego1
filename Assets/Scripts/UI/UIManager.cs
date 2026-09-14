using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private Text coinsText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text livesText;
    [SerializeField] private Text timeText;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject gameOverPanel;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        if (coinsText != null) coinsText.text = "Monedas: " + GameManager.Instance.Coins;
        if (scoreText != null) scoreText.text = "Puntaje: " + GameManager.Instance.Score;
        if (livesText != null) livesText.text = "Vidas: " + GameManager.Instance.Lives;
        if (timeText != null) timeText.text = "Tiempo: " + Mathf.FloorToInt(GameManager.Instance.ElapsedTime) + "s";
    }

    public void ShowWin()
    {
        if (winPanel != null) winPanel.SetActive(true);
    }

    public void ShowGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }
}
