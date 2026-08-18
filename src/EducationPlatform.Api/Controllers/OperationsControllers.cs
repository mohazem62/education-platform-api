using EducationPlatform.Application;
using EducationPlatform.Domain;
using EducationPlatform.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EducationPlatform.Api.Controllers;

[ApiController, Route("api/v1/teacher-assignments"), Authorize(Policy = "AcademicOperations")]
public sealed class TeacherAssignmentsController(AppDbContext db, ICurrentUser current) : ControllerBase
{
    public sealed record AssignmentRequest(Guid TeacherId, Guid StudentId, Guid SubjectId, decimal SessionPrice = 0, string Currency = "EGP");
    [HttpPost] public async Task<IActionResult> Create(AssignmentRequest r, CancellationToken ct) { if (!await db.TeacherSubjects.AnyAsync(x => x.TeacherId == r.TeacherId && x.SubjectId == r.SubjectId, ct) || !await db.StudentSubjects.AnyAsync(x => x.StudentId == r.StudentId && x.SubjectId == r.SubjectId, ct)) throw new AppException(409, ErrorCodes.Validation, "المادة ليست مشتركة بين الطالب والمعلم."); if (r.SessionPrice < 0 || string.IsNullOrWhiteSpace(r.Currency) || r.Currency.Trim().Length != 3) throw new AppException(422, ErrorCodes.Validation, "سعر الحصة أو العملة غير صالح."); var x = new TeacherStudentAssignment { TeacherId = r.TeacherId, StudentId = r.StudentId, SubjectId = r.SubjectId, SessionPrice = r.SessionPrice, Currency = r.Currency.Trim().ToUpperInvariant(), AssignedAt = DateTimeOffset.UtcNow }; db.Add(x); await db.SaveChangesAsync(ct); return StatusCode(201, ApiResponse<object>.Ok(new { x.Id, x.SessionPrice, x.Currency })); }
    [HttpGet] public async Task<IActionResult> List([FromQuery] Guid? teacherId, [FromQuery] Guid? studentId, CancellationToken ct) { var q = db.TeacherStudentAssignments.AsNoTracking(); if (teacherId.HasValue) q = q.Where(x => x.TeacherId == teacherId); if (studentId.HasValue) q = q.Where(x => x.StudentId == studentId); return Ok(ApiResponse<object>.Ok(await q.Take(100).Select(x => new { x.Id, x.TeacherId, x.StudentId, x.SubjectId, x.SessionPrice, x.Currency, x.AssignedAt }).ToListAsync(ct))); }
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { var x = await db.TeacherStudentAssignments.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new AppException(404, ErrorCodes.NotFound, "التكليف غير موجود."); x.IsDeleted = true; x.DeletedAt = DateTimeOffset.UtcNow; x.DeletedBy = current.UserId; await db.SaveChangesAsync(ct); return NoContent(); }
}

