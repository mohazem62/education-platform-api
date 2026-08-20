using EducationPlatform.Application;
using EducationPlatform.Domain;
using EducationPlatform.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EducationPlatform.Api.Controllers;

[ApiController, Route("api/v1/home"), Authorize]
public sealed class HomeController(AppDbContext db, ICurrentUser current, IDateTimeProvider clock) : ControllerBase
{
    private static readonly string[] Themes = ["blue", "orange", "purple", "emerald", "rose"];
    private static readonly TimeZoneInfo CairoTimeZone = FindCairoTimeZone();

    [HttpGet("cards")]
    public async Task<ActionResult<ApiResponse<HomeCardsResponse>>> Cards(CancellationToken ct)
    {
        var isTeacher = User.IsInRole(Roles.Teacher);
        var isStudent = User.IsInRole(Roles.Student);
        if (!isTeacher && !isStudent) throw new AppException(403, ErrorCodes.Forbidden, "This endpoint is available to teachers and students only.");

        var now = clock.UtcNow;
        var localNow = TimeZoneInfo.ConvertTime(now, CairoTimeZone);
        var monthStart = AtCairo(new DateOnly(localNow.Year, localNow.Month, 1));
        var nextMonth = AtCairo(new DateOnly(localNow.Year, localNow.Month, 1).AddMonths(1));
        Guid profileId;
        int registeredSubjects;
        List<(decimal Grade, decimal MaxGrade)> grades;

        if (isTeacher)
        {
            profileId = await db.Teachers.Where(x => x.UserId == current.UserId).Select(x => x.Id).SingleAsync(ct);
            registeredSubjects = await db.TeacherSubjects.CountAsync(x => x.TeacherId == profileId, ct);
            var gradeRows = await (from submission in db.AssignmentSubmissions.AsNoTracking()
                                   join assignment in db.Assignments.AsNoTracking() on submission.AssignmentId equals assignment.Id
                                   where assignment.TeacherId == profileId && submission.Status == SubmissionStatus.Graded && submission.Grade.HasValue
                                   select new { Grade = submission.Grade!.Value, assignment.MaxGrade }).ToListAsync(ct);
            grades = gradeRows.Select(x => (x.Grade, x.MaxGrade)).ToList();
        }
        else
        {
            profileId = await db.Students.Where(x => x.UserId == current.UserId).Select(x => x.Id).SingleAsync(ct);
            registeredSubjects = await db.StudentSubjects.CountAsync(x => x.StudentId == profileId, ct);
            var gradeRows = await (from submission in db.AssignmentSubmissions.AsNoTracking()
                                   join assignment in db.Assignments.AsNoTracking() on submission.AssignmentId equals assignment.Id
                                   where submission.StudentId == profileId && submission.Status == SubmissionStatus.Graded && submission.Grade.HasValue
                                   select new { Grade = submission.Grade!.Value, assignment.MaxGrade }).ToListAsync(ct);
            grades = gradeRows.Select(x => (x.Grade, x.MaxGrade)).ToList();
        }

        var monthSessions = await LoadSessions(profileId, isTeacher, monthStart, nextMonth, ct);
        var total = monthSessions.Count;
        var attended = monthSessions.Count(x => x.Status == SessionStatus.Completed);
        var currentCandidates = await LoadSessions(profileId, isTeacher, now.AddHours(-8), now.AddDays(30), ct);
        var currentSession = currentCandidates.FirstOrDefault(x => x.ScheduledAt <= now && x.ScheduledAt.AddMinutes(x.DurationMinutes) > now)
                             ?? currentCandidates.FirstOrDefault(x => x.ScheduledAt > now);
        var average = grades.Count == 0 ? 0 : Math.Round(grades.Average(x => x.MaxGrade == 0 ? 0 : x.Grade * 100m / x.MaxGrade), 2);
        var response = new HomeCardsResponse(isTeacher ? Roles.Teacher : Roles.Student, average, registeredSubjects,
            new HomeMonthlySessionsResponse(attended, total, $"{localNow:yyyy-MM}"), currentSession is null ? null : Map(currentSession, now, isTeacher));
        return Ok(ApiResponse<HomeCardsResponse>.Ok(response));
    }

