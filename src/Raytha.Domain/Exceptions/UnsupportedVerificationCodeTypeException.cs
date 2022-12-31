namespace Raytha.Domain.Exceptions;

public class UnsupportedVerificationCodeTypeException : Exception
{
    public UnsupportedVerificationCodeTypeException(string developerName)
        : base($"Verification code type \"{developerName}\" is unsupported.")
    {
    }
}
