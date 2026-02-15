"""
AI logic for Recovery mode.

Responsibility:
  1. generate_start_comment()  — short phrase on session start
  2. generate_end_feedback()   — personalised feedback on session end
  3. compute_summary()         — pure arithmetic delta + efficiency score

Provider selection is driven by the AI_PROVIDER environment variable:
  mock   (default) — deterministic rule-based text, no external calls
  openai           — calls OpenAI Chat Completions
  claude           — calls Anthropic Messages API

Only the mock provider is implemented inline.
OpenAI / Claude providers are thin wrappers; plug in your API key via env vars:
  OPENAI_API_KEY or ANTHROPIC_API_KEY
"""

from __future__ import annotations

import os
from typing import Protocol

from .schemas import (
    RecoverySessionEnd,
    RecoverySessionStart,
    SessionSummary,
    UserState,
)


# ── Helper: compute summary (pure, provider-agnostic) ─────────────────────────

def compute_summary(req: RecoverySessionEnd) -> SessionSummary:
    """Calculate deltas and efficiency_score from before/after states."""
    b = req.user_state_before
    a = req.user_state_after

    fatigue_delta = a.fatigue_level - b.fatigue_level   # negative = improved
    focus_delta   = a.focus_level   - b.focus_level     # positive = improved
    mood_delta    = a.mood_level    - b.mood_level       # positive = improved

    # Normalise: max possible improvement per axis is 4 (5→1 or 1→5).
    # Desired direction: fatigue down (-), focus up (+), mood up (+).
    # raw ∈ [-1, 1] → clamp to [0, 1]
    raw = (-fatigue_delta + focus_delta + mood_delta) / 12.0  # 12 = 3 axes × 4 max
    efficiency_score = max(0.0, min(1.0, (raw + 1.0) / 2.0))

    if efficiency_score >= 0.75:
        label = "excellent"
    elif efficiency_score >= 0.50:
        label = "good"
    elif efficiency_score >= 0.25:
        label = "fair"
    else:
        label = "poor"

    notes = _build_summary_notes(
        req.duration_min, fatigue_delta, focus_delta, mood_delta,
        b.fatigue_level, a.fatigue_level, efficiency_score,
    )

    return SessionSummary(
        session_id       = req.session_id,
        duration_min     = req.duration_min,
        fatigue_delta    = fatigue_delta,
        focus_delta      = focus_delta,
        mood_delta       = mood_delta,
        efficiency_score = round(efficiency_score, 2),
        efficiency_label = label,
        notes            = notes,
    )


def _build_summary_notes(
    duration: int,
    fatigue_delta: int,
    focus_delta: int,
    mood_delta: int,
    fatigue_before: int,
    fatigue_after: int,
    score: float,
) -> str:
    parts: list[str] = []

    if fatigue_delta < 0:
        parts.append(
            f"{duration}分で疲労レベルが{fatigue_before}→{fatigue_after}に低下。"
        )
    elif fatigue_delta == 0:
        parts.append(f"疲労レベルは変化なし（{fatigue_before}）。")
    else:
        parts.append(f"疲労レベルが{fatigue_before}→{fatigue_after}に増加（要観察）。")

    if focus_delta > 0:
        parts.append("集中力も回復傾向。")
    if mood_delta > 0:
        parts.append("気分も改善。")

    if score >= 0.75:
        parts.append("かなり効率の良い休息パターン。")
    elif score >= 0.50:
        parts.append("標準的な回復が見られた。")
    elif score >= 0.25:
        parts.append("回復効果はやや限定的。")
    else:
        parts.append("今回は回復が乏しかった。次回は時間や環境音を変えてみても良いかもしれない。")

    return "".join(parts)


# ── Provider protocol ─────────────────────────────────────────────────────────

class AIProvider(Protocol):
    def start_comment(self, req: RecoverySessionStart) -> str: ...
    def end_feedback(self, req: RecoverySessionEnd, summary: SessionSummary) -> str: ...


# ── Mock provider (default, no external API) ──────────────────────────────────

class MockProvider:
    """
    Rule-based text generation.
    Deterministic — safe to use in tests without mocking HTTP.
    """

    _START_HIGH_FATIGUE = [
        "かなり疲れていますね。{duration}分、頭を空っぽにして休みましょう。",
        "MTGや作業が続いたのでしょうか。{duration}分、深呼吸しながら過ごしてください。",
        "疲労が溜まっています。無理せず、ただ休むことに集中してください。",
    ]
    _START_MID_FATIGUE = [
        "少し疲れ気味ですね。{duration}分のリフレッシュで整えていきましょう。",
        "中程度の疲れがありますね。{duration}分、ゆっくり休みましょう。",
    ]
    _START_LOW_FATIGUE = [
        "今は比較的元気そうです。{duration}分で軽くリフレッシュしましょう。",
        "コンディションは悪くないですね。{duration}分、気分転換に使ってください。",
    ]

    def start_comment(self, req: RecoverySessionStart) -> str:
        f = req.user_state_before.fatigue_level
        d = req.duration_min

        if f >= 4:
            templates = self._START_HIGH_FATIGUE
        elif f >= 3:
            templates = self._START_MID_FATIGUE
        else:
            templates = self._START_LOW_FATIGUE

        # Use session_id as a stable seed so the same session always gets the same line
        idx = hash(req.session_id) % len(templates)
        return templates[idx].format(duration=d)

    def end_feedback(self, req: RecoverySessionEnd, summary: SessionSummary) -> str:
        parts: list[str] = []

        # Recovery effectiveness
        if summary.efficiency_label == "excellent":
            parts.append(
                f"かなり疲れていたところから、しっかり回復できましたね。"
                f"疲労が{abs(summary.fatigue_delta)}段階下がったのは"
                f"{summary.duration_min}分の休息としては優秀です。"
            )
        elif summary.efficiency_label == "good":
            parts.append(
                f"{summary.duration_min}分で標準的な回復ができました。"
                f"疲労はΔ{summary.fatigue_delta}、集中力はΔ+{summary.focus_delta}の変化です。"
            )
        elif summary.efficiency_label == "fair":
            parts.append(
                f"少し回復できましたが、まだ疲れが残っているかもしれません。"
                f"もう少し休んでも良いかもしれませんよ。"
            )
        else:
            parts.append(
                f"今回はあまり回復できなかったようです。"
                f"環境音の種類や時間の長さを変えてみることをおすすめします。"
            )

        # After-state observation
        after = req.user_state_after
        if after.mood_level >= 4:
            parts.append(" 気分が良くなっているのも良いサインです。")
        if after.focus_level >= 4:
            parts.append(" 集中力も十分に戻っていますね。そのままFocusモードへどうぞ。")

        # Note on pattern recording
        parts.append(f" このパターン（{summary.duration_min}分・Recovery）を記録しておきます。")

        return "".join(parts).strip()


