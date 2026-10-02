using System.Reflection;
using System.Text.Json.Serialization;
using CSharpVitamins;
using FluentAssertions;
using Raytha.Application.AuditLogs.Queries;
using Raytha.Application.Common.Attributes;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Application.Login.Commands;
using Raytha.Application.OrganizationSettings.Commands;
using Raytha.Application.RaythaFunctions.Commands;
using Raytha.Application.SitePages.Commands;

namespace Raytha.Application.UnitTests.AuditLogs;

public class AuditRequestSanitizerTests
{
    [Test]
    public void DefaultAuditOrder_IsCreationTimeDescending()
    {
        var items = new GetAuditLogs.Query().GetOrderByItems().ToList();

        items.Should().ContainSingle();
        items[0].OrderByPropertyName.Should().Be("CreationTime");
        items[0].OrderByDirection.Should().Be("desc");
    }

    [Test]
    public void Sanitize_RedactsCredentialPropertiesAndJsonSettings()
    {
        var json = AuditRequestSanitizer.Sanitize(
            new
            {
                Headline = "Hello",
                ApiKey = "super-secret",
                SettingsJson = """
                    {"headline":"Hi","apiKey":"key-value","clientSecret":"sec-value","items":[{"question":"Q","password":"pw-value"}],"buttonUrl":"https://example.com/go?token=live-token"}
                    """,
                Note = "Password: hunter2",
            }
        );

        json.Should().Contain("Hello").And.Contain("Hi").And.Contain("question");
        json.Should()
            .NotContain("super-secret")
            .And.NotContain("key-value")
            .And.NotContain("sec-value")
            .And.NotContain("pw-value")
            .And.NotContain("live-token")
            .And.NotContain("hunter2");
        json.Should().Contain(AuditRequestSanitizer.RedactedValue);
    }

    [Test]
    public void Sanitize_WidgetSettings_KeepsCopyAndDropsSecrets()
    {
        var json = AuditRequestSanitizer.Sanitize(
            new EditWidget.Command
            {
                SectionName = "main",
                SettingsJson = """{"headline":"Welcome","apiKey":"widget-key"}""",
            }
        );

        json.Should().Contain("main").And.Contain("Welcome");
        json.Should().NotContain("widget-key");
    }

    [Test]
    public void Sanitize_SmtpCommand_OmitsHostAndCredentials()
    {
        var json = AuditRequestSanitizer.Sanitize(
            new EditSmtp.Command
            {
                SmtpOverrideSystem = true,
                SmtpHost = "mail.internal",
                SmtpPort = 587,
                SmtpUsername = "smtp-user",
                SmtpPassword = "smtp-pass",
            }
        );

        json.Should().Contain("SmtpOverrideSystem");
        json.Should()
            .NotContain("mail.internal")
            .And.NotContain("smtp-user")
            .And.NotContain("smtp-pass")
            .And.NotContain("587");
    }

    [Test]
    public void Sanitize_MagicLinkCode_IsNotStored()
    {
        var json = AuditRequestSanitizer.Sanitize(
            new CompleteLoginWithMagicLink.Command
            {
                EmailAddress = "a@b.com",
                Code = "magic-code-value",
            }
        );

        json.Should().Contain("a@b.com");
        json.Should().NotContain("magic-code-value");
    }

    [Test]
    public void Sanitize_ForgotPasswordToken_IsNotStored()
    {
        typeof(CompleteForgotPassword.Command)
            .GetCustomAttribute<AuditCredentialIdentifierAttribute>()
            .Should()
            .NotBeNull();

        var token = ShortGuid.NewGuid();
        var json = AuditRequestSanitizer.Sanitize(
            new CompleteForgotPassword.Command
            {
                Id = token,
                NewPassword = "hunter2-password",
                ConfirmNewPassword = "hunter2-password",
                SendEmail = false,
            },
            redactIdentifier: true
        );

        json.Should().NotContain("hunter2-password");
        json.Should().NotContain(token.ToString());
        json.Should().NotContain(token.Guid.ToString());
        json.Should().Contain(AuditRequestSanitizer.RedactedValue);
    }

    [Test]
    public void Sanitize_FunctionSource_IsKept()
    {
        var json = AuditRequestSanitizer.Sanitize(
            new EditRaythaFunction.Command
            {
                Name = "Hello",
                TriggerType = "http_request",
                Code = "const token = 1; return API_V1;",
                IsActive = true,
            }
        );

        json.Should().Contain("const token = 1; return API_V1;");
    }

    [Test]
    public void LoggableCommands_DoNotExposeCredentialProperties()
    {
        var offenders = new List<string>();
        foreach (var command in typeof(AuditRequestSanitizer).Assembly.GetTypes())
        {
            if (command.IsAbstract || !typeof(ILoggableRequest).IsAssignableFrom(command))
            {
                continue;
            }

            foreach (var property in command.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetIndexParameters().Length > 0 || !IsCredential(command, property))
                {
                    continue;
                }

                if (property.GetCustomAttribute<JsonIgnoreAttribute>() is null)
                {
                    offenders.Add($"{command.FullName}.{property.Name}");
                }
            }
        }

        offenders.Should().BeEmpty();
    }

    private static bool IsCredential(Type command, PropertyInfo property)
    {
        if (AuditRequestSanitizer.IsSensitiveName(property.Name))
        {
            return true;
        }

        return property.Name.Equals("Code", StringComparison.OrdinalIgnoreCase)
            && command.Namespace?.Contains(".Login.", StringComparison.Ordinal) == true;
    }
}
