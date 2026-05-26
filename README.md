# The_Benedict_Project

## Gemini API integration

This repo includes a small local backend proxy in `server/` for Gemini calls. Keep
your real API key in your local `.env` file as `GEMINI_API_KEY`; do not put the
key directly in Unity, a browser app, or any committed source file.

Install and run the proxy:

```bash
cd server
npm install
npm run dev
```

Send a test request:

```bash
curl http://localhost:3000/api/gemini \
  -H "Content-Type: application/json" \
  -d '{"prompt":"Write one friendly sentence about Benedict."}'
```

Example Unity call:

```csharp
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GeminiClient : MonoBehaviour
{
    [SerializeField] private string proxyUrl = "http://localhost:3000/api/gemini";

    public IEnumerator Generate(string prompt)
    {
        var body = JsonUtility.ToJson(new GeminiRequest { prompt = prompt });
        using var request = new UnityWebRequest(proxyUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(request.error);
            yield break;
        }

        var response = JsonUtility.FromJson<GeminiResponse>(request.downloadHandler.text);
        Debug.Log(response.text);
    }

    [System.Serializable]
    private class GeminiRequest
    {
        public string prompt;
    }

    [System.Serializable]
    private class GeminiResponse
    {
        public string text;
    }
}
```
