# The_Benedict_Project

Unity project with a Python/FastAPI backend for Gemini text calls and
Gemini Live real-time voice recognition.

## Backend

The backend lives in `server/`. Keep your real API key in a local `.env` file;
do not put the key directly in Unity or committed source files.

Create `.env` in the project root:

```env
GEMINI_API_KEY=your-key
GEMINI_MODEL=gemini-2.5-flash
GEMINI_LIVE_MODEL=gemini-3.1-flash-live-preview
PORT=3000
```

Install and run:

```bash
cd server
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
uvicorn main:app --reload --host 0.0.0.0 --port 3000
```

Health check:

```bash
curl http://localhost:3000/health
```

## Text endpoint

```bash
curl http://localhost:3000/api/gemini \
  -H "Content-Type: application/json" \
  -d '{"prompt":"Write one friendly sentence about Benedict."}'
```

## Real-time voice recognition

Connect Unity to:

```text
ws://localhost:3000/ws/live
```

Unity should stream microphone audio as small base64 chunks of raw PCM:

```json
{
  "type": "audio",
  "data": "BASE64_PCM_CHUNK",
  "mime_type": "audio/pcm;rate=16000"
}
```

Expected input format:

- Raw PCM, not WAV headers
- 16-bit signed integer
- Little-endian
- Mono
- 16 kHz preferred

When the mic stream pauses, send:

```json
{ "type": "audio_stream_end" }
```

The backend sends Unity events like:

```json
{
  "type": "input_transcript",
  "text": "open the inventory"
}
```

Other possible events are `ready`, `audio`, `output_transcript`, `text`,
`interrupted`, `turn_complete`, and `error`.

## Unity integration shape

This repo includes the Unity-side voice scripts in `Assets/Scripts/Voice/`.
`Packages/manifest.json` references `NativeWebSocket`, which Unity will fetch
through Package Manager when the project opens.

Add `VoiceRecognitionClient` to a GameObject in your scene. It connects to:

```text
ws://127.0.0.1:3000/ws/live
```

By default it starts streaming the default microphone after connecting. It
captures mic samples with `Microphone`, converts them to 16-bit mono PCM, base64
encodes each chunk, and sends it to the FastAPI backend.

For gameplay commands, add `VoiceCommandRouter` to the same or another
GameObject, then connect:

```text
VoiceRecognitionClient.onInputTranscript -> VoiceCommandRouter.HandleTranscript
```

You can wire the router's `onOpenInventory`, `onCloseInventory`, `onStart`,
`onStop`, and `onUnknownCommand` events to your own scene logic in the Inspector.
