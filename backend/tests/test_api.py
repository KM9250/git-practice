"""
Integration tests for the FastAPI routes using TestClient.
No external APIs, no real disk writes (tmp_path fixture overrides data dir).
"""

import json
import os
from pathlib import Path
from unittest.mock import patch

import pytest
from fastapi.testclient import TestClient

from app.main import app

client = TestClient(app)

START_PAYLOAD = {
    "event": "recovery_session_start",
    "session_id": "2026-02-15T22-30-00",
    "duration_min": 25,
    "user_state_before": {
        "fatigue_level": 4,
        "focus_level": 2,
        "mood_level": 2,
        "notes": "仕事でMTG3本続き",
    },
}

END_PAYLOAD = {
    "event": "recovery_session_end",
    "session_id": "2026-02-15T22-30-00",
    "duration_min": 25,
    "user_state_before": {
        "fatigue_level": 4,
        "focus_level": 2,
        "mood_level": 2,
        "notes": "仕事でMTG3本続き",
    },
    "user_state_after": {
        "fatigue_level": 2,
        "focus_level": 3,
        "mood_level": 3,
        "notes": "少しスッキリした",
    },
}


# ── /health ───────────────────────────────────────────────────────────────────

def test_health():
    r = client.get("/health")
    assert r.status_code == 200
    assert r.json()["status"] == "ok"


# ── /recovery/start ───────────────────────────────────────────────────────────

def test_start_returns_200():
    r = client.post("/recovery/start", json=START_PAYLOAD)
    assert r.status_code == 200


def test_start_response_shape():
    r = client.post("/recovery/start", json=START_PAYLOAD)
    body = r.json()
    assert "session_id" in body
    assert "ai_comment" in body
    assert "state_snapshot" in body


def test_start_session_id_echoed():
    r = client.post("/recovery/start", json=START_PAYLOAD)
    assert r.json()["session_id"] == "2026-02-15T22-30-00"


def test_start_ai_comment_non_empty():
    r = client.post("/recovery/start", json=START_PAYLOAD)
    assert len(r.json()["ai_comment"]) > 0


def test_start_invalid_session_id():
    bad = dict(START_PAYLOAD, session_id="bad-id")
    r = client.post("/recovery/start", json=bad)
    assert r.status_code == 422


def test_start_level_out_of_range():
    bad = json.loads(json.dumps(START_PAYLOAD))
    bad["user_state_before"]["fatigue_level"] = 9
    r = client.post("/recovery/start", json=bad)
    assert r.status_code == 422


# ── /recovery/end ─────────────────────────────────────────────────────────────

def test_end_returns_200(tmp_path, monkeypatch):
    monkeypatch.setattr("app.storage._DATA_DIR", tmp_path)
    monkeypatch.setattr("app.storage._LOG_FILE", tmp_path / "sessions.jsonl")
    r = client.post("/recovery/end", json=END_PAYLOAD)
    assert r.status_code == 200


def test_end_response_shape(tmp_path, monkeypatch):
    monkeypatch.setattr("app.storage._DATA_DIR", tmp_path)
    monkeypatch.setattr("app.storage._LOG_FILE", tmp_path / "sessions.jsonl")
    r = client.post("/recovery/end", json=END_PAYLOAD)
    body = r.json()
    assert "session_summary" in body
    assert "ai_feedback" in body


def test_end_deltas_correct(tmp_path, monkeypatch):
    monkeypatch.setattr("app.storage._DATA_DIR", tmp_path)
    monkeypatch.setattr("app.storage._LOG_FILE", tmp_path / "sessions.jsonl")
    r = client.post("/recovery/end", json=END_PAYLOAD)
    s = r.json()["session_summary"]
    assert s["fatigue_delta"] == -2   # 2 - 4
    assert s["focus_delta"]   ==  1   # 3 - 2
    assert s["mood_delta"]    ==  1   # 3 - 2


def test_end_efficiency_score_range(tmp_path, monkeypatch):
    monkeypatch.setattr("app.storage._DATA_DIR", tmp_path)
    monkeypatch.setattr("app.storage._LOG_FILE", tmp_path / "sessions.jsonl")
    r = client.post("/recovery/end", json=END_PAYLOAD)
    score = r.json()["session_summary"]["efficiency_score"]
    assert 0.0 <= score <= 1.0


def test_end_writes_jsonl(tmp_path, monkeypatch):
    log = tmp_path / "sessions.jsonl"
    monkeypatch.setattr("app.storage._DATA_DIR", tmp_path)
    monkeypatch.setattr("app.storage._LOG_FILE", log)
    client.post("/recovery/end", json=END_PAYLOAD)
    assert log.exists()
    lines = [l for l in log.read_text().splitlines() if l.strip()]
    assert len(lines) == 1
    record = json.loads(lines[0])
    assert record["session_id"] == "2026-02-15T22-30-00"


# ── /recovery/sessions ────────────────────────────────────────────────────────

def test_list_sessions_empty(tmp_path, monkeypatch):
    monkeypatch.setattr("app.storage._LOG_FILE", tmp_path / "sessions.jsonl")
    r = client.get("/recovery/sessions")
    assert r.status_code == 200
    assert r.json() == []


def test_list_sessions_after_end(tmp_path, monkeypatch):
    log = tmp_path / "sessions.jsonl"
    monkeypatch.setattr("app.storage._DATA_DIR", tmp_path)
    monkeypatch.setattr("app.storage._LOG_FILE", log)
    client.post("/recovery/end", json=END_PAYLOAD)
    r = client.get("/recovery/sessions")
    assert r.status_code == 200
    assert len(r.json()) == 1


def test_get_session_not_found(tmp_path, monkeypatch):
    monkeypatch.setattr("app.storage._LOG_FILE", tmp_path / "sessions.jsonl")
    r = client.get("/recovery/sessions/does-not-exist")
    assert r.status_code == 404


def test_get_session_found(tmp_path, monkeypatch):
    log = tmp_path / "sessions.jsonl"
    monkeypatch.setattr("app.storage._DATA_DIR", tmp_path)
    monkeypatch.setattr("app.storage._LOG_FILE", log)
    client.post("/recovery/end", json=END_PAYLOAD)
    r = client.get("/recovery/sessions/2026-02-15T22-30-00")
    assert r.status_code == 200
    assert r.json()["session_id"] == "2026-02-15T22-30-00"


# ── /session-data compat ──────────────────────────────────────────────────────

def test_session_data_compat_empty(tmp_path, monkeypatch):
    monkeypatch.setattr("app.storage._LOG_FILE", tmp_path / "sessions.jsonl")
    r = client.get("/session-data")
    assert r.status_code == 200
    body = r.json()
    assert "tasks" in body
    assert "summary" in body