[ApiController, Route("api/v1/materials"), Authorize]
public sealed class MaterialsController(AppDbContext db, IFileStorageService storage, ICurrentUser current) : ControllerBase
{
    [HttpPost, Authorize(Policy = "TeacherOnly"), RequestSizeLimit(10_485_760)]
    public async Task<IActionResult> Upload([FromForm] Guid subjectId, [FromForm] string title, [FromForm] string? description, IFormFile file, CancellationToken ct)
    {
        var teacher = await db.Teachers.SingleAsync(x => x.UserId == current.UserId, ct); if (!await db.TeacherSubjects.AnyAsync(x => x.TeacherId == teacher.Id && x.SubjectId == subjectId, ct)) throw new AppException(403, ErrorCodes.Forbidden, "المادة غير مسندة إلى المعلم.");
        await using var input = file.OpenReadStream(); var saved = await storage.SaveAsync(input, file.FileName, file.ContentType, ct); var x = new LessonMaterial { TeacherId = teacher.Id, SubjectId = subjectId, Title = title, Description = description, FileName = Path.GetFileName(file.FileName), StoredFileName = saved.StoredName, StorageKey = saved.Key, ContentType = file.ContentType, FileSize = file.Length }; db.Add(x); await db.SaveChangesAsync(ct); return StatusCode(201, ApiResponse<object>.Ok(new { x.Id, x.Title, x.FileName, x.ContentType, x.FileSize }));
    }
    [HttpGet] public async Task<IActionResult> List(CancellationToken ct) { var teacherId = await db.Teachers.Where(x => x.UserId == current.UserId).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct); var studentId = await db.Students.Where(x => x.UserId == current.UserId).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct); var q = db.LessonMaterials.AsNoTracking(); if (teacherId.HasValue) q = q.Where(x => x.TeacherId == teacherId); if (studentId.HasValue) q = q.Where(x => db.TeacherStudentAssignments.Any(a => a.StudentId == studentId && a.TeacherId == x.TeacherId && a.SubjectId == x.SubjectId)); return Ok(ApiResponse<object>.Ok(await q.Select(x => new { x.Id, x.TeacherId, x.SubjectId, x.Title, x.Description, x.FileName, x.ContentType, x.FileSize, x.CreatedAt }).ToListAsync(ct)));
    }
}

[ApiController, Route("api/v1/submissions"), Authorize]
public sealed class SubmissionsController(AppDbContext db, ICurrentUser current) : ControllerBase
{
    [HttpGet, Authorize(Policy = "TeacherOnly")] public async Task<IActionResult> List(CancellationToken ct) { var teacher = await db.Teachers.Where(x => x.UserId == current.UserId).Select(x => x.Id).SingleAsync(ct); return Ok(ApiResponse<object>.Ok(await db.AssignmentSubmissions.Where(x => db.Assignments.Any(a => a.Id == x.AssignmentId && a.TeacherId == teacher)).Select(x => new { x.Id, x.AssignmentId, x.StudentId, x.TextAnswer, status = x.Status.ToString(), x.SubmittedAt, x.Grade, x.TeacherFeedback }).ToListAsync(ct))); }

