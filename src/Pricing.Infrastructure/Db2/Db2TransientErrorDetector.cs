namespace Pricing.Infrastructure.Db2;

using System.Data.Common;

public static class Db2TransientErrorDetector
{
    private static readonly HashSet<string> TransientStates =
        new(StringComparer.Ordinal) { "40001", "57014", "57033", "58004" };

    private static readonly HashSet<int> TransientCodes = [-911, -913, -924];

    public static bool IsTransient(DbException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        string? state = exception.SqlState;
        return (state is not null && (state.StartsWith("08", StringComparison.Ordinal) || TransientStates.Contains(state)))
            || TransientCodes.Contains(exception.ErrorCode);
    }
}
