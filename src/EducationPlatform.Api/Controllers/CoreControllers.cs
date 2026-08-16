using EducationPlatform.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using EducationPlatform.Infrastructure;

namespace EducationPlatform.Api.Controllers;

[ApiController, Route("api/v1/auth")]
public sealed class AuthController(IAuthService service) : ControllerBase
{
    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("auth")] public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(LoginRequest request, CancellationToken ct) => Ok(ApiResponse<AuthResponse>.Ok(await service.LoginAsync(request, ct), "تم تسجيل الدخول بنجاح."));
    [HttpPost("refresh-token"), AllowAnonymous, EnableRateLimiting("auth")] public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh(RefreshRequest request, CancellationToken ct) => Ok(ApiResponse<AuthResponse>.Ok(await service.RefreshAsync(request, ct), "تم تحديث الجلسة."));
    [HttpPost("logout"), Authorize] public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken ct) { await service.LogoutAsync(request.RefreshToken, ct); return NoContent(); }
    [HttpPost("change-password"), Authorize] public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct) { await service.ChangePasswordAsync(request, ct); return NoContent(); }
    [HttpPost("forgot-password"), AllowAnonymous] public async Task<IActionResult> Forgot(ForgotPasswordRequest request, CancellationToken ct) { await service.ForgotPasswordAsync(request, ct); return Ok(ApiResponse<object>.Ok(new { }, "إذا كان الحساب موجودًا فستصل تعليمات الاستعادة.")); }
    [HttpPost("reset-password"), AllowAnonymous] public async Task<IActionResult> Reset(ResetPasswordRequest request, CancellationToken ct) { await service.ResetPasswordAsync(request, ct); return NoContent(); }
    [HttpGet("me"), Authorize] public async Task<ActionResult<ApiResponse<CurrentUserResponse>>> Me(CancellationToken ct) => Ok(ApiResponse<CurrentUserResponse>.Ok(await service.MeAsync(ct)));
}

[ApiController, Route("api/v1/students"), Authorize(Policy = "AcademicOperations")]
public sealed class StudentsController(IStudentService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<PageResult<StudentResponse>>>> Search([FromQuery] PageRequest page, [FromQuery] Guid? teacherId, [FromQuery] Guid? subjectId, [FromQuery] string? status, CancellationToken ct) => Ok(ApiResponse<PageResult<StudentResponse>>.Ok(await service.SearchAsync(page, teacherId, subjectId, status, ct)));
    [HttpGet("{id:guid}")] public async Task<ActionResult<ApiResponse<StudentResponse>>> Get(Guid id, CancellationToken ct) => Ok(ApiResponse<StudentResponse>.Ok(await service.GetAsync(id, ct)));
    [HttpPost] public async Task<ActionResult<ApiResponse<StudentResponse>>> Create(CreateStudentRequest request, CancellationToken ct) { var item = await service.CreateAsync(request, ct); return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<StudentResponse>.Ok(item, "تم إنشاء الطالب بنجاح.")); }
    [HttpPut("{id:guid}")] public async Task<ActionResult<ApiResponse<StudentResponse>>> Update(Guid id, UpdateStudentRequest request, CancellationToken ct) => Ok(ApiResponse<StudentResponse>.Ok(await service.UpdateAsync(id, request, ct)));
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Archive(Guid id, CancellationToken ct) { await service.ArchiveAsync(id, ct); return NoContent(); }
    [HttpPost("{id:guid}/restore"), Authorize(Roles = "Admin")] public async Task<IActionResult> Restore(Guid id, CancellationToken ct) { await service.RestoreAsync(id, ct); return NoContent(); }
    [HttpPost("{id:guid}/credits/adjust"), Authorize(Roles = "Admin")] public async Task<IActionResult> Credits(Guid id, CreditAdjustmentRequest request, CancellationToken ct) { await service.AdjustCreditsAsync(id, request, ct); return NoContent(); }
    [HttpPost("{id:guid}/payments"), Authorize(Roles = "Admin")] public async Task<IActionResult> Payment(Guid id, PaymentRequest request, CancellationToken ct) { await service.RecordPaymentAsync(id, request, ct); return NoContent(); }
}

