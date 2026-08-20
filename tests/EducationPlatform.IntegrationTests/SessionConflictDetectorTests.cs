using EducationPlatform.Domain;
using EducationPlatform.Infrastructure;

namespace EducationPlatform.IntegrationTests;

public sealed class SessionConflictDetectorTests
{
    [Fact]
    public void Detects_overlapping_once_sessions()
    {
        var existing = Session(new DateTimeOffset(2026, 8, 22, 14, 0, 0, TimeSpan.Zero), 60);
        var requested = Session(new DateTimeOffset(2026, 8, 22, 14, 30, 0, TimeSpan.Zero), 60);

        Assert.NotNull(SessionConflictDetector.Find(requested, existing));
    }

    [Fact]
    public void Allows_back_to_back_sessions()
    {
        var existing = Session(new DateTimeOffset(2026, 8, 22, 14, 0, 0, TimeSpan.Zero), 60);
        var requested = Session(new DateTimeOffset(2026, 8, 22, 15, 0, 0, TimeSpan.Zero), 60);

        Assert.Null(SessionConflictDetector.Find(requested, existing));
    }

    [Fact]
    public void Detects_conflict_in_later_weekly_occurrence()
    {
        var existing = Session(new DateTimeOffset(2026, 8, 22, 14, 0, 0, TimeSpan.Zero), 60,
            SessionRecurrenceType.Weekly, new DateTimeOffset(2026, 10, 31, 14, 0, 0, TimeSpan.Zero));
        var requested = Session(new DateTimeOffset(2026, 8, 29, 14, 30, 0, TimeSpan.Zero), 30);

        Assert.NotNull(SessionConflictDetector.Find(requested, existing));
    }

    private static ClassSession Session(DateTimeOffset at, int duration, SessionRecurrenceType recurrence = SessionRecurrenceType.Once, DateTimeOffset? recurrenceEnd = null) =>
        new() { ScheduledAt = at, DurationMinutes = duration, RecurrenceType = recurrence, RecurrenceEndDate = recurrenceEnd };
}
