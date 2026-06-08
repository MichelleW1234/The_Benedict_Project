# Benedict Project backend

Minimal FastAPI backend kept for project status checks.

The Unity voice pipeline now runs locally in Unity:

```text
Meta Voice SDK STT -> hard-coded response mapping -> VoiceCommandRouter -> animations
```

No websocket voice streaming or Gemini backend calls are required for the current flow.

## Setup

```bash
cd server
py -m venv .venv
.venv\Scripts\activate
pip install -r requirements.txt
uvicorn main:app --reload --host 0.0.0.0 --port 3000
```

## Endpoint

`GET /health`

Returns basic backend status:

```json
{
  "ok": true,
  "voice_pipeline": "Meta Voice SDK STT in Unity, hard-coded local responses",
  "websocket_enabled": false
}
```
