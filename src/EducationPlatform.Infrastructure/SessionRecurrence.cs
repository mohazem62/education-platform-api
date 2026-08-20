using EducationPlatform.Domain;

namespace EducationPlatform.Infrastructure;

public static class SessionRecurrence
{
    public static IReadOnlyList<DateTimeOffset> Expand(ClassSession session, DateTimeOffset rangeStart, DateTimeOffset rangeEnd)
    {
        var result = new List<DateTimeOffset>();
        if (rangeEnd <= rangeStart || session.ScheduledAt >= rangeEnd) return result;
        if (session.RecurrenceType == SessionRecurrenceType.Once)
        {
            if (session.ScheduledAt >= rangeStart) result.Add(session.ScheduledAt);
            return result;
        }

        var limit = session.RecurrenceEndDate;
        if (limit.HasValue && limit.Value < rangeStart) return result;
        if (session.RecurrenceType == SessionRecurrenceType.Weekly)
        {
            var elapsedWeeks = Math.Max(0, (int)Math.Floor((rangeStart - session.ScheduledAt).TotalDays / 7));
            var occurrence = session.ScheduledAt.AddDays(elapsedWeeks * 7);
            while (occurrence < rangeStart) occurrence = occurrence.AddDays(7);
            while (occurrence < rangeEnd && (!limit.HasValue || occurrence <= limit.Value))
            {
                result.Add(occurrence);
                occurrence = occurrence.AddDays(7);
            }
            return result;
        }

        var months = Math.Max(0, (rangeStart.Year - session.ScheduledAt.Year) * 12 + rangeStart.Month - session.ScheduledAt.Month - 1);
        var monthly = session.ScheduledAt.AddMonths(months);
        while (monthly < rangeStart) monthly = session.ScheduledAt.AddMonths(++months);
        while (monthly < rangeEnd && (!limit.HasValue || monthly <= limit.Value))
        {
            result.Add(monthly);
            monthly = session.ScheduledAt.AddMonths(++months);
        }
        return result;
    }
}

public sealed record SessionTimeConflict(Guid SessionId, DateTimeOffset RequestedOccurrence, DateTimeOffset ExistingOccurrence);

public static class SessionConflictDetector
{
    private static readonly TimeSpan SearchHorizon = TimeSpan.FromDays(366 * 5);

    public static SessionTimeConflict? Find(ClassSession requested, ClassSession existing)
    {
        var longestDuration = Math.Max(requested.DurationMinutes, existing.DurationMinutes);
        var rangeStart = (requested.ScheduledAt > existing.ScheduledAt ? requested.ScheduledAt : existing.ScheduledAt)
            .AddMinutes(-longestDuration);
        var requestedEnd = EffectiveEnd(requested);
        var existingEnd = EffectiveEnd(existing);
        var rangeEnd = requestedEnd < existingEnd ? requestedEnd : existingEnd;
        var horizonEnd = rangeStart.Add(SearchHorizon);
        if (rangeEnd > horizonEnd) rangeEnd = horizonEnd;
        if (rangeEnd <= rangeStart) return null;

        var requestedOccurrences = SessionRecurrence.Expand(requested, rangeStart, rangeEnd);
        var existingOccurrences = SessionRecurrence.Expand(existing, rangeStart, rangeEnd);
        var requestedIndex = 0;
        var existingIndex = 0;

        while (requestedIndex < requestedOccurrences.Count && existingIndex < existingOccurrences.Count)
        {
            var requestedAt = requestedOccurrences[requestedIndex];
            var existingAt = existingOccurrences[existingIndex];
            if (requestedAt < existingAt.AddMinutes(existing.DurationMinutes) &&
                existingAt < requestedAt.AddMinutes(requested.DurationMinutes))
                return new(existing.Id, requestedAt, existingAt);

            if (requestedAt <= existingAt) requestedIndex++;
            else existingIndex++;
        }

        return null;
    }

    private static DateTimeOffset EffectiveEnd(ClassSession session)
    {
        if (session.RecurrenceType == SessionRecurrenceType.Once)
            return session.ScheduledAt.AddMinutes(session.DurationMinutes).AddTicks(1);
        return session.RecurrenceEndDate?.AddMinutes(session.DurationMinutes).AddTicks(1) ?? DateTimeOffset.MaxValue;
    }
}
