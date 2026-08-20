using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EducationPlatform.Application;
using EducationPlatform.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EducationPlatform.Infrastructure;

public sealed class JwtOptions { public const string Section = "Jwt"; public string Issuer { get; set; } = "EducationPlatform"; public string Audience { get; set; } = "EducationPlatform.Frontend"; public string SigningKey { get; set; } = string.Empty; public int AccessTokenMinutes { get; set; } = 10_080; public int RefreshTokenDays { get; set; } = 30; }
public sealed class FileStorageOptions { public const string Section = "FileStorage"; public string RootPath { get; set; } = "uploads"; public long MaxFileSizeBytes { get; set; } = 10_485_760; public string[] AllowedExtensions { get; set; } = [".pdf", ".doc", ".docx", ".png", ".jpg", ".jpeg"]; }
public sealed class BusinessOptions { public const string Section = "Business"; public string Currency { get; set; } = "EGP"; public string TimeZone { get; set; } = "Africa/Cairo"; }

public sealed class AuthService(AppDbContext db, UserManager<ApplicationUser> users, IOptions<JwtOptions> jwtOptions, IDateTimeProvider clock, ICurrentUser current) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await users.FindByNameAsync(request.UserName);
        if (user is null || user.IsDeleted || !await users.CheckPasswordAsync(user, request.Password))
        {
            if (user is not null) await users.AccessFailedAsync(user);
            throw new AppException(401, ErrorCodes.InvalidCredentials, "اسم المستخدم أو كلمة المرور غير صحيحة.");
        }
        if (await users.IsLockedOutAsync(user)) throw new AppException(423, ErrorCodes.AccountLocked, "الحساب مقفل مؤقتًا بسبب محاولات دخول متكررة.");
        await users.ResetAccessFailedCountAsync(user);
        await db.AuditAsync(current, "LoginSucceeded", nameof(ApplicationUser), user.Id, null, null, ct);
        return await IssueAsync(user, request.Device, ct);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct)
    {
        var hash = Hash(request.RefreshToken);
        var old = await db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == hash, ct) ?? throw new AppException(401, ErrorCodes.InvalidCredentials, "رمز التحديث غير صالح.");
        if (old.RevokedAt is not null || old.ExpiresAt <= clock.UtcNow) throw new AppException(401, ErrorCodes.InvalidCredentials, "انتهت صلاحية رمز التحديث أو تم إلغاؤه.");
        var user = await users.FindByIdAsync(old.UserId) ?? throw new AppException(401, ErrorCodes.InvalidCredentials, "المستخدم غير موجود.");
        old.RevokedAt = clock.UtcNow;
        var response = await IssueAsync(user, request.Device, ct, save: false);
        var replacement = db.RefreshTokens.Local.Single(x => x.TokenHash == Hash(response.RefreshToken));
        old.ReplacedByTokenId = replacement.Id;
        await db.SaveChangesAsync(ct);
        return response;
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct)
    {
        var entity = await db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == Hash(refreshToken), ct);
        if (entity is not null && entity.RevokedAt is null) { entity.RevokedAt = clock.UtcNow; await db.SaveChangesAsync(ct); }
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await RequiredCurrentAsync();
        var result = await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        EnsureIdentity(result);
        await RevokeUserTokens(user.Id, ct);
    }
    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct)
    {
        // Deliberately returns uniformly. A configured notification sender can deliver the Identity token.
        await Task.CompletedTask;
    }
    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(request.Email) ?? throw new AppException(400, ErrorCodes.Validation, "تعذر إعادة تعيين كلمة المرور.");
        EnsureIdentity(await users.ResetPasswordAsync(user, request.Token, request.NewPassword));
        await RevokeUserTokens(user.Id, ct);
    }
    public async Task<CurrentUserResponse> MeAsync(CancellationToken ct)
    {
        var user = await RequiredCurrentAsync();
        return new(user.Id, user.UserName!, user.Email, (await users.GetRolesAsync(user)).ToArray());
    }
    private async Task<ApplicationUser> RequiredCurrentAsync() => await users.FindByIdAsync(current.UserId ?? "") ?? throw new AppException(401, ErrorCodes.InvalidCredentials, "يلزم تسجيل الدخول.");
    private async Task<AuthResponse> IssueAsync(ApplicationUser user, string? device, CancellationToken ct, bool save = true)
    {
        if (_jwt.SigningKey.Length < 32) throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 characters.");
        var roles = await users.GetRolesAsync(user);
        var expires = clock.UtcNow.AddDays(7);
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, user.Id), new(ClaimTypes.NameIdentifier, user.Id), new(ClaimTypes.Name, user.UserName!), new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        var token = new JwtSecurityToken(_jwt.Issuer, _jwt.Audience, claims, clock.UtcNow.UtcDateTime, expires.UtcDateTime, new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey)), SecurityAlgorithms.HmacSha256));
        var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshExpires = clock.UtcNow.AddDays(_jwt.RefreshTokenDays);
        db.RefreshTokens.Add(new RefreshToken { UserId = user.Id, TokenHash = Hash(refresh), ExpiresAt = refreshExpires, CreatedByIp = current.IpAddress, Device = device });
        if (save) await db.SaveChangesAsync(ct);
        var currentUser = new CurrentUserResponse(user.Id, user.UserName!, user.Email, roles.ToArray());
        return new(new JwtSecurityTokenHandler().WriteToken(token), expires, refresh, refreshExpires, currentUser);
    }
    private async Task RevokeUserTokens(string userId, CancellationToken ct) { var active = await db.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAt == null).ToListAsync(ct); foreach (var x in active) x.RevokedAt = clock.UtcNow; await db.SaveChangesAsync(ct); }
    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static void EnsureIdentity(IdentityResult result) { if (!result.Succeeded) throw new AppException(400, ErrorCodes.Validation, string.Join(" ", result.Errors.Select(x => x.Description))); }
}

