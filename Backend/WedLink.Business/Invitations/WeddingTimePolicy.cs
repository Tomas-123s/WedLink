namespace WedLink.Business.Invitations;

public static class WeddingTimePolicy
{
    public static DateTimeOffset AddCalendarMonths(DateTime localDateTime, string timeZoneId, int months)
    {
        if (localDateTime.Kind == DateTimeKind.Utc)
            throw new ArgumentException("Expected a local civil date and time.", nameof(localDateTime));
        var target = DateTime.SpecifyKind(localDateTime.AddMonths(months), DateTimeKind.Unspecified);
        return ToUtc(target, timeZoneId);
    }

    public static DateTimeOffset ToUtc(DateTime localDateTime, string timeZoneId)
    {
        if (localDateTime.Kind == DateTimeKind.Utc)
            throw new ArgumentException("Expected a local civil date and time.", nameof(localDateTime));
        var zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var target = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        var advancedMinutes = 0;
        while (zone.IsInvalidTime(target) && advancedMinutes < 24 * 60)
        {
            target = target.AddMinutes(1);
            advancedMinutes++;
        }
        if (zone.IsInvalidTime(target)) throw new InvalidTimeZoneException("Could not resolve a local time skipped by daylight saving.");

        // When the local time occurs twice, use the larger offset (the earlier UTC instant).
        var offset = zone.IsAmbiguousTime(target)
            ? zone.GetAmbiguousTimeOffsets(target).Max()
            : zone.GetUtcOffset(target);
        return new DateTimeOffset(target, offset).ToUniversalTime();
    }
}
