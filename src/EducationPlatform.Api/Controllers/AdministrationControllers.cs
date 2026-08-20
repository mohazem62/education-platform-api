using EducationPlatform.Application;
using EducationPlatform.Domain;
using EducationPlatform.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EducationPlatform.Api.Controllers;

[ApiController, Route("api/v1/users"), Authorize(Roles = "Admin")]
public sealed class UsersController(UserManager<ApplicationUser> users, AppDbContext db) : ControllerBase
{
    public sealed record StaffRequest(string UserName, string Password, string DisplayName, string? Email, string PhoneNumber, string Role, string? ContactInformation);
    [HttpGet] public async Task<IActionResult> List([FromQuery] PageRequest p, CancellationToken ct) { var n = Math.Max(1, p.PageNumber); var z = Math.Clamp(p.PageSize, 1, 100); var q = users.Users.AsNoTracking(); if (!string.IsNullOrWhiteSpace(p.Search)) q = q.Where(x => x.UserName!.Contains(p.Search) || x.DisplayName.Contains(p.Search)); var count = await q.CountAsync(ct); var data = await q.OrderBy(x => x.DisplayName).Skip((n - 1) * z).Take(z).Select(x => new { x.Id, x.UserName, x.DisplayName, x.Email, x.PhoneNumber, x.LockoutEnd, x.IsDeleted }).ToListAsync(ct); return Ok(ApiResponse<object>.Ok(new PageResult<object>(data, n, z, count))); }
    [HttpPost("staff")] public async Task<IActionResult> CreateStaff(StaffRequest r, CancellationToken ct) { if (r.Role is not (Roles.Admin or Roles.Moderator or Roles.Partner)) throw new AppException(422, ErrorCodes.Validation, "هذا المسار مخصص للحسابات الإدارية والشركاء."); var user = new ApplicationUser { UserName = r.UserName, DisplayName = r.DisplayName, Email = r.Email, PhoneNumber = PhoneNormalizer.Normalize(r.PhoneNumber) }; var result = await users.CreateAsync(user, r.Password); if (!result.Succeeded) throw new AppException(422, ErrorCodes.Validation, string.Join(" ", result.Errors.Select(x => x.Description))); await users.AddToRoleAsync(user, r.Role); if (r.Role == Roles.Moderator) db.Moderators.Add(new Moderator { UserId = user.Id, FullName = r.DisplayName, PhoneNumber = user.PhoneNumber }); if (r.Role == Roles.Partner) db.Partners.Add(new Partner { UserId = user.Id, Name = r.DisplayName, ContactInformation = r.ContactInformation }); await db.SaveChangesAsync(ct); return StatusCode(201, ApiResponse<object>.Ok(new { user.Id, r.Role })); }
    [HttpPost("{id}/activate")] public async Task<IActionResult> Activate(string id) { var u = await users.FindByIdAsync(id) ?? throw new AppException(404, ErrorCodes.NotFound, "المستخدم غير موجود."); await users.SetLockoutEndDateAsync(u, null); return NoContent(); }
    [HttpPost("{id}/deactivate")] public async Task<IActionResult> Deactivate(string id) { var u = await users.FindByIdAsync(id) ?? throw new AppException(404, ErrorCodes.NotFound, "المستخدم غير موجود."); await users.SetLockoutEndDateAsync(u, DateTimeOffset.MaxValue); return NoContent(); }
}