[ApiController, Route("api/v1/teachers"), Authorize]
public sealed class TeachersController(ITeacherService service) : ControllerBase
{
    [HttpGet, Authorize(Policy = "AcademicOperations")] public async Task<ActionResult<ApiResponse<PageResult<TeacherResponse>>>> Search([FromQuery] PageRequest page, [FromQuery] Guid? subjectId, [FromQuery] Guid? curriculumId, CancellationToken ct) => Ok(ApiResponse<PageResult<TeacherResponse>>.Ok(await service.SearchAsync(page, subjectId, curriculumId, ct)));
    [HttpGet("{id:guid}"), Authorize(Policy = "AcademicOperations")] public async Task<ActionResult<ApiResponse<TeacherResponse>>> Get(Guid id, CancellationToken ct) => Ok(ApiResponse<TeacherResponse>.Ok(await service.GetAsync(id, ct)));
    [HttpPost, Authorize(Policy = "AcademicOperations")] public async Task<ActionResult<ApiResponse<TeacherResponse>>> Create(CreateTeacherRequest request, CancellationToken ct) { var x = await service.CreateAsync(request, ct); return CreatedAtAction(nameof(Get), new { id = x.Id }, ApiResponse<TeacherResponse>.Ok(x)); }
    [HttpPut("{id:guid}"), Authorize(Policy = "AcademicOperations")] public async Task<IActionResult> Update(Guid id, UpdateTeacherRequest request, CancellationToken ct) => Ok(ApiResponse<TeacherResponse>.Ok(await service.UpdateAsync(id, request, ct)));
    [HttpDelete("{id:guid}"), Authorize(Policy = "AcademicOperations")] public async Task<IActionResult> Archive(Guid id, CancellationToken ct) { await service.ArchiveAsync(id, ct); return NoContent(); }
    [HttpPost("{id:guid}/restore"), Authorize(Roles = "Admin")] public async Task<IActionResult> Restore(Guid id, CancellationToken ct) { await service.RestoreAsync(id, ct); return NoContent(); }
}

[ApiController, Route("api/v1")]
public sealed class CatalogController(ICatalogService service) : ControllerBase
{
    [HttpGet("subjects"), Authorize] public async Task<IActionResult> Subjects(CancellationToken ct) => Ok(ApiResponse<IReadOnlyList<LookupResponse>>.Ok(await service.ListSubjectsAsync(ct)));
    [HttpPost("subjects"), Authorize(Policy = "AcademicOperations")] public async Task<IActionResult> Subject(LookupRequest request, CancellationToken ct) => StatusCode(201, ApiResponse<LookupResponse>.Ok(await service.CreateSubjectAsync(request, ct)));
    [HttpGet("curricula"), Authorize] public async Task<IActionResult> Curricula(CancellationToken ct) => Ok(ApiResponse<IReadOnlyList<LookupResponse>>.Ok(await service.ListCurriculaAsync(ct)));
    [HttpPost("curricula"), Authorize(Policy = "AcademicOperations")] public async Task<IActionResult> Curriculum(LookupRequest request, CancellationToken ct) => StatusCode(201, ApiResponse<LookupResponse>.Ok(await service.CreateCurriculumAsync(request, ct)));
    [HttpGet("grade-levels"), Authorize] public async Task<IActionResult> Grades(CancellationToken ct) => Ok(ApiResponse<IReadOnlyList<LookupResponse>>.Ok(await service.ListGradesAsync(ct)));
    [HttpPost("grade-levels"), Authorize(Policy = "AcademicOperations")] public async Task<IActionResult> Grade(LookupRequest request, CancellationToken ct) => StatusCode(201, ApiResponse<LookupResponse>.Ok(await service.CreateGradeAsync(request, ct)));
}

[ApiController, Route("api/v1/sessions"), Authorize]
public sealed class SessionsController(ISessionService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Search([FromQuery] PageRequest page, CancellationToken ct) => Ok(ApiResponse<PageResult<SessionResponse>>.Ok(await service.SearchAsync(page, ct)));
    [HttpPost, Authorize(Policy = "AcademicOperations")] public async Task<IActionResult> Create(CreateSessionRequest request, CancellationToken ct) => StatusCode(201, ApiResponse<SessionResponse>.Ok(await service.CreateAsync(request, ct)));
}

[ApiController, Route("api/v1/attendance"), Authorize]
public sealed class AttendanceController(ISessionService service, AppDbContext db) : ControllerBase
{
    [HttpGet, Authorize(Policy = "AcademicOperations")] public async Task<IActionResult> List([FromQuery] string? status, CancellationToken ct) { var q = db.AttendanceRecords.AsNoTracking(); if (Enum.TryParse<EducationPlatform.Domain.AttendanceStatus>(status, true, out var parsed)) q = q.Where(x => x.Status == parsed); return Ok(ApiResponse<object>.Ok(await q.OrderByDescending(x => x.RequestedAt).Take(100).Select(x => new { x.Id, x.SessionId, x.StudentId, x.TeacherId, x.RequestedAt, x.ConfirmedAt, x.ConfirmedBy, status = x.Status.ToString(), x.Notes }).ToListAsync(ct))); }
    [HttpPost("requests"), Authorize(Policy = "StudentOnly")] public async Task<IActionResult> CreateRequest(AttendanceRequestDto request, CancellationToken ct) { await service.RequestAttendanceAsync(request, ct); return StatusCode(201, ApiResponse<object>.Ok(new { })); }
    [HttpPost("{id:guid}/confirm"), Authorize(Policy = "AcademicOperations")] public async Task<IActionResult> Confirm(Guid id, AttendanceDecisionRequest request, CancellationToken ct) { await service.ConfirmAttendanceAsync(id, request, ct); return NoContent(); }
    [HttpPost("{id:guid}/reject"), Authorize(Policy = "AcademicOperations")] public async Task<IActionResult> Reject(Guid id, AttendanceDecisionRequest request, CancellationToken ct) { await service.RejectAttendanceAsync(id, request, ct); return NoContent(); }
}

