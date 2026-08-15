IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [DisplayName] nvarchar(max) NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetimeoffset NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [Assignments] (
        [Id] uniqueidentifier NOT NULL,
        [TeacherId] uniqueidentifier NOT NULL,
        [SubjectId] uniqueidentifier NOT NULL,
        [Title] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [DueDate] datetimeoffset NOT NULL,
        [MaxGrade] decimal(18,2) NOT NULL,
        [Status] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_Assignments] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [AssignmentSubmissions] (
        [Id] uniqueidentifier NOT NULL,
        [AssignmentId] uniqueidentifier NOT NULL,
        [StudentId] uniqueidentifier NOT NULL,
        [TextAnswer] nvarchar(max) NULL,
        [SubmittedAt] datetimeoffset NULL,
        [Status] int NOT NULL,
        [Grade] decimal(18,2) NULL,
        [TeacherFeedback] nvarchar(max) NULL,
        [GradedAt] datetimeoffset NULL,
        [GradedBy] nvarchar(max) NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_AssignmentSubmissions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [AttendanceRecords] (
        [Id] uniqueidentifier NOT NULL,
        [SessionId] uniqueidentifier NOT NULL,
        [StudentId] uniqueidentifier NOT NULL,
        [TeacherId] uniqueidentifier NOT NULL,
        [RequestedAt] datetimeoffset NOT NULL,
        [ConfirmedAt] datetimeoffset NULL,
        [ConfirmedBy] nvarchar(max) NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(max) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_AttendanceRecords] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] nvarchar(max) NULL,
        [Action] nvarchar(max) NOT NULL,
        [EntityType] nvarchar(max) NOT NULL,
        [EntityId] nvarchar(max) NULL,
        [OldValues] nvarchar(max) NULL,
        [NewValues] nvarchar(max) NULL,
        [IpAddress] nvarchar(max) NULL,
        [UserAgent] nvarchar(max) NULL,
        [CorrelationId] nvarchar(max) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [Curricula] (
        [Id] uniqueidentifier NOT NULL,
        [NameAr] nvarchar(max) NOT NULL,
        [NameEn] nvarchar(max) NULL,
        [Code] nvarchar(450) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_Curricula] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [FinancialPeriods] (
        [Id] uniqueidentifier NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NOT NULL,
        [Status] int NOT NULL,
        [ClosedAt] datetimeoffset NULL,
        [ClosedBy] nvarchar(max) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_FinancialPeriods] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [FinancialTransactions] (
        [Id] uniqueidentifier NOT NULL,
        [TransactionType] int NOT NULL,
        [ReferenceType] nvarchar(max) NOT NULL,
        [ReferenceId] uniqueidentifier NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Direction] int NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [FinancialPeriodId] uniqueidentifier NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_FinancialTransactions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [GradeLevels] (
        [Id] uniqueidentifier NOT NULL,
        [NameAr] nvarchar(max) NOT NULL,
        [NameEn] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_GradeLevels] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [LessonMaterials] (
        [Id] uniqueidentifier NOT NULL,
        [TeacherId] uniqueidentifier NOT NULL,
        [SubjectId] uniqueidentifier NOT NULL,
        [Title] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [FileName] nvarchar(max) NOT NULL,
        [StoredFileName] nvarchar(max) NOT NULL,
        [StorageKey] nvarchar(max) NOT NULL,
        [ContentType] nvarchar(max) NOT NULL,
        [FileSize] bigint NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_LessonMaterials] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [Moderators] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] nvarchar(max) NOT NULL,
        [FullName] nvarchar(max) NOT NULL,
        [PhoneNumber] nvarchar(max) NOT NULL,
        [Status] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_Moderators] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] nvarchar(max) NOT NULL,
        [Type] nvarchar(max) NOT NULL,
        [Title] nvarchar(max) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [IsRead] bit NOT NULL,
        [ReadAt] datetimeoffset NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [OperatingExpenses] (
        [Id] uniqueidentifier NOT NULL,
        [Category] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [ExpenseDate] date NOT NULL,
        [Reference] nvarchar(max) NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [ApprovedBy] nvarchar(max) NULL,
        [Status] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_OperatingExpenses] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Expense_Amount] CHECK ([Amount] >= 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [PartnerDividends] (
        [Id] uniqueidentifier NOT NULL,
        [FinancialPeriodId] uniqueidentifier NOT NULL,
        [PartnerId] uniqueidentifier NOT NULL,
        [SharePercentageSnapshot] decimal(18,2) NOT NULL,
        [NetProfitSnapshot] decimal(18,2) NOT NULL,
        [DividendAmount] decimal(18,2) NOT NULL,
        [Status] int NOT NULL,
        [PaidAt] datetimeoffset NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_PartnerDividends] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [Partners] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] nvarchar(max) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [ContactInformation] nvarchar(max) NULL,
        [Status] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_Partners] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [PartnerShares] (
        [Id] uniqueidentifier NOT NULL,
        [PartnerId] uniqueidentifier NOT NULL,
        [Percentage] decimal(18,2) NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_PartnerShares] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PartnerShare_Percentage] CHECK ([Percentage] >= 0 AND [Percentage] <= 100)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] nvarchar(max) NOT NULL,
        [TokenHash] nvarchar(450) NOT NULL,
        [ExpiresAt] datetimeoffset NOT NULL,
        [RevokedAt] datetimeoffset NULL,
        [ReplacedByTokenId] uniqueidentifier NULL,
        [CreatedByIp] nvarchar(max) NULL,
        [Device] nvarchar(max) NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [Sessions] (
        [Id] uniqueidentifier NOT NULL,
        [StudentId] uniqueidentifier NOT NULL,
        [TeacherId] uniqueidentifier NOT NULL,
        [SubjectId] uniqueidentifier NOT NULL,
        [ScheduledAt] datetimeoffset NOT NULL,
        [DurationMinutes] int NOT NULL,
        [ClassLink] nvarchar(max) NULL,
        [Status] int NOT NULL,
        [AttendanceStatus] int NULL,
        [TeacherRateSnapshot] decimal(18,2) NOT NULL,
        [StudentCreditCost] int NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_Sessions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [StudentCreditTransactions] (
        [Id] uniqueidentifier NOT NULL,
        [StudentId] uniqueidentifier NOT NULL,
        [Type] int NOT NULL,
        [Quantity] int NOT NULL,
        [BalanceBefore] int NOT NULL,
        [BalanceAfter] int NOT NULL,
        [ReferenceType] nvarchar(450) NOT NULL,
        [ReferenceId] uniqueidentifier NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_StudentCreditTransactions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [StudentPayments] (
        [Id] uniqueidentifier NOT NULL,
        [StudentId] uniqueidentifier NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Currency] nvarchar(max) NOT NULL,
        [PaymentMethod] nvarchar(max) NOT NULL,
        [PaymentReference] nvarchar(max) NULL,
        [PaidAt] datetimeoffset NOT NULL,
        [RecordedBy] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NULL,
        [Status] int NOT NULL,
        [IdempotencyKey] nvarchar(450) NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_StudentPayments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Payment_Amount] CHECK ([Amount] > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [Students] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] nvarchar(max) NOT NULL,
        [FullName] nvarchar(max) NOT NULL,
        [PhoneNumber] nvarchar(450) NOT NULL,
        [ParentName] nvarchar(max) NULL,
        [ParentPhoneNumber] nvarchar(max) NULL,
        [GradeLevelId] uniqueidentifier NOT NULL,
        [CurriculumId] uniqueidentifier NOT NULL,
        [SessionCreditBalance] int NOT NULL,
        [ExpirationDate] datetimeoffset NULL,
        [Status] int NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_Students] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Student_Balance] CHECK ([SessionCreditBalance] >= 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [Subjects] (
        [Id] uniqueidentifier NOT NULL,
        [NameAr] nvarchar(max) NOT NULL,
        [NameEn] nvarchar(max) NULL,
        [Code] nvarchar(450) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_Subjects] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [TeacherEarnings] (
        [Id] uniqueidentifier NOT NULL,
        [TeacherId] uniqueidentifier NOT NULL,
        [SessionId] uniqueidentifier NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_TeacherEarnings] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [TeacherPayoutAdjustmentRequests] (
        [Id] uniqueidentifier NOT NULL,
        [TeacherPayoutId] uniqueidentifier NOT NULL,
        [TeacherId] uniqueidentifier NOT NULL,
        [Type] nvarchar(max) NOT NULL,
        [RequestedAmount] decimal(18,2) NOT NULL,
        [Reason] nvarchar(max) NOT NULL,
        [Status] int NOT NULL,
        [AdminResponse] nvarchar(max) NULL,
        [ReviewedBy] nvarchar(max) NULL,
        [ReviewedAt] datetimeoffset NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_TeacherPayoutAdjustmentRequests] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [TeacherPayoutPeriods] (
        [Id] uniqueidentifier NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NOT NULL,
        [IsClosed] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_TeacherPayoutPeriods] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [TeacherPayouts] (
        [Id] uniqueidentifier NOT NULL,
        [TeacherId] uniqueidentifier NOT NULL,
        [PeriodId] uniqueidentifier NOT NULL,
        [GrossAmount] decimal(18,2) NOT NULL,
        [AdjustmentAmount] decimal(18,2) NOT NULL,
        [FinalAmount] decimal(18,2) NOT NULL,
        [Status] int NOT NULL,
        [ApprovedBy] nvarchar(max) NULL,
        [ApprovedAt] datetimeoffset NULL,
        [PaidAt] datetimeoffset NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_TeacherPayouts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [Teachers] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] nvarchar(max) NOT NULL,
        [FullName] nvarchar(max) NOT NULL,
        [PhoneNumber] nvarchar(450) NOT NULL,
        [WhatsApp] nvarchar(max) NULL,
        [DefaultPerSessionRate] decimal(18,2) NOT NULL,
        [PreferredPayoutMethod] nvarchar(max) NULL,
        [EWalletNumber] nvarchar(max) NULL,
        [InstaPayIdentifier] nvarchar(max) NULL,
        [PaymentDetails] nvarchar(max) NULL,
        [Status] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_Teachers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [TeacherStudentAssignments] (
        [Id] uniqueidentifier NOT NULL,
        [TeacherId] uniqueidentifier NOT NULL,
        [StudentId] uniqueidentifier NOT NULL,
        [SubjectId] uniqueidentifier NOT NULL,
        [AssignedAt] datetimeoffset NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetimeoffset NULL,
        [DeletedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_TeacherStudentAssignments] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [AssignmentTargets] (
        [AssignmentId] uniqueidentifier NOT NULL,
        [StudentId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AssignmentTargets] PRIMARY KEY ([AssignmentId], [StudentId]),
        CONSTRAINT [FK_AssignmentTargets_Assignments_AssignmentId] FOREIGN KEY ([AssignmentId]) REFERENCES [Assignments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [SubmissionAttachments] (
        [Id] uniqueidentifier NOT NULL,
        [SubmissionId] uniqueidentifier NOT NULL,
        [FileName] nvarchar(max) NOT NULL,
        [StorageKey] nvarchar(max) NOT NULL,
        [ContentType] nvarchar(max) NOT NULL,
        [FileSize] bigint NOT NULL,
        [AssignmentSubmissionId] uniqueidentifier NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_SubmissionAttachments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SubmissionAttachments_AssignmentSubmissions_AssignmentSubmissionId] FOREIGN KEY ([AssignmentSubmissionId]) REFERENCES [AssignmentSubmissions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [StudentSubjects] (
        [StudentId] uniqueidentifier NOT NULL,
        [SubjectId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_StudentSubjects] PRIMARY KEY ([StudentId], [SubjectId]),
        CONSTRAINT [FK_StudentSubjects_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StudentSubjects_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [TeacherPayoutItems] (
        [Id] uniqueidentifier NOT NULL,
        [TeacherPayoutId] uniqueidentifier NOT NULL,
        [SessionId] uniqueidentifier NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_TeacherPayoutItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TeacherPayoutItems_TeacherPayouts_TeacherPayoutId] FOREIGN KEY ([TeacherPayoutId]) REFERENCES [TeacherPayouts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [TeacherCurricula] (
        [TeacherId] uniqueidentifier NOT NULL,
        [CurriculumId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_TeacherCurricula] PRIMARY KEY ([TeacherId], [CurriculumId]),
        CONSTRAINT [FK_TeacherCurricula_Curricula_CurriculumId] FOREIGN KEY ([CurriculumId]) REFERENCES [Curricula] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TeacherCurricula_Teachers_TeacherId] FOREIGN KEY ([TeacherId]) REFERENCES [Teachers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE TABLE [TeacherSubjects] (
        [TeacherId] uniqueidentifier NOT NULL,
        [SubjectId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_TeacherSubjects] PRIMARY KEY ([TeacherId], [SubjectId]),
        CONSTRAINT [FK_TeacherSubjects_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TeacherSubjects_Teachers_TeacherId] FOREIGN KEY ([TeacherId]) REFERENCES [Teachers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AssignmentSubmissions_AssignmentId_StudentId] ON [AssignmentSubmissions] ([AssignmentId], [StudentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AttendanceRecords_SessionId] ON [AttendanceRecords] ([SessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Curricula_Code] ON [Curricula] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PartnerDividends_FinancialPeriodId_PartnerId] ON [PartnerDividends] ([FinancialPeriodId], [PartnerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StudentCreditTransactions_StudentId_ReferenceType_ReferenceId] ON [StudentCreditTransactions] ([StudentId], [ReferenceType], [ReferenceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_StudentPayments_IdempotencyKey] ON [StudentPayments] ([IdempotencyKey]) WHERE [IdempotencyKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Students_PhoneNumber] ON [Students] ([PhoneNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Students_Status_ExpirationDate] ON [Students] ([Status], [ExpirationDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StudentSubjects_SubjectId] ON [StudentSubjects] ([SubjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Subjects_Code] ON [Subjects] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SubmissionAttachments_AssignmentSubmissionId] ON [SubmissionAttachments] ([AssignmentSubmissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TeacherCurricula_CurriculumId] ON [TeacherCurricula] ([CurriculumId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TeacherEarnings_SessionId] ON [TeacherEarnings] ([SessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TeacherPayoutItems_SessionId] ON [TeacherPayoutItems] ([SessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TeacherPayoutItems_TeacherPayoutId] ON [TeacherPayoutItems] ([TeacherPayoutId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Teachers_PhoneNumber] ON [Teachers] ([PhoneNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TeacherStudentAssignments_TeacherId_StudentId_SubjectId] ON [TeacherStudentAssignments] ([TeacherId], [StudentId], [SubjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TeacherSubjects_SubjectId] ON [TeacherSubjects] ([SubjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171339_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260815171339_InitialCreate', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815171908_AlignSoftDeleteFilters'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260815171908_AlignSoftDeleteFilters', N'9.0.6');
END;

COMMIT;
GO