[ApiController, Route("api/v1/moderators"), Authorize(Roles = Roles.Admin)]
public sealed class ModeratorsController(AppDbContext db, UserManager<ApplicationUser> users, ICurrentUser current, IDateTimeProvider clock) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ModeratorResponse>>>> List(CancellationToken ct)
    {
        var rows = await (from moderator in db.Moderators.AsNoTracking()
                          join user in db.Users.AsNoTracking() on moderator.UserId equals user.Id
                          orderby moderator.FullName
                          select new ModeratorResponse(moderator.Id, user.UserName!, moderator.FullName, moderator.PhoneNumber, moderator.Status.ToString())).ToListAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<ModeratorResponse>>.Ok(rows));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ModeratorResponse>>> Create(CreateModeratorRequest request, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var phone = PhoneNormalizer.Normalize(request.PhoneNumber);
        if (await db.Moderators.AnyAsync(x => x.PhoneNumber == phone, ct)) throw new AppException(409, ErrorCodes.Validation, "رقم الهاتف مستخدم بالفعل.", "phoneNumber");
        var user = new ApplicationUser { UserName = request.UserName, DisplayName = request.FullName, PhoneNumber = phone };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded) throw new AppException(400, ErrorCodes.Validation, string.Join(" ", result.Errors.Select(x => x.Description)));
        await users.AddToRoleAsync(user, Roles.Moderator);
        var moderator = new Moderator { UserId = user.Id, FullName = request.FullName, PhoneNumber = phone };
        db.Moderators.Add(moderator); await db.AuditAsync(current, "ModeratorCreated", nameof(Moderator), moderator.Id, null, request.FullName, ct);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return StatusCode(201, ApiResponse<ModeratorResponse>.Ok(new(moderator.Id, request.UserName, moderator.FullName, moderator.PhoneNumber, moderator.Status.ToString())));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ModeratorResponse>>> Update(Guid id, UpdateModeratorRequest request, CancellationToken ct)
    {
        var moderator = await db.Moderators.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new AppException(404, ErrorCodes.NotFound, "المشرف غير موجود.");
        if (!Enum.TryParse<AccountStatus>(request.Status, true, out var status)) throw new AppException(422, ErrorCodes.Validation, "حالة الحساب غير صالحة.", "status");
        moderator.FullName = request.FullName; moderator.PhoneNumber = PhoneNormalizer.Normalize(request.PhoneNumber); moderator.Status = status; moderator.UpdatedAt = clock.UtcNow;
        var user = await users.FindByIdAsync(moderator.UserId) ?? throw new AppException(404, ErrorCodes.NotFound, "حساب المشرف غير موجود."); user.DisplayName = request.FullName; user.PhoneNumber = moderator.PhoneNumber;
        await db.SaveChangesAsync(ct); return Ok(ApiResponse<ModeratorResponse>.Ok(new(moderator.Id, user.UserName!, moderator.FullName, moderator.PhoneNumber, moderator.Status.ToString())));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    {
        var moderator = await db.Moderators.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new AppException(404, ErrorCodes.NotFound, "المشرف غير موجود.");
        moderator.IsDeleted = true; moderator.DeletedAt = clock.UtcNow; moderator.DeletedBy = current.UserId;
        var user = await users.FindByIdAsync(moderator.UserId); if (user is not null) { user.IsDeleted = true; user.DeletedAt = clock.UtcNow; }
        await db.SaveChangesAsync(ct); return NoContent();
    }
}

[ApiController, Route("api/v1/partners"), Authorize(Roles = "Admin")]
public sealed class PartnersController(AppDbContext db) : ControllerBase { [HttpGet] public async Task<IActionResult> List(CancellationToken ct) => Ok(ApiResponse<object>.Ok(await db.Partners.Select(x => new { x.Id, x.UserId, x.Name, x.ContactInformation, status = x.Status.ToString() }).ToListAsync(ct))); }

[ApiController, Route("api/v1/archived-records"), Authorize(Roles = "Admin")]
public sealed class ArchivedRecordsController(AppDbContext db) : ControllerBase
{
    [HttpGet("students")] public async Task<IActionResult> Students(CancellationToken ct) => Ok(ApiResponse<object>.Ok(await db.Students.IgnoreQueryFilters().Where(x => x.IsDeleted).Select(x => new { x.Id, x.FullName, x.DeletedAt, x.DeletedBy }).ToListAsync(ct)));
    [HttpGet("teachers")] public async Task<IActionResult> Teachers(CancellationToken ct) => Ok(ApiResponse<object>.Ok(await db.Teachers.IgnoreQueryFilters().Where(x => x.IsDeleted).Select(x => new { x.Id, x.FullName, x.DeletedAt, x.DeletedBy }).ToListAsync(ct)));
}

