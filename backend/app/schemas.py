"""
Pydantic v2 models for the Recovery mode API.
All models are shared between request parsing, response serialisation,
and persistence (JSONL log).
"""

from __future__ import annotations

from datetime import datetime
from typing import Literal, Optional

from pydantic import BaseModel, Field, field_validator, model_validator


# ── Shared ────────────────────────────────────────────────────────────────────

class UserState(BaseModel):
    """Subjective user state snapshot.  All levels are 1–5 integers."""

    fatigue_level: int = Field(..., ge=1, le=5, description="5 = most fatigued")
    focus_level:   int = Field(..., ge=1, le=5, description="5 = most focused")
    mood_level:    int = Field(..., ge=1, le=5, description="5 = best mood")
    notes: str = Field(default="", max_length=500)


# ── Request bodies ────────────────────────────────────────────────────────────

class RecoverySessionStart(BaseModel):
    event: Literal["recovery_session_start"] = "recovery_session_start"
    session_id: str = Field(
        ...,
        description='ISO8601-like ID, e.g. "2026-02-15T22-30-00"',
        pattern=r"^\d{4}-\d{2}-\d{2}T\d{2}-\d{2}-\d{2}$",
    )
    duration_min: int = Field(..., ge=1, le=240, description="Planned session length in minutes")
    user_state_before: UserState


class RecoverySessionEnd(BaseModel):
    event: Literal["recovery_session_end"] = "recovery_session_end"
    session_id: str = Field(..., pattern=r"^\d{4}-\d{2}-\d{2}T\d{2}-\d{2}-\d{2}$")
    duration_min: int = Field(..., ge=1, le=240)
    user_state_before: UserState
    user_state_after:  UserState


# ── Response bodies ───────────────────────────────────────────────────────────

class StartResponse(BaseModel):
    session_id:     str
    ai_comment:     str
    state_snapshot: UserState


EfficiencyLabel = Literal["excellent", "good", "fair", "poor"]


class SessionSummary(BaseModel):
    session_id:       str
    duration_min:     int
    fatigue_delta:    int   = Field(..., description="after - before; negative = improved")
    focus_delta:      int   = Field(..., description="after - before; positive = improved")
    mood_delta:       int   = Field(..., description="after - before; positive = improved")
    efficiency_score: float = Field(..., ge=0.0, le=1.0)
    efficiency_label: EfficiencyLabel
    notes:            str


class EndResponse(BaseModel):
    session_summary: SessionSummary
    ai_feedback:     str


# ── Persistence record ────────────────────────────────────────────────────────

class SessionRecord(BaseModel):
    """Full record appended to the JSONL log after a session ends."""

    session_id:        str
    recorded_at:       datetime
    duration_min:      int
    user_state_before: UserState
    user_state_after:  UserState
    summary:           SessionSummary
    ai_comment:        str
    ai_feedback:       str
