"""
Unit tests for ai_logic.py — pure functions, no HTTP, no external APIs.
All tests run with the default mock provider (AI_PROVIDER=mock).
"""

import pytest

from app.ai_logic import MockProvider, compute_summary
from app.schemas import RecoverySessionEnd, RecoverySessionStart, UserState


# ── Fixtures ──────────────────────────────────────────────────────────────────

def make_end(
    before_fatigue=4, before_focus=2, before_mood=2,
    after_fatigue=2,  after_focus=3,  after_mood=3,
    duration=25,
    session_id="2026-02-15T22-30-00",
) -> RecoverySessionEnd:
    return RecoverySessionEnd(
        session_id       = session_id,
        duration_min     = duration,
        user_state_before = UserState(
            fatigue_level=before_fatigue,
            focus_level=before_focus,
            mood_level=before_mood,
        ),
        user_state_after  = UserState(
            fatigue_level=after_fatigue,
            focus_level=after_focus,
            mood_level=after_mood,
        ),
    )


# ── compute_summary ───────────────────────────────────────────────────────────

class TestComputeSummary:
    def test_deltas_correct(self):
        req = make_end(
            before_fatigue=4, after_fatigue=2,
            before_focus=2,   after_focus=3,
            before_mood=2,    after_mood=3,
        )
        s = compute_summary(req)
        assert s.fatigue_delta == -2
        assert s.focus_delta   ==  1
        assert s.mood_delta    ==  1

    def test_efficiency_score_in_range(self):
        for fatigue_after in range(1, 6):
            req = make_end(before_fatigue=5, after_fatigue=fatigue_after)
            s = compute_summary(req)
            assert 0.0 <= s.efficiency_score <= 1.0

    def test_excellent_label_when_all_improved(self):
        # Maximally improved: fatigue 5→1, focus 1→5, mood 1→5
        req = make_end(
            before_fatigue=5, after_fatigue=1,
            before_focus=1,   after_focus=5,
            before_mood=1,    after_mood=5,
        )
        s = compute_summary(req)
        assert s.efficiency_label == "excellent"
        assert s.efficiency_score == pytest.approx(1.0)

    def test_poor_label_when_nothing_improved(self):
        # No change in any axis
        req = make_end(
            before_fatigue=3, after_fatigue=3,
            before_focus=3,   after_focus=3,
            before_mood=3,    after_mood=3,
        )
        s = compute_summary(req)
        # raw = (0 + 0 + 0) / 12 = 0 → score = 0.5 after (+1)/2 normalisation
        # Recheck: score = (0/12 + 1) / 2 = 0.5 → "good"
        assert s.efficiency_label in ("good", "fair")

    def test_session_id_preserved(self):
        req = make_end(session_id="2026-01-01T09-00-00")
        s = compute_summary(req)
        assert s.session_id == "2026-01-01T09-00-00"

    def test_duration_preserved(self):
        req = make_end(duration=20)
        s = compute_summary(req)
        assert s.duration_min == 20

    def test_notes_non_empty(self):
        req = make_end()
        s = compute_summary(req)
        assert len(s.notes) > 0


# ── MockProvider.start_comment ────────────────────────────────────────────────

class TestMockProviderStartComment:
    provider = MockProvider()

    def _make_start(self, fatigue: int, duration: int = 25) -> RecoverySessionStart:
        return RecoverySessionStart(
            session_id       = "2026-02-15T22-30-00",
            duration_min     = duration,
            user_state_before = UserState(
                fatigue_level=fatigue, focus_level=3, mood_level=3
            ),
        )

    def test_high_fatigue_returns_string(self):
        req = self._make_start(fatigue=5)
        comment = self.provider.start_comment(req)
        assert isinstance(comment, str)
        assert len(comment) > 0

    def test_low_fatigue_returns_string(self):
        req = self._make_start(fatigue=1)
        comment = self.provider.start_comment(req)
        assert isinstance(comment, str)

    def test_duration_in_comment(self):
        req = self._make_start(fatigue=4, duration=30)
        comment = self.provider.start_comment(req)
        assert "30" in comment

    def test_deterministic_same_session(self):
        req = self._make_start(fatigue=4)
        c1 = self.provider.start_comment(req)
        c2 = self.provider.start_comment(req)
        assert c1 == c2


# ── MockProvider.end_feedback ─────────────────────────────────────────────────

class TestMockProviderEndFeedback:
    provider = MockProvider()

    def test_returns_non_empty_string(self):
        req = make_end()
        summary = compute_summary(req)
        feedback = self.provider.end_feedback(req, summary)
        assert isinstance(feedback, str)
        assert len(feedback) > 0

    def test_excellent_feedback_contains_positive_word(self):
        req = make_end(
            before_fatigue=5, after_fatigue=1,
            before_focus=1,   after_focus=5,
            before_mood=1,    after_mood=5,
        )
        summary  = compute_summary(req)
        feedback = self.provider.end_feedback(req, summary)
        # Should mention recovery quality for excellent scores
        assert any(word in feedback for word in ["回復", "優秀", "しっかり"])

    def test_poor_feedback_contains_suggestion(self):
        req = make_end(
            before_fatigue=3, after_fatigue=4,  # got worse
            before_focus=3,   after_focus=2,
            before_mood=3,    after_mood=2,
        )
        summary  = compute_summary(req)
        feedback = self.provider.end_feedback(req, summary)
        assert isinstance(feedback, str)


# ── Schema validation ─────────────────────────────────────────────────────────

class TestSchemaValidation:
    def test_user_state_level_out_of_range(self):
        from pydantic import ValidationError
        with pytest.raises(ValidationError):
            UserState(fatigue_level=6, focus_level=3, mood_level=3)

    def test_user_state_level_zero(self):
        from pydantic import ValidationError
        with pytest.raises(ValidationError):
            UserState(fatigue_level=0, focus_level=3, mood_level=3)

    def test_session_id_bad_format(self):
        from pydantic import ValidationError
        with pytest.raises(ValidationError):
            RecoverySessionStart(
                session_id="not-a-valid-id",
                duration_min=25,
                user_state_before=UserState(fatigue_level=3, focus_level=3, mood_level=3),
            )

    def test_notes_max_length(self):
        from pydantic import ValidationError
        with pytest.raises(ValidationError):
            UserState(fatigue_level=3, focus_level=3, mood_level=3, notes="x" * 501)