    [HttpGet("teacher-view"), Authorize(Policy = "TeacherOnly")]
    public async Task<ActionResult<ApiResponse<PageResult<TeacherSubmissionRowResponse>>>> TeacherView(
        [FromQuery] PageRequest page, [FromQuery] Guid? subjectId, [FromQuery] string? status, CancellationToken ct)
    {
        var teacherId = await db.Teachers.Where(x => x.UserId == current.UserId).Select(x => x.Id).SingleAsync(ct);
        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? "all" : status.Trim().Replace("-", "").Replace("_", "").ToLowerInvariant();
        if (normalizedStatus is not ("all" or "notsubmitted" or "pendinggrading" or "graded"))
            throw new AppException(422, ErrorCodes.Validation, "status must be All, NotSubmitted, PendingGrading, or Graded.", "status");

        var query =
            from target in db.AssignmentTargets.AsNoTracking()
            join assignment in db.Assignments.AsNoTracking() on target.AssignmentId equals assignment.Id
            join student in db.Students.AsNoTracking() on target.StudentId equals student.Id
            join gradeLevel in db.GradeLevels.AsNoTracking() on student.GradeLevelId equals gradeLevel.Id
            join subject in db.Subjects.AsNoTracking() on assignment.SubjectId equals subject.Id
            join submission in db.AssignmentSubmissions.AsNoTracking()
                on new { target.AssignmentId, target.StudentId } equals new { submission.AssignmentId, submission.StudentId } into submissions
            from submission in submissions.DefaultIfEmpty()
            where assignment.TeacherId == teacherId && assignment.Status != AssignmentStatus.Draft
            select new
            {
                target.AssignmentId,
                SubmissionId = submission == null ? (Guid?)null : submission.Id,
                target.StudentId,
                StudentName = student.FullName,
                GradeLevelName = gradeLevel.NameAr,
                assignment.SubjectId,
                SubjectName = subject.NameAr,
                AssignmentTitle = assignment.Title,
                assignment.MaxGrade,
                SubmissionStatus = submission == null ? (SubmissionStatus?)null : submission.Status,
                SubmittedAt = submission == null ? null : submission.SubmittedAt,
                Grade = submission == null ? null : submission.Grade,
                Feedback = submission == null ? null : submission.TeacherFeedback
            };

        if (subjectId.HasValue) query = query.Where(x => x.SubjectId == subjectId.Value);
        if (!string.IsNullOrWhiteSpace(page.Search))
        {
            var search = page.Search.Trim();
            query = query.Where(x => x.StudentName.Contains(search));
        }
        query = normalizedStatus switch
        {
            "notsubmitted" => query.Where(x => x.SubmissionStatus == null || x.SubmissionStatus == SubmissionStatus.Draft),
            "pendinggrading" => query.Where(x => x.SubmissionStatus == SubmissionStatus.Submitted || x.SubmissionStatus == SubmissionStatus.Late || x.SubmissionStatus == SubmissionStatus.Returned),
            "graded" => query.Where(x => x.SubmissionStatus == SubmissionStatus.Graded),
            _ => query
        };

        var pageNumber = Math.Max(1, page.PageNumber);
        var pageSize = Math.Clamp(page.PageSize, 1, 100);
        var totalCount = await query.CountAsync(ct);
        var rawItems = await query.OrderByDescending(x => x.SubmittedAt).ThenBy(x => x.StudentName).ThenBy(x => x.AssignmentTitle)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var items = rawItems.Select(x => new TeacherSubmissionRowResponse(
            x.AssignmentId, x.SubmissionId, x.StudentId, x.StudentName, x.GradeLevelName, x.SubjectId, x.SubjectName,
            x.AssignmentTitle, x.MaxGrade,
            x.SubmissionStatus switch
            {
                null or SubmissionStatus.Draft => "NotSubmitted",
                SubmissionStatus.Graded => "Graded",
                _ => "PendingGrading"
            },
            x.SubmittedAt, x.Grade, x.Feedback)).ToList();

        return Ok(ApiResponse<PageResult<TeacherSubmissionRowResponse>>.Ok(new(items, pageNumber, pageSize, totalCount)));
    }
}

