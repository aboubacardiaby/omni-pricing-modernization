namespace PricingLoadRunner;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            (string planPath, string outputPath, TimeSpan timeout) = Parse(args);
            using var cancellation = new CancellationTokenSource();
            ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cancellation.Cancel();
            };
            Console.CancelKeyPress += cancelHandler;
            try
            {
                LoadPlan plan = await LoadPlanFile.LoadAsync(planPath, cancellation.Token).ConfigureAwait(false);
                using var client = new HttpClient { Timeout = timeout };
                var runner = new LoadTestRunner(new HttpLoadTarget(client, plan.Endpoint));
                LoadTestReport report = await runner.RunAsync(plan, cancellation.Token).ConfigureAwait(false);
                await LoadPlanFile.SaveAsync(outputPath, report, cancellation.Token).ConfigureAwait(false);
                Console.WriteLine($"Measured {report.Workloads.Length} workloads in '{Path.GetFullPath(outputPath)}'.");
                return report.Workloads.Any(item => item.Errors > 0) ? 3 : 0;
            }
            finally
            {
                Console.CancelKeyPress -= cancelHandler;
            }
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Load test cancelled.");
            return 2;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or IOException or HttpRequestException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static (string Plan, string Output, TimeSpan Timeout) Parse(string[] args)
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

        string plan = Required("plan");
        string output = Required("output");
        int seconds = values.TryGetValue("timeout-seconds", out string? configured)
            && int.TryParse(configured, out int parsed)
            && parsed is > 0 and <= 600
                ? parsed
                : 30;
        return (plan, output, TimeSpan.FromSeconds(seconds));

        string Required(string name) => values.TryGetValue(name, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException($"Missing required --{name} argument.");
    }
}
