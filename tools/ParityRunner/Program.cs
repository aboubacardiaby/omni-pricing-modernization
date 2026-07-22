namespace ParityRunner;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length > 0 && args[0].Equals("classify", StringComparison.OrdinalIgnoreCase))
            {
                return await ClassifyAsync(args[1..]).ConfigureAwait(false);
            }

            RunnerOptions options = RunnerOptions.Parse(args);
            using var cancellation = new CancellationTokenSource();
            ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cancellation.Cancel();
            };
            Console.CancelKeyPress += cancelHandler;
            try
            {
                var handler = new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                };
                using var httpClient = new HttpClient(handler)
                {
                    Timeout = options.Timeout,
                };
                var runner = new ParityExecutionRunner(
                    new HttpPricingEngineClient(httpClient, options.CobolAdapterEndpoint),
                    new HttpPricingEngineClient(httpClient, options.CSharpEndpoint));
                var cases = await ParityCaseFile.LoadAsync(options.CasesPath, cancellation.Token)
                    .ConfigureAwait(false);
                ParityRunReport report = await runner.RunAsync(
                    cases,
                    options.CobolAdapterEndpoint.ToString(),
                    options.CSharpEndpoint.ToString(),
                    cancellation.Token).ConfigureAwait(false);
                await ParityCaseFile.SaveReportAsync(options.OutputPath, report, cancellation.Token)
                    .ConfigureAwait(false);
                Console.WriteLine($"Captured {report.Cases.Length} parity cases in '{Path.GetFullPath(options.OutputPath)}'.");
                return 0;
            }
            finally
            {
                Console.CancelKeyPress -= cancelHandler;
            }
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Parity run cancelled.");
            return 2;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or HttpRequestException or IOException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static async Task<int> ClassifyAsync(string[] args)
    {
        ClassificationOptions options = ClassificationOptions.Parse(args);
        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };
        Console.CancelKeyPress += cancelHandler;
        try
        {
            ParityRunReport run = await ClassificationReportFile.LoadRunAsync(
                options.ReportPath,
                cancellation.Token).ConfigureAwait(false);
            ClassifiedParityReport report = ClassificationReportFile.Classify(
                run,
                options.RoundingTolerance);
            await ClassificationReportFile.SaveAsync(
                options.OutputPath,
                report,
                cancellation.Token).ConfigureAwait(false);
            int critical = report.Cases.Sum(item => item.Differences.Count(
                difference => difference.Severity == DifferenceSeverity.Critical));
            Console.WriteLine(
                $"Classified {report.Cases.Length} cases with {critical} critical differences in '{Path.GetFullPath(options.OutputPath)}'.");
            return critical == 0 ? 0 : 3;
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }
}

public sealed record RunnerOptions(
    string CasesPath,
    Uri CobolAdapterEndpoint,
    Uri CSharpEndpoint,
    string OutputPath,
    TimeSpan Timeout)
{
    public static RunnerOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length || !args[index].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException("Arguments must be supplied as --name value pairs.");
            }

            values[args[index][2..]] = args[index + 1];
        }

        string cases = Required("cases");
        string output = Required("output");
        Uri cobol = AbsoluteEndpoint("cobol-url");
        Uri csharp = AbsoluteEndpoint("csharp-url");
        int timeoutSeconds = values.TryGetValue("timeout-seconds", out string? timeoutValue)
            && int.TryParse(timeoutValue, out int parsed)
            && parsed is > 0 and <= 600
                ? parsed
                : 30;
        return new(cases, cobol, csharp, output, TimeSpan.FromSeconds(timeoutSeconds));

        string Required(string name) => values.TryGetValue(name, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException($"Missing required --{name} argument.");

        Uri AbsoluteEndpoint(string name) => Uri.TryCreate(Required(name), UriKind.Absolute, out Uri? uri)
            && uri.Scheme is "http" or "https"
                ? uri
                : throw new ArgumentException($"--{name} must be an absolute HTTP(S) URL.");
    }
}

public sealed record ClassificationOptions(
    string ReportPath,
    string OutputPath,
    decimal RoundingTolerance)
{
    public static ClassificationOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length || !args[index].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException("Arguments must be supplied as --name value pairs.");
            }

            values[args[index][2..]] = args[index + 1];
        }

        string report = Required("report");
        string output = Required("output");
        decimal tolerance = 0.01m;
        if (values.TryGetValue("rounding-tolerance", out string? configured)
            && (!decimal.TryParse(
                configured,
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out tolerance)
                || tolerance < 0m))
        {
            throw new ArgumentException("--rounding-tolerance must be a non-negative decimal.");
        }

        return new(report, output, tolerance);

        string Required(string name) => values.TryGetValue(name, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException($"Missing required --{name} argument.");
    }
}
