using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace EducationPlatform.IntegrationTests;

public sealed class ApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public ApiSmokeTests(WebApplicationFactory<Program> factory) => _client = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["BackgroundJobs:Enabled"] = "false" }))).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
    [Fact] public async Task Swagger_document_is_available() { var response = await _client.GetAsync("/swagger/v1/swagger.json"); Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Contains("/api/v1/auth/login", await response.Content.ReadAsStringAsync()); }
    [Fact] public async Task Swagger_is_grouped_by_consumer_role() { var json = await _client.GetStringAsync("/swagger/v1/swagger.json"); foreach (var tag in new[] { "Authentication", "Student APIs", "Teacher APIs", "Admin & Partner APIs", "Shared APIs" }) Assert.Contains(tag, json); }
    [Fact] public async Task Swagger_exposes_teacher_assignments_page_endpoints() { var json = await _client.GetStringAsync("/swagger/v1/swagger.json"); Assert.Contains("/api/v1/assignments/teacher-overview", json); Assert.Contains("/api/v1/submissions/teacher-view", json); }
    [Fact] public async Task Swagger_exposes_home_cards_and_schedule_endpoints() { var json = await _client.GetStringAsync("/swagger/v1/swagger.json"); Assert.Contains("/api/v1/home/cards", json); Assert.Contains("/api/v1/home/schedule", json); Assert.Contains("zoomUrl", json, StringComparison.OrdinalIgnoreCase); }
    [Fact] public async Task Login_contract_includes_user_roles() { var json = await _client.GetStringAsync("/swagger/v1/swagger.json"); Assert.Contains("CurrentUserResponse", json); Assert.Contains("roles", json, StringComparison.OrdinalIgnoreCase); }
    [Fact] public async Task Protected_student_endpoint_rejects_anonymous_access() { var response = await _client.GetAsync("/api/v1/students"); Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); }
    [Fact] public async Task Liveness_does_not_require_database() { var response = await _client.GetAsync("/health/live"); Assert.Equal(HttpStatusCode.OK, response.StatusCode); }
}
