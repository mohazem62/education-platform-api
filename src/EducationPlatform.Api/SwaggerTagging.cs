using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EducationPlatform.Api;

public static class SwaggerTagging
{
    public const string Authentication = "Authentication";
    public const string Student = "Student APIs";
    public const string Teacher = "Teacher APIs";
    public const string AdminPartner = "Admin & Partner APIs";
    public const string Shared = "Shared APIs";

    public static readonly string[] OrderedTags = [Authentication, Student, Teacher, AdminPartner, Shared];

    public static IList<string> For(ApiDescription api)
    {
        if (api.ActionDescriptor is not ControllerActionDescriptor action)
            return [Shared];

        if (action.ControllerName == "Auth")
            return [Authentication];

        var authorization = action.ControllerTypeInfo.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Concat(action.MethodInfo.GetCustomAttributes(typeof(AuthorizeAttribute), true))
            .Cast<AuthorizeAttribute>()
            .ToArray();

        var policies = authorization.Select(x => x.Policy).Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.Ordinal);
        var roles = authorization.SelectMany(x => (x.Roles ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (policies.Contains("StudentOnly") || roles.Contains("Student"))
            return [Student];
        if (policies.Contains("TeacherOnly") || roles.Contains("Teacher"))
            return [Teacher];
        if (policies.Overlaps(["AcademicOperations", "FinancialAdmin", "PartnerOnly"]) || roles.Overlaps(["Admin", "Moderator", "Partner"]))
            return [AdminPartner];

        if (action.ControllerName == "PartnerDividends")
            return [AdminPartner];

        return [Shared];
    }
}

public sealed class SwaggerTagOrderFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Tags = SwaggerTagging.OrderedTags.Select(name => new OpenApiTag { Name = name }).ToList();
    }
}