public sealed class StudentService(AppDbContext db, UserManager<ApplicationUser> users, ICurrentUser current, IDateTimeProvider clock) : IStudentService
{
    public async Task<PageResult<StudentResponse>> SearchAsync(PageRequest page, Guid? teacherId, Guid? subjectId, string? status, CancellationToken ct)
    {
        var q = db.Students.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(page.Search)) { var s = page.Search.Trim(); q = q.Where(x => x.FullName.Contains(s) || x.PhoneNumber.Contains(s) || (x.ParentPhoneNumber != null && x.ParentPhoneNumber.Contains(s))); }
        if (teacherId.HasValue) q = q.Where(x => db.TeacherStudentAssignments.Any(a => a.StudentId == x.Id && a.TeacherId == teacherId));
        if (subjectId.HasValue) q = q.Where(x => x.Subjects.Any(s => s.SubjectId == subjectId));
        if (Enum.TryParse<AccountStatus>(status, true, out var parsed)) q = q.Where(x => x.Status == parsed);
        var total = await q.CountAsync(ct); var items = await q.OrderBy(x => x.FullName).Skip(Skip(page)).Take(Size(page)).Select(x => Map(x)).ToListAsync(ct);
        var itemIds = items.Select(x => x.Id).ToArray(); var assignmentRows = await AssignmentRows(itemIds, ct);
        items = items.Select(x => x with { TeacherAssignments = assignmentRows.Where(a => a.StudentId == x.Id).Select(a => a.Response).ToList() }).ToList();
        return new(items, Number(page), Size(page), total);
    }
    public async Task<StudentResponse> GetAsync(Guid id, CancellationToken ct) { var student = await db.Students.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new AppException(404, ErrorCodes.StudentNotFound, "الطالب غير موجود."); var links = await AssignmentRows([id], ct); return Map(student) with { TeacherAssignments = links.Select(x => x.Response).ToList() }; }
    public async Task<StudentResponse> CreateAsync(CreateStudentRequest r, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var phone = PhoneNormalizer.Normalize(r.PhoneNumber); if (await db.Students.AnyAsync(x => x.PhoneNumber == phone, ct)) throw new AppException(409, ErrorCodes.Validation, "رقم الهاتف مستخدم بالفعل.", "phoneNumber");
        var user = new ApplicationUser { UserName = r.UserName, DisplayName = r.FullName, PhoneNumber = phone };
        var identity = await users.CreateAsync(user, r.Password); if (!identity.Succeeded) throw new AppException(400, ErrorCodes.Validation, string.Join(" ", identity.Errors.Select(x => x.Description)));
        await users.AddToRoleAsync(user, Roles.Student);
        await ValidateAssignments(r.SubjectIds, r.TeacherAssignments, ct);
        var entity = new Student { UserId = user.Id, FullName = r.FullName, PhoneNumber = phone, ParentName = r.ParentName, ParentPhoneNumber = r.ParentPhoneNumber is null ? null : PhoneNormalizer.Normalize(r.ParentPhoneNumber), GradeLevelId = r.GradeLevelId, CurriculumId = r.CurriculumId, ExpirationDate = r.ExpirationDate, Subjects = r.SubjectIds.Distinct().Select(x => new StudentSubject { SubjectId = x }).ToList() };
        db.Students.Add(entity); if (r.TeacherAssignments is not null) db.TeacherStudentAssignments.AddRange(r.TeacherAssignments.Select(x => new TeacherStudentAssignment { StudentId = entity.Id, TeacherId = x.TeacherId, SubjectId = x.SubjectId, SessionPrice = x.SessionPrice, Currency = NormalizeCurrency(x.Currency), AssignedAt = clock.UtcNow }));
        await db.AuditAsync(current, "StudentCreated", nameof(Student), entity.Id, null, r.FullName, ct); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return await GetAsync(entity.Id, ct);
    }
    public async Task<StudentResponse> UpdateAsync(Guid id, UpdateStudentRequest r, CancellationToken ct)
    {
        var x = await Required(id, ct);
        x.FullName = r.FullName; x.PhoneNumber = PhoneNormalizer.Normalize(r.PhoneNumber); x.ParentName = r.ParentName;
        x.ParentPhoneNumber = r.ParentPhoneNumber is null ? null : PhoneNormalizer.Normalize(r.ParentPhoneNumber);
        x.GradeLevelId = r.GradeLevelId; x.CurriculumId = r.CurriculumId; x.ExpirationDate = r.ExpirationDate;
        if (!Enum.TryParse<AccountStatus>(r.Status, true, out var status)) throw new AppException(422, ErrorCodes.Validation, "حالة الحساب غير صالحة.", "status");
        x.Status = status; x.UpdatedAt = clock.UtcNow;
        var links = await db.StudentSubjects.Where(s => s.StudentId == id).ToListAsync(ct); db.StudentSubjects.RemoveRange(links);
        db.StudentSubjects.AddRange(r.SubjectIds.Distinct().Select(subjectId => new StudentSubject { StudentId = id, SubjectId = subjectId }));
        if (r.TeacherAssignments is not null) { await ValidateAssignments(r.SubjectIds, r.TeacherAssignments, ct); db.TeacherStudentAssignments.RemoveRange(await db.TeacherStudentAssignments.Where(a => a.StudentId == id).ToListAsync(ct)); db.TeacherStudentAssignments.AddRange(r.TeacherAssignments.Select(a => new TeacherStudentAssignment { StudentId = id, TeacherId = a.TeacherId, SubjectId = a.SubjectId, SessionPrice = a.SessionPrice, Currency = NormalizeCurrency(a.Currency), AssignedAt = clock.UtcNow })); }
        await db.AuditAsync(current, "StudentUpdated", nameof(Student), id, null, r.FullName, ct); await db.SaveChangesAsync(ct); return await GetAsync(id, ct);
    }
    public async Task ArchiveAsync(Guid id, CancellationToken ct) { var x = await Required(id, ct); x.IsDeleted = true; x.DeletedAt = clock.UtcNow; x.DeletedBy = current.UserId; (await users.FindByIdAsync(x.UserId))!.IsDeleted = true; await db.AuditAsync(current, "StudentArchived", nameof(Student), id, null, null, ct); await db.SaveChangesAsync(ct); }
    public async Task RestoreAsync(Guid id, CancellationToken ct) { var x = await db.Students.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new AppException(404, ErrorCodes.StudentNotFound, "الطالب غير موجود."); x.IsDeleted = false; x.DeletedAt = null; x.DeletedBy = null; (await users.FindByIdAsync(x.UserId))!.IsDeleted = false; await db.SaveChangesAsync(ct); }
    public async Task AdjustCreditsAsync(Guid id, CreditAdjustmentRequest r, CancellationToken ct) { await using var tx = await db.Database.BeginTransactionAsync(ct); var x = await Required(id, ct); var before = x.SessionCreditBalance; if (before + r.Quantity < 0) throw new AppException(409, ErrorCodes.InsufficientBalance, "الرصيد غير كافٍ."); x.SessionCreditBalance += r.Quantity; db.StudentCreditTransactions.Add(new StudentCreditTransaction { StudentId = id, Type = CreditTransactionType.ManualAdjustment, Quantity = r.Quantity, BalanceBefore = before, BalanceAfter = x.SessionCreditBalance, ReferenceType = "ManualAdjustment", ReferenceId = Guid.NewGuid(), Description = r.Description, CreatedBy = current.UserId! }); await db.AuditAsync(current, "StudentCreditAdjusted", nameof(Student), id, before.ToString(), x.SessionCreditBalance.ToString(), ct); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); }
    public async Task RecordPaymentAsync(Guid id, PaymentRequest r, CancellationToken ct) { if (await db.StudentPayments.AnyAsync(x => x.IdempotencyKey == r.IdempotencyKey, ct)) return; await using var tx = await db.Database.BeginTransactionAsync(ct); var x = await Required(id, ct); var payment = new StudentPayment { StudentId = id, Amount = r.Amount, Currency = r.Currency.ToUpperInvariant(), PaymentMethod = r.PaymentMethod, PaymentReference = r.PaymentReference, PaidAt = r.PaidAt, RecordedBy = current.UserId!, Notes = r.Notes, Status = RecordStatus.Paid, IdempotencyKey = r.IdempotencyKey }; db.StudentPayments.Add(payment); var before = x.SessionCreditBalance; x.SessionCreditBalance += r.PurchasedCredits; db.StudentCreditTransactions.Add(new StudentCreditTransaction { StudentId = id, Type = CreditTransactionType.Purchase, Quantity = r.PurchasedCredits, BalanceBefore = before, BalanceAfter = x.SessionCreditBalance, ReferenceType = nameof(StudentPayment), ReferenceId = payment.Id, Description = "شراء رصيد جلسات", CreatedBy = current.UserId! }); db.FinancialTransactions.Add(new FinancialTransaction { TransactionType = FinancialTransactionType.StudentPayment, ReferenceType = nameof(StudentPayment), ReferenceId = payment.Id, Amount = r.Amount, Direction = TransactionDirection.Credit, Description = "دفعة طالب", CreatedBy = current.UserId! }); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); }
    private async Task<Student> Required(Guid id, CancellationToken ct) => await db.Students.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new AppException(404, ErrorCodes.StudentNotFound, "الطالب غير موجود.");
    private static StudentResponse Map(Student x) => new(x.Id, x.FullName, x.PhoneNumber, x.ParentName, x.ParentPhoneNumber, x.GradeLevelId, x.CurriculumId, x.SessionCreditBalance, x.ExpirationDate, x.Status.ToString());
    private async Task ValidateAssignments(IReadOnlyList<Guid> subjectIds, IReadOnlyList<StudentTeacherAssignmentRequest>? assignments, CancellationToken ct) { if (assignments is null) return; if (assignments.GroupBy(x => new { x.SubjectId, x.TeacherId }).Any(x => x.Count() > 1)) throw new AppException(422, ErrorCodes.Validation, "توجد تكليفات مدرس مكررة.", "teacherAssignments"); if (assignments.Any(x => !subjectIds.Contains(x.SubjectId))) throw new AppException(422, ErrorCodes.Validation, "كل مادة في تكليفات المدرسين يجب أن تكون ضمن مواد الطالب.", "teacherAssignments"); foreach (var a in assignments) if (!await db.TeacherSubjects.AnyAsync(x => x.TeacherId == a.TeacherId && x.SubjectId == a.SubjectId, ct)) throw new AppException(422, ErrorCodes.Validation, "المعلم غير مسجل في إحدى المواد المحددة.", "teacherAssignments"); }
    private async Task<List<(Guid StudentId, StudentTeacherAssignmentResponse Response)>> AssignmentRows(Guid[] studentIds, CancellationToken ct) { var rows = await (from a in db.TeacherStudentAssignments.AsNoTracking() join t in db.Teachers.AsNoTracking() on a.TeacherId equals t.Id where studentIds.Contains(a.StudentId) select new { a.StudentId, a.SubjectId, a.TeacherId, TeacherName = t.FullName, a.SessionPrice, a.Currency }).ToListAsync(ct); return rows.Select(x => (x.StudentId, new StudentTeacherAssignmentResponse(x.SubjectId, x.TeacherId, x.TeacherName, x.SessionPrice, x.Currency))).ToList(); }
    private static string NormalizeCurrency(string value) => string.IsNullOrWhiteSpace(value) ? "EGP" : value.Trim().ToUpperInvariant();
    private static int Number(PageRequest p) => Math.Max(1, p.PageNumber); private static int Size(PageRequest p) => Math.Clamp(p.PageSize, 1, 100); private static int Skip(PageRequest p) => (Number(p) - 1) * Size(p);
}

