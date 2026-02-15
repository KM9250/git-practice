using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace VRRoom.UI
{
    /// <summary>
    /// Task board panel — displayed on the left wall in Focus mode (~45° off forward).
    ///
    /// Receives task data from an external system (REST hook, clipboard, etc.)
    /// via the public API.  Renders tasks as a simple numbered list on a
    /// TextMeshPro component inside a World Space Canvas.
    ///
    /// Setup in Editor:
    ///   - Attach to a Quad or Panel mesh at left-45° wall, ~1.4 m height, ~1.5 m away.
    ///   - Assign titleText and bodyText TMP components.
    ///   - Set Canvas render mode to World Space; scale ~0.002 (so 1000 px = 2 m).
    /// </summary>
    public class TaskBoard : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;

        [Header("Checked-off task colour")]
        [SerializeField] private Color doneColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color todoColor = Color.white;

        // ── Data ─────────────────────────────────────────────────────────────
        public struct TaskItem
        {
            public string text;
            public bool   done;
        }

        private readonly List<TaskItem> _tasks = new();

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Start() => Refresh();

        // ── Public API ───────────────────────────────────────────────────────

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        /// <summary>Replace all tasks (called from external data pipeline).</summary>
        public void SetTasks(IEnumerable<string> taskLines)
        {
            _tasks.Clear();
            foreach (var line in taskLines)
                _tasks.Add(new TaskItem { text = line.TrimStart('-', ' '), done = false });
            Refresh();
        }

        /// <summary>Toggle a task's done state by index.</summary>
        public void ToggleDone(int index)
        {
            if (index < 0 || index >= _tasks.Count) return;
            var t = _tasks[index];
            t.done = !t.done;
            _tasks[index] = t;
            Refresh();
        }

        /// <summary>Set board title (e.g. date or project name).</summary>
        public void SetTitle(string title)
        {
            if (titleText != null) titleText.text = title;
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private void Refresh()
        {
            if (bodyText == null) return;

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _tasks.Count; i++)
            {
                var task = _tasks[i];
                string mark = task.done ? "✓" : $"{i + 1}.";
                // TMP rich text: colour tag
                string hex = ColorUtility.ToHtmlStringRGB(task.done ? doneColor : todoColor);
                sb.AppendLine($"<color=#{hex}>{mark}  {task.text}</color>");
            }

            if (_tasks.Count == 0)
                sb.AppendLine("<color=#666666>— No tasks loaded —</color>");

            bodyText.text = sb.ToString();
        }
    }
}
