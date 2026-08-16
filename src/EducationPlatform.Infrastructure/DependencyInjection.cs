using System.Text;
using EducationPlatform.Application;
using EducationPlatform.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace EducationPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Section));
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.Section));
        services.Configure<BusinessOptions>(configuration.GetSection(BusinessOptions.Section));
        services.AddDbContext<AppDbContext>(o => o.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        services.AddIdentityCore<ApplicationUser>(o =>
        {
            o.Password.RequiredLength = configuration.GetValue("Security:PasswordRequiredLength", 10);
            o.Password.RequireDigit = true; o.Password.RequireLowercase = true; o.Password.RequireUppercase = true; o.Password.RequireNonAlphanumeric = true;
            o.Lockout.MaxFailedAccessAttempts = configuration.GetValue("Security:MaxFailedAccessAttempts", 5); o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(configuration.GetValue("Security:LockoutMinutes", 15));
            o.User.RequireUniqueEmail = false;
        }).AddRoles<IdentityRole>().AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();
        var jwt = configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => { o.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, ValidIssuer = jwt.Issuer, ValidateAudience = true, ValidAudience = jwt.Audience, ValidateLifetime = true, ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)), ClockSkew = TimeSpan.FromSeconds(30) }; });
        services.AddAuthorizationBuilder()
            .AddPolicy("AcademicOperations", p => p.RequireRole(Roles.Admin, Roles.Moderator))
            .AddPolicy("FinancialAdmin", p => p.RequireRole(Roles.Admin))
            .AddPolicy("TeacherOnly", p => p.RequireRole(Roles.Teacher))
            .AddPolicy("StudentOnly", p => p.RequireRole(Roles.Student))
            .AddPolicy("PartnerOnly", p => p.RequireRole(Roles.Partner));
        services.AddScoped<IAuthService, AuthService>(); services.AddScoped<IStudentService, StudentService>(); services.AddScoped<ITeacherService, TeacherService>(); services.AddScoped<ICatalogService, CatalogService>(); services.AddScoped<ISessionService, SessionService>(); services.AddScoped<ILearningService, LearningService>(); services.AddScoped<IFinanceService, FinanceService>(); services.AddScoped<IDashboardService, DashboardService>(); services.AddScoped<IFileStorageService, LocalFileStorage>(); services.AddScoped<INotificationSender, DatabaseNotificationSender>(); services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>(); if (configuration.GetValue("BackgroundJobs:Enabled", true)) services.AddHostedService<MaintenanceWorker>();
        return services;
    }

    public static async Task SeedIdentityAsync(IServiceProvider provider, IConfiguration configuration)
    {
        using var scope = provider.CreateScope(); var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>(); foreach (var role in Roles.All) if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new IdentityRole(role));
        var userName = configuration["InitialAdmin:UserName"]; var password = configuration["InitialAdmin:Password"];
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password)) return;
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); if (await users.FindByNameAsync(userName) is not null) return;
        var user = new ApplicationUser { UserName = userName, DisplayName = configuration["InitialAdmin:DisplayName"] ?? "مدير النظام", Email = configuration["InitialAdmin:Email"] };
        var result = await users.CreateAsync(user, password); if (!result.Succeeded) throw new InvalidOperationException("Initial admin creation failed: " + string.Join(", ", result.Errors.Select(x => x.Code))); await users.AddToRoleAsync(user, Roles.Admin);
    }
}