public sealed class TeacherService(AppDbContext db, UserManager<ApplicationUser> users, ICurrentUser current, IDateTimeProvider clock) : ITeacherService
{
    public async Task<PageResult<TeacherResponse>> SearchAsync(PageRequest p, Guid? subjectId, Guid? curriculumId, CancellationToken ct) { var q = db.Teachers.AsNoTracking(); if (!string.IsNullOrWhiteSpace(p.Search)) q = q.Where(x => x.FullName.Contains(p.Search) || x.PhoneNumber.Contains(p.Search)); if (subjectId.HasValue) q = q.Where(x => x.Subjects.Any(y => y.SubjectId == subjectId)); if (curriculumId.HasValue) q = q.Where(x => x.Curricula.Any(y => y.CurriculumId == curriculumId)); var n = Math.Max(1, p.PageNumber); var z = Math.Clamp(p.PageSize, 1, 100); var count = await q.CountAsync(ct); var data = await q.OrderBy(x => x.FullName).Skip((n - 1) * z).Take(z).Select(x => Map(x, false, null)).ToListAsync(ct); return new(data, n, z, count); }
    public async Task<TeacherResponse> GetAsync(Guid id, CancellationToken ct) { var teacher = await Required(id, ct); return Map(teacher, true, await RateRows(id, ct)); }
    public async Task<TeacherResponse> CreateAsync(CreateTeacherRequest r, CancellationToken ct) { await using var tx = await db.Database.BeginTransactionAsync(ct); EnsureRates(r.StageRates); var phone = PhoneNormalizer.Normalize(r.PhoneNumber); var user = new ApplicationUser { UserName = r.UserName, DisplayName = r.FullName, PhoneNumber = phone }; var result = await users.CreateAsync(user, r.Password); if (!result.Succeeded) throw new AppException(400, ErrorCodes.Validation, string.Join(" ", result.Errors.Select(x => x.Description))); await users.AddToRoleAsync(user, Roles.Teacher); var x = new Teacher { UserId = user.Id, FullName = r.FullName, PhoneNumber = phone, WhatsApp = r.WhatsApp, DefaultPerSessionRate = r.DefaultPerSessionRate, DefaultCurrency = NormalizeCurrency(r.DefaultCurrency), PreferredPayoutMethod = r.PreferredPayoutMethod, EWalletNumber = r.EWalletNumber, InstaPayIdentifier = r.InstaPayIdentifier, Subjects = r.SubjectIds.Distinct().Select(id => new TeacherSubject { SubjectId = id }).ToList(), Curricula = r.CurriculumIds.Distinct().Select(id => new TeacherCurriculum { CurriculumId = id }).ToList(), GradeRates = (r.StageRates ?? []).Select(y => new TeacherGradeRate { GradeLevelId = y.GradeLevelId, Rate = y.Rate, Currency = NormalizeCurrency(y.Currency) }).ToList() }; db.Teachers.Add(x); await db.AuditAsync(current, "TeacherCreated", nameof(Teacher), x.Id, null, r.FullName, ct); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return await GetAsync(x.Id, ct); }
    public async Task<TeacherResponse> UpdateAsync(Guid id, UpdateTeacherRequest r, CancellationToken ct) { var x = await Required(id, ct); EnsureRates(r.StageRates); if (!Enum.TryParse<AccountStatus>(r.Status, true, out var status)) throw new AppException(422, ErrorCodes.Validation, "حالة الحساب غير صالحة."); x.FullName = r.FullName; x.PhoneNumber = PhoneNormalizer.Normalize(r.PhoneNumber); x.WhatsApp = r.WhatsApp; x.DefaultPerSessionRate = r.DefaultPerSessionRate; x.DefaultCurrency = NormalizeCurrency(r.DefaultCurrency); x.PreferredPayoutMethod = r.PreferredPayoutMethod; x.EWalletNumber = r.EWalletNumber; x.InstaPayIdentifier = r.InstaPayIdentifier; x.Status = status; x.UpdatedAt = clock.UtcNow; db.TeacherSubjects.RemoveRange(await db.TeacherSubjects.Where(s => s.TeacherId == id).ToListAsync(ct)); db.TeacherCurricula.RemoveRange(await db.TeacherCurricula.Where(s => s.TeacherId == id).ToListAsync(ct)); db.TeacherSubjects.AddRange(r.SubjectIds.Distinct().Select(subjectId => new TeacherSubject { TeacherId = id, SubjectId = subjectId })); db.TeacherCurricula.AddRange(r.CurriculumIds.Distinct().Select(curriculumId => new TeacherCurriculum { TeacherId = id, CurriculumId = curriculumId })); if (r.StageRates is not null) { db.TeacherGradeRates.RemoveRange(await db.TeacherGradeRates.Where(y => y.TeacherId == id).ToListAsync(ct)); db.TeacherGradeRates.AddRange(r.StageRates.Select(y => new TeacherGradeRate { TeacherId = id, GradeLevelId = y.GradeLevelId, Rate = y.Rate, Currency = NormalizeCurrency(y.Currency) })); } await db.AuditAsync(current, "TeacherUpdated", nameof(Teacher), id, null, r.FullName, ct); await db.SaveChangesAsync(ct); return await GetAsync(id, ct); }
    public async Task ArchiveAsync(Guid id, CancellationToken ct) { var x = await Required(id, ct); x.IsDeleted = true; x.DeletedAt = clock.UtcNow; x.DeletedBy = current.UserId; (await users.FindByIdAsync(x.UserId))!.IsDeleted = true; await db.SaveChangesAsync(ct); }
    public async Task RestoreAsync(Guid id, CancellationToken ct) { var x = await db.Teachers.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new AppException(404, ErrorCodes.TeacherNotFound, "المعلم غير موجود."); x.IsDeleted = false; x.DeletedAt = null; x.DeletedBy = null; (await users.FindByIdAsync(x.UserId))!.IsDeleted = false; await db.SaveChangesAsync(ct); }
    private async Task<Teacher> Required(Guid id, CancellationToken ct) => await db.Teachers.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new AppException(404, ErrorCodes.TeacherNotFound, "المعلم غير موجود.");
    private static TeacherResponse Map(Teacher x, bool sensitive, IReadOnlyList<TeacherStageRateResponse>? rates = null) => new(x.Id, x.FullName, x.PhoneNumber, x.WhatsApp, x.DefaultPerSessionRate, x.Status.ToString(), sensitive ? PhoneNormalizer.Mask(x.EWalletNumber ?? x.InstaPayIdentifier) : null, x.DefaultCurrency, rates);
    private async Task<IReadOnlyList<TeacherStageRateResponse>> RateRows(Guid teacherId, CancellationToken ct) => await (from r in db.TeacherGradeRates.AsNoTracking() join g in db.GradeLevels.AsNoTracking() on r.GradeLevelId equals g.Id where r.TeacherId == teacherId orderby g.SortOrder select new TeacherStageRateResponse(r.GradeLevelId, g.NameAr, r.Rate, r.Currency)).ToListAsync(ct);
    private static void EnsureRates(IReadOnlyList<TeacherStageRateRequest>? rates) { if (rates?.GroupBy(x => x.GradeLevelId).Any(x => x.Count() > 1) == true) throw new AppException(422, ErrorCodes.Validation, "لا يمكن تكرار المرحلة في أسعار المدرس.", "stageRates"); }
    private static string NormalizeCurrency(string value) => string.IsNullOrWhiteSpace(value) ? "EGP" : value.Trim().ToUpperInvariant();
}

