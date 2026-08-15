using System.Security.Claims;
using System.Text.Json;
using EducationPlatform.Application;
using EducationPlatform.Domain;
using Microsoft.EntityFrameworkCore;

namespace EducationPlatform.Api;

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private HttpContext? Context => accessor.HttpContext;
    public string? UserId => Context?.User.FindFirstValue(ClaimTypes.NameIdentifier);
    public string? IpAddress => Context?.Connection.RemoteIpAddress?.ToString();
    public string? UserAgent => Context?.Request.Headers.UserAgent.ToString();
    public string CorrelationId => Context?.TraceIdentifier ?? Guid.NewGuid().ToString("N");
}

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception ex)
        {
            var (status, code, message, field) = ex switch
            {
                AppException a => (a.StatusCode, a.Code, a.Message, a.Field),
                DomainException d => (409, d.Code, d.Message, (string?)null),
                DbUpdateConcurrencyException => (409, "CONCURRENCY_CONFLICT", "تم تعديل السجل بواسطة طلب آخر. أعد تحميل البيانات وحاول مجددًا.", null),
                DbUpdateException => (409, "DATA_CONFLICT", "تعارضت العملية مع قيد لحماية سلامة البيانات.", null),
                _ => (500, "INTERNAL_ERROR", "حدث خطأ غير متوقع. يرجى المحاولة لاحقًا.", null)
            };
            if (status == 500) logger.LogError(ex, "Unhandled error. TraceId {TraceId}", context.TraceIdentifier); else logger.LogWarning("Request failed with {Code}. TraceId {TraceId}", code, context.TraceIdentifier);
            context.Response.StatusCode = status; context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new ApiResponse<object>(false, message, null, [new ApiError(field, code, message)], context.TraceIdentifier), new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        }
    }
}
