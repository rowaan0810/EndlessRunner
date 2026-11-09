// GameManager.cs — Central game state machine.
// Manages Menu → Playing → GameOver transitions, score tracking, and restart flow.

using UnityEngine;
using UnityEngine.Events;

namespace ActiveMotion3D.EndlessRunner.Core
{
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        GameOver
    }

    /// <summary>
    /// Singleton game manager. Controls the overall game flow, score,
    /// and notifies other systems of state changes via events.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Scene Startup")]
        [SerializeField] private bool startAutomatically;
        [SerializeField, Min(0)] private int pointsPerCoin = 10;

        /// <summary>Whether this scene should run the automatic camera/countdown startup flow.</summary>
        public bool StartAutomatically => startAutomatically;

        [Header("Events")]
        public UnityEvent OnGameStart = new UnityEvent();
        public UnityEvent OnGameOver = new UnityEvent();
        public UnityEvent OnGameRestart = new UnityEvent();
        public UnityEvent OnGamePause = new UnityEvent();
        public UnityEvent OnGameResume = new UnityEvent();

        /// <summary>Current game state.</summary>
        public GameState State { get; private set; } = GameState.Menu;

        /// <summary>Time reserved for the final impact effect before showing the results UI.</summary>
        public float GameOverPresentationDelay { get; private set; }

        /// <summary>Distance-based score (meters run).</summary>
        public float DistanceScore { get; private set; }

        /// <summary>Coins collected this run.</summary>
        public int CoinScore { get; private set; }

        /// <summary>Combined score for display.</summary>
        public int TotalScore => Mathf.FloorToInt(DistanceScore) + (CoinScore * pointsPerCoin);

        /// <summary>All-time high score from PlayerPrefs.</summary>
        public int HighScore
        {
            get => PlayerPrefs.GetInt("HighScore", 0);
            private set
            {
                PlayerPrefs.SetInt("HighScore", value);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Time survived this run in seconds.</summary>
        public float TimeSurvived { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Time.timeScale = 1f;
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (State != GameState.Playing) return;

            // Accumulate distance based on current speed
            float speedNow = GameSpeed.Instance != null ? GameSpeed.Instance.Current : 10f;
            DistanceScore += speedNow * Time.deltaTime;
            TimeSurvived += Time.deltaTime;
        }

        /// <summary>
        /// Start a new game run. Called from the main menu.
        /// </summary>
        public void StartGame()
        {
            GameOverPresentationDelay = 0f;
            Time.timeScale = 1f;
            State = GameState.Playing;
            DistanceScore = 0f;
            CoinScore = 0;
            TimeSurvived = 0f;

            if (GameSpeed.Instance != null)
                GameSpeed.Instance.StartRunning();

            OnGameStart?.Invoke();
            Debug.Log("Game Started!");
        }

        /// <summary>
        /// End the current run. Called when the player hits an obstacle.
        /// </summary>
        public void TriggerGameOver() => TriggerGameOver(0f);

        public void TriggerGameOver(float presentationDelay)
        {
            if (State != GameState.Playing) return;

            GameOverPresentationDelay = Mathf.Max(0f, presentationDelay);
            State = GameState.GameOver;

            // Centralized so obstacle collisions and failed Human Tetris gates
            // play the crash sound once, after the Playing-state guard above.
            if (ActiveMotion3D.EndlessRunner.Core.SfxPlayer.Instance != null)
                ActiveMotion3D.EndlessRunner.Core.SfxPlayer.Instance.PlayHit();

            if (GameSpeed.Instance != null)
                GameSpeed.Instance.StopRunning();

            // Update high score
            if (TotalScore > HighScore)
            {
                HighScore = TotalScore;
            }

            OnGameOver?.Invoke();
            Debug.Log($"Game Over! Score: {TotalScore} | Distance: {DistanceScore:F0}m | Coins: {CoinScore}");
        }

        public void PauseGame()
        {
            if (State != GameState.Playing) return;
            State = GameState.Paused;
            Time.timeScale = 0f;
            OnGamePause?.Invoke();
        }

        public void ResumeGame()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            State = GameState.Playing;
            OnGameResume?.Invoke();
        }

        /// <summary>
        /// Restart the game. Resets all state and starts a new run.
        /// </summary>
        public void RestartGame()
        {
            Time.timeScale = 1f;
            if (GameSpeed.Instance != null)
                GameSpeed.Instance.ResetSpeed();

            OnGameRestart?.Invoke();
            StartGame();
        }

        /// <summary>
        /// Return to main menu from game over.
        /// </summary>
        public void ReturnToMenu()
        {
            Time.timeScale = 1f;
            State = GameState.Menu;

            if (GameSpeed.Instance != null)
                GameSpeed.Instance.ResetSpeed();
        }

        /// <summary>
        /// Add coins to the current score. Called by Coin collectibles.
        /// </summary>
        public void AddCoin(int count = 1)
        {
            CoinScore += count;
        }
    }
}