public sealed class CatalogService(AppDbContext db) : ICatalogService
{
    public async Task<IReadOnlyList<LookupResponse>> ListSubjectsAsync(CancellationToken ct) => await db.Subjects.AsNoTracking().OrderBy(x => x.NameAr).Select(x => new LookupResponse(x.Id, x.NameAr, x.NameEn, x.Code)).ToListAsync(ct);
    public async Task<LookupResponse> CreateSubjectAsync(LookupRequest r, CancellationToken ct) { var x = new Subject { NameAr = r.NameAr, NameEn = r.NameEn, Code = r.Code }; db.Add(x); await db.SaveChangesAsync(ct); return new(x.Id, x.NameAr, x.NameEn, x.Code); }
    public async Task<IReadOnlyList<LookupResponse>> ListCurriculaAsync(CancellationToken ct) => await db.Curricula.AsNoTracking().OrderBy(x => x.NameAr).Select(x => new LookupResponse(x.Id, x.NameAr, x.NameEn, x.Code)).ToListAsync(ct);
    public async Task<LookupResponse> CreateCurriculumAsync(LookupRequest r, CancellationToken ct) { var x = new Curriculum { NameAr = r.NameAr, NameEn = r.NameEn, Code = r.Code }; db.Add(x); await db.SaveChangesAsync(ct); return new(x.Id, x.NameAr, x.NameEn, x.Code); }
    public async Task<IReadOnlyList<LookupResponse>> ListGradesAsync(CancellationToken ct) => await db.GradeLevels.AsNoTracking().OrderBy(x => x.SortOrder).Select(x => new LookupResponse(x.Id, x.NameAr, x.NameEn, x.Id.ToString())).ToListAsync(ct);
    public async Task<LookupResponse> CreateGradeAsync(LookupRequest r, CancellationToken ct) { var x = new GradeLevel { NameAr = r.NameAr, NameEn = r.NameEn, SortOrder = await db.GradeLevels.CountAsync(ct) + 1 }; db.Add(x); await db.SaveChangesAsync(ct); return new(x.Id, x.NameAr, x.NameEn, x.Id.ToString()); }
}

