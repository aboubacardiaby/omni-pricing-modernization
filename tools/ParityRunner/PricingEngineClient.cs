namespace ParityRunner;

using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;

public interface IPricingEngineClient
{
    ValueTask<EngineObservation> ExecuteAsync(
        ReadOnlyMemory<byte> input,
        CancellationToken cancellationToken);
}

public sealed class HttpPricingEngineClient(HttpClient httpClient, Uri endpoint) : IPricingEngineClient
{
    public async ValueTask<EngineObservation> ExecuteAsync(
        ReadOnlyMemory<byte> input,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Content = new ByteArrayContent(input.ToArray());
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        long started = Stopwatch.GetTimestamp();
        using HttpResponseMessage response = await httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        byte[] responseBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        long elapsed = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;

        JsonElement? json = null;
        string? nonJson = null;
        if (responseBytes.Length > 0)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(responseBytes);
                json = document.RootElement.Clone();
            }
            catch (JsonException)
            {
                nonJson = System.Text.Encoding.UTF8.GetString(responseBytes);
            }
        }

        string? correlationId = response.Headers.TryGetValues("X-Correlation-ID", out IEnumerable<string>? values)
            ? values.FirstOrDefault()
            : null;
        return new EngineObservation(
            (int)response.StatusCode,
            json,
            nonJson,
            elapsed,
            correlationId);
    }
}
