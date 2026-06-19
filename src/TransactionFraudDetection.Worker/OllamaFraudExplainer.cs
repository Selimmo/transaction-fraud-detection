using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace TransactionFraudDetection.Worker;

public class OllamaFraudExplainer(HttpClient httpClient, string model = "qwen3:8b") : IFraudExplainer
{
    public async Task<string> ExplainAsync(string prompt)
    {
        var response = await httpClient.PostAsJsonAsync(
            "/api/generate",
            new OllamaGenerateRequest(model, prompt, Stream: false, Think: false));

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>()
            ?? throw new InvalidOperationException("Ollama returned an empty or malformed response body.");

        return body.Response
            ?? throw new InvalidOperationException("Ollama returned a response body with no 'response' field.");
    }

    private record OllamaGenerateRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("prompt")] string Prompt,
        [property: JsonPropertyName("stream")] bool Stream,
        [property: JsonPropertyName("think")] bool Think);

    private record OllamaGenerateResponse(
        [property: JsonPropertyName("response")] string Response);
}