public sealed class SessionService(AppDbContext db, ICurrentUser current, IDateTimeProvider clock) : ISessionService
{
    public async Task<SessionResponse> CreateAsync(CreateSessionRequest r, CancellationToken ct)
    {
        var pricing = await Pricing(r.StudentId, r.TeacherId, r.SubjectId, ct);
        var x = new ClassSession();
        Apply(x, r.StudentId, r.TeacherId, r.SubjectId, r.ScheduledAt, r.DurationMinutes, r.ClassLink, r.StudentCreditCost, r.RecurrenceType, r.RecurrenceEndDate, pricing);
        db.Sessions.Add(x); await db.AuditAsync(current, "SessionCreated", nameof(ClassSession), x.Id, null, r.RecurrenceType.ToString(), ct); await db.SaveChangesAsync(ct); return Map(x);
    }
    public async Task<SessionResponse> UpdateAsync(Guid id, UpdateSessionRequest r, CancellationToken ct)
    {
        var x = await db.Sessions.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new AppException(404, ErrorCodes.SessionNotFound, "الجلسة غير موجودة.");
        if (x.Status is not (SessionStatus.Scheduled or SessionStatus.Cancelled) || await db.AttendanceRecords.AnyAsync(a => a.SessionId == id, ct)) throw new AppException(409, ErrorCodes.Validation, "لا يمكن تعديل جلسة بدأ عليها تسجيل حضور أو تسوية مالية.");
        var pricing = await Pricing(r.StudentId, r.TeacherId, r.SubjectId, ct);
        var before = $"{x.ScheduledAt:O}|{x.RecurrenceType}";
        Apply(x, r.StudentId, r.TeacherId, r.SubjectId, r.ScheduledAt, r.DurationMinutes, r.ClassLink, r.StudentCreditCost, r.RecurrenceType, r.RecurrenceEndDate, pricing);
        x.UpdatedAt = clock.UtcNow; await db.AuditAsync(current, "SessionUpdated", nameof(ClassSession), x.Id, before, $"{x.ScheduledAt:O}|{x.RecurrenceType}", ct); await db.SaveChangesAsync(ct); return Map(x);
    }
    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var x = await db.Sessions.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new AppException(404, ErrorCodes.SessionNotFound, "الجلسة غير موجودة.");
        if (x.Status is not (SessionStatus.Scheduled or SessionStatus.Cancelled) || await db.AttendanceRecords.AnyAsync(a => a.SessionId == id, ct)) throw new AppException(409, ErrorCodes.Validation, "لا يمكن حذف جلسة بدأ عليها تسجيل حضور أو تسوية مالية.");
        x.IsDeleted = true; x.DeletedAt = clock.UtcNow; x.DeletedBy = current.UserId; await db.AuditAsync(current, "SessionDeleted", nameof(ClassSession), x.Id, x.ScheduledAt.ToString("O"), null, ct); await db.SaveChangesAsync(ct);
    }
    public async Task<PageResult<SessionResponse>> SearchAsync(PageRequest p, CancellationToken ct)
    {
        var q = db.Sessions.AsNoTracking();
        var teacherId = await db.Teachers.Where(x => x.UserId == current.UserId).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct); if (teacherId.HasValue) q = q.Where(x => x.TeacherId == teacherId);
        var studentId = await db.Students.Where(x => x.UserId == current.UserId).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct); if (studentId.HasValue) q = q.Where(x => x.StudentId == studentId);
        var n = Math.Max(1, p.PageNumber); var z = Math.Clamp(p.PageSize, 1, 100); var count = await q.CountAsync(ct); var rows = await q.OrderByDescending(x => x.ScheduledAt).Skip((n - 1) * z).Take(z).Select(x => Map(x)).ToListAsync(ct); return new(rows, n, z, count);
    }
    public async Task RequestAttendanceAsync(AttendanceRequestDto r, CancellationToken ct)
    {
        var studentId = await db.Students.Where(x => x.UserId == current.UserId).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct) ?? throw new AppException(403, ErrorCodes.Forbidden, "غير مسموح بهذا المورد.");
        var session = await db.Sessions.SingleOrDefaultAsync(x => x.Id == r.SessionId && x.StudentId == studentId, ct) ?? throw new AppException(404, ErrorCodes.SessionNotFound, "الجلسة غير موجودة.");
        if (await db.AttendanceRecords.AnyAsync(x => x.SessionId == r.SessionId, ct)) throw new AppException(409, ErrorCodes.Validation, "تم إنشاء طلب حضور لهذه الجلسة بالفعل.");
        session.Status = SessionStatus.AttendancePending; session.AttendanceStatus = AttendanceStatus.Pending;
        db.AttendanceRecords.Add(new AttendanceRecord { SessionId = session.Id, StudentId = session.StudentId, TeacherId = session.TeacherId, RequestedAt = clock.UtcNow, Notes = r.Notes }); await db.SaveChangesAsync(ct);
    }
    public async Task ConfirmAttendanceAsync(Guid attendanceId, AttendanceDecisionRequest r, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var attendance = await db.AttendanceRecords.SingleOrDefaultAsync(x => x.Id == attendanceId, ct) ?? throw new AppException(404, ErrorCodes.NotFound, "طلب الحضور غير موجود.");
        if (attendance.Status == AttendanceStatus.Confirmed) return;
        if (attendance.Status != AttendanceStatus.Pending) throw new AppException(409, ErrorCodes.Validation, "لا يمكن تأكيد طلب الحضور في حالته الحالية.");
        var session = await db.Sessions.SingleAsync(x => x.Id == attendance.SessionId, ct);
        var student = await db.Students.SingleAsync(x => x.Id == attendance.StudentId, ct);
        var balance = CreditRules.Deduct(student.SessionCreditBalance, session.StudentCreditCost);
        var before = balance.Before; student.SessionCreditBalance = balance.After;
        attendance.Status = AttendanceStatus.Confirmed; attendance.ConfirmedAt = clock.UtcNow; attendance.ConfirmedBy = current.UserId; attendance.Notes = r.Notes;
        session.Status = SessionStatus.Completed; session.AttendanceStatus = AttendanceStatus.Confirmed;
        db.StudentCreditTransactions.Add(new StudentCreditTransaction { StudentId = student.Id, Type = CreditTransactionType.SessionDeduction, Quantity = -session.StudentCreditCost, BalanceBefore = before, BalanceAfter = student.SessionCreditBalance, ReferenceType = nameof(ClassSession), ReferenceId = session.Id, Description = "خصم جلسة مؤكدة", CreatedBy = current.UserId! });
        db.TeacherEarnings.Add(new TeacherEarning { TeacherId = session.TeacherId, SessionId = session.Id, Amount = session.TeacherRateSnapshot });
        db.FinancialTransactions.Add(new FinancialTransaction { TransactionType = FinancialTransactionType.TeacherEarning, ReferenceType = nameof(ClassSession), ReferenceId = session.Id, Amount = session.TeacherRateSnapshot, Direction = TransactionDirection.Debit, Description = "استحقاق معلم عن جلسة مؤكدة", CreatedBy = current.UserId! });
        await db.AuditAsync(current, "AttendanceConfirmed", nameof(AttendanceRecord), attendance.Id, "Pending", "Confirmed", ct); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task RejectAttendanceAsync(Guid attendanceId, AttendanceDecisionRequest r, CancellationToken ct) { var a = await db.AttendanceRecords.SingleOrDefaultAsync(x => x.Id == attendanceId, ct) ?? throw new AppException(404, ErrorCodes.NotFound, "طلب الحضور غير موجود."); if (a.Status != AttendanceStatus.Pending) throw new AppException(409, ErrorCodes.Validation, "تمت معالجة الطلب سابقًا."); a.Status = AttendanceStatus.Rejected; a.ConfirmedAt = clock.UtcNow; a.ConfirmedBy = current.UserId; a.Notes = r.Notes; var s = await db.Sessions.SingleAsync(x => x.Id == a.SessionId, ct); s.Status = SessionStatus.Rejected; s.AttendanceStatus = AttendanceStatus.Rejected; await db.AuditAsync(current, "AttendanceRejected", nameof(AttendanceRecord), a.Id, "Pending", "Rejected", ct); await db.SaveChangesAsync(ct); }
    private async Task<SessionPricing> Pricing(Guid studentId, Guid teacherId, Guid subjectId, CancellationToken ct)
    {
        var teacher = await db.Teachers.SingleOrDefaultAsync(x => x.Id == teacherId, ct) ?? throw new AppException(404, ErrorCodes.TeacherNotFound, "المعلم غير موجود.");
        var student = await db.Students.SingleOrDefaultAsync(x => x.Id == studentId, ct) ?? throw new AppException(404, ErrorCodes.StudentNotFound, "الطالب غير موجود.");
        var assignment = await db.TeacherStudentAssignments.SingleOrDefaultAsync(x => x.TeacherId == teacherId && x.StudentId == studentId && x.SubjectId == subjectId, ct) ?? throw new AppException(409, ErrorCodes.Validation, "المعلم غير مكلّف بهذا الطالب والمادة.");
        var stageRate = await db.TeacherGradeRates.Where(x => x.TeacherId == teacherId && x.GradeLevelId == student.GradeLevelId).Select(x => new { x.Rate, x.Currency }).SingleOrDefaultAsync(ct);
        return new(stageRate?.Rate ?? teacher.DefaultPerSessionRate, stageRate?.Currency ?? teacher.DefaultCurrency, assignment.SessionPrice, assignment.Currency);
    }
    private static void Apply(ClassSession x, Guid studentId, Guid teacherId, Guid subjectId, DateTimeOffset scheduledAt, int durationMinutes, string? classLink, int studentCreditCost, SessionRecurrenceType recurrenceType, DateTimeOffset? recurrenceEndDate, SessionPricing pricing)
    {
        x.StudentId = studentId; x.TeacherId = teacherId; x.SubjectId = subjectId; x.ScheduledAt = scheduledAt.ToUniversalTime(); x.DurationMinutes = durationMinutes; x.ClassLink = classLink; x.StudentCreditCost = studentCreditCost; x.RecurrenceType = recurrenceType; x.RecurrenceEndDate = recurrenceType == SessionRecurrenceType.Once ? null : recurrenceEndDate?.ToUniversalTime(); x.TeacherRateSnapshot = pricing.TeacherRate; x.TeacherRateCurrencySnapshot = pricing.TeacherCurrency; x.StudentPriceSnapshot = pricing.StudentPrice; x.StudentPriceCurrencySnapshot = pricing.StudentCurrency;
    }
    private static SessionResponse Map(ClassSession x) => new(x.Id, x.StudentId, x.TeacherId, x.SubjectId, x.ScheduledAt, x.DurationMinutes, x.ClassLink, x.Status.ToString(), x.AttendanceStatus?.ToString(), x.TeacherRateSnapshot, x.StudentCreditCost, x.TeacherRateCurrencySnapshot, x.StudentPriceSnapshot, x.StudentPriceCurrencySnapshot, x.RecurrenceType, x.RecurrenceEndDate);
    private sealed record SessionPricing(decimal TeacherRate, string TeacherCurrency, decimal StudentPrice, string StudentCurrency);
}