[ApiController, Route("api/v1/financial-transactions"), Authorize(Roles = "Admin")]
public sealed class FinancialTransactionsController(AppDbContext db) : ControllerBase { [HttpGet] public async Task<IActionResult> List([FromQuery] Guid? financialPeriodId, CancellationToken ct) { var q = db.FinancialTransactions.AsNoTracking(); if (financialPeriodId.HasValue) q = q.Where(x => x.FinancialPeriodId == financialPeriodId); return Ok(ApiResponse<object>.Ok(await q.OrderByDescending(x => x.CreatedAt).Take(100).Select(x => new { x.Id, type = x.TransactionType.ToString(), x.ReferenceType, x.ReferenceId, x.Amount, direction = x.Direction.ToString(), x.Description, x.FinancialPeriodId, x.CreatedAt }).ToListAsync(ct))); } }

[ApiController, Route("api/v1/partner-dividends"), Authorize]
public sealed class PartnerDividendsController(AppDbContext db, ICurrentUser current) : ControllerBase { [HttpGet] public async Task<IActionResult> List(CancellationToken ct) { var q = db.PartnerDividends.AsNoTracking(); if (User.IsInRole(Roles.Partner)) { var id = await db.Partners.Where(x => x.UserId == current.UserId).Select(x => x.Id).SingleAsync(ct); q = q.Where(x => x.PartnerId == id); } else if (!User.IsInRole(Roles.Admin)) throw new AppException(403, ErrorCodes.Forbidden, "غير مسموح."); return Ok(ApiResponse<object>.Ok(await q.Select(x => new { x.Id, x.FinancialPeriodId, x.SharePercentageSnapshot, x.NetProfitSnapshot, x.DividendAmount, status = x.Status.ToString(), x.PaidAt }).ToListAsync(ct))); } }

[ApiController, Route("api/v1/operating-expenses"), Authorize(Roles = "Admin")]
public sealed class OperatingExpensesController(AppDbContext db, ICurrentUser current) : ControllerBase { [HttpGet] public async Task<IActionResult> List(CancellationToken ct) => Ok(ApiResponse<object>.Ok(await db.OperatingExpenses.Select(x => new { x.Id, x.Category, x.Description, x.Amount, x.ExpenseDate, status = x.Status.ToString() }).ToListAsync(ct))); [HttpPost("{id:guid}/{decision}")] public async Task<IActionResult> Decide(Guid id, string decision, CancellationToken ct) { var x = await db.OperatingExpenses.SingleOrDefaultAsync(x => x.Id == id && x.Status == RecordStatus.Pending, ct) ?? throw new AppException(409, ErrorCodes.Validation, "المصروف غير موجود أو تمت مراجعته."); x.Status = decision.ToLowerInvariant() switch { "approve" => RecordStatus.Approved, "reject" => RecordStatus.Rejected, _ => throw new AppException(422, ErrorCodes.Validation, "قرار غير صالح.") }; x.ApprovedBy = current.UserId; if (x.Status == RecordStatus.Approved) db.FinancialTransactions.Add(new FinancialTransaction { TransactionType = FinancialTransactionType.OperatingExpense, ReferenceType = nameof(OperatingExpense), ReferenceId = x.Id, Amount = x.Amount, Direction = TransactionDirection.Debit, Description = x.Description, CreatedBy = current.UserId! }); await db.AuditAsync(current, "OperatingExpenseDecided", nameof(OperatingExpense), x.Id, "Pending", x.Status.ToString(), ct); await db.SaveChangesAsync(ct); return NoContent(); } }

[ApiController, Route("api/v1/financial-periods"), Authorize(Roles = "Admin")]
public sealed class FinancialPeriodsController(AppDbContext db) : ControllerBase { [HttpGet] public async Task<IActionResult> List(CancellationToken ct) => Ok(ApiResponse<object>.Ok(await db.FinancialPeriods.Select(x => new { x.Id, x.StartDate, x.EndDate, status = x.Status.ToString(), x.ClosedAt, x.ClosedBy }).ToListAsync(ct))); }

