using FluentValidation;
using EducationPlatform.Domain;

namespace EducationPlatform.Application;

public static class ErrorCodes
{
    public const string Validation = "VALIDATION_ERROR";
    public const string InvalidCredentials = "AUTH_INVALID_CREDENTIALS";
    public const string AccountLocked = "AUTH_ACCOUNT_LOCKED";
    public const string Forbidden = "FORBIDDEN_RESOURCE";
    public const string NotFound = "RESOURCE_NOT_FOUND";
    public const string StudentNotFound = "STUDENT_NOT_FOUND";
    public const string TeacherNotFound = "TEACHER_NOT_FOUND";
    public const string SessionNotFound = "SESSION_NOT_FOUND";
    public const string StudentSessionConflict = "STUDENT_SESSION_CONFLICT";
    public const string TeacherSessionConflict = "TEACHER_SESSION_CONFLICT";
    public const string InsufficientBalance = "INSUFFICIENT_SESSION_BALANCE";
    public const string AttendanceConfirmed = "ATTENDANCE_ALREADY_CONFIRMED";
    public const string FinancialPeriodClosed = "FINANCIAL_PERIOD_CLOSED";
    public const string InvalidPartnerPercentages = "INVALID_PARTNER_PERCENTAGES";
}

public sealed record ApiError(string? Field, string Code, string Message);
public sealed record ApiResponse<T>(bool Success, string Message, T? Data, IReadOnlyList<ApiError>? Errors, string? TraceId = null)
{
    public static ApiResponse<T> Ok(T value, string message = "تم تنفيذ العملية بنجاح") => new(true, message, value, null);
}
public sealed record PageResult<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
public sealed record PageRequest(int PageNumber = 1, int PageSize = 20, string? Search = null, string? SortBy = null, string SortDirection = "asc");

public sealed class AppException(int statusCode, string code, string message, string? field = null) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code;
    public string? Field { get; } = field;
}

public sealed record LoginRequest(string UserName, string Password, string? Device);
public sealed record RefreshRequest(string RefreshToken, string? Device);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
public sealed record CurrentUserResponse(string Id, string UserName, string FullName, string? Email, IReadOnlyList<string> Roles);
public sealed record AuthResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAt, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt, CurrentUserResponse User);

