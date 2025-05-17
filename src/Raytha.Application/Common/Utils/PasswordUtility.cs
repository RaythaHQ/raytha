using System.Security.Cryptography;
using System.Text;

namespace Raytha.Application.Common.Utils;

public static class PasswordUtility
{
    public const int PASSWORD_MIN_CHARACTER_LENGTH = 8;

    public static string RandomPassword(int length)
    {
        char[] characters =
            "abcdefghijklmnopqursuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%(){}[]_".ToCharArray();
        Random random = new Random();
        StringBuilder newPassword = new StringBuilder(string.Empty, length);
        for (int i = 0; i < length; i++)
        {
            newPassword.Append(characters[random.Next(characters.Length)]);
        }
        return newPassword.ToString();
    }

    public static byte[] RandomSalt()
    {
        byte[] bytes = new byte[128 / 8];
        using (var keyGenerator = RandomNumberGenerator.Create())
        {
            keyGenerator.GetBytes(bytes);
            return bytes;
        }
    }

    public static byte[] Hash(string value)
    {
        return SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(value));
    }

    public static byte[] Hash(string value, byte[] salt)
    {
        return Hash(Encoding.UTF8.GetBytes(value), salt);
    }

    public static byte[] Hash(byte[] value, byte[] salt)
    {
        byte[] saltedValue = value.Concat(salt).ToArray();
        return SHA256.Create().ComputeHash(saltedValue);
    }

    public static bool IsMatch(byte[] password1, byte[] password2)
    {
        if (password1.Length != password2.Length)
        {
            return false;
        }

        for (int i = 0; i < password1.Length; i++)
        {
            if (password1[i] != password2[i])
            {
                return false;
            }
        }

        return true;
    }
}
