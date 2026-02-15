using UnityEngine;

namespace VRRoom.Audio
{
    /// <summary>
    /// Manages ambient background audio for each mode.
    ///
    /// Usage:
    ///   - Attach one instance to each mode root.
    ///   - Assign an AudioSource (set to Loop = true, Play On Awake = false).
    ///   - Assign one or more AudioClips; the manager picks the active one.
    ///   - Call Play() / Stop() from the mode controller.
    ///
    /// Fades in/out smoothly to avoid abrupt cuts.
    /// </summary>
    public class AmbientAudioManager : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────
        [Header("Audio source (loop, no play-on-awake)")]
        [SerializeField] private AudioSource audioSource;

        [Header("Available ambient clips")]
        [SerializeField] private AudioClip[] clips;   // rain, wind, fire, etc.
        [SerializeField] private int         defaultClipIndex = 0;

        [Header("Volume")]
        [SerializeField, Range(0f, 1f)] private float targetVolume = 0.4f;
        [SerializeField] private float fadeDuration = 1.5f;

        // ── State ────────────────────────────────────────────────────────────
        private int       _activeIndex = -1;
        private float     _fadeTarget;
        private bool      _fading;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (audioSource != null) audioSource.volume = 0f;
        }

        private void Update()
        {
            if (!_fading || audioSource == null) return;

            audioSource.volume = Mathf.MoveTowards(
                audioSource.volume, _fadeTarget,
                (targetVolume / fadeDuration) * Time.deltaTime
            );

            if (Mathf.Approximately(audioSource.volume, _fadeTarget))
            {
                _fading = false;
                if (_fadeTarget <= 0f) audioSource.Stop();
            }
        }

        // ── Public API ───────────────────────────────────────────────────────

        public void Play(int clipIndex = -1)
        {
            if (audioSource == null) return;

            int idx = clipIndex < 0 ? defaultClipIndex : clipIndex;

            if (idx != _activeIndex && clips != null && idx < clips.Length)
            {
                audioSource.Stop();
                audioSource.clip = clips[idx];
                _activeIndex = idx;
            }

            if (!audioSource.isPlaying)
                audioSource.Play();

            FadeTo(targetVolume);
        }

        public void Stop()
        {
            FadeTo(0f);
        }

        /// <summary>Switch to a different clip by index while playing (crossfade).</summary>
        public void SwitchClip(int index)
        {
            if (index == _activeIndex) return;
            Stop();
            // Short delay to let fade out complete, then play new clip
            Invoke(nameof(PlayDefault), fadeDuration + 0.05f);
            defaultClipIndex = index;
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private void PlayDefault() => Play(defaultClipIndex);

        private void FadeTo(float vol)
        {
            _fadeTarget = vol;
            _fading     = true;
        }
    }
}
