using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using VRRoom.Modes;
using VRRoom.UI;

namespace VRRoom.Integration
{
    /// <summary>
    /// Polls or receives data from an external system (AI pipeline, task manager, etc.)
    /// and pushes it to the appropriate UI components.
    ///
    /// Default transport: HTTP GET poll against a local server (e.g. a Python FastAPI
    /// process running on the same machine).  Replace with WebSocket, gRPC, named pipe,
    /// or any other IPC mechanism as needed.
    ///
    /// Expected JSON schema from the local server:
    /// {
    ///   "tasks":   ["Task 1", "Task 2", ...],
    ///   "summary": "AI-generated summary text",
    ///   "notes":   "User's freeform notes",
    ///   "title":   "2026-02-15 — Focus session"
    /// }
    /// </summary>
    public class ExternalDataReceiver : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────
        [Header("Local server endpoint (e.g. http://localhost:5000/session-data)")]
        [SerializeField] private string endpoint = "http://localhost:5000/session-data";
        [SerializeField] private float  pollIntervalSeconds = 10f;
        [SerializeField] private bool   pollOnStart = true;

        [Header("UI targets")]
        [SerializeField] private TaskBoard       taskBoard;
        [SerializeField] private ReflectionMode  reflectionMode;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Start()
        {
            if (pollOnStart) InvokeRepeating(nameof(FetchData), 0f, pollIntervalSeconds);
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Manual trigger — e.g. call from a VR button.</summary>
        public void FetchNow() => FetchData();

        // ── Private ──────────────────────────────────────────────────────────
        private void FetchData() => StartCoroutine(FetchCoroutine());

        private IEnumerator FetchCoroutine()
        {
            using var req = UnityWebRequest.Get(endpoint);
            req.timeout = 5;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[ExternalDataReceiver] Fetch failed: {req.error}");
                yield break;
            }

            try
            {
                var data = JsonUtility.FromJson<SessionData>(req.downloadHandler.text);
                ApplyData(data);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ExternalDataReceiver] Parse error: {e.Message}");
            }
        }

        private void ApplyData(SessionData data)
        {
            if (taskBoard != null)
            {
                if (!string.IsNullOrEmpty(data.title))
                    taskBoard.SetTitle(data.title);
                if (data.tasks != null)
                    taskBoard.SetTasks(data.tasks);
            }

            if (reflectionMode != null)
            {
                if (!string.IsNullOrEmpty(data.summary))
                    reflectionMode.SetSummary(data.summary);
                if (!string.IsNullOrEmpty(data.notes))
                    reflectionMode.SetUserNotes(data.notes);
            }

            Debug.Log("[ExternalDataReceiver] Data applied");
        }

        // ── Data model ───────────────────────────────────────────────────────
        [Serializable]
        private class SessionData
        {
            public string[] tasks;
            public string   summary;
            public string   notes;
            public string   title;
        }
    }
}
