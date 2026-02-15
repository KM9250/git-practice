using UnityEngine;
using TMPro;
using System.Collections;

namespace VRRoom.UI
{
    /// <summary>
    /// AI avatar hologram UI.
    ///
    /// In Focus mode: small hologram, top-left corner of room, floating.
    ///   Position: ~(-1.5, 2.0, 1.5) from seated origin (left-forward-up).
    ///   Scale: ~0.35 (miniature standing figure).
    ///
    /// In Reflection mode: full-size seated avatar across the desk — managed
    ///   separately via a dedicated GameObject (see ReflectionMode.aiAvatarSeated).
    ///
    /// Speech bubble:
    ///   - SpeechBubblePanel fades in when text is set, auto-hides after duration.
    ///   - External systems call SetSpeechText() to push lines.
    ///
    /// Hologram shader:
    ///   - Apply a simple scanline/emissive material to the avatar mesh.
    ///   - Suggested shader: URP/Unlit with custom scanline texture + alpha flickering.
    /// </summary>
    public class AIAvatarUI : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────
        [Header("Speech bubble")]
        [SerializeField] private GameObject speechBubblePanel;
        [SerializeField] private TMP_Text   speechText;
        [SerializeField] private float      speechDisplaySeconds = 5f;

        [Header("Idle animation")]
        [SerializeField] private Animator   avatarAnimator;
        [SerializeField] private string     idleStateName = "Idle";

        [Header("Hologram flicker")]
        [SerializeField] private Renderer   holoRenderer;
        [SerializeField, Range(0f, 0.05f)]
        private float                        flickerAmplitude = 0.015f;
        [SerializeField] private float       flickerSpeed     = 8f;

        // ── State ────────────────────────────────────────────────────────────
        private Coroutine _hideSpeechCoroutine;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            if (speechBubblePanel != null) speechBubblePanel.SetActive(false);
        }

        private void Update()
        {
            ApplyHologramFlicker();
        }

        // ── Public API ───────────────────────────────────────────────────────
        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        /// <summary>
        /// Push a line of spoken text to the speech bubble.
        /// Called by AIVoiceHook when TTS begins.
        /// </summary>
        public void SetSpeechText(string line)
        {
            if (speechText != null)       speechText.text = line;
            if (speechBubblePanel != null) speechBubblePanel.SetActive(true);

            if (_hideSpeechCoroutine != null) StopCoroutine(_hideSpeechCoroutine);
            _hideSpeechCoroutine = StartCoroutine(HideSpeechAfterDelay(speechDisplaySeconds));
        }

        /// <summary>Clear speech bubble immediately.</summary>
        public void ClearSpeech()
        {
            if (_hideSpeechCoroutine != null) StopCoroutine(_hideSpeechCoroutine);
            if (speechBubblePanel != null)    speechBubblePanel.SetActive(false);
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private IEnumerator HideSpeechAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (speechBubblePanel != null) speechBubblePanel.SetActive(false);
        }

        private void ApplyHologramFlicker()
        {
            if (holoRenderer == null) return;

            float alpha = 1f - flickerAmplitude * (0.5f + 0.5f * Mathf.Sin(Time.time * flickerSpeed));
            Color c = holoRenderer.material.color;
            c.a = alpha;
            holoRenderer.material.color = c;
        }
    }
}
