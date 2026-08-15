using System.Globalization;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using EducationPlatform.Api;
using EducationPlatform.Application;
using EducationPlatform.Infrastructure;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using Microsoft.AspNetCore.HttpOverrides;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();
try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration).Enrich.FromLogContext());
    builder.Services.AddHttpContextAccessor(); builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>(); builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    builder.Services.Configure<ApiBehaviorOptions>(o => o.InvalidModelStateResponseFactory = ctx => new BadRequestObjectResult(new ApiResponse<object>(false, "بيانات الطلب غير صالحة.", null, ctx.ModelState.SelectMany(x => x.Value?.Errors.Select(e => new ApiError(x.Key, ErrorCodes.Validation, e.ErrorMessage)) ?? []).ToList(), ctx.HttpContext.TraceIdentifier)));
    builder.Services.AddLocalization(); builder.Services.Configure<RequestLocalizationOptions>(o => { var cultures = new[] { new CultureInfo("ar"), new CultureInfo("en") }; o.DefaultRequestCulture = new("ar"); o.SupportedCultures = cultures; o.SupportedUICultures = cultures; });
    builder.Services.AddCors(o => o.AddPolicy("Frontend", p => { var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? []; if (origins.Length > 0) p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials(); }));
    builder.Services.AddRateLimiter(o => { o.RejectionStatusCode = 429; o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })); });
    builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("sqlserver");
    builder.Services.Configure<ForwardedHeadersOptions>(o => { o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto; o.KnownNetworks.Clear(); o.KnownProxies.Clear(); });
    builder.Services.AddEndpointsApiExplorer(); builder.Services.AddSwaggerGen(o => { o.SwaggerDoc("v1", new OpenApiInfo { Title = "منصة التعليم - API", Version = "v1", Description = "واجهة عربية أولًا لإدارة التعليم والعمليات المالية" }); o.CustomSchemaIds(t => (t.FullName ?? t.Name).Replace('+', '.')); o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", Description = "أدخل JWT الناتج من /api/v1/auth/login" }); o.AddSecurityRequirement(new OpenApiSecurityRequirement { [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = [] }); });
    var app = builder.Build();
    app.UseForwardedHeaders(); app.UseMiddleware<ApiExceptionMiddleware>(); app.UseSerilogRequestLogging(); app.UseRequestLocalization(); app.UseHttpsRedirection();
    app.Use(async (ctx, next) => { ctx.Response.Headers["X-Content-Type-Options"] = "nosniff"; ctx.Response.Headers["X-Frame-Options"] = "DENY"; ctx.Response.Headers["Referrer-Policy"] = "no-referrer"; await next(); });
    app.UseCors("Frontend"); app.UseRateLimiter(); app.UseAuthentication(); app.UseAuthorization();
    app.UseSwagger(); app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "Education Platform v1")); app.MapControllers();
    app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }); app.MapHealthChecks("/health/ready"); app.MapHealthChecks("/health");
    if (app.Configuration.GetValue("Database:AutoMigrate", false)) { using var scope = app.Services.CreateScope(); await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync(); await DependencyInjection.SeedIdentityAsync(app.Services, app.Configuration); }
    app.Run();
}
catch (HostAbortedException) { }
catch (Exception ex) { Log.Fatal(ex, "Application terminated unexpectedly"); }
finally { await Log.CloseAndFlushAsync(); }

public partial class Program;