[ApiController, Route("api/v1/finance"), Authorize(Policy = "FinancialAdmin")]
public sealed class FinanceController(IFinanceService service, AppDbContext db) : ControllerBase
{
    public sealed record PayoutPeriodRequest(DateOnly StartDate, DateOnly EndDate);
    [HttpPost("operating-expenses")] public async Task<IActionResult> Expense(ExpenseRequest request, CancellationToken ct) => StatusCode(201, ApiResponse<object>.Ok(new { id = await service.CreateExpenseAsync(request, ct) }));
    [HttpPost("periods")] public async Task<IActionResult> Period(FinancialPeriodRequest request, CancellationToken ct) => StatusCode(201, ApiResponse<object>.Ok(new { id = await service.CreatePeriodAsync(request, ct) }));
    [HttpPost("periods/{id:guid}/close")] public async Task<IActionResult> Close(Guid id, [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken ct) => Ok(ApiResponse<FinancialSummary>.Ok(await service.ClosePeriodAsync(id, key, ct)));
    [HttpPost("partner-shares")] public async Task<IActionResult> Share(PartnerShareRequest request, CancellationToken ct) => StatusCode(201, ApiResponse<object>.Ok(new { id = await service.SetPartnerShareAsync(request, ct) }));
    [HttpPost("payout-periods")] public async Task<IActionResult> CreatePayoutPeriod(PayoutPeriodRequest request, CancellationToken ct) { if (request.EndDate < request.StartDate) throw new AppException(422, ErrorCodes.Validation, "تاريخ دورة الراتب غير صالح."); var x = new TeacherPayoutPeriod { StartDate = request.StartDate, EndDate = request.EndDate }; db.Add(x); await db.SaveChangesAsync(ct); return StatusCode(201, ApiResponse<object>.Ok(new { x.Id })); }
    [HttpPost("payout-periods/{id:guid}/generate")] public async Task<IActionResult> Generate(Guid id, CancellationToken ct) { await service.GenerateTeacherPayoutsAsync(id, ct); return NoContent(); }
    [HttpPost("payouts/{id:guid}/{action}")] public async Task<IActionResult> Payout(Guid id, string action, CancellationToken ct) { await service.ChangePayoutStatusAsync(id, action, ct); return NoContent(); }
}

[ApiController, Route("api/v1/teacher-payouts"), Authorize]
public sealed class TeacherPayoutsController(AppDbContext db, ICurrentUser current) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List(CancellationToken ct) { var q = db.TeacherPayouts.AsNoTracking(); if (User.IsInRole(Roles.Teacher)) { var id = await db.Teachers.Where(x => x.UserId == current.UserId).Select(x => x.Id).SingleAsync(ct); q = q.Where(x => x.TeacherId == id); } else if (!User.IsInRole(Roles.Admin)) throw new AppException(403, ErrorCodes.Forbidden, "غير مسموح بعرض الرواتب."); return Ok(ApiResponse<object>.Ok(await q.Select(x => new { x.Id, x.TeacherId, x.PeriodId, x.GrossAmount, x.AdjustmentAmount, x.FinalAmount, status = x.Status.ToString(), x.ApprovedAt, x.PaidAt }).ToListAsync(ct))); }
}

[ApiController, Route("api/v1/payout-adjustments"), Authorize]
public sealed class PayoutAdjustmentsController(AppDbContext db, ICurrentUser current) : ControllerBase
{
    public sealed record CreateRequest(Guid TeacherPayoutId, string Type, decimal RequestedAmount, string Reason);
    public sealed record DecisionRequest(string Status, decimal? ApprovedAmount, string? AdminResponse);
    [HttpPost, Authorize(Policy = "TeacherOnly")] public async Task<IActionResult> Create(CreateRequest r, CancellationToken ct) { var teacher = await db.Teachers.Where(x => x.UserId == current.UserId).Select(x => x.Id).SingleAsync(ct); if (!await db.TeacherPayouts.AnyAsync(x => x.Id == r.TeacherPayoutId && x.TeacherId == teacher && x.Status != PayoutStatus.Paid, ct)) throw new AppException(404, ErrorCodes.NotFound, "كشف الراتب غير موجود أو مغلق."); var x = new TeacherPayoutAdjustmentRequest { TeacherPayoutId = r.TeacherPayoutId, TeacherId = teacher, Type = r.Type, RequestedAmount = r.RequestedAmount, Reason = r.Reason, Status = AdjustmentStatus.Pending }; db.Add(x); await db.SaveChangesAsync(ct); return StatusCode(201, ApiResponse<object>.Ok(new { x.Id })); }
    [HttpPost("{id:guid}/decision"), Authorize(Policy = "FinancialAdmin")] public async Task<IActionResult> Decide(Guid id, DecisionRequest r, CancellationToken ct) { await using var tx = await db.Database.BeginTransactionAsync(ct); var x = await db.TeacherPayoutAdjustmentRequests.SingleOrDefaultAsync(x => x.Id == id && x.Status == AdjustmentStatus.Pending, ct) ?? throw new AppException(409, ErrorCodes.Validation, "الطلب غير موجود أو تمت مراجعته."); if (!Enum.TryParse<AdjustmentStatus>(r.Status, true, out var status) || status == AdjustmentStatus.Pending) throw new AppException(422, ErrorCodes.Validation, "قرار غير صالح."); var approved = status is AdjustmentStatus.Approved or AdjustmentStatus.PartiallyApproved ? r.ApprovedAmount ?? (status == AdjustmentStatus.Approved ? x.RequestedAmount : throw new AppException(422, ErrorCodes.Validation, "القيمة المعتمدة مطلوبة للقبول الجزئي.")) : 0m; var payout = await db.TeacherPayouts.SingleAsync(p => p.Id == x.TeacherPayoutId, ct); if (payout.Status is PayoutStatus.Approved or PayoutStatus.Paid) throw new AppException(409, ErrorCodes.Validation, "لا يمكن تعديل كشف معتمد أو مدفوع."); payout.AdjustmentAmount += approved; payout.FinalAmount = payout.GrossAmount + payout.AdjustmentAmount; payout.Status = PayoutStatus.PendingReview; x.Status = status; x.AdminResponse = r.AdminResponse; x.ReviewedBy = current.UserId; x.ReviewedAt = DateTimeOffset.UtcNow; await db.AuditAsync(current, "PayoutAdjustmentDecided", nameof(TeacherPayoutAdjustmentRequest), x.Id, "Pending", $"{status}:{approved}", ct); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return NoContent(); }
}

