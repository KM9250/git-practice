using UnityEngine;
using VRRoom.Audio;
using VRRoom.Integration;
using VRRoom.UI;

namespace VRRoom.Modes
{
    /// <summary>
    /// Recovery mode controller.
    ///
    /// Scene requirements (set up in Editor):
    ///   - Assign this component to the RecoveryRoot GameObject.
    ///   - RecoveryRoot should contain:
    ///       • Directional/Ambient light (dark blue-purple, intensity ~0.15)
    ///       • Skybox or background mesh (stars or pre-dawn lake)
    ///       • BreathingGuide sphere (child, ~2 m forward, eye height)
    ///       • ExitButton (child, ~0.6 m forward, hand height, ~0.9 m down)
    ///       • AmbientAudioManager component on a child AudioSource
    ///
    /// Colour palette (set in materials / light inspector):
    ///   Deep sky   #0D1B2A    Fog blue   #1B3A4B
    ///   Accent     #2D6A4F    Dim star   #A8DADC
    ///   All HSV saturation < 0.45
    /// </summary>
    public class RecoveryMode : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────
        [Header("Child component references")]
        [SerializeField] private BreathingGuide breathingGuide;
        [SerializeField] private AmbientAudioManager ambientAudio;

        [Header("Scene lighting")]
        [SerializeField] private Light sceneLight;
        [SerializeField] private Color  lightColor  = new Color(0.05f, 0.08f, 0.18f);
        [SerializeField] private float  lightIntensity = 0.15f;

        [Header("Slow particle system for stars / sparkles (optional)")]
        [SerializeField] private ParticleSystem ambientParticles;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void OnEnable()
        {
            ApplyLighting();

            breathingGuide?.StartBreathing();
            ambientAudio?.Play();

            if (ambientParticles != null) ambientParticles.Play();

            // Fire AI voice hook: "Recovery mode starting"
            AIVoiceHook.Instance?.Speak(AIVoiceHook.Cue.ModeStart, ModeManager.Core.RoomMode.Recovery);
        }

        private void OnDisable()
        {
            breathingGuide?.StopBreathing();
            ambientAudio?.Stop();

            if (ambientParticles != null) ambientParticles.Stop();

            // Fire AI voice hook: "Recovery mode ending"
            AIVoiceHook.Instance?.Speak(AIVoiceHook.Cue.ModeEnd, ModeManager.Core.RoomMode.Recovery);
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private void ApplyLighting()
        {
            if (sceneLight == null) return;
            sceneLight.color     = lightColor;
            sceneLight.intensity = lightIntensity;
        }
    }
}
