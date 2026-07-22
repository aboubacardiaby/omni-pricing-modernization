namespace PricingLoadRunner;

using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;

public interface ILoadTarget
{
    ValueTask<LoadObservation> ExecuteAsync(ReadOnlyMemory<byte> request, CancellationToken cancellationToken);
}

public sealed class HttpLoadTarget(HttpClient client, Uri endpoint) : ILoadTarget
{
    public async ValueTask<LoadObservation> ExecuteAsync(
        ReadOnlyMemory<byte> request,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, endpoint);
        message.Content = new ByteArrayContent(request.ToArray());
        message.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        long started = Stopwatch.GetTimestamp();
        using HttpResponseMessage response = await client
            .SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        await response.Content.CopyToAsync(Stream.Null, cancellationToken).ConfigureAwait(false);
        double elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        return new(
            (int)response.StatusCode,
            elapsed,
            Header<int>(response, "X-DB-Call-Count", int.TryParse),
            Header<long>(response, "X-Process-Working-Set-Bytes", long.TryParse));
    }

    private delegate bool Parser<T>(string value, NumberStyles styles, IFormatProvider? provider, out T result);

    private static T? Header<T>(HttpResponseMessage response, string name, Parser<T> parser)
        where T : struct
    {
        if (!response.Headers.TryGetValues(name, out IEnumerable<string>? values))
        {
            return null;
        }

        string? value = values.FirstOrDefault();
        return value is not null
            && parser(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out T parsed)
                ? parsed
                : null;
    }
}