[ApiController, Route("api/v1/notifications"), Authorize]
public sealed class NotificationsController(AppDbContext db, ICurrentUser current) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List([FromQuery] PageRequest p, CancellationToken ct) { var n = Math.Max(1, p.PageNumber); var z = Math.Clamp(p.PageSize, 1, 100); var q = db.Notifications.AsNoTracking().Where(x => x.UserId == current.UserId); var count = await q.CountAsync(ct); var items = await q.OrderByDescending(x => x.CreatedAt).Skip((n - 1) * z).Take(z).Select(x => new { x.Id, x.Type, x.Title, x.Body, x.IsRead, x.CreatedAt, x.ReadAt }).ToListAsync(ct); return Ok(ApiResponse<object>.Ok(new PageResult<object>(items, n, z, count))); }
    [HttpPatch("{id:guid}/read")] public async Task<IActionResult> Read(Guid id, CancellationToken ct) { var x = await db.Notifications.SingleOrDefaultAsync(x => x.Id == id && x.UserId == current.UserId, ct) ?? throw new AppException(404, ErrorCodes.NotFound, "الإشعار غير موجود."); x.IsRead = true; x.ReadAt = DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct); return NoContent(); }
    [HttpPatch("read-all")] public async Task<IActionResult> ReadAll(CancellationToken ct) { await db.Notifications.Where(x => x.UserId == current.UserId && !x.IsRead).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true).SetProperty(x => x.ReadAt, DateTimeOffset.UtcNow), ct); return NoContent(); }
}

[ApiController, Route("api/v1/audit-logs"), Authorize(Policy = "FinancialAdmin")]
public sealed class AuditLogsController(AppDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List([FromQuery] PageRequest p, CancellationToken ct) { var n = Math.Max(1, p.PageNumber); var z = Math.Clamp(p.PageSize, 1, 100); var q = db.AuditLogs.AsNoTracking().OrderByDescending(x => x.CreatedAt); var count = await q.CountAsync(ct); var items = await q.Skip((n - 1) * z).Take(z).Select(x => new { x.Id, x.UserId, x.Action, x.EntityType, x.EntityId, x.OldValues, x.NewValues, x.IpAddress, x.CreatedAt, x.CorrelationId }).ToListAsync(ct); return Ok(ApiResponse<object>.Ok(new PageResult<object>(items, n, z, count))); }
}

