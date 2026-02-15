using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using VRRoom.UI;

namespace VRRoom.Integration
{
    /// <summary>
    /// Hook point for AI voice integration.
    ///
    /// This class is intentionally decoupled from any specific TTS/AI backend.
    /// External systems connect by:
    ///   (a) Subscribing to the UnityEvents exposed here, OR
    ///   (b) Implementing IAIVoiceBackend and assigning it at runtime.
    ///
    /// Built-in fallback: plays a local AudioClip if no backend is attached.
    ///
    /// Integration examples:
    ///   - Unity Sentis (local LLM/TTS)
    ///   - REST call to Claude / OpenAI TTS API (send cue, receive audio bytes)
    ///   - WebSocket stream from a companion Python process
    ///
    /// How to connect a backend at runtime:
    ///     AIVoiceHook.Instance.SetBackend(new MyCustomBackend());
    /// </summary>
    public class AIVoiceHook : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        public static AIVoiceHook Instance { get; private set; }

        // ── Cue enum ─────────────────────────────────────────────────────────
        public enum Cue { ModeStart, ModeEnd, Custom }

        // ── Backend interface ────────────────────────────────────────────────
        public interface IAIVoiceBackend
        {
            /// <summary>Called when the game wants TTS spoken.  Implementations should
            /// eventually call AIVoiceHook.Instance.OnSpeechReady() with a clip or text.</summary>
            void RequestSpeech(Cue cue, VRRoom.Core.ModeManager.RoomMode mode, string customText);
        }

        // ── Inspector ────────────────────────────────────────────────────────
        [Header("Optional fallback clips (played when no backend is set)")]
        [SerializeField] private AudioClip fallbackModeStart;
        [SerializeField] private AudioClip fallbackModeEnd;
        [SerializeField] private AudioSource audioSource;

        [Header("Reference to AI avatar UI for speech bubble")]
        [SerializeField] private AIAvatarUI avatarUI;

        // ── Events (subscribe from Inspector or code) ─────────────────────────
        [Serializable] public class SpeakEvent : UnityEvent<string> {}
        [Header("Fired with display text whenever TTS is triggered")]
        public SpeakEvent onSpeakText;

        // ── State ────────────────────────────────────────────────────────────
        private IAIVoiceBackend _backend;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (audioSource == null) audioSource = GetComponent<AudioSource>();
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Attach a custom TTS/AI backend at runtime.</summary>
        public void SetBackend(IAIVoiceBackend backend) => _backend = backend;

        /// <summary>Trigger a cue.  Called by mode controllers on Enable/Disable.</summary>
        public void Speak(Cue cue, VRRoom.Core.ModeManager.RoomMode mode, string customText = "")
        {
            if (_backend != null)
            {
                _backend.RequestSpeech(cue, mode, customText);
                return;
            }

            // Fallback: play local clip + show mode name
            PlayFallback(cue);
            string label = cue == Cue.Custom ? customText : $"{cue} — {mode}";
            DeliverText(label);
        }

        /// <summary>
        /// Called by the backend implementation when audio is ready (AudioClip path).
        /// </summary>
        public void OnSpeechReady(AudioClip clip, string displayText)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
            DeliverText(displayText);
        }

        /// <summary>
        /// Called by the backend when only text is available (no audio).
        /// </summary>
        public void OnSpeechText(string text) => DeliverText(text);

        // ── Helpers ──────────────────────────────────────────────────────────
        private void PlayFallback(Cue cue)
        {
            if (audioSource == null) return;
            AudioClip clip = cue == Cue.ModeStart ? fallbackModeStart : fallbackModeEnd;
            if (clip != null) audioSource.PlayOneShot(clip);
        }

        private void DeliverText(string text)
        {
            onSpeakText?.Invoke(text);
            avatarUI?.SetSpeechText(text);
        }
    }
}
