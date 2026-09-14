using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private int startingLives = 3;

    public int Coins { get; private set; }
    public int Score { get; private set; }
    public int Lives { get; private set; }
    public float ElapsedTime { get; private set; }
    public bool LevelEnded { get; private set; }

    private Vector3 spawnPoint;
    private bool spawnPointSet;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Lives = startingLives;
    }

    private void Update()
    {
        if (!LevelEnded)
        {
            ElapsedTime += Time.deltaTime;
        }
        else if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space))
        {
            RestartLevel();
        }
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void SetSpawnPoint(Vector3 position)
    {
        if (spawnPointSet) return;
        spawnPoint = position;
        spawnPointSet = true;
    }

    public void AddCoin(int amount = 1)
    {
        if (LevelEnded) return;
        Coins += amount;
        AddScore(amount * 10);
    }

    public void AddScore(int amount)
    {
        if (LevelEnded) return;
        Score += amount;
    }

    public void LoseLife()
    {
        if (LevelEnded) return;

        Lives--;
        if (Lives <= 0)
        {
            Lives = 0;
            GameOver();
        }
        else
        {
            PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>();
            if (player != null && spawnPointSet)
            {
                player.Respawn(spawnPoint);
            }
        }
    }

    public void GameOver()
    {
        if (LevelEnded) return;
        LevelEnded = true;
        UIManager.Instance?.ShowGameOver();
    }

    public void WinLevel()
    {
        if (LevelEnded) return;
        LevelEnded = true;
        AudioManager.Instance?.PlayWin();
        UIManager.Instance?.ShowWin();
    }
}
