"""
FastAPI application entry point.

Run locally:
    cd backend
    uvicorn app.main:app --reload --port 5000

The Unity ExternalDataReceiver.cs polls http://localhost:5000/session-data.
This server's /recovery/* endpoints are called by the VR client (or a
companion Python script) to process Recovery mode sessions.
"""

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from .routers import recovery

app = FastAPI(
    title="精神と時の部屋 — Recovery Mode API",
    description=(
        "Backend for the VR work-efficiency environment. "
        "Handles Recovery session start/end events, AI comment generation, "
        "and session log persistence."
    ),
    version="0.1.0",
)

# Allow requests from Unity (WebGL or local HTTP calls)
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],   # tighten in production
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(recovery.router)


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}


# Compatibility endpoint expected by ExternalDataReceiver.cs
@app.get("/session-data")
def session_data_compat() -> dict:
    """
    Minimal stub for Unity ExternalDataReceiver.cs.
    Returns the most recent session's task/summary data, or defaults.
    """
    from . import storage
    sessions = storage.list_sessions()
    if sessions:
        latest = sessions[-1]
        return {
            "tasks":   [],
            "summary": latest.summary.notes,
            "notes":   latest.ai_feedback,
            "title":   latest.session_id,
        }
    return {
        "tasks":   [],
        "summary": "",
        "notes":   "",
        "title":   "",
    }
