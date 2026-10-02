namespace Raytha.Application.Common.Attributes;

/// <summary>
/// Marks a loggable command whose <c>Id</c> is a bearer credential (a reset token,
/// not the entity the audit row should point at). The audit payload stores
/// <c>[redacted]</c> instead of that id.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AuditCredentialIdentifierAttribute : Attribute { }