# ── OpenAI provider stub ──────────────────────────────────────────────────────

class OpenAIProvider:
    """
    Real OpenAI implementation.
    Requires: pip install openai
    Env var:  OPENAI_API_KEY
    """

    def __init__(self) -> None:
        import openai  # type: ignore[import]
        self._client = openai.OpenAI()

    def _chat(self, system: str, user: str) -> str:
        resp = self._client.chat.completions.create(
            model="gpt-4o-mini",
            messages=[
                {"role": "system", "content": system},
                {"role": "user",   "content": user},
            ],
            max_tokens=200,
            temperature=0.7,
        )
        return resp.choices[0].message.content.strip()

    def start_comment(self, req: RecoverySessionStart) -> str:
        system = (
            "あなたはVR休息アシスタントです。"
            "ユーザーの状態を見て、Recoveryセッション開始時に短い一言（1〜2文）を日本語で返してください。"
        )
        user = (
            f"疲労:{req.user_state_before.fatigue_level}/5, "
            f"集中:{req.user_state_before.focus_level}/5, "
            f"気分:{req.user_state_before.mood_level}/5, "
            f"メモ:{req.user_state_before.notes}, "
            f"予定時間:{req.duration_min}分"
        )
        return self._chat(system, user)

    def end_feedback(self, req: RecoverySessionEnd, summary: SessionSummary) -> str:
        system = (
            "あなたはVR休息アシスタントです。"
            "セッション前後の状態変化を見て、ユーザーへのフィードバックを2〜3文、日本語で返してください。"
        )
        user = (
            f"疲労: {req.user_state_before.fatigue_level}→{req.user_state_after.fatigue_level} "
            f"(Δ{summary.fatigue_delta}), "
            f"集中: {req.user_state_before.focus_level}→{req.user_state_after.focus_level} "
            f"(Δ{summary.focus_delta}), "
            f"気分: {req.user_state_before.mood_level}→{req.user_state_after.mood_level} "
            f"(Δ{summary.mood_delta}), "
            f"効率スコア: {summary.efficiency_score}, "
            f"時間: {summary.duration_min}分"
        )
        return self._chat(system, user)


# ── Claude provider stub ──────────────────────────────────────────────────────

class ClaudeProvider:
    """
    Anthropic Claude implementation.
    Requires: pip install anthropic
    Env var:  ANTHROPIC_API_KEY
    """

    def __init__(self) -> None:
        import anthropic  # type: ignore[import]
        self._client = anthropic.Anthropic()

    def _message(self, system: str, user: str) -> str:
        resp = self._client.messages.create(
            model="claude-haiku-4-5-20251001",
            max_tokens=200,
            system=system,
            messages=[{"role": "user", "content": user}],
        )
        return resp.content[0].text.strip()

    def start_comment(self, req: RecoverySessionStart) -> str:
        system = (
            "あなたはVR休息アシスタントです。"
            "ユーザーの状態を見て、Recoveryセッション開始時に短い一言（1〜2文）を日本語で返してください。"
        )
        user = (
            f"疲労:{req.user_state_before.fatigue_level}/5, "
            f"集中:{req.user_state_before.focus_level}/5, "
            f"気分:{req.user_state_before.mood_level}/5, "
            f"メモ:{req.user_state_before.notes}, "
            f"予定時間:{req.duration_min}分"
        )
        return self._message(system, user)

    def end_feedback(self, req: RecoverySessionEnd, summary: SessionSummary) -> str:
        system = (
            "あなたはVR休息アシスタントです。"
            "セッション前後の状態変化を見て、ユーザーへのフィードバックを2〜3文、日本語で返してください。"
        )
        user = (
            f"疲労: {req.user_state_before.fatigue_level}→{req.user_state_after.fatigue_level} "
            f"(Δ{summary.fatigue_delta}), "
            f"集中: {req.user_state_before.focus_level}→{req.user_state_after.focus_level} "
            f"(Δ{summary.focus_delta}), "
            f"気分: {req.user_state_before.mood_level}→{req.user_state_after.mood_level} "
            f"(Δ{summary.mood_delta}), "
            f"効率スコア: {summary.efficiency_score}, "
            f"時間: {summary.duration_min}分"
        )
        return self._message(system, user)


# ── Factory ───────────────────────────────────────────────────────────────────

def get_provider() -> AIProvider:
    """
    Select provider based on AI_PROVIDER env var.
    Defaults to 'mock' so tests never need external API keys.
    """
    name = os.environ.get("AI_PROVIDER", "mock").lower()
    if name == "openai":
        return OpenAIProvider()
    if name == "claude":
        return ClaudeProvider()
    return MockProvider()
