# 仕事効率版「精神と時の部屋」VR — 設計ドキュメント

> **概要**: 自宅 PC VR（座位）用の 3 モード環境。Recovery / Focus / Reflection。
> **エンジン**: Unity 2022 LTS 以降 + URP + OpenXR (Meta / SteamVR 両対応)
> **操作**: 頭の向き ＋ 右手トリガーのみ。ルームスケール不要。

---

## アーキテクチャ概要

```
ModeManager (singleton, DontDestroyOnLoad)
  │
  ├── SessionManager (singleton, DontDestroyOnLoad)
  ├── VRCameraRig   (singleton, DontDestroyOnLoad)
  ├── AIVoiceHook   (singleton, DontDestroyOnLoad)
  │
  ├── RecoveryRoot  [GameObject, active/inactive で切替]
  │     └── RecoveryMode.cs
  │
  ├── FocusRoot     [GameObject]
  │     └── FocusMode.cs
  │
  └── ReflectionRoot [GameObject]
        └── ReflectionMode.cs
```

モードの切替 = `ModeManager.SwitchTo(RoomMode.XXX)` を呼ぶだけ。
各 Root GameObject の `OnEnable` / `OnDisable` がライティング・UI・音声を自動管理する。

---

## Unity プロジェクト設定

### XR Plug-in Management
```
Edit → Project Settings → XR Plug-in Management
  ✅ OpenXR  (PC)
  Runtime   : Meta OpenXR / Windows Mixed Reality / SteamVR
  Tracking Origin: Device (=座位)
```

### URP Pipeline Asset
```
Quality → Render Pipeline Asset: URP-Balanced.asset
  Post Processing: ON
  Bloom: medium intensity (Recovery でのみ有効にする)
```

### Package dependencies
```
com.unity.xr.openxr
com.unity.xr.interaction.toolkit  >= 2.5
com.unity.textmeshpro              >= 3.0
com.unity.render-pipelines.universal
```

---

## シーン構成

```
SampleScene
├── [Persistent]          ← DontDestroyOnLoad 群
│   ├── RoomManager       (ModeManager, SessionManager, AIVoiceHook)
│   └── XR Origin         (VRCameraRig, Camera Offset, Main Camera)
│
├── RecoveryRoot          ← Recovery モード
│   ├── RecoveryMode.cs
│   ├── SkyboxEnvironment (スカイボックス or 球面メッシュ)
│   ├── SceneLight        (Directional, intensity 0.15, 青紫系)
│   ├── BreathingSphere   (BreathingGuide.cs)
│   ├── AmbientParticles  (ParticleSystem, slow stars)
│   ├── AmbientAudio      (AmbientAudioManager.cs)
│   └── ExitButton        (VR WorldSpace UI Button)
│
├── FocusRoot             ← Focus モード
│   ├── FocusMode.cs
│   ├── RoomMesh          (壁・床・天井)
│   ├── DeskMesh
│   ├── SceneLight        (Directional, intensity 0.60, 白系)
│   ├── Monitor_1         (Quad + RawImage, VRDesktopBridge)
│   ├── Monitor_2         (optional)
│   ├── Monitor_3         (optional)
│   ├── TaskBoardPanel    (TaskBoard.cs, 左45°)
│   ├── TimerHUD          (TimerHUD.cs, 右30°)
│   ├── AIAvatarHologram  (AIAvatarUI.cs, 左上)
│   ├── AmbientAudio      (AmbientAudioManager.cs, optional)
│   ├── StartButton
│   └── EndButton
│
└── ReflectionRoot        ← Reflection モード (FocusRoot の子でもよい)
    ├── ReflectionMode.cs
    ├── SceneLightOverride (暖色, intensity 0.55)
    ├── SummaryMonitor    (TMP_Text)
    ├── TimelinePanel     (TMP_Text, 左壁)
    ├── UserNotesPanel    (TMP_Text)
    └── AIAvatarSeated    (着席アバターメッシュ)
```

---

## Recovery モード — 詳細仕様

### カラーパレット
| 用途 | Hex | HSV |
|------|-----|-----|
| 背景深部 | `#0D1B2A` | H=210, S=0.43, V=0.16 |
| 霧・遠景 | `#1B3A4B` | H=200, S=0.63, V=0.29 |
| アクセント | `#2D6A4F` | H=155, S=0.57, V=0.42 |
| 星・ハイライト | `#A8DADC` | H=182, S=0.30, V=0.86 |

> **原則**: HSV 彩度 < 0.45、明度 < 0.50。高彩度・高輝度は禁止。

### 呼吸ガイド (BreathingGuide.cs)
```
配置  : 前方 2.0 m、視線高 (y=1.2 m)
スケール: r_min=0.08 m → r_max=0.18 m
サイクル: 4s 吸う → 1s 止める → 4s 吐く → 1s 止める (10s)
マテリアル: URP Lit, Emission ON, アルファ 0.6, Bloom あり
```

### 禁止事項
- 速い動き (≥0.5 m/s の移動アニメーション)
- 明滅・ストロボ
- 通知 UI
- BGM (環境音のみ)

---

## Focus モード — 詳細仕様