public sealed record StudentTeacherAssignmentRequest(Guid SubjectId, Guid TeacherId, decimal SessionPrice, string Currency);
public sealed record StudentTeacherAssignmentResponse(Guid SubjectId, Guid TeacherId, string TeacherName, decimal SessionPrice, string Currency);
public sealed record CreateStudentRequest(string UserName, string Password, string FullName, string PhoneNumber, string? ParentName, string? ParentPhoneNumber, Guid GradeLevelId, Guid CurriculumId, IReadOnlyList<Guid> SubjectIds, DateTimeOffset? ExpirationDate, IReadOnlyList<StudentTeacherAssignmentRequest>? TeacherAssignments = null);
public sealed record UpdateStudentRequest(string FullName, string PhoneNumber, string? ParentName, string? ParentPhoneNumber, Guid GradeLevelId, Guid CurriculumId, IReadOnlyList<Guid> SubjectIds, DateTimeOffset? ExpirationDate, string Status, IReadOnlyList<StudentTeacherAssignmentRequest>? TeacherAssignments = null);
public sealed record StudentResponse(Guid Id, string FullName, string PhoneNumber, string? ParentName, string? ParentPhoneNumber, Guid GradeLevelId, Guid CurriculumId, int SessionCreditBalance, DateTimeOffset? ExpirationDate, string Status, IReadOnlyList<StudentTeacherAssignmentResponse>? TeacherAssignments = null);
public sealed record TeacherStageRateRequest(Guid GradeLevelId, decimal Rate, string Currency);
public sealed record TeacherStageRateResponse(Guid GradeLevelId, string GradeLevelName, decimal Rate, string Currency);
public sealed record CreateTeacherRequest(string UserName, string Password, string FullName, string PhoneNumber, string? WhatsApp, decimal DefaultPerSessionRate, IReadOnlyList<Guid> SubjectIds, IReadOnlyList<Guid> CurriculumIds, string? PreferredPayoutMethod, string? EWalletNumber, string? InstaPayIdentifier, string DefaultCurrency = "EGP", IReadOnlyList<TeacherStageRateRequest>? StageRates = null);
public sealed record UpdateTeacherRequest(string FullName, string PhoneNumber, string? WhatsApp, decimal DefaultPerSessionRate, IReadOnlyList<Guid> SubjectIds, IReadOnlyList<Guid> CurriculumIds, string? PreferredPayoutMethod, string? EWalletNumber, string? InstaPayIdentifier, string Status, string DefaultCurrency = "EGP", IReadOnlyList<TeacherStageRateRequest>? StageRates = null);
public sealed record TeacherResponse(Guid Id, string FullName, string PhoneNumber, string? WhatsApp, decimal DefaultPerSessionRate, string Status, string? MaskedPayoutDestination, string DefaultCurrency = "EGP", IReadOnlyList<TeacherStageRateResponse>? StageRates = null, IReadOnlyList<LookupResponse>? Subjects = null, IReadOnlyList<LookupResponse>? Curricula = null);
public sealed record CreateModeratorRequest(string UserName, string Password, string FullName, string PhoneNumber);
public sealed record UpdateModeratorRequest(string FullName, string PhoneNumber, string Status);
public sealed record ModeratorResponse(Guid Id, string UserName, string FullName, string PhoneNumber, string Status);
public sealed record LookupRequest(string NameAr, string? NameEn, string Code);
public sealed record LookupResponse(Guid Id, string NameAr, string? NameEn, string Code);
public sealed record CreateSessionRequest(Guid StudentId, Guid TeacherId, Guid SubjectId, DateTimeOffset ScheduledAt, int DurationMinutes, string? ClassLink, int StudentCreditCost, SessionRecurrenceType RecurrenceType = SessionRecurrenceType.Once, DateTimeOffset? RecurrenceEndDate = null);
public sealed record UpdateSessionRequest(Guid StudentId, Guid TeacherId, Guid SubjectId, DateTimeOffset ScheduledAt, int DurationMinutes, string? ClassLink, int StudentCreditCost, SessionRecurrenceType RecurrenceType = SessionRecurrenceType.Once, DateTimeOffset? RecurrenceEndDate = null);
public sealed record SessionResponse(Guid Id, Guid StudentId, Guid TeacherId, Guid SubjectId, DateTimeOffset ScheduledAt, int DurationMinutes, string? ClassLink, string Status, string? AttendanceStatus, decimal TeacherRateSnapshot, int StudentCreditCost, string TeacherRateCurrency = "EGP", decimal StudentPriceSnapshot = 0, string StudentPriceCurrency = "EGP", SessionRecurrenceType RecurrenceType = SessionRecurrenceType.Once, DateTimeOffset? RecurrenceEndDate = null);
public sealed record WeeklyScheduleResponse(Guid Id, Guid StudentId, string StudentName, Guid TeacherId, string TeacherName, Guid SubjectId, string SubjectName, DayOfWeek DayOfWeek, string Day, TimeOnly StartTime, TimeOnly EndTime, string TimeSlot, string TimeRange, string Period, string ColorTheme, string? ZoomUrl, string Teacher, string Name, string? Zoom, SessionRecurrenceType RecurrenceType, DateTimeOffset? RecurrenceEndDate, DateTimeOffset OccurrenceAt);
public sealed record DailySupervisionScheduleResponse(Guid Id, Guid StudentId, string StudentName, Guid TeacherId, string TeacherName, Guid SubjectId, string SubjectName, DateOnly Date, string Day, TimeOnly StartTime, TimeOnly EndTime, string TimeRange, string? ClassLink, string Status, DateTimeOffset OccurrenceAt);
public sealed record AttendanceListItemResponse(Guid Id, Guid SessionId, Guid StudentId, string StudentName, Guid TeacherId, string TeacherName, Guid SubjectId, string SubjectName, DateTimeOffset RequestedAt, DateTimeOffset? ConfirmedAt, string? ConfirmedBy, string Status, string? Notes);
public sealed record AttendanceRequestDto(Guid SessionId, string? Notes);
public sealed record AttendanceDecisionRequest(string? Notes, string? IdempotencyKey);
public sealed record CreditAdjustmentRequest(int Quantity, string Description, string? IdempotencyKey);
public sealed record PaymentRequest(decimal Amount, string Currency, string PaymentMethod, string? PaymentReference, DateTimeOffset PaidAt, string? Notes, string IdempotencyKey, int PurchasedCredits);
public sealed record AssignmentRequest(Guid SubjectId, string Title, string? Description, DateTimeOffset DueDate, decimal MaxGrade, IReadOnlyList<Guid> StudentIds, bool Publish);
public sealed record SubmissionRequest(string? TextAnswer, bool Submit);
public sealed record GradeRequest(decimal Grade, string? Feedback);
public sealed record TeacherAssignmentOverviewResponse(Guid SubjectId, string SubjectName, int TotalAssignments, int ExpectedSubmissions, int SubmittedCount, int PendingGradingCount, int GradedCount, int NotSubmittedCount, decimal SubmissionPercentage);
public sealed record TeacherSubmissionRowResponse(Guid AssignmentId, Guid? SubmissionId, Guid StudentId, string StudentName, string GradeLevelName, Guid SubjectId, string SubjectName, string AssignmentTitle, decimal MaxGrade, string Status, DateTimeOffset? SubmittedAt, decimal? Grade, string? Feedback);
public sealed record HomeClassItemResponse(Guid Id, string Subject, string? Teacher, string Name, string Day, string TimeSlot, string TimeRange, string Period, string ColorTheme, string? ZoomUrl, DateTimeOffset ScheduledAt, int DurationMinutes, string Status, bool IsLive);
public sealed record HomeMonthlySessionsResponse(int Attended, int Total, string Month);
public sealed record HomeCardsResponse(string Role, decimal AverageGradePercentage, int RegisteredSubjects, HomeMonthlySessionsResponse Sessions, HomeClassItemResponse? CurrentSession);
public sealed record HomeScheduleResponse(DateOnly WeekStart, DateOnly WeekEnd, IReadOnlyList<HomeClassItemResponse> Items);
public sealed record ExpenseRequest(string Category, string Description, decimal Amount, DateOnly ExpenseDate, string? Reference);
public sealed record PartnerShareRequest(Guid PartnerId, decimal Percentage, DateOnly EffectiveFrom, DateOnly? EffectiveTo);
public sealed record FinancialPeriodRequest(DateOnly StartDate, DateOnly EndDate);
public sealed record FinancialSummary(decimal Revenue, decimal TeacherCosts, decimal OperatingExpenses, decimal NetProfit);

