using EducationPlatform.Application;
using EducationPlatform.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EducationPlatform.Infrastructure;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Moderator> Moderators => Set<Moderator>();
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Curriculum> Curricula => Set<Curriculum>();
    public DbSet<GradeLevel> GradeLevels => Set<GradeLevel>();
    public DbSet<TeacherSubject> TeacherSubjects => Set<TeacherSubject>();
    public DbSet<TeacherCurriculum> TeacherCurricula => Set<TeacherCurriculum>();
    public DbSet<TeacherGradeRate> TeacherGradeRates => Set<TeacherGradeRate>();
    public DbSet<StudentSubject> StudentSubjects => Set<StudentSubject>();
    public DbSet<TeacherStudentAssignment> TeacherStudentAssignments => Set<TeacherStudentAssignment>();
    public DbSet<ClassSession> Sessions => Set<ClassSession>();
    public DbSet<WeeklySchedule> WeeklySchedules => Set<WeeklySchedule>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<StudentCreditTransaction> StudentCreditTransactions => Set<StudentCreditTransaction>();
    public DbSet<StudentPayment> StudentPayments => Set<StudentPayment>();
    public DbSet<LessonMaterial> LessonMaterials => Set<LessonMaterial>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<AssignmentTarget> AssignmentTargets => Set<AssignmentTarget>();
    public DbSet<AssignmentSubmission> AssignmentSubmissions => Set<AssignmentSubmission>();
    public DbSet<SubmissionAttachment> SubmissionAttachments => Set<SubmissionAttachment>();
    public DbSet<TeacherEarning> TeacherEarnings => Set<TeacherEarning>();
    public DbSet<TeacherPayoutPeriod> TeacherPayoutPeriods => Set<TeacherPayoutPeriod>();
    public DbSet<TeacherPayout> TeacherPayouts => Set<TeacherPayout>();
    public DbSet<TeacherPayoutItem> TeacherPayoutItems => Set<TeacherPayoutItem>();
    public DbSet<TeacherPayoutAdjustmentRequest> TeacherPayoutAdjustmentRequests => Set<TeacherPayoutAdjustmentRequest>();
    public DbSet<OperatingExpense> OperatingExpenses => Set<OperatingExpense>();
    public DbSet<PartnerShare> PartnerShares => Set<PartnerShare>();
    public DbSet<FinancialPeriod> FinancialPeriods => Set<FinancialPeriod>();
    public DbSet<PartnerDividend> PartnerDividends => Set<PartnerDividend>();
    public DbSet<FinancialTransaction> FinancialTransactions => Set<FinancialTransaction>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        foreach (var type in b.Model.GetEntityTypes().Where(x => typeof(Entity).IsAssignableFrom(x.ClrType)))
        {
            b.Entity(type.ClrType).Property(nameof(Entity.CreatedAt)).HasDefaultValueSql("SYSUTCDATETIME()");
        }
        foreach (var p in b.Model.GetEntityTypes().SelectMany(x => x.GetProperties()).Where(x => x.ClrType == typeof(decimal) || x.ClrType == typeof(decimal?))) p.SetPrecision(18); 
        foreach (var p in b.Model.GetEntityTypes().SelectMany(x => x.GetProperties()).Where(x => x.ClrType == typeof(decimal) || x.ClrType == typeof(decimal?))) p.SetScale(2);

        b.Entity<Student>().HasIndex(x => x.PhoneNumber).IsUnique();
        b.Entity<Student>().HasIndex(x => new { x.Status, x.ExpirationDate });
        b.Entity<Student>().Property(x => x.RowVersion).IsRowVersion();
        b.Entity<Student>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<Teacher>().HasIndex(x => x.PhoneNumber).IsUnique();
        b.Entity<Teacher>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<Moderator>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<Partner>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<Subject>().HasIndex(x => x.Code).IsUnique(); b.Entity<Subject>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<Curriculum>().HasIndex(x => x.Code).IsUnique(); b.Entity<Curriculum>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<GradeLevel>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<TeacherStudentAssignment>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<WeeklySchedule>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<LessonMaterial>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<Assignment>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<OperatingExpense>().HasQueryFilter(x => !x.IsDeleted);

        b.Entity<TeacherSubject>().HasKey(x => new { x.TeacherId, x.SubjectId });
        b.Entity<TeacherCurriculum>().HasKey(x => new { x.TeacherId, x.CurriculumId });
        b.Entity<TeacherGradeRate>().HasKey(x => new { x.TeacherId, x.GradeLevelId });
        b.Entity<StudentSubject>().HasKey(x => new { x.StudentId, x.SubjectId });
        b.Entity<AssignmentTarget>().HasKey(x => new { x.AssignmentId, x.StudentId });
        b.Entity<TeacherSubject>().HasQueryFilter(x => !x.Teacher.IsDeleted && !x.Subject.IsDeleted);
        b.Entity<TeacherCurriculum>().HasQueryFilter(x => !x.Teacher.IsDeleted && !x.Curriculum.IsDeleted);
        b.Entity<TeacherGradeRate>().HasQueryFilter(x => !x.Teacher.IsDeleted && !x.GradeLevel.IsDeleted);
        b.Entity<StudentSubject>().HasQueryFilter(x => !x.Student.IsDeleted && !x.Subject.IsDeleted);
        b.Entity<AssignmentTarget>().HasQueryFilter(x => !x.Assignment.IsDeleted);
        b.Entity<TeacherStudentAssignment>().HasIndex(x => new { x.TeacherId, x.StudentId, x.SubjectId }).IsUnique().HasFilter("[IsDeleted] = 0");
        b.Entity<WeeklySchedule>().HasIndex(x => new { x.StudentId, x.TeacherId, x.SubjectId, x.DayOfWeek, x.StartTime }).IsUnique().HasFilter("[IsDeleted] = 0");
        b.Entity<Teacher>().Property(x => x.DefaultCurrency).HasMaxLength(3).HasDefaultValue("EGP");
        b.Entity<TeacherStudentAssignment>().Property(x => x.Currency).HasMaxLength(3).HasDefaultValue("EGP");
        b.Entity<TeacherGradeRate>().Property(x => x.Currency).HasMaxLength(3);
        b.Entity<ClassSession>().Property(x => x.TeacherRateCurrencySnapshot).HasMaxLength(3).HasDefaultValue("EGP");
        b.Entity<ClassSession>().Property(x => x.StudentPriceCurrencySnapshot).HasMaxLength(3).HasDefaultValue("EGP");
        b.Entity<AttendanceRecord>().HasIndex(x => x.SessionId).IsUnique();
        b.Entity<AttendanceRecord>().Property(x => x.RowVersion).IsRowVersion();
        b.Entity<StudentCreditTransaction>().HasIndex(x => new { x.StudentId, x.ReferenceType, x.ReferenceId });
        b.Entity<StudentPayment>().HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");
        b.Entity<AssignmentSubmission>().HasIndex(x => new { x.AssignmentId, x.StudentId }).IsUnique();
        b.Entity<TeacherEarning>().HasIndex(x => x.SessionId).IsUnique();
        b.Entity<TeacherPayoutItem>().HasIndex(x => x.SessionId).IsUnique();
        b.Entity<PartnerDividend>().HasIndex(x => new { x.FinancialPeriodId, x.PartnerId }).IsUnique();
        b.Entity<RefreshToken>().HasIndex(x => x.TokenHash).IsUnique();
        b.Entity<ClassSession>().Property(x => x.RowVersion).IsRowVersion();
        b.Entity<TeacherPayout>().Property(x => x.RowVersion).IsRowVersion();
        b.Entity<FinancialPeriod>().Property(x => x.RowVersion).IsRowVersion();

        b.Entity<Student>().ToTable(t => t.HasCheckConstraint("CK_Student_Balance", "[SessionCreditBalance] >= 0"));
        b.Entity<StudentPayment>().ToTable(t => t.HasCheckConstraint("CK_Payment_Amount", "[Amount] > 0"));
        b.Entity<PartnerShare>().ToTable(t => t.HasCheckConstraint("CK_PartnerShare_Percentage", "[Percentage] >= 0 AND [Percentage] <= 100"));
        b.Entity<OperatingExpense>().ToTable(t => t.HasCheckConstraint("CK_Expense_Amount", "[Amount] >= 0"));

        foreach (var relationship in b.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys())) relationship.DeleteBehavior = DeleteBehavior.Restrict;
    }
}

public sealed class SystemDateTimeProvider : IDateTimeProvider { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; public DateOnly UtcToday => DateOnly.FromDateTime(DateTime.UtcNow); }

public static class DbHelpers
{
    public static async Task AuditAsync(this AppDbContext db, ICurrentUser user, string action, string entityType, object? entityId, string? oldValues, string? newValues, CancellationToken ct)
    {
        db.AuditLogs.Add(new AuditLog { UserId = user.UserId, Action = action, EntityType = entityType, EntityId = entityId?.ToString(), OldValues = oldValues, NewValues = newValues, IpAddress = user.IpAddress, UserAgent = user.UserAgent, CorrelationId = user.CorrelationId });
        await Task.CompletedTask;
    }
}