public sealed class LearningService(AppDbContext db, ICurrentUser current, IDateTimeProvider clock) : ILearningService
{
    public async Task<Guid> CreateAssignmentAsync(AssignmentRequest r, CancellationToken ct) { var teacher = await db.Teachers.SingleOrDefaultAsync(x => x.UserId == current.UserId, ct) ?? throw new AppException(403, ErrorCodes.Forbidden, "يلزم حساب معلم."); if (!await db.TeacherSubjects.AnyAsync(x => x.TeacherId == teacher.Id && x.SubjectId == r.SubjectId, ct)) throw new AppException(403, ErrorCodes.Forbidden, "المادة غير مسندة إلى المعلم."); var permitted = await db.TeacherStudentAssignments.Where(x => x.TeacherId == teacher.Id && x.SubjectId == r.SubjectId).Select(x => x.StudentId).ToListAsync(ct); if (r.StudentIds.Any(x => !permitted.Contains(x))) throw new AppException(403, ErrorCodes.Forbidden, "يوجد طالب غير مسند إلى المعلم."); var a = new Assignment { TeacherId = teacher.Id, SubjectId = r.SubjectId, Title = r.Title, Description = r.Description, DueDate = r.DueDate.ToUniversalTime(), MaxGrade = r.MaxGrade, Status = r.Publish ? AssignmentStatus.Published : AssignmentStatus.Draft, Targets = r.StudentIds.Distinct().Select(x => new AssignmentTarget { StudentId = x }).ToList() }; db.Assignments.Add(a); await db.SaveChangesAsync(ct); return a.Id; }
    public async Task<Guid> UpsertSubmissionAsync(Guid assignmentId, SubmissionRequest r, CancellationToken ct) { var studentId = await db.Students.Where(x => x.UserId == current.UserId).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct) ?? throw new AppException(403, ErrorCodes.Forbidden, "يلزم حساب طالب."); var assignment = await db.Assignments.SingleOrDefaultAsync(x => x.Id == assignmentId && x.Status == AssignmentStatus.Published && x.Targets.Any(t => t.StudentId == studentId), ct) ?? throw new AppException(404, ErrorCodes.NotFound, "الواجب غير موجود."); var s = await db.AssignmentSubmissions.SingleOrDefaultAsync(x => x.AssignmentId == assignmentId && x.StudentId == studentId, ct); if (s is not null && s.Status is SubmissionStatus.Graded) throw new AppException(409, ErrorCodes.Validation, "لا يمكن تعديل واجب تم تقييمه."); s ??= new AssignmentSubmission { AssignmentId = assignmentId, StudentId = studentId, Status = SubmissionStatus.Draft }; if (s.Id != Guid.Empty && db.Entry(s).State == EntityState.Detached) db.Add(s); s.TextAnswer = r.TextAnswer; if (r.Submit) { s.SubmittedAt = clock.UtcNow; s.Status = clock.UtcNow > assignment.DueDate ? SubmissionStatus.Late : SubmissionStatus.Submitted; } if (db.Entry(s).State == EntityState.Detached) db.Add(s); await db.SaveChangesAsync(ct); return s.Id; }
    public async Task GradeAsync(Guid submissionId, GradeRequest r, CancellationToken ct) { var teacherId = await db.Teachers.Where(x => x.UserId == current.UserId).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct) ?? throw new AppException(403, ErrorCodes.Forbidden, "يلزم حساب معلم."); var submission = await db.AssignmentSubmissions.SingleOrDefaultAsync(x => x.Id == submissionId && db.Assignments.Any(a => a.Id == x.AssignmentId && a.TeacherId == teacherId), ct) ?? throw new AppException(404, ErrorCodes.NotFound, "التسليم غير موجود."); var max = await db.Assignments.Where(x => x.Id == submission.AssignmentId).Select(x => x.MaxGrade).SingleAsync(ct); if (r.Grade < 0 || r.Grade > max) throw new AppException(422, ErrorCodes.Validation, "الدرجة خارج النطاق المسموح.", "grade"); submission.Grade = r.Grade; submission.TeacherFeedback = r.Feedback; submission.GradedAt = clock.UtcNow; submission.GradedBy = current.UserId; submission.Status = SubmissionStatus.Graded; await db.SaveChangesAsync(ct); }
}

