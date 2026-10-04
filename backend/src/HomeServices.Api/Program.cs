using System.Diagnostics;
using System.Text.Json.Serialization;
using HomeServices.Api.Authorization;
using HomeServices.Api.Chat;
using HomeServices.Api.Commissions;
using HomeServices.Api.Errors;
using HomeServices.Api.Files;
using HomeServices.Api.Identity;
using HomeServices.Api.Localization;
using HomeServices.Api.Notifications;
using HomeServices.Api.Orders;
using HomeServices.Api.Requests;
using HomeServices.Api.Staff;
using HomeServices.Application;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Chat;
using HomeServices.Application.Notifications;
using HomeServices.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using HomeServices.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Logging: Serilog configured from appsettings (console everywhere, Seq in development).
builder.Services.AddSerilog((services, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

// Errors: every failure becomes ProblemDetails with a code and a traceId.
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? context.HttpContext.TraceIdentifier));
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Request language: negotiated from Accept-Language among active languages.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentLanguage, HttpCurrentLanguage>();

// Signed-in user from the access token (replaces the "system" user registered by Infrastructure).
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

// Back Office permissions: [HasPermission("partners.approve")] on endpoints.
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

// Enums travel as names ("Specialist"), and unreadable requests get the usual validation error shape.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = InvalidRequestProblems.Create);
// Live chat: SignalR hub; WebSocket clients send the Portal access token in the query string.
builder.Services.AddSignalR()
    .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton<IChatNotifier, SignalRChatNotifier>();
builder.Services.AddSingleton<INotificationPusher, SignalRNotificationPusher>();
builder.Services.PostConfigure<JwtBearerOptions>(AuthSchemes.Portal, options => options.Events = new JwtBearerEvents
{
    OnMessageReceived = context =>
    {
        if (context.HttpContext.Request.Path.StartsWithSegments(ChatHub.Path) && context.Request.Query["access_token"] is { Count: > 0 } token)
        {
            context.Token = token;
        }

        return Task.CompletedTask;
    },
});

// Background: unanswered requests go to the operator queue.
builder.Services.AddSingleton<RequestFollowUpService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<RequestFollowUpService>());

// Background: orders marked as done complete when the customer doesn't answer.
builder.Services.AddSingleton<OrderAutoCompleteService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<OrderAutoCompleteService>());

// Background: weekly commission statements, overdue reminders, pausing partners who don't pay.
builder.Services.AddSingleton<CommissionJobService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<CommissionJobService>());

// Background: new notifications go out as SMS (the important ones) and live updates.
builder.Services.AddSingleton<NotificationDeliveryService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<NotificationDeliveryService>());

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// A fixed SMS code is a testing convenience; never let it reach production.
if (builder.Environment.IsProduction() && !string.IsNullOrEmpty(builder.Configuration["Auth:FixedOtpCode"]))
{
    throw new InvalidOperationException("Auth:FixedOtpCode is for staging and must not be set in Production.");
}

var app = builder.Build();

// Request logging is outermost so it records the final status code (after error handling).
app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

// Migrations run on start in development and where "Database:MigrateOnStartup" is on (staging, one API instance).
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await app.Services.MigrateDatabaseAsync();
}

await app.Services.PrepareFileStorageAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Home Services API v1"));
}

// First Super Admin from configuration (no-op when not configured or when staff already exist).
await app.Services.BootstrapSuperAdminAsync(app.Configuration);

app.UseMiddleware<RequestLanguageMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();
app.MapHub<ChatHub>(ChatHub.Path);

await app.RunAsync();

// Exposes Program to WebApplicationFactory<Program> in the integration tests.
public partial class Program;