### レイアウト寸法 (座位目線 = 原点)
```
正面モニター  : z=+1.0 m, y=+0.95 m (机の上面 y=0.75 + 台座)
              幅 0.9 m × 高 0.5 m (= 40" 相当)
              2枚目 : x=+1.0 m, 右15°傾け
              3枚目 : x=-1.0 m, 左15°傾け

タスクボード  : x=-1.4 m, z=+1.2 m (左45°), y=+1.4 m
              幅 0.6 m × 高 0.8 m

タイマーHUD   : x=+0.5 m, z=+0.8 m (右30°), y=+1.1 m
              幅 0.25 m × 高 0.15 m

AIアバター    : x=-1.2 m, z=+1.5 m, y=+2.2 m (左上・空中)
              スケール 0.35 (miniature)
```

### VR デスクトップ連携 (推奨順)
1. **Virtual Desktop** (Meta) — アプリレベルで完結、Unity 側は RenderTexture 不要
2. **OVR Overlay** (Meta SDK) — 低遅延、Quad Layer として合成
3. **Spout2 Plugin** (PC) — Unity ↔ NDI/Spout でキャプチャ転送
4. **VRDesktopBridge.cs** (本リポジトリ) — プレースホルダー実装、上記と差し替え可

---

## Reflection モード — 詳細仕様

### Focus との差分のみ
| 要素 | Focus | Reflection |
|------|-------|------------|
| ライト色温度 | 6500K (白) | 3000K (暖色 `#FFD8A0`) |
| AIアバター位置 | 左上・ホログラム | 机向かい着席 |
| 正面モニター内容 | デスクトップ | AI 要約テキスト |
| 左パネル | タスクボード | セッションタイムライン |
| 右パネル | タイマーHUD | ユーザーメモ |

### 外部データ入力 (ExternalDataReceiver.cs)
```
GET http://localhost:5000/session-data
→ {
    "tasks":   ["...", "..."],
    "summary": "AI が生成した本日のサマリ",
    "notes":   "ユーザーの自由記述メモ",
    "title":   "2026-02-15 — フォーカスセッション"
  }
```
Python 側サンプル (FastAPI):
```python
# server.py
from fastapi import FastAPI
app = FastAPI()

@app.get("/session-data")
def get_session_data():
    return {
        "tasks":   ["タスク1", "タスク2"],
        "summary": "本日 2 つのタスクを完了。集中度 高。",
        "notes":   "",
        "title":   "2026-02-15"
    }
```

---

## AI 音声フック (AIVoiceHook.cs)

モード開始・終了時に `Speak(Cue, Mode)` が呼ばれる。
バックエンドは後から差し込む方式：

```csharp
// 例: Claude API / OpenAI TTS を使うバックエンド
public class ClaudeTTSBackend : AIVoiceHook.IAIVoiceBackend
{
    public void RequestSpeech(AIVoiceHook.Cue cue, ModeManager.RoomMode mode, string custom)
    {
        string prompt = cue == AIVoiceHook.Cue.ModeStart
            ? $"{mode} モードを開始します。"
            : $"{mode} モードを終了します。お疲れ様でした。";

        // HTTP POST → TTS API → AudioClip → AIVoiceHook.Instance.OnSpeechReady(clip, prompt)
    }
}

// アタッチ方法
void Start() {
    AIVoiceHook.Instance.SetBackend(new ClaudeTTSBackend());
}
```

---

## コントローラー操作マッピング

| 操作 | 実装 |
|------|------|
| 頭の向き (全モード) | HMD 標準トラッキング、BodyTracking 不要 |
| 右トリガー クリック | XRI → UI Ray Interactor → Button.onClick |
| START ボタン | FocusMode.OnStartPressed() |
| END ボタン | FocusMode.OnEndPressed() |
| EXIT ボタン | ModeManager.SwitchTo(None) |

ハンドジェスチャー・テレポート・グラブは **使用しない**（座位固定のため不要）。

---

## フォルダ構成

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── ModeManager.cs          # モード状態機械
│   │   ├── SessionManager.cs       # セッション記録
│   │   └── VRCameraRig.cs          # 座位カメラ管理
│   ├── Modes/
│   │   ├── RecoveryMode.cs
│   │   ├── FocusMode.cs
│   │   └── ReflectionMode.cs
│   ├── UI/
│   │   ├── BreathingGuide.cs       # 呼吸球
│   │   ├── TaskBoard.cs            # タスクボード
│   │   ├── TimerHUD.cs             # タイマー
│   │   └── AIAvatarUI.cs           # ホログラム吹き出し
│   ├── Audio/
│   │   └── AmbientAudioManager.cs  # 環境音フェード
│   └── Integration/
│       ├── AIVoiceHook.cs          # AI音声フックポイント
│       ├── VRDesktopBridge.cs      # デスクトップ映像
│       └── ExternalDataReceiver.cs # 外部データ受信
├── Scenes/
│   └── SampleScene.unity
└── Materials/
    ├── RecoveryBackground.mat
    ├── BreathingSphere.mat         # URP Lit + Bloom emission
    ├── HologramAvatar.mat          # Scanline + alpha flicker
    └── FocusWall.mat               # Neutral grey
```

---

## 拡張ロードマップ

| フェーズ | 内容 |
|---------|------|
| v0.1 | Recovery モードのみ。呼吸球 + 環境音。 |
| v0.2 | Focus モード。モニター表示 (プレースホルダー)。 |
| v0.3 | VRDesktopBridge を実プラグインに差し替え。 |
| v0.4 | Reflection モード。SessionManager タイムライン表示。 |
| v0.5 | ExternalDataReceiver で外部 AI 連携。 |
| v1.0 | AIVoiceHook に TTS バックエンド接続。 |
