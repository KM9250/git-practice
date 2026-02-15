# Recovery モード — 仕様書

> **プロジェクト**: 仕事効率版・精神と時の部屋
> **対象モード**: Recovery（本ドキュメントで詳細化）
> **作成日**: 2026-02-15

---

## 1. システム全体概要

### 「仕事効率版・精神と時の部屋」とは

自宅 PC VR（座位）環境で「疲れた頭をリセットし、集中状態へ切り替える」ことを目的とした
ソフトウェア群。Dragon Ball の「精神と時の部屋」になぞらえ、
**現実の時間を最小化しながら状態回復・集中・振り返りを行う** コンセプト。

### 3モード構成

| モード | 目的 | 典型的な利用シーン |
|--------|------|-------------------|
| **Recovery** | 疲労・気分の回復。インプット不要の受動的休息 | 連続MTG後、昼休み、就業後の切り替え |
| **Focus** | 深い集中状態の維持。デスクトップをVR内に引き込む | コーディング、執筆、資料作成 |
| **Reflection** | セッションの振り返りと記録。AIとの対話で学習 | 1日の終わり、スプリントレビュー後 |

> **本ドキュメントのスコープ**: Recovery モードの仕様のみ詳細化する。
> Focus / Reflection は `VR-Room/Docs/DESIGN.md` を参照。

---

## 2. Recovery モードの目的

### 2.1 ゴール

短時間（20〜30 分）の VR 休息セッションで次の 3 軸を回復させる。

| 軸 | 指標名 | 説明 |
|----|--------|------|
| 疲労感 | `fatigue_level` | 身体的・認知的疲労の主観評価 |
| 集中力 | `focus_level` | 次のタスクへ向かえる度合い |
| 気分 | `mood_level` | 感情的な快/不快の評価 |

### 2.2 分析目的

セッション前後の状態をスコア化して記録・蓄積することで、
「どの休息パターン（環境音の種類、時間長、入室タイミング）が
どのユーザー状態に最も効いているか」を後から分析できるようにする。

蓄積データはのちに機械学習や統計分析で個人最適化された
「最適休息レシピ」を提案することを見越した設計とする。

---

## 3. データ構造

### 3.1 ユーザー状態スキーマ `UserState`

全モード共通で使用するユーザー状態の構造体。

```jsonc
{
  "fatigue_level": 4,    // 1〜5 整数。5 = 最大疲労
  "focus_level":   2,    // 1〜5 整数。5 = 最高集中
  "mood_level":    2,    // 1〜5 整数。5 = 最良気分
  "notes":         "任意のメモ（テキスト、省略可）"
}
```

**バリデーションルール**

- `fatigue_level` / `focus_level` / `mood_level` はすべて `1 ≤ x ≤ 5` の整数。
- `notes` は省略可（デフォルト空文字列）、最大 500 文字。

### 3.2 セッション開始イベント `RecoverySessionStart`

```jsonc
{
  "event": "recovery_session_start",
  "session_id": "2026-02-15T22-30-00",   // ISO8601 日時（ハイフン区切り）
  "duration_min": 25,                     // 予定休息時間（分）
  "user_state_before": {
    "fatigue_level": 4,
    "focus_level":   2,
    "mood_level":    2,
    "notes":         "仕事でMTG3本続き"
  }
}
```

### 3.3 セッション終了イベント `RecoverySessionEnd`

```jsonc
{
  "event": "recovery_session_end",
  "session_id": "2026-02-15T22-30-00",
  "duration_min": 25,
  "user_state_before": {
    "fatigue_level": 4,
    "focus_level":   2,
    "mood_level":    2,
    "notes":         "仕事でMTG3本続き"
  },
  "user_state_after": {
    "fatigue_level": 2,
    "focus_level":   3,
    "mood_level":    3,
    "notes":         "少しスッキリした"
  }
}
```

### 3.4 ログ用セッションサマリ `SessionSummary`

```jsonc
{
  "session_summary": {
    "session_id":       "2026-02-15T22-30-00",
    "duration_min":     25,
    "fatigue_delta":    -2,    // after - before（負 = 改善）
    "focus_delta":       1,    // after - before（正 = 改善）
    "mood_delta":        1,    // after - before（正 = 改善）
    "efficiency_score":  0.78, // 0.0〜1.0（後述の算出ロジック参照）
    "notes": "25分で疲労レベルが4→2に低下。かなり効率の良い休息パターン。"
  }
}
```

