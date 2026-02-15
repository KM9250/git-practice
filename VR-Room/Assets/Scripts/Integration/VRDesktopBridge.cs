using UnityEngine;
using UnityEngine.UI;

namespace VRRoom.Integration
{
    /// <summary>
    /// Abstraction layer for streaming the real desktop into the VR monitor(s).
    ///
    /// Strategy pattern: swap implementations without changing FocusMode.
    ///
    /// Recommended third-party solutions (tested with PC VR):
    ///   - Virtual Desktop (consumer app) — native desktop streaming; no Unity code needed
    ///     for the basic case.  Use this VRDesktopBridge only if you need to composite
    ///     the desktop texture into a custom Unity scene.
    ///   - OVR Overlay (Meta SDK) — low-latency layer overlay for Quest Link.
    ///   - OpenVR Dashboard Overlay — renders a mirror of the desktop on a texture.
    ///   - NDI / Spout plugin — capture from screen, receive as RenderTexture in Unity.
    ///
    /// This file implements a simple RenderTexture approach as a placeholder.
    /// Replace _captureImpl with a real screen-capture plugin as needed.
    /// </summary>
    public class VRDesktopBridge : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────
        [Header("Monitor render targets (assign RawImage components on your VR monitors)")]
        [SerializeField] private RawImage[] monitorTargets; // 1–3 monitors

        [Header("Capture resolution")]
        [SerializeField] private int captureWidth  = 2560;
        [SerializeField] private int captureHeight = 1440;
        [SerializeField] private int frameRate     = 30;

        [Header("Placeholder texture shown when capture is unavailable")]
        [SerializeField] private Texture2D placeholderTexture;

        // ── State ────────────────────────────────────────────────────────────
        private RenderTexture _captureTexture;
        private bool          _capturing;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            _captureTexture = new RenderTexture(captureWidth, captureHeight, 0);
        }

        private void OnDestroy()
        {
            StopCapture();
            if (_captureTexture != null) _captureTexture.Release();
        }

        // ── Public API ───────────────────────────────────────────────────────

        public void StartCapture()
        {
            if (_capturing) return;
            _capturing = true;

            bool pluginAvailable = TryStartNativeCapture();
            ApplyTextureToMonitors(pluginAvailable ? (Texture)_captureTexture : placeholderTexture);

            Debug.Log($"[VRDesktopBridge] Capture started (plugin={pluginAvailable})");
        }

        public void StopCapture()
        {
            if (!_capturing) return;
            _capturing = false;
            StopNativeCapture();
            Debug.Log("[VRDesktopBridge] Capture stopped");
        }

        /// <summary>
        /// Select which physical monitor index to capture (0 = primary).
        /// Requires the native plugin to support monitor selection.
        /// </summary>
        public void SetMonitorIndex(int index)
        {
            // TODO: pass to native plugin
            Debug.Log($"[VRDesktopBridge] Monitor index set to {index}");
        }

        // ── Plugin stubs (replace with real plugin calls) ─────────────────────

        /// <returns>true if a native capture plugin is available and started.</returns>
        private bool TryStartNativeCapture()
        {
            // Example: Spout / NDI plugin call would go here.
            // SpoutManager.GetTexture(0, ref _captureTexture);
            // For now: return false → placeholder texture is used.
            return false;
        }

        private void StopNativeCapture()
        {
            // SpoutManager.Release();
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private void ApplyTextureToMonitors(Texture tex)
        {
            if (monitorTargets == null) return;
            foreach (var monitor in monitorTargets)
                if (monitor != null) monitor.texture = tex;
        }
    }
}
