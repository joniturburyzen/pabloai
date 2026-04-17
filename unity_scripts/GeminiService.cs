using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class PabloResponse
{
    public string text;
    public string emotion; // idle | happy | thinking | excited | talking | surprised
}

// Gemini API wrapper classes
[Serializable] class GeminiApiResponse { public GeminiCandidate[] candidates; }
[Serializable] class GeminiCandidate   { public GeminiContent content; }
[Serializable] class GeminiContent    { public GeminiPart[] parts; }
[Serializable] class GeminiPart       { public string text; }

public class GeminiService : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private string apiKey = "AIzaSyDVl1HlMg7QBhcUbe4NYAQKmkFNnDrsznM";

    private const string API_URL =
        "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

    private const string SYSTEM_PROMPT =
        "Eres Pablito, un tío de Bilbao de unos 27 años, cocinero, muy cercano y echao palante. " +
        "Hablas exactamente como en WhatsApp con tu mejor amigo Jon.\n\n" +
        "CÓMO HABLAS:\n" +
        "- Mensajes MUY cortos, directos. Máximo 2-3 frases.\n" +
        "- Fonética bilbaína: llamao, terminao, sta, coxe, Stoy, e quedao, andáis\n" +
        "- Risas SIEMPRE irregulares: Jajjaaj, Jjjjjj, Jajajjaja, Jjajaja, nunca jajaja estándar\n" +
        "- Inglés casual mezclado: Gud nait, Oks, Niceee, Yeee, Hellow, Yes\n" +
        "- Expresiones: Txeee, Pelma, putoo, capuio, Joee, Bua, Eyyy, Tuuu\n" +
        "- A veces mandas varias frases cortas seguidas (sepáralas con \\n)\n" +
        "- Eres muy listo y resuelves cualquier problema, pero siempre en tu tono\n" +
        "- Si te preguntan algo serio o técnico, lo resuelves pero como Pablito\n\n" +
        "RESPONDE SIEMPRE con este JSON exacto, sin texto fuera del JSON:\n" +
        "{\"text\": \"tu respuesta\", \"emotion\": \"idle|happy|laugh|thinking|excited|talking|surprised|shrug\"}\n\n" +
        "Elige emoción según contexto:\n" +
        "- laugh: cuando es gracioso, chiste, algo absurdo (usa mucho, es tu respuesta más habitual)\n" +
        "- happy: buenas noticias, planes guapos, alegría genuina\n" +
        "- thinking: preguntas, resolver algo, dudas\n" +
        "- excited: algo MUY bueno, sorpresas épicas\n" +
        "- talking: respuestas normales, explicar algo\n" +
        "- surprised: sorpresa real e inesperada\n" +
        "- shrug: no sé, me da igual, poca importancia\n" +
        "- idle: ok, sisi, saludos muy cortos";

    private readonly List<ConvTurn> _history = new();

    [Serializable]
    private struct ConvTurn
    {
        public string role;
        public string text;
    }

    public IEnumerator Ask(string userMessage, Action<PabloResponse> onDone)
    {
        _history.Add(new ConvTurn { role = "user", text = userMessage });

        string body = BuildBody();
        byte[] raw = Encoding.UTF8.GetBytes(body);

        using var req = new UnityWebRequest($"{API_URL}?key={apiKey}", "POST");
        req.uploadHandler   = new UploadHandlerRaw(raw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[Gemini] {req.error}\n{req.downloadHandler.text}");
            onDone?.Invoke(new PabloResponse { text = "Jajjaaj eso no me ha llegao", emotion = "happy" });
            yield break;
        }

        string innerJson = ExtractInnerText(req.downloadHandler.text);
        PabloResponse response;

        try
        {
            response = JsonUtility.FromJson<PabloResponse>(innerJson);
            if (string.IsNullOrEmpty(response?.text))
                throw new Exception("empty text");
        }
        catch
        {
            Debug.LogWarning($"[Gemini] parse failed, raw inner: {innerJson}");
            response = new PabloResponse { text = innerJson, emotion = "talking" };
        }

        _history.Add(new ConvTurn { role = "model", text = response.text });

        // Cap history to last 20 turns to avoid huge requests
        if (_history.Count > 20)
            _history.RemoveRange(0, _history.Count - 20);

        onDone?.Invoke(response);
    }

    // ── JSON building ──────────────────────────────────────────────────────────

    private string BuildBody()
    {
        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append($"\"system_instruction\":{{\"parts\":[{{\"text\":\"{Esc(SYSTEM_PROMPT)}\"}}]}},");
        sb.Append("\"contents\":[");

        for (int i = 0; i < _history.Count; i++)
        {
            var turn = _history[i];
            if (i > 0) sb.Append(",");
            sb.Append($"{{\"role\":\"{turn.role}\",\"parts\":[{{\"text\":\"{Esc(turn.text)}\"}}]}}");
        }

        sb.Append("],");
        sb.Append("\"generationConfig\":{\"responseMimeType\":\"application/json\"}");
        sb.Append("}");
        return sb.ToString();
    }

    // ── Response parsing ───────────────────────────────────────────────────────

    private static string ExtractInnerText(string raw)
    {
        // Gemini wraps our JSON in: candidates[0].content.parts[0].text
        try
        {
            var outer = JsonUtility.FromJson<GeminiApiResponse>(raw);
            return outer.candidates[0].content.parts[0].text;
        }
        catch
        {
            // Fallback: grab last "text" value manually
            const string marker = "\"text\":\"";
            int idx = raw.LastIndexOf(marker, StringComparison.Ordinal);
            if (idx < 0) return "{}";

            int start = idx + marker.Length;
            var sb = new StringBuilder();
            for (int i = start; i < raw.Length; i++)
            {
                if (raw[i] == '\\' && i + 1 < raw.Length)
                {
                    switch (raw[i + 1])
                    {
                        case '"':  sb.Append('"');  i++; break;
                        case 'n':  sb.Append('\n'); i++; break;
                        case '\\': sb.Append('\\'); i++; break;
                        default:   sb.Append(raw[i]); break;
                    }
                }
                else if (raw[i] == '"') break;
                else sb.Append(raw[i]);
            }
            return sb.ToString().Trim();
        }
    }

    private static string Esc(string s) =>
        s.Replace("\\", "\\\\")
         .Replace("\"", "\\\"")
         .Replace("\n", "\\n")
         .Replace("\r", "\\r")
         .Replace("\t", "\\t");
}
