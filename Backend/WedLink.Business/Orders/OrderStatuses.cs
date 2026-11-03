namespace WedLink.Business.Orders;

public static class OrderStatuses
{
    public const string New = "New";
    public const string Confirmed = "Confirmed";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    private static readonly IReadOnlyDictionary<string, string[]> AllowedTransitions =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [New] = [Confirmed, Cancelled],
            [Confirmed] = [InProgress, Cancelled],
            [InProgress] = [Completed],
            [Completed] = [],
            [Cancelled] = []
        };

    public static bool TryNormalize(string? status, out string normalized)
    {
        normalized = AllowedTransitions.Keys.FirstOrDefault(key =>
            string.Equals(key, status?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
        return normalized.Length > 0;
    }

    public static bool CanTransition(string currentStatus, string newStatus) =>
        AllowedTransitions.TryGetValue(currentStatus, out var allowed)
        && allowed.Contains(newStatus, StringComparer.OrdinalIgnoreCase);
}