public sealed class FinanceService(AppDbContext db, ICurrentUser current, IDateTimeProvider clock) : IFinanceService
{
    public async Task<Guid> CreateExpenseAsync(ExpenseRequest r, CancellationToken ct) { var x = new OperatingExpense { Category = r.Category, Description = r.Description, Amount = r.Amount, ExpenseDate = r.ExpenseDate, Reference = r.Reference, CreatedBy = current.UserId!, Status = RecordStatus.Pending }; db.Add(x); await db.SaveChangesAsync(ct); return x.Id; }
    public async Task<Guid> CreatePeriodAsync(FinancialPeriodRequest r, CancellationToken ct) { if (r.EndDate < r.StartDate || await db.FinancialPeriods.AnyAsync(x => x.StartDate <= r.EndDate && x.EndDate >= r.StartDate, ct)) throw new AppException(409, ErrorCodes.Validation, "الفترة المالية غير صالحة أو متداخلة."); var x = new FinancialPeriod { StartDate = r.StartDate, EndDate = r.EndDate, Status = FinancialPeriodStatus.Open }; db.Add(x); await db.SaveChangesAsync(ct); return x.Id; }
    public async Task<FinancialSummary> ClosePeriodAsync(Guid id, string? key, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); var p = await db.FinancialPeriods.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new AppException(404, ErrorCodes.NotFound, "الفترة المالية غير موجودة.");
        if (p.Status == FinancialPeriodStatus.Closed) return await Summary(p, ct); if (p.Status != FinancialPeriodStatus.Open) throw new AppException(409, ErrorCodes.FinancialPeriodClosed, "الفترة قيد المعالجة أو مغلقة."); p.Status = FinancialPeriodStatus.Calculating; await db.SaveChangesAsync(ct);
        var summary = await Summary(p, ct); var shares = await db.PartnerShares.Where(x => x.EffectiveFrom <= p.EndDate && (x.EffectiveTo == null || x.EffectiveTo >= p.EndDate)).ToListAsync(ct); var percentages = shares.Select(x => x.Percentage).ToArray(); var amounts = FinancialCalculator.Distribute(summary.NetProfit, percentages);
        for (var i = 0; i < shares.Count; i++) { var d = new PartnerDividend { FinancialPeriodId = p.Id, PartnerId = shares[i].PartnerId, SharePercentageSnapshot = shares[i].Percentage, NetProfitSnapshot = summary.NetProfit, DividendAmount = amounts[i], Status = RecordStatus.Approved }; db.PartnerDividends.Add(d); db.FinancialTransactions.Add(new FinancialTransaction { TransactionType = FinancialTransactionType.PartnerDividend, ReferenceType = nameof(PartnerDividend), ReferenceId = d.Id, Amount = d.DividendAmount, Direction = TransactionDirection.Debit, Description = "توزيع أرباح شريك", FinancialPeriodId = p.Id, CreatedBy = current.UserId! }); }
        p.Status = FinancialPeriodStatus.Closed; p.ClosedAt = clock.UtcNow; p.ClosedBy = current.UserId; await db.AuditAsync(current, "FinancialPeriodClosed", nameof(FinancialPeriod), p.Id, "Open", System.Text.Json.JsonSerializer.Serialize(summary), ct); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return summary;
    }
    public async Task<Guid> SetPartnerShareAsync(PartnerShareRequest r, CancellationToken ct) { if (r.Percentage < 0 || r.Percentage > 100 || r.EffectiveTo < r.EffectiveFrom) throw new AppException(422, ErrorCodes.InvalidPartnerPercentages, "نسبة الشريك أو تاريخ سريانها غير صالح."); var x = new PartnerShare { PartnerId = r.PartnerId, Percentage = r.Percentage, EffectiveFrom = r.EffectiveFrom, EffectiveTo = r.EffectiveTo }; db.Add(x); await db.AuditAsync(current, "PartnerShareCreated", nameof(PartnerShare), x.Id, null, r.Percentage.ToString(), ct); await db.SaveChangesAsync(ct); return x.Id; }
    public async Task GenerateTeacherPayoutsAsync(Guid periodId, CancellationToken ct) { var period = await db.TeacherPayoutPeriods.SingleAsync(x => x.Id == periodId, ct); var from = period.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc); var to = period.EndDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc); var earnings = await db.TeacherEarnings.Where(e => db.Sessions.Any(s => s.Id == e.SessionId && s.ScheduledAt >= from && s.ScheduledAt < to)).ToListAsync(ct); foreach (var group in earnings.GroupBy(x => x.TeacherId)) { if (await db.TeacherPayouts.AnyAsync(x => x.TeacherId == group.Key && x.PeriodId == periodId, ct)) continue; var payout = new TeacherPayout { TeacherId = group.Key, PeriodId = periodId, GrossAmount = group.Sum(x => x.Amount), FinalAmount = group.Sum(x => x.Amount), Status = PayoutStatus.Draft, Items = group.Select(x => new TeacherPayoutItem { SessionId = x.SessionId, Amount = x.Amount }).ToList() }; db.Add(payout); } await db.SaveChangesAsync(ct); }
    public async Task ChangePayoutStatusAsync(Guid id, string action, CancellationToken ct) { var p = await db.TeacherPayouts.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new AppException(404, ErrorCodes.NotFound, "كشف الراتب غير موجود."); var target = action.ToLowerInvariant() switch { "submit" => PayoutStatus.PendingReview, "approve" => PayoutStatus.Approved, "reject" => PayoutStatus.Rejected, "pay" => PayoutStatus.Paid, _ => throw new AppException(400, ErrorCodes.Validation, "إجراء غير معروف.") }; if (!PayoutRules.CanTransition(p.Status, target)) throw new AppException(409, ErrorCodes.Validation, "انتقال حالة كشف الراتب غير مسموح."); p.Status = target; if (target == PayoutStatus.Approved) { p.ApprovedAt = clock.UtcNow; p.ApprovedBy = current.UserId; } if (target == PayoutStatus.Paid) { p.PaidAt = clock.UtcNow; db.FinancialTransactions.Add(new FinancialTransaction { TransactionType = FinancialTransactionType.TeacherPayout, ReferenceType = nameof(TeacherPayout), ReferenceId = p.Id, Amount = p.FinalAmount, Direction = TransactionDirection.Debit, Description = "صرف راتب معلم", CreatedBy = current.UserId! }); } await db.SaveChangesAsync(ct); }
    private async Task<FinancialSummary> Summary(FinancialPeriod p, CancellationToken ct) { var from = p.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc); var to = p.EndDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc); var revenue = await db.StudentPayments.Where(x => x.Status == RecordStatus.Paid && x.PaidAt >= from && x.PaidAt < to).SumAsync(x => (decimal?)x.Amount, ct) ?? 0; var teacher = await db.TeacherPayouts.Where(x => (x.Status == PayoutStatus.Approved || x.Status == PayoutStatus.Paid) && x.ApprovedAt >= from && x.ApprovedAt < to).SumAsync(x => (decimal?)x.FinalAmount, ct) ?? 0; var expenses = await db.OperatingExpenses.Where(x => x.Status == RecordStatus.Approved && x.ExpenseDate >= p.StartDate && x.ExpenseDate <= p.EndDate).SumAsync(x => (decimal?)x.Amount, ct) ?? 0; return new(revenue, teacher, expenses, FinancialCalculator.NetProfit(revenue, teacher, expenses)); }
}

