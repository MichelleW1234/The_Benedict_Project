import asyncio
import base64
import os
from pathlib import Path
from typing import Any

from dotenv import load_dotenv
from fastapi import FastAPI, WebSocket, WebSocketDisconnect
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, Field

try:
    from google import genai
    from google.genai import types
except ImportError:  # pragma: no cover - handled at startup with a clearer message.
    genai = None
    types = None


ROOT_DIR = Path(__file__).resolve().parents[1]
load_dotenv(ROOT_DIR / ".env")
load_dotenv(Path(__file__).resolve().parent / ".env")

GEMINI_MODEL = os.getenv("GEMINI_MODEL", "gemini-2.5-flash")
GEMINI_LIVE_MODEL = os.getenv("GEMINI_LIVE_MODEL", "gemini-3.1-flash-live-preview")
DEFAULT_AUDIO_MIME_TYPE = "audio/pcm;rate=16000"

app = FastAPI(title="The Benedict Project Voice API")

app.add_middleware(
    CORSMiddleware,
    allow_origins=os.getenv("CORS_ORIGINS", "*").split(","),
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


class GeminiRequest(BaseModel):
    prompt: str = Field(min_length=1)


class GeminiResponse(BaseModel):
    text: str


def require_genai_client() -> Any:
    if genai is None or types is None:
        raise RuntimeError("Missing dependency: install google-genai from server/requirements.txt.")

    if not os.getenv("GEMINI_API_KEY"):
        raise RuntimeError("Missing GEMINI_API_KEY. Add it to .env before starting the server.")

    return genai.Client()


@app.get("/health")
async def health() -> dict[str, Any]:
    return {
        "ok": True,
        "gemini_model": GEMINI_MODEL,
        "gemini_live_model": GEMINI_LIVE_MODEL,
    }


@app.post("/api/gemini", response_model=GeminiResponse)
async def generate_text(request: GeminiRequest) -> GeminiResponse:
    client = require_genai_client()
    response = client.models.generate_content(
        model=GEMINI_MODEL,
        contents=request.prompt.strip(),
    )
    return GeminiResponse(text=response.text or "")


@app.websocket("/ws/live")
async def live_voice(websocket: WebSocket) -> None:
    await websocket.accept()

    try:
        client = require_genai_client()
    except RuntimeError as error:
        await websocket.send_json({"type": "error", "message": str(error)})
        await websocket.close(code=1011)
        return

    config = {
        "response_modalities": ["AUDIO"],
        "input_audio_transcription": {},
        "output_audio_transcription": {},
        "system_instruction": (
            "You are connected to a Unity application for real-time voice recognition. "
            "Keep spoken responses short. Prioritize accurately transcribing user speech."
        ),
    }

    try:
        async with client.aio.live.connect(model=GEMINI_LIVE_MODEL, config=config) as session:
            await websocket.send_json(
                {
                    "type": "ready",
                    "model": GEMINI_LIVE_MODEL,
                    "audio_mime_type": DEFAULT_AUDIO_MIME_TYPE,
                }
            )

            gemini_to_unity = asyncio.create_task(_forward_gemini_messages(session, websocket))
            unity_to_gemini = asyncio.create_task(_forward_unity_messages(websocket, session))

            done, pending = await asyncio.wait(
                {gemini_to_unity, unity_to_gemini},
                return_when=asyncio.FIRST_COMPLETED,
            )

            for task in pending:
                task.cancel()

            for task in done:
                task.result()
    except WebSocketDisconnect:
        return
    except asyncio.CancelledError:
        raise
    except Exception as error:
        await _safe_send_json(websocket, {"type": "error", "message": str(error)})
        await _safe_close(websocket)


async def _forward_unity_messages(websocket: WebSocket, session: Any) -> None:
    while True:
        message = await websocket.receive_json()
        message_type = message.get("type")

        if message_type == "audio":
            audio_data = _decode_audio(message)
            mime_type = message.get("mime_type") or message.get("mimeType") or DEFAULT_AUDIO_MIME_TYPE

            await session.send_realtime_input(
                audio=types.Blob(data=audio_data, mime_type=mime_type)
            )
            continue

        if message_type == "text":
            text = str(message.get("text", "")).strip()
            if text:
                await session.send_realtime_input(text=text)
            continue

        if message_type == "audio_stream_end":
            await session.send_realtime_input(audio_stream_end=True)
            continue

        if message_type == "close":
            return

        await websocket.send_json(
            {
                "type": "error",
                "message": f"Unsupported live message type: {message_type}",
            }
        )


async def _forward_gemini_messages(session: Any, websocket: WebSocket) -> None:
    async for message in session.receive():
        server_content = getattr(message, "server_content", None)

        if text := getattr(message, "text", None):
            await websocket.send_json({"type": "text", "text": text})

        if data := getattr(message, "data", None):
            await websocket.send_json(
                {
                    "type": "audio",
                    "data": base64.b64encode(data).decode("ascii"),
                    "mime_type": "audio/pcm;rate=24000",
                }
            )

        if not server_content:
            continue

        input_transcription = getattr(server_content, "input_transcription", None)
        if input_transcription and getattr(input_transcription, "text", None):
            await websocket.send_json(
                {
                    "type": "input_transcript",
                    "text": input_transcription.text,
                }
            )

        output_transcription = getattr(server_content, "output_transcription", None)
        if output_transcription and getattr(output_transcription, "text", None):
            await websocket.send_json(
                {
                    "type": "output_transcript",
                    "text": output_transcription.text,
                }
            )

        model_turn = getattr(server_content, "model_turn", None)
        for part in getattr(model_turn, "parts", []) or []:
            inline_data = getattr(part, "inline_data", None)
            if inline_data and getattr(inline_data, "data", None):
                mime_type = getattr(inline_data, "mime_type", None) or "audio/pcm;rate=24000"
                await websocket.send_json(
                    {
                        "type": "audio",
                        "data": base64.b64encode(inline_data.data).decode("ascii"),
                        "mime_type": mime_type,
                    }
                )

        if getattr(server_content, "interrupted", False):
            await websocket.send_json({"type": "interrupted"})

        if getattr(server_content, "turn_complete", False):
            await websocket.send_json({"type": "turn_complete"})


def _decode_audio(message: dict[str, Any]) -> bytes:
    raw_data = message.get("data")
    if not isinstance(raw_data, str) or not raw_data:
        raise ValueError("Audio messages must include base64 audio in the data field.")
    return base64.b64decode(raw_data)


async def _safe_send_json(websocket: WebSocket, message: dict[str, Any]) -> None:
    try:
        await websocket.send_json(message)
    except Exception:
        pass


async def _safe_close(websocket: WebSocket) -> None:
    try:
        await websocket.close()
    except Exception:
        pass