[ApiController, Route("api/v1/student-credits"), Authorize]
public sealed class StudentCreditsController(AppDbContext db, ICurrentUser current) : ControllerBase
{
    [HttpGet("me")] public async Task<IActionResult> Mine(CancellationToken ct) { var id = await db.Students.Where(x => x.UserId == current.UserId).Select(x => x.Id).SingleOrDefaultAsync(ct); if (id == Guid.Empty) throw new AppException(403, ErrorCodes.Forbidden, "يلزم حساب طالب."); return Ok(ApiResponse<object>.Ok(await db.StudentCreditTransactions.Where(x => x.StudentId == id).OrderByDescending(x => x.CreatedAt).Take(100).Select(x => new { x.Id, type = x.Type.ToString(), x.Quantity, x.BalanceBefore, x.BalanceAfter, x.ReferenceType, x.ReferenceId, x.Description, x.CreatedAt }).ToListAsync(ct))); }
}

[ApiController, Route("api/v1/student-payments"), Authorize(Roles = "Admin")]
public sealed class StudentPaymentsController(AppDbContext db) : ControllerBase { [HttpGet] public async Task<IActionResult> List([FromQuery] Guid? studentId, CancellationToken ct) { var q = db.StudentPayments.AsNoTracking(); if (studentId.HasValue) q = q.Where(x => x.StudentId == studentId); return Ok(ApiResponse<object>.Ok(await q.OrderByDescending(x => x.PaidAt).Take(100).Select(x => new { x.Id, x.StudentId, x.Amount, x.Currency, x.PaymentMethod, x.PaymentReference, x.PaidAt, status = x.Status.ToString() }).ToListAsync(ct))); } }

[ApiController, Route("api/v1/profile"), Authorize]
public sealed class ProfileController(AppDbContext db, ICurrentUser current) : ControllerBase
{
    [HttpGet("student"), Authorize(Roles = "Student")] public async Task<IActionResult> Student(CancellationToken ct) => Ok(ApiResponse<object>.Ok(await db.Students.Where(x => x.UserId == current.UserId).Select(x => new { x.Id, x.FullName, x.PhoneNumber, x.ParentName, x.ParentPhoneNumber, x.GradeLevelId, x.CurriculumId, x.SessionCreditBalance, x.ExpirationDate, status = x.Status.ToString() }).SingleAsync(ct)));
    [HttpGet("teacher"), Authorize(Roles = "Teacher")] public async Task<IActionResult> Teacher(CancellationToken ct) => Ok(ApiResponse<object>.Ok(await db.Teachers.Where(x => x.UserId == current.UserId).Select(x => new { x.Id, x.FullName, x.PhoneNumber, x.WhatsApp, x.DefaultPerSessionRate, x.DefaultCurrency, x.PreferredPayoutMethod, payoutDestination = PhoneNormalizer.Mask(x.EWalletNumber ?? x.InstaPayIdentifier), status = x.Status.ToString() }).SingleAsync(ct)));
    [HttpGet("teacher/students"), Authorize(Roles = "Teacher")] public async Task<IActionResult> AssignedStudents(CancellationToken ct) { var id = await db.Teachers.Where(x => x.UserId == current.UserId).Select(x => x.Id).SingleAsync(ct); return Ok(ApiResponse<object>.Ok(await db.TeacherStudentAssignments.Where(x => x.TeacherId == id).Select(x => new { x.StudentId, x.SubjectId, x.SessionPrice, x.Currency, studentName = db.Students.Where(s => s.Id == x.StudentId).Select(s => s.FullName).Single() }).ToListAsync(ct))); }
}

