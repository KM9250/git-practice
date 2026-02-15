"""
Simple JSONL-based persistence for session records.

Each line in `data/sessions.jsonl` is one JSON-serialised SessionRecord.
This keeps the storage dependency-free (no database) while being trivially
importable into pandas / polars for later analysis.
"""

from __future__ import annotations

import json
from datetime import datetime, timezone
from pathlib import Path

from .schemas import RecoverySessionEnd, SessionRecord, SessionSummary

_DATA_DIR  = Path(__file__).parent.parent / "data"
_LOG_FILE  = _DATA_DIR / "sessions.jsonl"


def _ensure_data_dir() -> None:
    _DATA_DIR.mkdir(parents=True, exist_ok=True)


def append_session(
    req: RecoverySessionEnd,
    summary: SessionSummary,
    ai_comment: str,
    ai_feedback: str,
) -> SessionRecord:
    """Persist a completed session and return the full record."""
    _ensure_data_dir()

    record = SessionRecord(
        session_id        = req.session_id,
        recorded_at       = datetime.now(tz=timezone.utc),
        duration_min      = req.duration_min,
        user_state_before = req.user_state_before,
        user_state_after  = req.user_state_after,
        summary           = summary,
        ai_comment        = ai_comment,
        ai_feedback       = ai_feedback,
    )

    with _LOG_FILE.open("a", encoding="utf-8") as fh:
        fh.write(record.model_dump_json() + "\n")

    return record


def list_sessions() -> list[SessionRecord]:
    """Read all persisted session records from the JSONL log."""
    if not _LOG_FILE.exists():
        return []

    records: list[SessionRecord] = []
    with _LOG_FILE.open("r", encoding="utf-8") as fh:
        for line in fh:
            line = line.strip()
            if line:
                records.append(SessionRecord.model_validate_json(line))
    return records


def get_session(session_id: str) -> SessionRecord | None:
    """Look up a single session by ID (linear scan — suitable for small logs)."""
    for record in list_sessions():
        if record.session_id == session_id:
            return record
    return None
