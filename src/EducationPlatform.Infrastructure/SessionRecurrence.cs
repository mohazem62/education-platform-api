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
