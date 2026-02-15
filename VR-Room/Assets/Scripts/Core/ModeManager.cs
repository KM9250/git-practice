using System;
using UnityEngine;
using UnityEngine.Events;

namespace VRRoom.Core
{
    /// <summary>
    /// Central state machine that owns the current RoomMode and orchestrates
    /// transitions between Recovery / Focus / Reflection.
    /// Attach this to a persistent GameObject (e.g. "RoomManager") in every scene.
    /// </summary>
    public class ModeManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        public static ModeManager Instance { get; private set; }

        // ── Mode enum ────────────────────────────────────────────────────────
        public enum RoomMode { None, Recovery, Focus, Reflection }

        // ── Inspector ────────────────────────────────────────────────────────
        [Header("Starting mode (None = show mode-select screen)")]
        [SerializeField] private RoomMode startingMode = RoomMode.None;

        [Header("Mode GameObjects — assign root objects for each mode")]
        [SerializeField] private GameObject recoveryRoot;
        [SerializeField] private GameObject focusRoot;
        [SerializeField] private GameObject reflectionRoot;

        // ── Events ───────────────────────────────────────────────────────────
        [Header("Events")]
        public UnityEvent<RoomMode> onModeChanged;   // fires after transition completes
        public UnityEvent<RoomMode> onModeExiting;   // fires before transition starts

        // ── State ────────────────────────────────────────────────────────────
        public RoomMode CurrentMode { get; private set; } = RoomMode.None;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Deactivate all mode roots first so we start clean
            SetRootActive(recoveryRoot, false);
            SetRootActive(focusRoot, false);
            SetRootActive(reflectionRoot, false);

            if (startingMode != RoomMode.None)
                SwitchTo(startingMode);
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Transition to <paramref name="target"/> mode.</summary>
        public void SwitchTo(RoomMode target)
        {
            if (target == CurrentMode) return;

            onModeExiting?.Invoke(CurrentMode);
            DeactivateCurrent();
            CurrentMode = target;
            ActivateCurrent();
            onModeChanged?.Invoke(CurrentMode);
            Debug.Log($"[ModeManager] Switched to {CurrentMode}");
        }

        // Convenience wrappers for UI buttons
        public void SwitchToRecovery()   => SwitchTo(RoomMode.Recovery);
        public void SwitchToFocus()      => SwitchTo(RoomMode.Focus);
        public void SwitchToReflection() => SwitchTo(RoomMode.Reflection);

        // ── Private helpers ──────────────────────────────────────────────────
        private void DeactivateCurrent()
        {
            switch (CurrentMode)
            {
                case RoomMode.Recovery:   SetRootActive(recoveryRoot,   false); break;
                case RoomMode.Focus:      SetRootActive(focusRoot,      false); break;
                case RoomMode.Reflection: SetRootActive(reflectionRoot, false); break;
            }
        }

        private void ActivateCurrent()
        {
            switch (CurrentMode)
            {
                case RoomMode.Recovery:   SetRootActive(recoveryRoot,   true); break;
                case RoomMode.Focus:      SetRootActive(focusRoot,      true); break;
                case RoomMode.Reflection: SetRootActive(reflectionRoot, true); break;
            }
        }

        private static void SetRootActive(GameObject root, bool active)
        {
            if (root != null) root.SetActive(active);
        }
    }
}
