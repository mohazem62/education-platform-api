namespace EducationPlatform.Domain;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Partner = "Partner";
    public const string Moderator = "Moderator";
    public const string Teacher = "Teacher";
    public const string Student = "Student";
    public static readonly string[] All = [Admin, Partner, Moderator, Teacher, Student];
}

public enum AccountStatus { Active, Inactive, Suspended }
public enum SessionStatus { Scheduled, AttendancePending, Confirmed, Completed, Cancelled, NoShow, Rejected }
public enum AttendanceStatus { Pending, Confirmed, Rejected }
public enum CreditTransactionType { Purchase, SessionDeduction, ManualAdjustment, Refund, Correction }
public enum SubmissionStatus { Draft, Submitted, Late, Graded, Returned }
public enum AssignmentStatus { Draft, Published, Closed }
public enum PayoutStatus { Draft, PendingReview, DiscrepancyFlagged, Approved, Paid, Rejected }
public enum AdjustmentStatus { Pending, Approved, PartiallyApproved, Rejected }
public enum FinancialPeriodStatus { Open, Calculating, Closed }
public enum RecordStatus { Pending, Approved, Paid, Rejected, Cancelled }
public enum TransactionDirection { Debit, Credit }
public enum FinancialTransactionType { StudentPayment, TeacherEarning, TeacherPayout, OperatingExpense, PartnerDividend, Refund, Adjustment }

public interface ISoftDelete { bool IsDeleted { get; set; } DateTimeOffset? DeletedAt { get; set; } string? DeletedBy { get; set; } }
public abstract class Entity { public Guid Id { get; set; } = Guid.NewGuid(); public DateTimeOffset CreatedAt { get; set; } public DateTimeOffset? UpdatedAt { get; set; } }
public abstract class SoftDeleteEntity : Entity, ISoftDelete { public bool IsDeleted { get; set; } public DateTimeOffset? DeletedAt { get; set; } public string? DeletedBy { get; set; } }

public sealed class Student : SoftDeleteEntity
{
    public required string UserId { get; set; }
    public required string FullName { get; set; }
    public required string PhoneNumber { get; set; }
    public string? ParentName { get; set; }
    public string? ParentPhoneNumber { get; set; }
    public Guid GradeLevelId { get; set; }
    public Guid CurriculumId { get; set; }
    public int SessionCreditBalance { get; set; }
    public DateTimeOffset? ExpirationDate { get; set; }
    public AccountStatus Status { get; set; } = AccountStatus.Active;
    public byte[] RowVersion { get; set; } = [];
    public ICollection<StudentSubject> Subjects { get; set; } = [];
}

public sealed class Teacher : SoftDeleteEntity
{
    public required string UserId { get; set; }
    public required string FullName { get; set; }
    public required string PhoneNumber { get; set; }
    public string? WhatsApp { get; set; }
    public decimal DefaultPerSessionRate { get; set; }
    public string? PreferredPayoutMethod { get; set; }
    public string? EWalletNumber { get; set; }
    public string? InstaPayIdentifier { get; set; }
    public string? PaymentDetails { get; set; }
    public string? ZoomMeetingUrl { get; set; }
    public string DefaultCurrency { get; set; } = "EGP";
    public AccountStatus Status { get; set; } = AccountStatus.Active;
    public ICollection<TeacherSubject> Subjects { get; set; } = [];
    public ICollection<TeacherCurriculum> Curricula { get; set; } = [];
    public ICollection<TeacherGradeRate> GradeRates { get; set; } = [];
}