---

## 4. AI アシスタントの処理仕様

### 4.1 セッション開始時 (`/recovery/start`)

**入力**: `RecoverySessionStart`

**処理**:

1. `user_state_before` を解析し、現在の状態を評価する。
2. ユーザー向けの短い開始コメント（1〜2 文）を生成する。
   - 疲労度が高い（4〜5）場合は「しっかり休んでください」系のトーン。
   - 軽めの疲労（1〜2）なら「軽くリフレッシュしましょう」系。
3. ログ用 JSON（`session_id`, `state_snapshot`, `ai_comment`）を返す。

**レスポンス例**:

```json
{
  "session_id": "2026-02-15T22-30-00",
  "ai_comment": "MTGが続いていたんですね。25分、しっかり頭を休ませましょう。",
  "state_snapshot": {
    "fatigue_level": 4,
    "focus_level": 2,
    "mood_level": 2
  }
}
```

### 4.2 セッション終了時 (`/recovery/end`)

**入力**: `RecoverySessionEnd`

**処理**:

1. before / after のデルタを計算する。
   ```
   fatigue_delta = after.fatigue_level - before.fatigue_level  # 負が望ましい
   focus_delta   = after.focus_level   - before.focus_level    # 正が望ましい
   mood_delta    = after.mood_level    - before.mood_level      # 正が望ましい
   ```

2. `efficiency_score` を算出する（0.0〜1.0）。

   ```
   raw = (-fatigue_delta + focus_delta + mood_delta) / (4 + 4 + 4)
   efficiency_score = clamp(raw, 0.0, 1.0)
   ```

   | スコア範囲 | 評価ラベル |
   |-----------|-----------|
   | 0.75〜1.0 | `excellent` |
   | 0.50〜0.74 | `good` |
   | 0.25〜0.49 | `fair` |
   | 0.00〜0.24 | `poor` |

3. ユーザー向けフィードバック文を生成（2〜3 文）。
   - delta の値・efficiency_score・notes を踏まえた自然な文章。
   - 疲労が下がっていない場合は「もう少し休んでもよいかも」等の提案も行う。

4. `SessionSummary` を含むレスポンス JSON を返す。

**レスポンス例**:

```json
{
  "session_summary": {
    "session_id": "2026-02-15T22-30-00",
    "duration_min": 25,
    "fatigue_delta": -2,
    "focus_delta": 1,
    "mood_delta": 1,
    "efficiency_score": 0.78,
    "efficiency_label": "excellent",
    "notes": "25分で疲労レベルが4→2に低下。かなり効率の良い休息パターン。"
  },
  "ai_feedback": "かなり疲れていたところから、しっかり回復できましたね。疲労が2段階下がったのは25分の休息としては優秀です。このパターン（25分・暗い星空・雨音）を記録しておきます。"
}
```

---

## 5. API エンドポイント一覧

| メソッド | パス | 説明 |
|---------|------|------|
| `POST` | `/recovery/start` | セッション開始処理 |
| `POST` | `/recovery/end` | セッション終了・サマリ生成 |
| `GET` | `/recovery/sessions` | 過去セッション一覧取得 |
| `GET` | `/recovery/sessions/{session_id}` | 特定セッション取得 |
| `GET` | `/health` | ヘルスチェック |

---

## 6. 非機能要件

| 項目 | 要件 |
|------|------|
| レスポンスタイム | `/recovery/start` と `/recovery/end` は 200ms 以内（AI 呼び出しなしの場合） |
| ストレージ | セッションログは JSON Lines ファイル（`backend/data/sessions.jsonl`）に追記 |
| 外部 AI 連携 | `AI_PROVIDER` 環境変数で切替（`mock` / `openai` / `claude`）。デフォルト `mock` |
| テスト | `mock` モードでは全エンドポイントが外部 API 呼び出しなしで動作すること |

---

## 7. 将来拡張

- 複数セッションを統計解析し「最適休息レシピ」を提案する `/analytics/recommend` エンドポイント
- Unity 側の `ExternalDataReceiver.cs` と直接 WebSocket 接続するリアルタイムモード
- `focus_level` / `mood_level` のトレンドグラフを Reflection モードへ配信