public interface IDateTimeProvider { DateTimeOffset UtcNow { get; } DateOnly UtcToday { get; } }
public interface ICurrentUser { string? UserId { get; } string? IpAddress { get; } string? UserAgent { get; } string CorrelationId { get; } }
public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct);
    Task LogoutAsync(string refreshToken, CancellationToken ct);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct);
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct);
    Task<CurrentUserResponse> MeAsync(CancellationToken ct);
}
public interface IStudentService { Task<PageResult<StudentResponse>> SearchAsync(PageRequest page, Guid? teacherId, Guid? subjectId, string? status, CancellationToken ct); Task<StudentResponse> GetAsync(Guid id, CancellationToken ct); Task<StudentResponse> CreateAsync(CreateStudentRequest request, CancellationToken ct); Task<StudentResponse> UpdateAsync(Guid id, UpdateStudentRequest request, CancellationToken ct); Task ArchiveAsync(Guid id, CancellationToken ct); Task RestoreAsync(Guid id, CancellationToken ct); Task AdjustCreditsAsync(Guid id, CreditAdjustmentRequest request, CancellationToken ct); Task RecordPaymentAsync(Guid id, PaymentRequest request, CancellationToken ct); }
public interface ITeacherService { Task<PageResult<TeacherResponse>> SearchAsync(PageRequest page, Guid? subjectId, Guid? curriculumId, CancellationToken ct); Task<TeacherResponse> GetAsync(Guid id, CancellationToken ct); Task<TeacherResponse> CreateAsync(CreateTeacherRequest request, CancellationToken ct); Task<TeacherResponse> UpdateAsync(Guid id, UpdateTeacherRequest request, CancellationToken ct); Task ArchiveAsync(Guid id, CancellationToken ct); Task RestoreAsync(Guid id, CancellationToken ct); }
public interface ICatalogService { Task<IReadOnlyList<LookupResponse>> ListSubjectsAsync(CancellationToken ct); Task<LookupResponse> CreateSubjectAsync(LookupRequest request, CancellationToken ct); Task<IReadOnlyList<LookupResponse>> ListCurriculaAsync(CancellationToken ct); Task<LookupResponse> CreateCurriculumAsync(LookupRequest request, CancellationToken ct); Task<IReadOnlyList<LookupResponse>> ListGradesAsync(CancellationToken ct); Task<LookupResponse> CreateGradeAsync(LookupRequest request, CancellationToken ct); }
public interface ISessionService { Task<SessionResponse> CreateAsync(CreateSessionRequest request, CancellationToken ct); Task<SessionResponse> UpdateAsync(Guid id, UpdateSessionRequest request, CancellationToken ct); Task DeleteAsync(Guid id, CancellationToken ct); Task<PageResult<SessionResponse>> SearchAsync(PageRequest page, CancellationToken ct); Task RequestAttendanceAsync(AttendanceRequestDto request, CancellationToken ct); Task ConfirmAttendanceAsync(Guid attendanceId, AttendanceDecisionRequest request, CancellationToken ct); Task RejectAttendanceAsync(Guid attendanceId, AttendanceDecisionRequest request, CancellationToken ct); }
public interface ILearningService { Task<Guid> CreateAssignmentAsync(AssignmentRequest request, CancellationToken ct); Task<Guid> UpsertSubmissionAsync(Guid assignmentId, SubmissionRequest request, CancellationToken ct); Task GradeAsync(Guid submissionId, GradeRequest request, CancellationToken ct); }
public interface IFinanceService { Task<Guid> CreateExpenseAsync(ExpenseRequest request, CancellationToken ct); Task<Guid> CreatePeriodAsync(FinancialPeriodRequest request, CancellationToken ct); Task<FinancialSummary> ClosePeriodAsync(Guid periodId, string? idempotencyKey, CancellationToken ct); Task<Guid> SetPartnerShareAsync(PartnerShareRequest request, CancellationToken ct); Task GenerateTeacherPayoutsAsync(Guid payoutPeriodId, CancellationToken ct); Task ChangePayoutStatusAsync(Guid payoutId, string action, CancellationToken ct); }
public interface IDashboardService { Task<object> AdminAsync(DateOnly? from, DateOnly? to, CancellationToken ct); Task<object> TeacherAsync(CancellationToken ct); Task<object> StudentAsync(CancellationToken ct); Task<object> PartnerAsync(CancellationToken ct); }
public interface IFileStorageService { Task<(string Key, string StoredName)> SaveAsync(Stream content, string originalName, string contentType, CancellationToken ct); Task DeleteAsync(string key, CancellationToken ct); }
public interface INotificationSender { Task SendAsync(string userId, string title, string body, CancellationToken ct); }
public interface IEmailSender { Task SendAsync(string address, string subject, string body, CancellationToken ct); }
public interface IWhatsAppSender { Task SendAsync(string phone, string message, CancellationToken ct); }

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest> { public LoginRequestValidator() { RuleFor(x => x.UserName).NotEmpty(); RuleFor(x => x.Password).NotEmpty(); } }
public sealed class CreateStudentValidator : AbstractValidator<CreateStudentRequest> { public CreateStudentValidator() { RuleFor(x => x.FullName).NotEmpty().MaximumLength(200); RuleFor(x => x.PhoneNumber).NotEmpty(); RuleFor(x => x.Password).MinimumLength(8); RuleFor(x => x.SubjectIds).NotEmpty(); RuleForEach(x => x.TeacherAssignments).ChildRules(x => { x.RuleFor(y => y.SessionPrice).GreaterThanOrEqualTo(0); x.RuleFor(y => y.Currency).NotEmpty().Length(3); }); } }
public sealed class CreateTeacherValidator : AbstractValidator<CreateTeacherRequest> { public CreateTeacherValidator() { RuleFor(x => x.FullName).NotEmpty().MaximumLength(200); RuleFor(x => x.PhoneNumber).NotEmpty(); RuleFor(x => x.DefaultPerSessionRate).GreaterThanOrEqualTo(0); RuleFor(x => x.DefaultCurrency).NotEmpty().Length(3); RuleFor(x => x.Password).MinimumLength(8); RuleForEach(x => x.StageRates).ChildRules(x => { x.RuleFor(y => y.Rate).GreaterThanOrEqualTo(0); x.RuleFor(y => y.Currency).NotEmpty().Length(3); }); } }
public sealed class CreateModeratorValidator : AbstractValidator<CreateModeratorRequest> { public CreateModeratorValidator() { RuleFor(x => x.UserName).NotEmpty(); RuleFor(x => x.Password).MinimumLength(8); RuleFor(x => x.FullName).NotEmpty().MaximumLength(200); RuleFor(x => x.PhoneNumber).NotEmpty(); } }
public sealed class CreateSessionValidator : AbstractValidator<CreateSessionRequest> { public CreateSessionValidator() { RuleFor(x => x.DurationMinutes).InclusiveBetween(15, 480); RuleFor(x => x.StudentCreditCost).InclusiveBetween(1, 10); RuleFor(x => x.ScheduledAt).NotEmpty(); RuleFor(x => x.RecurrenceType).IsInEnum(); RuleFor(x => x.RecurrenceEndDate).GreaterThanOrEqualTo(x => x.ScheduledAt).When(x => x.RecurrenceEndDate.HasValue); RuleFor(x => x.ClassLink).Must(IsValidUrl).When(x => !string.IsNullOrWhiteSpace(x.ClassLink)).WithMessage("رابط الحصة غير صالح."); } private static bool IsValidUrl(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"; }
public sealed class UpdateSessionValidator : AbstractValidator<UpdateSessionRequest> { public UpdateSessionValidator() { RuleFor(x => x.DurationMinutes).InclusiveBetween(15, 480); RuleFor(x => x.StudentCreditCost).InclusiveBetween(1, 10); RuleFor(x => x.ScheduledAt).NotEmpty(); RuleFor(x => x.RecurrenceType).IsInEnum(); RuleFor(x => x.RecurrenceEndDate).GreaterThanOrEqualTo(x => x.ScheduledAt).When(x => x.RecurrenceEndDate.HasValue); RuleFor(x => x.ClassLink).Must(IsValidUrl).When(x => !string.IsNullOrWhiteSpace(x.ClassLink)).WithMessage("رابط الحصة غير صالح."); } private static bool IsValidUrl(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"; }
public sealed class PaymentValidator : AbstractValidator<PaymentRequest> { public PaymentValidator() { RuleFor(x => x.Amount).GreaterThan(0); RuleFor(x => x.Currency).Length(3); RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(100); RuleFor(x => x.PurchasedCredits).GreaterThanOrEqualTo(0); } }
public sealed class AssignmentValidator : AbstractValidator<AssignmentRequest> { public AssignmentValidator() { RuleFor(x => x.Title).NotEmpty().MaximumLength(250); RuleFor(x => x.MaxGrade).GreaterThan(0); RuleFor(x => x.StudentIds).NotEmpty(); } }

public static class PhoneNormalizer
{
    public static string Normalize(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return value.TrimStart().StartsWith('+') ? "+" + digits : digits;
    }
    public static string? Mask(string? value) => string.IsNullOrWhiteSpace(value) || value.Length < 6 ? value : value[..2] + new string('*', value.Length - 5) + value[^3..];
}
