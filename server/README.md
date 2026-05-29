# Voice API backend

FastAPI backend for Gemini text calls and Gemini Live voice recognition.

## Setup

```bash
cd server
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
uvicorn main:app --reload --host 0.0.0.0 --port 3000
```

Keep secrets in the project root `.env` file:

```env
GEMINI_API_KEY=your-key
GEMINI_MODEL=gemini-2.5-flash
GEMINI_LIVE_MODEL=gemini-3.1-flash-live-preview
PORT=3000
```

## HTTP endpoints

`GET /health`

Returns backend status and selected Gemini models.

`POST /api/gemini`

```json
{ "prompt": "Write one friendly sentence about Benedict." }
```

## Live voice WebSocket

Connect Unity to:

```text
ws://localhost:3000/ws/live
```

Send 16-bit little-endian PCM audio chunks as base64:

```json
{
  "type": "audio",
  "data": "BASE64_PCM_CHUNK",
  "mime_type": "audio/pcm;rate=16000"
}
```

When the mic pauses, flush Gemini's activity detector:

```json
{ "type": "audio_stream_end" }
```

The server emits transcription events:

```json
{
  "type": "input_transcript",
  "text": "open the inventory"
}
```

The server may also emit `ready`, `audio`, `output_transcript`, `text`,
`interrupted`, `turn_complete`, and `error` events.

## Unity client

The Unity-side implementation lives in `Assets/Scripts/Voice/`:

- `VoiceRecognitionClient` streams microphone audio to `/ws/live` and exposes
  transcript/status events.
- `VoiceCommandRouter` maps simple transcript phrases to UnityEvents that can
  be connected in the Inspector.

Open the project in Unity after starting the backend so Package Manager can
fetch `NativeWebSocket`.
