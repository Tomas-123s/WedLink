using WedLink.Business.Invitations;

namespace WedLink.Tests.Invitations;

public sealed class WeddingTimePolicyTests
{
    [Fact]
    public void ToUtcConvertsCairoLocalTimeUsingTheCorrectOffset()
    {
        var utc = WeddingTimePolicy.ToUtc(new DateTime(2026, 1, 15, 12, 0, 0), "Africa/Cairo");
        Assert.Equal(new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero), utc);
    }

    [Fact]
    public void ToUtcRejectsUtcKindToAvoidAccidentalDoubleConversion()
    {
        Assert.Throws<ArgumentException>(() => WeddingTimePolicy.ToUtc(DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc), "Africa/Cairo"));
    }

    [Fact]
    public void AddCalendarMonthsClampsAtMonthEndInTheWeddingTimeZone()
    {
        var utc = WeddingTimePolicy.AddCalendarMonths(new DateTime(2026, 1, 31, 18, 0, 0), "Africa/Cairo", 1);
        Assert.Equal(new DateTimeOffset(2026, 2, 28, 16, 0, 0, TimeSpan.Zero), utc);
    }
}
