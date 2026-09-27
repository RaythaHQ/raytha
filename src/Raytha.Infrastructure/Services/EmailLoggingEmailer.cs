using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.EmailLogs;
using Raytha.Domain.Common;
using Raytha.Domain.Entities;
using Raytha.Infrastructure.Persistence;

namespace Raytha.Infrastructure.Services;

/// <summary>
/// Decorator around the configured <see cref="IEmailer"/> that records every send attempt
/// to the EmailLogs table. Bodies are persisted only after <see cref="EmailBodySanitizer"/>
/// redacts reset links, magic-link tokens, and labeled passwords. Logging failures never
/// mask the send outcome.
/// </summary>
public sealed class EmailLoggingEmailer : IEmailer
{
    public const int MaxBodyLength = 200_000;

    private readonly IEmailer _inner;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailLoggingEmailer> _logger;

    public EmailLoggingEmailer(
        IEmailer inner,
        IServiceScopeFactory scopeFactory,
        ILogger<EmailLoggingEmailer> logger
    )
    {
        _inner = inner;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void SendEmail(EmailMessage message)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _inner.SendEmail(message);
            stopwatch.Stop();
            WriteLog(message, isSuccess: true, error: null, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            WriteLog(message, isSuccess: false, error: ex.Message, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private void WriteLog(EmailMessage message, bool isSuccess, string? error, long durationMs)
    {
        try
        {
            // A dedicated scope keeps email-log writes out of the caller's change tracker.
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RaythaDbContext>();

            db.EmailLogs.Add(
                new EmailLog
                {
                    Id = Guid.NewGuid(),
                    ToAddress = string.Join(", ", message.To ?? Array.Empty<string>()),
                    FromAddress = message.FromEmailAddress ?? string.Empty,
                    Subject = RedactSensitiveContent(
                        message.Subject ?? string.Empty,
                        message.SensitiveContent
                    ),
                    Body = Sanitize(RedactSensitiveContent(message.Content, message.SensitiveContent)),
                    IsHtml = message.IsHtml,
                    IsSuccess = isSuccess,
                    ErrorMessage = error,
                    DurationMs = durationMs,
                }
            );
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to write email log entry for {Recipient}",
                string.Join(", ", message.To ?? Array.Empty<string>())
            );
        }
    }

    /// <summary>Redacts credential-bearing values, then truncates oversized bodies.</summary>
    public static string Sanitize(string? body)
    {
        var redacted = EmailBodySanitizer.Sanitize(body);
        return redacted.Length > MaxBodyLength ? redacted[..MaxBodyLength] : redacted;
    }

    /// <summary>
    /// Strips values the sender declared secret (<see cref="EmailMessage.SensitiveContent"/>).
    /// Pattern-based sanitization cannot find a bare one-time code in a
    /// site-builder-authored template, but the sender knows the exact string.
    /// </summary>
    public static string RedactSensitiveContent(string? text, IEnumerable<string>? sensitiveValues)
    {
        var result = text ?? string.Empty;
        foreach (var value in sensitiveValues ?? Array.Empty<string>())
        {
            if (!string.IsNullOrEmpty(value))
            {
                result = result.Replace(value, EmailBodySanitizer.RedactedValue);
            }
        }
        return result;
    }
}
