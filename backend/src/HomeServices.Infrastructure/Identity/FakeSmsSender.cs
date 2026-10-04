using System.Collections.Concurrent;
using HomeServices.Application.Identity;
using HomeServices.Domain.Identity;
using Microsoft.Extensions.Logging;

namespace HomeServices.Infrastructure.Identity;

/// <summary>
/// Development SMS sender: writes the message to the log (look for "SMS to" in the console or Seq)
/// and keeps it in memory for tests. Replaced by a real SMS gateway in task T70.
/// </summary>
public sealed class FakeSmsSender(ILogger<FakeSmsSender> logger) : ISmsSender
{
    // LoggerMessage.Define rather than [LoggerMessage]: the attribute's LogLevel argument stops the
    // coverage tool from instrumenting this assembly (it can't resolve the ASP.NET Core shared framework).
    private static readonly Action<ILogger, string, string, Exception?> LogSms =
        LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(1, "SmsSent"), "SMS to {Phone}: {Message}");

    private readonly ConcurrentQueue<(PhoneNumber To, string Message)> _sent = new();

    public IReadOnlyCollection<(PhoneNumber To, string Message)> Sent => _sent;

    public Task SendAsync(PhoneNumber to, string message, CancellationToken cancellationToken)
    {
        _sent.Enqueue((to, message));
        LogSms(logger, to.Value, message, null);
        return Task.CompletedTask;
    }

    /// <summary>The newest message sent to <paramref name="to"/>, if any.</summary>
    public string? LastMessageTo(PhoneNumber to) => _sent.LastOrDefault(m => m.To == to).Message;
}