    [HttpGet("schedule")]
    public async Task<ActionResult<ApiResponse<HomeScheduleResponse>>> Schedule([FromQuery] DateOnly? weekStart, CancellationToken ct)
    {
        var isTeacher = User.IsInRole(Roles.Teacher);
        var isStudent = User.IsInRole(Roles.Student);
        if (!isTeacher && !isStudent) throw new AppException(403, ErrorCodes.Forbidden, "This endpoint is available to teachers and students only.");

        var localToday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.UtcNow, CairoTimeZone).DateTime);
        var startDate = weekStart ?? localToday.AddDays(-(((int)localToday.DayOfWeek - (int)DayOfWeek.Saturday + 7) % 7));
        var endDate = startDate.AddDays(6);
        var from = AtCairo(startDate);
        var to = AtCairo(endDate.AddDays(1));
        var profileId = isTeacher
            ? await db.Teachers.Where(x => x.UserId == current.UserId).Select(x => x.Id).SingleAsync(ct)
            : await db.Students.Where(x => x.UserId == current.UserId).Select(x => x.Id).SingleAsync(ct);
        var sessions = await LoadSessions(profileId, isTeacher, from, to, ct);
        var items = sessions.OrderBy(x => x.ScheduledAt).Select(x => Map(x, clock.UtcNow, isTeacher)).ToList();
        return Ok(ApiResponse<HomeScheduleResponse>.Ok(new(startDate, endDate, items)));
    }

    private async Task<List<HomeSessionRow>> LoadSessions(Guid profileId, bool isTeacher, DateTimeOffset rangeStart, DateTimeOffset rangeEnd, CancellationToken ct)
    {
        var sessionQuery = db.Sessions.AsNoTracking().Where(x => x.ScheduledAt < rangeEnd &&
            (x.RecurrenceType == SessionRecurrenceType.Once ? x.ScheduledAt >= rangeStart : x.RecurrenceEndDate == null || x.RecurrenceEndDate >= rangeStart));
        sessionQuery = isTeacher ? sessionQuery.Where(x => x.TeacherId == profileId) : sessionQuery.Where(x => x.StudentId == profileId);
        var rows = await (from session in sessionQuery
                          join subject in db.Subjects.AsNoTracking() on session.SubjectId equals subject.Id
                          join teacher in db.Teachers.AsNoTracking() on session.TeacherId equals teacher.Id
                          join student in db.Students.AsNoTracking() on session.StudentId equals student.Id
                          select new { Session = session, Subject = subject.NameAr, TeacherName = teacher.FullName, StudentName = student.FullName }).ToListAsync(ct);
        return rows.SelectMany(row => SessionRecurrence.Expand(row.Session, rangeStart, rangeEnd)
            .Select(occurrence => new HomeSessionRow(row.Session.Id, row.Session.SubjectId, row.Subject, row.TeacherName, row.StudentName,
                occurrence, row.Session.DurationMinutes, row.Session.ClassLink, row.Session.Status))).ToList();
    }

    private static HomeClassItemResponse Map(HomeSessionRow row, DateTimeOffset now, bool isTeacher)
    {
        var local = TimeZoneInfo.ConvertTime(row.ScheduledAt, CairoTimeZone);
        var end = local.AddMinutes(row.DurationMinutes);
        var oppositeName = isTeacher ? row.StudentName : row.TeacherName;
        return new(row.Id, row.Subject, isTeacher ? null : row.TeacherName, oppositeName, ArabicDay(local.DayOfWeek),
            local.ToString("HH:mm"), $"{local:HH:mm} - {end:HH:mm}", local.Hour < 12 ? "am" : "pm",
            Themes[row.SubjectId.ToByteArray()[0] % Themes.Length], row.ClassLink, row.ScheduledAt, row.DurationMinutes,
            row.Status.ToString(), row.ScheduledAt <= now && row.ScheduledAt.AddMinutes(row.DurationMinutes) > now);
    }

    private static DateTimeOffset AtCairo(DateOnly date)
    {
        var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, CairoTimeZone.GetUtcOffset(local));
    }

    private static string ArabicDay(DayOfWeek day) => day switch
    {
        DayOfWeek.Saturday => "السبت", DayOfWeek.Sunday => "الأحد", DayOfWeek.Monday => "الإثنين",
        DayOfWeek.Tuesday => "الثلاثاء", DayOfWeek.Wednesday => "الأربعاء", DayOfWeek.Thursday => "الخميس",
        _ => "الجمعة"
    };

    private static TimeZoneInfo FindCairoTimeZone()
    {
        foreach (var id in new[] { "Egypt Standard Time", "Africa/Cairo" })
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); } catch (TimeZoneNotFoundException) { }
        return TimeZoneInfo.Utc;
    }

    private sealed record HomeSessionRow(Guid Id, Guid SubjectId, string Subject, string TeacherName, string StudentName,
        DateTimeOffset ScheduledAt, int DurationMinutes, string? ClassLink, SessionStatus Status);
}
