# CLAUDE.md

This file provides guidance for AI assistants working in this repository.

## Repository Overview

**「仕事効率版・精神と時の部屋」** — a self-hosted VR work-efficiency environment
for seated PC VR, consisting of three modes: Recovery / Focus / Reflection.

Repository: `KM9250/git-practice`

---

## Repository Structure

```
git-practice/
├── docs/
│   └── recovery_mode_spec.md   # Full Recovery mode spec (data schemas, API, AI logic)
│
├── backend/                    # Python + FastAPI — Recovery mode AI API
│   ├── app/
│   │   ├── main.py             # FastAPI app entry point (uvicorn app.main:app)
│   │   ├── schemas.py          # Pydantic v2 models (UserState, SessionSummary, …)
│   │   ├── ai_logic.py         # AI provider abstraction + mock/OpenAI/Claude impls
│   │   ├── storage.py          # JSONL session log (data/sessions.jsonl)
│   │   └── routers/
│   │       └── recovery.py     # /recovery/start, /recovery/end, /recovery/sessions
│   ├── tests/
│   │   ├── test_ai_logic.py    # Unit tests: compute_summary, MockProvider
│   │   └── test_api.py         # Integration tests via FastAPI TestClient
│   ├── pyproject.toml
│   ├── requirements.txt
│   └── .gitignore
│
├── VR-Room/                    # Unity (URP + OpenXR) VR client — C# scripts
│   ├── Assets/Scripts/
│   │   ├── Core/               # ModeManager, SessionManager, VRCameraRig
│   │   ├── Modes/              # RecoveryMode, FocusMode, ReflectionMode
│   │   ├── UI/                 # BreathingGuide, TaskBoard, TimerHUD, AIAvatarUI
│   │   ├── Audio/              # AmbientAudioManager
│   │   └── Integration/        # AIVoiceHook, VRDesktopBridge, ExternalDataReceiver
│   └── Docs/
│       └── DESIGN.md           # Scene layout, colour palettes, coordinate specs
│
├── .gitignore
├── README.md
└── CLAUDE.md                   # This file
```

---

## Backend — Development Commands

```bash
cd backend

# Install dependencies
pip install -e ".[dev]"
# or: pip install -r requirements.txt

# Run the API server (port 5000, hot-reload)
uvicorn app.main:app --reload --port 5000

# Run all tests (mock provider, no external API key needed)
pytest

# Run tests with coverage
pytest --cov=app --cov-report=term-missing

# Use a real AI provider
AI_PROVIDER=claude uvicorn app.main:app --reload --port 5000
AI_PROVIDER=openai uvicorn app.main:app --reload --port 5000
```

### Key API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/recovery/start` | Session start → AI comment |
| `POST` | `/recovery/end` | Session end → delta + efficiency score + feedback |
| `GET` | `/recovery/sessions` | List all persisted sessions |
| `GET` | `/recovery/sessions/{id}` | Get one session |
| `GET` | `/session-data` | Unity `ExternalDataReceiver.cs` compatibility endpoint |
| `GET` | `/health` | Liveness check |

### AI Provider Selection

Set `AI_PROVIDER` environment variable:

| Value | Description |
|-------|-------------|
| `mock` (default) | Rule-based, deterministic, no API key needed |
| `openai` | GPT-4o-mini via `OPENAI_API_KEY` |
| `claude` | Claude Haiku via `ANTHROPIC_API_KEY` |

To add a new provider: implement the `AIProvider` protocol in `ai_logic.py`
and register it in `get_provider()`.

---

## Unity VR Client

See `VR-Room/Docs/DESIGN.md` for full setup instructions.

**Key script relationships:**

```
ModeManager.SwitchTo(RoomMode)
  → activates/deactivates RecoveryRoot / FocusRoot / ReflectionRoot
  → fires onModeChanged event
      → SessionManager.OnModeChanged()    (records segment)
      → RecoveryMode.OnEnable/Disable     (lighting, breathing, audio)
      → AIVoiceHook.Speak(Cue, Mode)      (TTS hook)
```

**Connecting Unity to the backend:**

`ExternalDataReceiver.cs` polls `http://localhost:5000/session-data` every 10 s.
For full session tracking, call `/recovery/start` and `/recovery/end` from a
companion script or from a custom `AIVoiceHook` backend implementation.

---

## Branch Conventions

- `master` — default branch
- `claude/<description>-<session-id>` — AI assistant work branches

Active branches:
- `master`
- `claude/add-claude-documentation-AhjUq` (current)

---

## Git Workflow

1. Branch off `master`
2. Make changes
3. `git push -u origin <branch-name>`
4. Open a PR when ready

Commit messages should be concise and describe *what* changed and *why*.
Never force-push to `master`.

---

## Notes for AI Assistants

- **Backend changes**: always run `pytest` before committing.
- **Schema changes**: update both `schemas.py` and `docs/recovery_mode_spec.md`.
- **New AI providers**: implement the `AIProvider` protocol; do not change `MockProvider` behaviour (tests depend on it).
- **Unity scripts**: C# only, no Unity Editor serialisation in this repo — test logic in isolation where possible.
- Keep this `CLAUDE.md` updated when new top-level directories, commands, or conventions are added.