public sealed class Moderator : SoftDeleteEntity { public required string UserId { get; set; } public required string FullName { get; set; } public required string PhoneNumber { get; set; } public AccountStatus Status { get; set; } = AccountStatus.Active; }
public sealed class Partner : SoftDeleteEntity { public required string UserId { get; set; } public required string Name { get; set; } public string? ContactInformation { get; set; } public AccountStatus Status { get; set; } = AccountStatus.Active; }
public sealed class Subject : SoftDeleteEntity { public required string NameAr { get; set; } public string? NameEn { get; set; } public required string Code { get; set; } }
public sealed class Curriculum : SoftDeleteEntity { public required string NameAr { get; set; } public string? NameEn { get; set; } public required string Code { get; set; } }
public sealed class GradeLevel : SoftDeleteEntity { public required string NameAr { get; set; } public string? NameEn { get; set; } public int SortOrder { get; set; } }
public sealed class TeacherSubject { public Guid TeacherId { get; set; } public Guid SubjectId { get; set; } public Teacher Teacher { get; set; } = null!; public Subject Subject { get; set; } = null!; }
public sealed class TeacherCurriculum { public Guid TeacherId { get; set; } public Guid CurriculumId { get; set; } public Teacher Teacher { get; set; } = null!; public Curriculum Curriculum { get; set; } = null!; }
public sealed class TeacherGradeRate { public Guid TeacherId { get; set; } public Guid GradeLevelId { get; set; } public decimal Rate { get; set; } public required string Currency { get; set; } public Teacher Teacher { get; set; } = null!; public GradeLevel GradeLevel { get; set; } = null!; }
public sealed class StudentSubject { public Guid StudentId { get; set; } public Guid SubjectId { get; set; } public Student Student { get; set; } = null!; public Subject Subject { get; set; } = null!; }
public sealed class TeacherStudentAssignment : SoftDeleteEntity { public Guid TeacherId { get; set; } public Guid StudentId { get; set; } public Guid SubjectId { get; set; } public decimal SessionPrice { get; set; } public string Currency { get; set; } = "EGP"; public DateTimeOffset AssignedAt { get; set; } }
public sealed class WeeklySchedule : SoftDeleteEntity { public Guid StudentId { get; set; } public Guid TeacherId { get; set; } public Guid SubjectId { get; set; } public DayOfWeek DayOfWeek { get; set; } public TimeOnly StartTime { get; set; } public TimeOnly EndTime { get; set; } public string? ZoomUrl { get; set; } public Student Student { get; set; } = null!; public Teacher Teacher { get; set; } = null!; public Subject Subject { get; set; } = null!; }