public sealed class DashboardService(AppDbContext db, ICurrentUser current, IDateTimeProvider clock) : IDashboardService
{
    public async Task<object> AdminAsync(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var start = (from ?? new DateOnly(clock.UtcToday.Year, clock.UtcToday.Month, 1)).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = (to ?? clock.UtcToday).AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var revenue = await db.StudentPayments.Where(x => x.Status == RecordStatus.Paid && x.PaidAt >= start && x.PaidAt < end).SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
        var costs = await db.TeacherPayouts.Where(x => (x.Status == PayoutStatus.Approved || x.Status == PayoutStatus.Paid) && x.ApprovedAt >= start && x.ApprovedAt < end).SumAsync(x => (decimal?)x.FinalAmount, ct) ?? 0;
        var expenses = await db.OperatingExpenses.Where(x => x.Status == RecordStatus.Approved && x.CreatedAt >= start && x.CreatedAt < end).SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
        return new { activeStudents = await db.Students.CountAsync(x => x.Status == AccountStatus.Active, ct), activeTeachers = await db.Teachers.CountAsync(x => x.Status == AccountStatus.Active, ct), sessionsToday = await db.Sessions.CountAsync(x => x.ScheduledAt.Date == clock.UtcNow.Date, ct), pendingAttendance = await db.AttendanceRecords.CountAsync(x => x.Status == AttendanceStatus.Pending, ct), expiringStudents = await db.Students.CountAsync(x => x.ExpirationDate >= clock.UtcNow && x.ExpirationDate <= clock.UtcNow.AddDays(7), ct), pendingPayouts = await db.TeacherPayouts.CountAsync(x => x.Status == PayoutStatus.PendingReview, ct), revenue, teacherCosts = costs, operatingExpenses = expenses, netProfit = FinancialCalculator.NetProfit(revenue, costs, expenses) };
    }
    public async Task<object> TeacherAsync(CancellationToken ct) { var id = await db.Teachers.Where(x => x.UserId == current.UserId).Select(x => x.Id).SingleAsync(ct); return new { assignedStudents = await db.TeacherStudentAssignments.CountAsync(x => x.TeacherId == id, ct), upcomingSessions = await db.Sessions.CountAsync(x => x.TeacherId == id && x.ScheduledAt > clock.UtcNow, ct), pendingSubmissions = await db.AssignmentSubmissions.CountAsync(x => (x.Status == SubmissionStatus.Submitted || x.Status == SubmissionStatus.Late) && db.Assignments.Any(a => a.Id == x.AssignmentId && a.TeacherId == id), ct), currentPayout = await db.TeacherPayouts.Where(x => x.TeacherId == id).OrderByDescending(x => x.CreatedAt).Select(x => (decimal?)x.FinalAmount).FirstOrDefaultAsync(ct) }; }
    public async Task<object> StudentAsync(CancellationToken ct) { var s = await db.Students.SingleAsync(x => x.UserId == current.UserId, ct); return new { remainingSessions = s.SessionCreditBalance, expirationDate = s.ExpirationDate, upcomingSessions = await db.Sessions.CountAsync(x => x.StudentId == s.Id && x.ScheduledAt > clock.UtcNow, ct), pendingAssignments = await db.AssignmentTargets.CountAsync(x => x.StudentId == s.Id && !db.AssignmentSubmissions.Any(y => y.AssignmentId == x.AssignmentId && y.StudentId == s.Id && y.Status != SubmissionStatus.Draft), ct) }; }
    public async Task<object> PartnerAsync(CancellationToken ct) { var id = await db.Partners.Where(x => x.UserId == current.UserId).Select(x => x.Id).SingleAsync(ct); return await db.PartnerDividends.Where(x => x.PartnerId == id).OrderByDescending(x => x.CreatedAt).Select(x => new { x.FinancialPeriodId, x.SharePercentageSnapshot, x.NetProfitSnapshot, x.DividendAmount, status = x.Status.ToString() }).ToListAsync(ct); }
}

public sealed class LocalFileStorage(IOptions<FileStorageOptions> options) : IFileStorageService
{
    private readonly FileStorageOptions _options = options.Value;
    public async Task<(string Key, string StoredName)> SaveAsync(Stream content, string originalName, string contentType, CancellationToken ct) { var ext = Path.GetExtension(Path.GetFileName(originalName)).ToLowerInvariant(); if (!_options.AllowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase)) throw new AppException(422, ErrorCodes.Validation, "امتداد الملف غير مسموح."); var allowedMime = ext switch { ".pdf" => new[] { "application/pdf" }, ".png" => new[] { "image/png" }, ".jpg" or ".jpeg" => new[] { "image/jpeg" }, ".doc" => new[] { "application/msword" }, ".docx" => new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" }, _ => Array.Empty<string>() }; if (!allowedMime.Contains(contentType, StringComparer.OrdinalIgnoreCase)) throw new AppException(422, ErrorCodes.Validation, "نوع MIME لا يطابق امتداد الملف."); if (content.CanSeek && content.Length > _options.MaxFileSizeBytes) throw new AppException(413, ErrorCodes.Validation, "حجم الملف يتجاوز الحد المسموح."); var name = $"{Guid.NewGuid():N}{ext}"; var root = Path.GetFullPath(_options.RootPath); Directory.CreateDirectory(root); var path = Path.Combine(root, name); await using var output = File.Create(path); await content.CopyToAsync(output, ct); return (name, name); }
    public Task DeleteAsync(string key, CancellationToken ct) { var root = Path.GetFullPath(_options.RootPath); var path = Path.GetFullPath(Path.Combine(root, key)); if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new AppException(400, ErrorCodes.Validation, "مسار تخزين غير صالح."); if (File.Exists(path)) File.Delete(path); return Task.CompletedTask; }
}
public sealed class DatabaseNotificationSender(AppDbContext db) : INotificationSender { public async Task SendAsync(string userId, string title, string body, CancellationToken ct) { db.Notifications.Add(new Notification { UserId = userId, Type = "System", Title = title, Body = body }); await db.SaveChangesAsync(ct); } }
