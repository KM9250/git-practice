using UnityEngine;
using TMPro;

namespace VRRoom.UI
{
    /// <summary>
    /// Small HUD panel on the right side showing:
    ///   - Current mode name
    ///   - Elapsed time (MM:SS)
    ///   - Optional countdown bar if a target duration is set
    ///
    /// Setup in Editor:
    ///   - Place at right ~30° off forward, ~1.2 m height, ~1.2 m away.
    ///   - Assign modeLabel, timerLabel, and (optionally) progressBar.
    ///   - Canvas in World Space, small (e.g. 0.3 × 0.15 m).
    /// </summary>
    public class TimerHUD : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────
        [SerializeField] private TMP_Text  modeLabel;
        [SerializeField] private TMP_Text  timerLabel;
        [SerializeField] private UnityEngine.UI.Image progressBar; // optional fill image

        [Header("Target session duration in minutes (0 = unlimited)")]
        [SerializeField] private float targetMinutes = 25f; // Pomodoro-style default

        // ── State ────────────────────────────────────────────────────────────
        private float _elapsed;
        private bool  _running;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Start()
        {
            if (VRRoom.Core.ModeManager.Instance != null)
                VRRoom.Core.ModeManager.Instance.onModeChanged.AddListener(OnModeChanged);
            UpdateLabels();
        }

        private void OnDestroy()
        {
            if (VRRoom.Core.ModeManager.Instance != null)
                VRRoom.Core.ModeManager.Instance.onModeChanged.RemoveListener(OnModeChanged);
        }

        private void Update()
        {
            if (!_running) return;
            _elapsed += Time.deltaTime;
            UpdateLabels();
        }

        // ── Public API ───────────────────────────────────────────────────────
        public void Show()       => gameObject.SetActive(true);
        public void Hide()       => gameObject.SetActive(false);

        public void StartTimer() { _elapsed = 0f; _running = true; }
        public void StopTimer()  { _running = false; }

        public void SetTargetMinutes(float minutes) { targetMinutes = minutes; }

        // ── Helpers ──────────────────────────────────────────────────────────
        private void UpdateLabels()
        {
            int mins = Mathf.FloorToInt(_elapsed / 60f);
            int secs = Mathf.FloorToInt(_elapsed % 60f);

            if (timerLabel != null)
                timerLabel.text = $"{mins:00}:{secs:00}";

            if (progressBar != null && targetMinutes > 0f)
                progressBar.fillAmount = Mathf.Clamp01(_elapsed / (targetMinutes * 60f));
        }

        private void OnModeChanged(VRRoom.Core.ModeManager.RoomMode mode)
        {
            if (modeLabel != null) modeLabel.text = mode.ToString().ToUpper();
        }
    }
}