[ApiController, Route("api/v1/schedules"), Authorize(Roles = "Admin,Moderator,Teacher,Student")]
public sealed class SchedulesController(AppDbContext db, ICurrentUser current, IDateTimeProvider clock) : ControllerBase
{
    private static readonly string[] Themes = ["blue", "orange", "purple", "emerald", "rose"];
    private static readonly TimeZoneInfo CairoTimeZone = FindCairoTimeZone();

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageResult<WeeklyScheduleResponse>>>> List([FromQuery] PageRequest page, [FromQuery] DateOnly? weekStart, [FromQuery] Guid? studentId, [FromQuery] Guid? teacherId, [FromQuery] Guid? subjectId, CancellationToken ct)
    {
        var localToday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.UtcNow, CairoTimeZone).DateTime);
        var startDate = weekStart ?? localToday.AddDays(-(((int)localToday.DayOfWeek - (int)DayOfWeek.Saturday + 7) % 7));
        var from = AtCairo(startDate); var to = AtCairo(startDate.AddDays(7));
        var query = db.Sessions.AsNoTracking().Where(x => x.ScheduledAt < to &&
            (x.RecurrenceType == SessionRecurrenceType.Once ? x.ScheduledAt >= from : x.RecurrenceEndDate == null || x.RecurrenceEndDate >= from));
        if (User.IsInRole(Roles.Student)) { var id = await db.Students.Where(x => x.UserId == current.UserId).Select(x => x.Id).SingleAsync(ct); query = query.Where(x => x.StudentId == id); }
        else if (User.IsInRole(Roles.Teacher)) { var id = await db.Teachers.Where(x => x.UserId == current.UserId).Select(x => x.Id).SingleAsync(ct); query = query.Where(x => x.TeacherId == id); }
        else { if (studentId.HasValue) query = query.Where(x => x.StudentId == studentId); if (teacherId.HasValue) query = query.Where(x => x.TeacherId == teacherId); }
        if (subjectId.HasValue) query = query.Where(x => x.SubjectId == subjectId);
        var rows = await (from session in query
                          join student in db.Students.AsNoTracking() on session.StudentId equals student.Id
                          join teacher in db.Teachers.AsNoTracking() on session.TeacherId equals teacher.Id
                          join subject in db.Subjects.AsNoTracking() on session.SubjectId equals subject.Id
                          select new { Session = session, StudentName = student.FullName, TeacherName = teacher.FullName, SubjectName = subject.NameAr }).ToListAsync(ct);
        var occurrences = rows.SelectMany(row => SessionRecurrence.Expand(row.Session, from, to)
            .Select(at => Map(row.Session, row.StudentName, row.TeacherName, row.SubjectName, at))).OrderBy(x => x.OccurrenceAt).ToList();
        var number = Math.Max(1, page.PageNumber); var size = Math.Clamp(page.PageSize, 1, 100); var total = occurrences.Count;
        return Ok(ApiResponse<PageResult<WeeklyScheduleResponse>>.Ok(new(occurrences.Skip((number - 1) * size).Take(size).ToList(), number, size, total)));
    }

    private static WeeklyScheduleResponse Map(ClassSession x, string student, string teacher, string subject, DateTimeOffset occurrenceAt)
    {
        var local = TimeZoneInfo.ConvertTime(occurrenceAt, CairoTimeZone); var end = local.AddMinutes(x.DurationMinutes);
        var startTime = TimeOnly.FromDateTime(local.DateTime); var endTime = TimeOnly.FromDateTime(end.DateTime);
        return new(x.Id, x.StudentId, student, x.TeacherId, teacher, x.SubjectId, subject, local.DayOfWeek, ArabicDay(local.DayOfWeek), startTime, endTime,
            local.ToString("HH:mm"), $"{local:HH:mm} - {end:HH:mm}", local.Hour < 12 ? "am" : "pm", Themes[x.SubjectId.ToByteArray()[0] % Themes.Length],
            x.ClassLink, teacher, student, x.ClassLink, x.RecurrenceType, x.RecurrenceEndDate, occurrenceAt);
    }
    private static DateTimeOffset AtCairo(DateOnly date) { var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified); return new DateTimeOffset(local, CairoTimeZone.GetUtcOffset(local)); }
    private static string ArabicDay(DayOfWeek day) => day switch { DayOfWeek.Saturday => "السبت", DayOfWeek.Sunday => "الأحد", DayOfWeek.Monday => "الإثنين", DayOfWeek.Tuesday => "الثلاثاء", DayOfWeek.Wednesday => "الأربعاء", DayOfWeek.Thursday => "الخميس", _ => "الجمعة" };
    private static TimeZoneInfo FindCairoTimeZone() { foreach (var id in new[] { "Egypt Standard Time", "Africa/Cairo" }) try { return TimeZoneInfo.FindSystemTimeZoneById(id); } catch (TimeZoneNotFoundException) { } return TimeZoneInfo.Utc; }
}
