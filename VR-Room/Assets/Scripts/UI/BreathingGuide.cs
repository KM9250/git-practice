using UnityEngine;

namespace VRRoom.UI
{
    /// <summary>
    /// A slowly pulsing sphere that guides the user's breath.
    ///
    /// Place at ~2 m forward, eye height in Recovery scene.
    /// Scale (0.1 → 0.2 uniform) matches the r_min / r_max radius spec.
    ///
    /// Cycle: 4s inhale → 1s hold → 4s exhale → 1s rest  (10 s box-breath)
    /// Override breathePeriodSeconds in the Inspector to change to 5 s simple.
    /// </summary>
    public class BreathingGuide : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────
        [Header("Sphere scale (maps to radius r_min → r_max)")]
        [SerializeField] private float rMin = 0.08f;   // smallest radius (metres)
        [SerializeField] private float rMax = 0.18f;   // largest radius

        [Header("Timing (seconds)")]
        [SerializeField] private float inhaleTime  = 4f;
        [SerializeField] private float holdInTime  = 1f;
        [SerializeField] private float exhaleTime  = 4f;
        [SerializeField] private float holdOutTime = 1f;

        [Header("Colour lerp — inhale → exhale")]
        [SerializeField] private Color inhaleColor = new Color(0.17f, 0.42f, 0.60f); // calm blue
        [SerializeField] private Color exhaleColor = new Color(0.18f, 0.55f, 0.34f); // calm green

        // ── State ────────────────────────────────────────────────────────────
        private float   _timer;
        private bool    _active;
        private Renderer _rend;

        private enum Phase { Inhale, HoldIn, Exhale, HoldOut }
        private Phase _phase = Phase.Inhale;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            _rend = GetComponent<Renderer>();
        }

        private void Update()
        {
            if (!_active) return;

            _timer += Time.deltaTime;

            float phaseLen = CurrentPhaseLength();
            float t        = Mathf.Clamp01(_timer / phaseLen);

            ApplyVisuals(t);

            if (_timer >= phaseLen)
            {
                _timer = 0f;
                _phase = NextPhase();
            }
        }

        // ── Public API ───────────────────────────────────────────────────────
        public void StartBreathing()
        {
            _active = true;
            _phase  = Phase.Inhale;
            _timer  = 0f;
            gameObject.SetActive(true);
        }

        public void StopBreathing()
        {
            _active = false;
            gameObject.SetActive(false);
        }

        // ── Private helpers ──────────────────────────────────────────────────
        private void ApplyVisuals(float t)
        {
            float radius;
            Color col;

            switch (_phase)
            {
                case Phase.Inhale:
                    radius = Mathf.Lerp(rMin, rMax, t);
                    col    = Color.Lerp(exhaleColor, inhaleColor, t);
                    break;
                case Phase.HoldIn:
                    radius = rMax;
                    col    = inhaleColor;
                    break;
                case Phase.Exhale:
                    radius = Mathf.Lerp(rMax, rMin, t);
                    col    = Color.Lerp(inhaleColor, exhaleColor, t);
                    break;
                default: // HoldOut
                    radius = rMin;
                    col    = exhaleColor;
                    break;
            }

            float diameter = radius * 2f;
            transform.localScale = new Vector3(diameter, diameter, diameter);

            if (_rend != null)
                _rend.material.SetColor("_BaseColor", col); // URP lit shader
        }

        private float CurrentPhaseLength() => _phase switch
        {
            Phase.Inhale  => inhaleTime,
            Phase.HoldIn  => holdInTime,
            Phase.Exhale  => exhaleTime,
            _              => holdOutTime
        };

        private Phase NextPhase() => _phase switch
        {
            Phase.Inhale  => Phase.HoldIn,
            Phase.HoldIn  => Phase.Exhale,
            Phase.Exhale  => Phase.HoldOut,
            _              => Phase.Inhale
        };
    }
}
