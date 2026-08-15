using EducationPlatform.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EducationPlatform.Infrastructure;

public sealed class MaintenanceWorker(IServiceScopeFactory scopes, ILogger<MaintenanceWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(6));
        do
        {
            try
            {
                using var scope = scopes.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var now = DateTimeOffset.UtcNow;
                await db.RefreshTokens.Where(x => x.ExpiresAt < now.AddDays(-7)).ExecuteDeleteAsync(stoppingToken);
                var expiring = await db.Students.Where(x => x.ExpirationDate >= now && x.ExpirationDate <= now.AddDays(3)).Select(x => new { x.UserId, x.ExpirationDate }).ToListAsync(stoppingToken);
                foreach (var student in expiring)
                    if (!await db.Notifications.AnyAsync(x => x.UserId == student.UserId && x.Type == "SubscriptionExpiring" && x.CreatedAt >= now.AddHours(-24), stoppingToken))
                        db.Notifications.Add(new Notification { UserId = student.UserId, Type = "SubscriptionExpiring", Title = "قرب انتهاء الاشتراك", Body = $"سينتهي اشتراكك في {student.ExpirationDate:yyyy-MM-dd}." });
                await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Background maintenance cycle failed"); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
