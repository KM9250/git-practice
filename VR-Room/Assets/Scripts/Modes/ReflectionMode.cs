using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRRoom.Audio;
using VRRoom.Integration;
using VRRoom.UI;

namespace VRRoom.Modes
{
    /// <summary>
    /// Reflection mode controller.
    ///
    /// Reuses the same physical room layout as Focus mode; only lighting and
    /// panel content differ.  Typically entered after a Focus session ends.
    ///
    /// Scene requirements:
    ///   - Assign this component to ReflectionRoot (a child of FocusRoot or a
    ///     separate root that shares the same room mesh via shared material swap).
    ///   - Children:
    ///       • SummaryMonitorPanel: forward, shows AI summary text
    ///       • TimelinePanel: left wall, lists mode segments
    ///       • UserNotesPanel: right side or same monitor, shows external notes
    ///       • AIAvatarSeated: avatar mesh sitting in the desk-facing chair
    ///       • Warm directional light (override)
    ///
    /// Colour palette (warm shift):
    ///   Ambient  #FFF3E0   Walls  #C8B89A   Light temperature ~3000 K
    ///   Accent   #FF8A65
    /// </summary>
    public class ReflectionMode : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────
        [Header("Panel references")]
        [SerializeField] private TMP_Text       summaryText;      // AI summary
        [SerializeField] private TMP_Text       timelineText;     // session timeline
        [SerializeField] private TMP_Text       userNotesText;    // external notes

        [Header("Avatar")]
        [SerializeField] private GameObject     aiAvatarSeated;   // sitting across desk

        [Header("Scene lighting")]
        [SerializeField] private Light          sceneLight;
        [SerializeField] private Color          lightColor     = new Color(1.00f, 0.85f, 0.60f);
        [SerializeField] private float          lightIntensity = 0.55f;

        [Header("Audio")]
        [SerializeField] private AmbientAudioManager ambientAudio;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void OnEnable()
        {
            ApplyLighting();
            RefreshTimeline();

            if (aiAvatarSeated != null) aiAvatarSeated.SetActive(true);
            ambientAudio?.Play();

            AIVoiceHook.Instance?.Speak(AIVoiceHook.Cue.ModeStart, VRRoom.Core.ModeManager.RoomMode.Reflection);
        }

        private void OnDisable()
        {
            if (aiAvatarSeated != null) aiAvatarSeated.SetActive(false);
            ambientAudio?.Stop();

            AIVoiceHook.Instance?.Speak(AIVoiceHook.Cue.ModeEnd, VRRoom.Core.ModeManager.RoomMode.Reflection);
        }

        // ── Public API (called by external AI / data pipeline) ───────────────

        /// <summary>Set the AI-generated daily summary (called from external system or AIVoiceHook).</summary>
        public void SetSummary(string text)
        {
            if (summaryText != null) summaryText.text = text;
        }

        /// <summary>Set freeform notes received from external pipeline (clipboard, REST, etc.).</summary>
        public void SetUserNotes(string notes)
        {
            if (userNotesText != null) userNotesText.text = notes;
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private void ApplyLighting()
        {
            if (sceneLight == null) return;
            sceneLight.color     = lightColor;
            sceneLight.intensity = lightIntensity;
        }

        /// <summary>Reads the session log from SessionManager and formats a timeline string.</summary>
        private void RefreshTimeline()
        {
            if (timelineText == null) return;

            var session = VRRoom.Core.SessionManager.Instance;
            if (session == null || session.Segments.Count == 0)
            {
                timelineText.text = "— No session data —";
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var seg in session.Segments)
            {
                sb.AppendLine(
                    $"{seg.startTime:HH:mm}  [{seg.mode}]  " +
                    $"{Mathf.RoundToInt(seg.durationSeconds / 60)} min" +
                    (string.IsNullOrEmpty(seg.note) ? "" : $"  — {seg.note}")
                );
            }
            timelineText.text = sb.ToString();
        }
    }
}
