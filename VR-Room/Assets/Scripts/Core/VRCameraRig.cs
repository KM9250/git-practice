using UnityEngine;
using UnityEngine.XR;

namespace VRRoom.Core
{
    /// <summary>
    /// Manages the seated, locked camera rig.
    ///
    /// Setup:
    ///   - Attach to the XR Origin (or Camera Offset) GameObject.
    ///   - The physical chair position in real space is treated as the origin.
    ///   - Tracking origin is set to Device (seated) so the HMD yaw/pitch
    ///     is relative to the recenter pose, not room-scale floor.
    ///
    /// Per-mode the rig position/rotation does NOT change — only the scene
    /// content around the user changes via ModeManager.
    /// </summary>
    public class VRCameraRig : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        public static VRCameraRig Instance { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────
        [Header("Seated height offset from floor (metres)")]
        [SerializeField] private float seatedHeightOffset = 1.2f;

        [Header("Recentre on mode change")]
        [SerializeField] private bool recentreOnModeChange = false;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Force seated/device tracking origin
            XRDevice.SetTrackingSpaceType(TrackingSpaceType.Stationary);

            // Position the rig so that seated eye level aligns with scene geometry
            var pos = transform.position;
            pos.y = seatedHeightOffset;
            transform.position = pos;

            if (ModeManager.Instance != null)
                ModeManager.Instance.onModeChanged.AddListener(OnModeChanged);
        }

        private void OnDestroy()
        {
            if (ModeManager.Instance != null)
                ModeManager.Instance.onModeChanged.RemoveListener(OnModeChanged);
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Resets the HMD yaw so that the current head-facing direction
        /// becomes "forward" in the scene.  Call this when a user sits down.
        /// </summary>
        public void Recentre()
        {
            InputTracking.Recenter();
            Debug.Log("[VRCameraRig] Recentred");
        }

        // ── Private helpers ──────────────────────────────────────────────────
        private void OnModeChanged(ModeManager.RoomMode _)
        {
            if (recentreOnModeChange) Recentre();
        }
    }
}
