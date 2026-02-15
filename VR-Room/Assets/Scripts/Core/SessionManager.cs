using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRRoom.Core
{
    /// <summary>
    /// Tracks the lifetime of a single "session" (start → end) within any mode.
    /// Records mode segments so Reflection mode can display a timeline.
    /// </summary>
    public class SessionManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        public static SessionManager Instance { get; private set; }

        // ── Data structures ──────────────────────────────────────────────────
        [Serializable]
        public struct ModeSegment
        {
            public ModeManager.RoomMode mode;
            public DateTime startTime;
            public DateTime endTime;
            public float durationSeconds;
            public string note; // optional user note injected by external system
        }

        // ── Inspector ────────────────────────────────────────────────────────
        [Header("Auto-start session on scene load")]
        [SerializeField] private bool autoStart = true;

        // ── Events ───────────────────────────────────────────────────────────
        public UnityEvent onSessionStarted;
        public UnityEvent onSessionEnded;

        // ── State ────────────────────────────────────────────────────────────
        public bool IsRunning { get; private set; }
        public DateTime SessionStartTime { get; private set; }
        public float ElapsedSeconds => IsRunning ? (float)(DateTime.Now - SessionStartTime).TotalSeconds : 0f;

        public IReadOnlyList<ModeSegment> Segments => _segments;
        private readonly List<ModeSegment> _segments = new();
        private ModeSegment _current;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (autoStart) StartSession();
            if (ModeManager.Instance != null)
                ModeManager.Instance.onModeChanged.AddListener(OnModeChanged);
        }

        private void OnDestroy()
        {
            if (ModeManager.Instance != null)
                ModeManager.Instance.onModeChanged.RemoveListener(OnModeChanged);
        }

        // ── Public API ───────────────────────────────────────────────────────

        public void StartSession()
        {
            if (IsRunning) return;
            IsRunning = true;
            SessionStartTime = DateTime.Now;
            _segments.Clear();
            BeginSegment(ModeManager.Instance ? ModeManager.Instance.CurrentMode : ModeManager.RoomMode.None);
            onSessionStarted?.Invoke();
            Debug.Log($"[SessionManager] Session started at {SessionStartTime:HH:mm:ss}");
        }

        public void EndSession()
        {
            if (!IsRunning) return;
            CloseCurrentSegment();
            IsRunning = false;
            onSessionEnded?.Invoke();
            Debug.Log($"[SessionManager] Session ended. Segments: {_segments.Count}");
        }

        /// <summary>Attach a freeform note to the most recent segment (called by external systems).</summary>
        public void AnnotateLastSegment(string note)
        {
            if (_segments.Count == 0) return;
            var seg = _segments[^1];
            seg.note = note;
            _segments[^1] = seg;
        }

        // ── Private helpers ──────────────────────────────────────────────────
        private void OnModeChanged(ModeManager.RoomMode newMode)
        {
            if (!IsRunning) return;
            CloseCurrentSegment();
            BeginSegment(newMode);
        }

        private void BeginSegment(ModeManager.RoomMode mode)
        {
            _current = new ModeSegment { mode = mode, startTime = DateTime.Now };
        }

        private void CloseCurrentSegment()
        {
            _current.endTime = DateTime.Now;
            _current.durationSeconds = (float)(_current.endTime - _current.startTime).TotalSeconds;
            _segments.Add(_current);
        }
    }
}
