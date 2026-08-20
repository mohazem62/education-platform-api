using EducationPlatform.Domain;
using EducationPlatform.Infrastructure;

namespace EducationPlatform.IntegrationTests;

public sealed class SessionRecurrenceTests
{
    [Fact]
    public void Weekly_session_expands_inside_requested_range()
    {
        var session = new ClassSession { ScheduledAt = new DateTimeOffset(2026, 8, 2, 9, 0, 0, TimeSpan.Zero), RecurrenceType = SessionRecurrenceType.Weekly };
        var occurrences = SessionRecurrence.Expand(session, new DateTimeOffset(2026, 8, 15, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 8, 31, 0, 0, 0, TimeSpan.Zero));
        Assert.Equal([new DateTimeOffset(2026, 8, 16, 9, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 8, 23, 9, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 8, 30, 9, 0, 0, TimeSpan.Zero)], occurrences);
    }

    [Fact]
    public void Monthly_session_preserves_original_day_when_possible()
    {
        var session = new ClassSession { ScheduledAt = new DateTimeOffset(2026, 1, 31, 18, 0, 0, TimeSpan.Zero), RecurrenceType = SessionRecurrenceType.Monthly };
        var occurrences = SessionRecurrence.Expand(session, new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero));
        Assert.Equal([new DateTimeOffset(2026, 2, 28, 18, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 3, 31, 18, 0, 0, TimeSpan.Zero)], occurrences);
    }
}
