import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import express from 'express';
import cors from 'cors';
import dotenv from 'dotenv';
import { GoogleGenAI } from '@google/genai';

const __dirname = dirname(fileURLToPath(import.meta.url));
dotenv.config({ path: join(__dirname, '..', '.env') });
dotenv.config({ path: join(__dirname, '.env') });

const PORT = Number(process.env.PORT ?? 3000);
const MODEL = process.env.GEMINI_MODEL ?? 'gemini-2.5-flash';

if (!process.env.GEMINI_API_KEY) {
  console.error('Missing GEMINI_API_KEY. Add it to your local .env before starting the server.');
  process.exit(1);
}

const ai = new GoogleGenAI({});
const app = express();

app.use(cors());
app.use(express.json({ limit: '1mb' }));

app.get('/health', (_req, res) => {
  res.json({ ok: true });
});

app.post('/api/gemini', async (req, res) => {
  const prompt = typeof req.body?.prompt === 'string' ? req.body.prompt.trim() : '';

  if (!prompt) {
    return res.status(400).json({ error: 'Request body must include a non-empty prompt string.' });
  }

  try {
    const response = await ai.models.generateContent({
      model: MODEL,
      contents: prompt
    });

    return res.json({ text: response.text ?? '' });
  } catch (error) {
    console.error(error);
    return res.status(500).json({ error: 'Gemini request failed.' });
  }
});

app.listen(PORT, () => {
  console.log(`Gemini proxy listening on http://localhost:${PORT}`);
});