[ApiController, Route("api/v1/dashboards"), Authorize]
public sealed class DashboardsController(IDashboardService service) : ControllerBase
{
    [HttpGet("admin"), Authorize(Policy = "FinancialAdmin")] public async Task<IActionResult> Admin([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.AdminAsync(from, to, ct)));
    [HttpGet("teacher"), Authorize(Policy = "TeacherOnly")] public async Task<IActionResult> Teacher(CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.TeacherAsync(ct)));
    [HttpGet("student"), Authorize(Policy = "StudentOnly")] public async Task<IActionResult> Student(CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.StudentAsync(ct)));
    [HttpGet("partner"), Authorize(Policy = "PartnerOnly")] public async Task<IActionResult> Partner(CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.PartnerAsync(ct)));
}

[ApiController, Route("api/v1/reports"), Authorize(Policy = "FinancialAdmin")]
public sealed class ReportsController(AppDbContext db) : ControllerBase
{
    [HttpGet("attendance")] public async Task<IActionResult> Attendance(DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct) { var q = db.AttendanceRecords.AsNoTracking(); if (from.HasValue) q = q.Where(x => x.RequestedAt >= from); if (to.HasValue) q = q.Where(x => x.RequestedAt <= to); return Ok(ApiResponse<object>.Ok(await q.GroupBy(x => x.Status).Select(x => new { status = x.Key.ToString(), count = x.Count() }).ToListAsync(ct))); }
    [HttpGet("student-balances")] public async Task<IActionResult> Balances(CancellationToken ct) => Ok(ApiResponse<object>.Ok(await db.Students.Select(x => new { x.Id, x.FullName, x.SessionCreditBalance, x.ExpirationDate }).ToListAsync(ct)));
    [HttpGet("teacher-payouts")] public async Task<IActionResult> Payouts(CancellationToken ct) => Ok(ApiResponse<object>.Ok(await db.TeacherPayouts.Select(x => new { x.Id, x.TeacherId, x.GrossAmount, x.FinalAmount, status = x.Status.ToString() }).ToListAsync(ct)));
    [HttpGet("revenue")] public async Task<IActionResult> Revenue(CancellationToken ct) => Ok(ApiResponse<object>.Ok(new { total = await db.StudentPayments.Where(x => x.Status == RecordStatus.Paid).SumAsync(x => (decimal?)x.Amount, ct) ?? 0 }));
    [HttpGet("expenses")] public async Task<IActionResult> Expenses(CancellationToken ct) => Ok(ApiResponse<object>.Ok(await db.OperatingExpenses.Select(x => new { x.Id, x.Category, x.Amount, x.ExpenseDate, status = x.Status.ToString() }).ToListAsync(ct)));
    [HttpGet("partner-dividends")] public async Task<IActionResult> Dividends(CancellationToken ct) => Ok(ApiResponse<object>.Ok(await db.PartnerDividends.Select(x => new { x.Id, x.FinancialPeriodId, x.PartnerId, x.SharePercentageSnapshot, x.DividendAmount }).ToListAsync(ct)));
    [HttpGet("net-profit")] public async Task<IActionResult> NetProfit(DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct) { var start = from ?? DateTimeOffset.MinValue; var end = to ?? DateTimeOffset.MaxValue; var revenue = await db.StudentPayments.Where(x => x.Status == RecordStatus.Paid && x.PaidAt >= start && x.PaidAt <= end).SumAsync(x => (decimal?)x.Amount, ct) ?? 0; var costs = await db.TeacherPayouts.Where(x => (x.Status == PayoutStatus.Approved || x.Status == PayoutStatus.Paid) && x.ApprovedAt >= start && x.ApprovedAt <= end).SumAsync(x => (decimal?)x.FinalAmount, ct) ?? 0; var expenses = await db.OperatingExpenses.Where(x => x.Status == RecordStatus.Approved && x.CreatedAt >= start && x.CreatedAt <= end).SumAsync(x => (decimal?)x.Amount, ct) ?? 0; return Ok(ApiResponse<object>.Ok(new { revenue, teacherCosts = costs, operatingExpenses = expenses, netProfit = FinancialCalculator.NetProfit(revenue, costs, expenses) })); }
}
