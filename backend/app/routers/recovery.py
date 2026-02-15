"""
Recovery mode API router.

POST /recovery/start  — process session start, return AI comment
POST /recovery/end    — process session end, compute summary, persist log
GET  /recovery/sessions            — list all sessions
GET  /recovery/sessions/{session_id} — fetch one session
"""

from __future__ import annotations

from fastapi import APIRouter, HTTPException

from ..ai_logic import compute_summary, get_provider
from ..schemas import (
    EndResponse,
    RecoverySessionEnd,
    RecoverySessionStart,
    SessionRecord,
    StartResponse,
)
from .. import storage

router = APIRouter(prefix="/recovery", tags=["recovery"])

# AI provider is instantiated once at import time.
# The mock provider is stateless so this is safe.
_provider = get_provider()


@router.post("/start", response_model=StartResponse)
def session_start(req: RecoverySessionStart) -> StartResponse:
    """
    Accept a session-start event.
    Returns an AI comment and a snapshot of the pre-session user state.
    """
    comment = _provider.start_comment(req)
    return StartResponse(
        session_id     = req.session_id,
        ai_comment     = comment,
        state_snapshot = req.user_state_before,
    )


@router.post("/end", response_model=EndResponse)
def session_end(req: RecoverySessionEnd) -> EndResponse:
    """
    Accept a session-end event.
    Computes delta + efficiency score, generates AI feedback, and persists the record.
    """
    summary  = compute_summary(req)
    feedback = _provider.end_feedback(req, summary)

    # Retrieve the comment stored implicitly; in a stateless design we re-generate it.
    comment = _provider.start_comment(
        RecoverySessionStart(
            session_id        = req.session_id,
            duration_min      = req.duration_min,
            user_state_before = req.user_state_before,
        )
    )

    storage.append_session(req, summary, comment, feedback)

    return EndResponse(
        session_summary = summary,
        ai_feedback     = feedback,
    )


@router.get("/sessions", response_model=list[SessionRecord])
def list_sessions() -> list[SessionRecord]:
    """Return all persisted session records, most recent last."""
    return storage.list_sessions()


@router.get("/sessions/{session_id}", response_model=SessionRecord)
def get_session(session_id: str) -> SessionRecord:
    """Return a single session record by ID."""
    record = storage.get_session(session_id)
    if record is None:
        raise HTTPException(status_code=404, detail=f"Session '{session_id}' not found")
    return record