public sealed class ClassSession : Entity
{
    public Guid StudentId { get; set; }
    public Guid TeacherId { get; set; }
    public Guid SubjectId { get; set; }
    public DateTimeOffset ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public string? ClassLink { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Scheduled;
    public AttendanceStatus? AttendanceStatus { get; set; }
    public decimal TeacherRateSnapshot { get; set; }
    public string TeacherRateCurrencySnapshot { get; set; } = "EGP";
    public decimal StudentPriceSnapshot { get; set; }
    public string StudentPriceCurrencySnapshot { get; set; } = "EGP";
    public int StudentCreditCost { get; set; } = 1;
    public byte[] RowVersion { get; set; } = [];
}

public sealed class AttendanceRecord : Entity
{
    public Guid SessionId { get; set; }
    public Guid StudentId { get; set; }
    public Guid TeacherId { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public string? ConfirmedBy { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Pending;
    public string? Notes { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class StudentCreditTransaction : Entity { public Guid StudentId { get; set; } public CreditTransactionType Type { get; set; } public int Quantity { get; set; } public int BalanceBefore { get; set; } public int BalanceAfter { get; set; } public required string ReferenceType { get; set; } public Guid ReferenceId { get; set; } public required string Description { get; set; } public required string CreatedBy { get; set; } }
public sealed class StudentPayment : Entity { public Guid StudentId { get; set; } public decimal Amount { get; set; } public required string Currency { get; set; } public required string PaymentMethod { get; set; } public string? PaymentReference { get; set; } public DateTimeOffset PaidAt { get; set; } public required string RecordedBy { get; set; } public string? Notes { get; set; } public RecordStatus Status { get; set; } public string? IdempotencyKey { get; set; } }
public sealed class LessonMaterial : SoftDeleteEntity { public Guid TeacherId { get; set; } public Guid SubjectId { get; set; } public required string Title { get; set; } public string? Description { get; set; } public required string FileName { get; set; } public required string StoredFileName { get; set; } public required string StorageKey { get; set; } public required string ContentType { get; set; } public long FileSize { get; set; } }
public sealed class Assignment : SoftDeleteEntity { public Guid TeacherId { get; set; } public Guid SubjectId { get; set; } public required string Title { get; set; } public string? Description { get; set; } public DateTimeOffset DueDate { get; set; } public decimal MaxGrade { get; set; } public AssignmentStatus Status { get; set; } public ICollection<AssignmentTarget> Targets { get; set; } = []; }
public sealed class AssignmentTarget { public Guid AssignmentId { get; set; } public Guid StudentId { get; set; } public Assignment Assignment { get; set; } = null!; }
public sealed class AssignmentSubmission : Entity { public Guid AssignmentId { get; set; } public Guid StudentId { get; set; } public string? TextAnswer { get; set; } public DateTimeOffset? SubmittedAt { get; set; } public SubmissionStatus Status { get; set; } public decimal? Grade { get; set; } public string? TeacherFeedback { get; set; } public DateTimeOffset? GradedAt { get; set; } public string? GradedBy { get; set; } public ICollection<SubmissionAttachment> Attachments { get; set; } = []; }
public sealed class SubmissionAttachment : Entity { public Guid SubmissionId { get; set; } public required string FileName { get; set; } public required string StorageKey { get; set; } public required string ContentType { get; set; } public long FileSize { get; set; } }

public sealed class TeacherEarning : Entity { public Guid TeacherId { get; set; } public Guid SessionId { get; set; } public decimal Amount { get; set; } }
public sealed class TeacherPayoutPeriod : Entity { public DateOnly StartDate { get; set; } public DateOnly EndDate { get; set; } public bool IsClosed { get; set; } }
public sealed class TeacherPayout : Entity { public Guid TeacherId { get; set; } public Guid PeriodId { get; set; } public decimal GrossAmount { get; set; } public decimal AdjustmentAmount { get; set; } public decimal FinalAmount { get; set; } public PayoutStatus Status { get; set; } public string? ApprovedBy { get; set; } public DateTimeOffset? ApprovedAt { get; set; } public DateTimeOffset? PaidAt { get; set; } public byte[] RowVersion { get; set; } = []; public ICollection<TeacherPayoutItem> Items { get; set; } = []; }
public sealed class TeacherPayoutItem : Entity { public Guid TeacherPayoutId { get; set; } public Guid SessionId { get; set; } public decimal Amount { get; set; } }
public sealed class TeacherPayoutAdjustmentRequest : Entity { public Guid TeacherPayoutId { get; set; } public Guid TeacherId { get; set; } public required string Type { get; set; } public decimal RequestedAmount { get; set; } public required string Reason { get; set; } public AdjustmentStatus Status { get; set; } public string? AdminResponse { get; set; } public string? ReviewedBy { get; set; } public DateTimeOffset? ReviewedAt { get; set; } }
public sealed class OperatingExpense : SoftDeleteEntity { public required string Category { get; set; } public required string Description { get; set; } public decimal Amount { get; set; } public DateOnly ExpenseDate { get; set; } public string? Reference { get; set; } public required string CreatedBy { get; set; } public string? ApprovedBy { get; set; } public RecordStatus Status { get; set; } }
public sealed class PartnerShare : Entity { public Guid PartnerId { get; set; } public decimal Percentage { get; set; } public DateOnly EffectiveFrom { get; set; } public DateOnly? EffectiveTo { get; set; } }
public sealed class FinancialPeriod : Entity { public DateOnly StartDate { get; set; } public DateOnly EndDate { get; set; } public FinancialPeriodStatus Status { get; set; } public DateTimeOffset? ClosedAt { get; set; } public string? ClosedBy { get; set; } public byte[] RowVersion { get; set; } = []; }
public sealed class PartnerDividend : Entity { public Guid FinancialPeriodId { get; set; } public Guid PartnerId { get; set; } public decimal SharePercentageSnapshot { get; set; } public decimal NetProfitSnapshot { get; set; } public decimal DividendAmount { get; set; } public RecordStatus Status { get; set; } public DateTimeOffset? PaidAt { get; set; } }
public sealed class FinancialTransaction : Entity { public FinancialTransactionType TransactionType { get; set; } public required string ReferenceType { get; set; } public Guid ReferenceId { get; set; } public decimal Amount { get; set; } public TransactionDirection Direction { get; set; } public required string Description { get; set; } public Guid? FinancialPeriodId { get; set; } public required string CreatedBy { get; set; } }
public sealed class Notification : Entity { public required string UserId { get; set; } public required string Type { get; set; } public required string Title { get; set; } public required string Body { get; set; } public bool IsRead { get; set; } public DateTimeOffset? ReadAt { get; set; } }
public sealed class AuditLog : Entity { public string? UserId { get; set; } public required string Action { get; set; } public required string EntityType { get; set; } public string? EntityId { get; set; } public string? OldValues { get; set; } public string? NewValues { get; set; } public string? IpAddress { get; set; } public string? UserAgent { get; set; } public required string CorrelationId { get; set; } }
public sealed class RefreshToken : Entity { public required string UserId { get; set; } public required string TokenHash { get; set; } public DateTimeOffset ExpiresAt { get; set; } public DateTimeOffset? RevokedAt { get; set; } public Guid? ReplacedByTokenId { get; set; } public string? CreatedByIp { get; set; } public string? Device { get; set; } }

public static class PayoutRules
{
    public static bool CanTransition(PayoutStatus from, PayoutStatus to) => (from, to) switch
    {
        (PayoutStatus.Draft, PayoutStatus.PendingReview) => true,
        (PayoutStatus.PendingReview, PayoutStatus.Approved or PayoutStatus.Rejected or PayoutStatus.DiscrepancyFlagged) => true,
        (PayoutStatus.DiscrepancyFlagged, PayoutStatus.PendingReview) => true,
        (PayoutStatus.Approved, PayoutStatus.Paid) => true,
        _ => false
    };
}

public static class FinancialCalculator
{
    public static decimal GrossTeacherEarnings(IEnumerable<decimal> rates) => rates.Sum();
    public static decimal NetProfit(decimal revenue, decimal teacherCosts, decimal expenses) => revenue - teacherCosts - expenses;
    public static IReadOnlyList<decimal> Distribute(decimal netProfit, IEnumerable<decimal> percentages)
    {
        var values = percentages.ToArray();
        if (values.Any(x => x < 0) || Math.Abs(values.Sum() - 100m) > 0.0001m) throw new DomainException("INVALID_PARTNER_PERCENTAGES", "يجب أن يساوي مجموع نسب الشركاء 100%.");
        return values.Select(x => Math.Round(netProfit * x / 100m, 2, MidpointRounding.AwayFromZero)).ToArray();
    }
}

public static class CreditRules
{
    public static (int Before, int After) Deduct(int currentBalance, int cost)
    {
        if (cost <= 0) throw new DomainException("INVALID_SESSION_CREDIT_COST", "تكلفة الجلسة غير صالحة.");
        if (currentBalance < cost) throw new DomainException("INSUFFICIENT_SESSION_BALANCE", "رصيد الجلسات غير كافٍ.");
        return (currentBalance, currentBalance - cost);
    }
}

public class DomainException(string code, string message) : Exception(message) { public string Code { get; } = code; }