[ApiController, Route("api/v1/assignments"), Authorize]
public sealed class AssignmentsController(ILearningService service, AppDbContext db, ICurrentUser current) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List(CancellationToken ct) { var q = db.Assignments.AsNoTracking().Where(x => x.Status == EducationPlatform.Domain.AssignmentStatus.Published); if (User.IsInRole("Teacher")) { var id = await db.Teachers.Where(x => x.UserId == current.UserId).Select(x => x.Id).SingleAsync(ct); q = q.Where(x => x.TeacherId == id); } else if (User.IsInRole("Student")) { var id = await db.Students.Where(x => x.UserId == current.UserId).Select(x => x.Id).SingleAsync(ct); q = q.Where(x => x.Targets.Any(t => t.StudentId == id)); } else if (!User.IsInRole("Admin") && !User.IsInRole("Moderator")) throw new AppException(403, ErrorCodes.Forbidden, "غير مسموح."); return Ok(ApiResponse<object>.Ok(await q.OrderBy(x => x.DueDate).Select(x => new { x.Id, x.TeacherId, x.SubjectId, x.Title, x.Description, x.DueDate, x.MaxGrade, status = x.Status.ToString() }).ToListAsync(ct))); }
    [HttpGet("teacher-overview"), Authorize(Policy = "TeacherOnly")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TeacherAssignmentOverviewResponse>>>> TeacherOverview(CancellationToken ct)
    {
        var teacherId = await db.Teachers.Where(x => x.UserId == current.UserId).Select(x => x.Id).SingleAsync(ct);
        var assignments = await db.Assignments.AsNoTracking()
            .Where(x => x.TeacherId == teacherId && x.Status != EducationPlatform.Domain.AssignmentStatus.Draft)
            .Join(db.Subjects, assignment => assignment.SubjectId, subject => subject.Id,
                (assignment, subject) => new { assignment.Id, assignment.SubjectId, SubjectName = subject.NameAr })
            .ToListAsync(ct);
        var assignmentIds = assignments.Select(x => x.Id).ToArray();
        var targets = await db.AssignmentTargets.AsNoTracking()
            .Where(x => assignmentIds.Contains(x.AssignmentId))
            .Select(x => new
            {
                x.AssignmentId,
                SubmissionStatus = db.AssignmentSubmissions
                    .Where(s => s.AssignmentId == x.AssignmentId && s.StudentId == x.StudentId)
                    .Select(s => (EducationPlatform.Domain.SubmissionStatus?)s.Status)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var result = assignments.GroupBy(x => new { x.SubjectId, x.SubjectName }).Select(group =>
        {
            var ids = group.Select(x => x.Id).ToHashSet();
            var subjectTargets = targets.Where(x => ids.Contains(x.AssignmentId)).ToList();
            var submitted = subjectTargets.Count(x => x.SubmissionStatus is not null and not EducationPlatform.Domain.SubmissionStatus.Draft);
            var graded = subjectTargets.Count(x => x.SubmissionStatus == EducationPlatform.Domain.SubmissionStatus.Graded);
            var pending = subjectTargets.Count(x => x.SubmissionStatus is EducationPlatform.Domain.SubmissionStatus.Submitted or EducationPlatform.Domain.SubmissionStatus.Late or EducationPlatform.Domain.SubmissionStatus.Returned);
            var expected = subjectTargets.Count;
            return new TeacherAssignmentOverviewResponse(group.Key.SubjectId, group.Key.SubjectName, group.Count(), expected, submitted, pending, graded,
                expected - submitted, expected == 0 ? 0 : Math.Round(submitted * 100m / expected, 2));
        }).OrderBy(x => x.SubjectName).ToList();

        return Ok(ApiResponse<IReadOnlyList<TeacherAssignmentOverviewResponse>>.Ok(result));
    }
    [HttpPost, Authorize(Policy = "TeacherOnly")] public async Task<IActionResult> Create(AssignmentRequest request, CancellationToken ct) => StatusCode(201, ApiResponse<object>.Ok(new { id = await service.CreateAssignmentAsync(request, ct) }));
    [HttpPut("{id:guid}/submission"), Authorize(Policy = "StudentOnly")] public async Task<IActionResult> Submit(Guid id, SubmissionRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(new { id = await service.UpsertSubmissionAsync(id, request, ct) }));
    [HttpPost("submissions/{id:guid}/grade"), Authorize(Policy = "TeacherOnly")] public async Task<IActionResult> Grade(Guid id, GradeRequest request, CancellationToken ct) { await service.GradeAsync(id, request, ct); return NoContent(); }
}
