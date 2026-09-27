using System.Security.Cryptography;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.Login;

/// <summary>
/// Emailed one-time sign-in codes for the magic link scheme. The OneTimePassword
/// row id is a hash of the user id and code together, so a low-entropy 6-digit
/// code can never locate a row on its own and only works for the account it was
/// issued to.
/// </summary>
public static class MagicLinkCode
{
    public static string Generate() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    public static string Normalize(string? code) =>
        string.IsNullOrWhiteSpace(code)
            ? string.Empty
            : new string(code.Where(char.IsDigit).ToArray());

    public static byte[] OtpId(Guid userId, string code) =>
        PasswordUtility.Hash($"{userId:N}:{code}");
}
