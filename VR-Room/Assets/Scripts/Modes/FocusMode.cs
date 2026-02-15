using UnityEngine;
using VRRoom.Audio;
using VRRoom.Integration;
using VRRoom.UI;

namespace VRRoom.Modes
{
    /// <summary>
    /// Focus mode controller.
    ///
    /// Scene requirements (set up in Editor):
    ///   - Assign this component to the FocusRoot GameObject.
    ///   - FocusRoot should contain:
    ///       • Directional/Ambient light (neutral cool-white, intensity ~0.6)
    ///       • Room mesh: grey walls (#B0B0B0), grey floor (#888888)
    ///       • Desk mesh at realistic height (~0.75 m)
    ///       • VirtualMonitorPanel(s): 1–3, positioned above the desk surface
    ///         (see VRDesktopBridge for capture setup)
    ///       • TaskBoardPanel: left side, ~45° from forward, ~1.5 m height
    ///       • TimerHUD: right side, small, floating at eye level
    ///       • AIAvatarUI: top-left corner of the room, 2–3 m up, hologram style
    ///       • StartButton / EndButton
    ///
    /// Colour palette:
    ///   Walls    #B8B8B8   Floor    #888888   Desk #5C4033 (dark wood, optional)
    ///   Monitor  #1A1A2E   Accent   #4FC3F7
    /// </summary>
    public class FocusMode : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────
        [Header("Child component references")]
        [SerializeField] private TimerHUD       timerHUD;
        [SerializeField] private TaskBoard      taskBoard;
        [SerializeField] private AIAvatarUI     aiAvatar;
        [SerializeField] private VRDesktopBridge desktopBridge;
        [SerializeField] private AmbientAudioManager ambientAudio;  // optional (silence is fine)

        [Header("Scene lighting")]
        [SerializeField] private Light sceneLight;
        [SerializeField] private Color  lightColor     = new Color(0.85f, 0.90f, 1.00f);
        [SerializeField] private float  lightIntensity = 0.60f;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void OnEnable()
        {
            ApplyLighting();

            timerHUD?.Show();
            taskBoard?.Show();
            aiAvatar?.Show();
            desktopBridge?.StartCapture();
            ambientAudio?.Play();

            AIVoiceHook.Instance?.Speak(AIVoiceHook.Cue.ModeStart, ModeManager.Core.RoomMode.Focus);
        }

        private void OnDisable()
        {
            timerHUD?.Hide();
            taskBoard?.Hide();
            aiAvatar?.Hide();
            desktopBridge?.StopCapture();
            ambientAudio?.Stop();

            AIVoiceHook.Instance?.Speak(AIVoiceHook.Cue.ModeEnd, ModeManager.Core.RoomMode.Focus);
        }

        // ── Public API (wired to Start/End buttons in Editor) ────────────────
        public void OnStartPressed()
        {
            if (SessionManager.Core.Instance != null && !SessionManager.Core.Instance.IsRunning)
                SessionManager.Core.Instance.StartSession();

            timerHUD?.StartTimer();
            Debug.Log("[FocusMode] Session started");
        }

        public void OnEndPressed()
        {
            timerHUD?.StopTimer();
            SessionManager.Core.Instance?.EndSession();
            Debug.Log("[FocusMode] Session ended");
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private void ApplyLighting()
        {
            if (sceneLight == null) return;
            sceneLight.color     = lightColor;
            sceneLight.intensity = lightIntensity;
        }
    }

    // Alias so Modes scripts can reference Core types without long namespaces
    internal static class ModeManager
    {
        internal static class Core
        {
            internal enum RoomMode { None, Recovery, Focus, Reflection }
        }
    }

    internal static class SessionManager
    {
        internal static class Core
        {
            internal static VRRoom.Core.SessionManager Instance =>
                VRRoom.Core.SessionManager.Instance;
        }
    }
}
