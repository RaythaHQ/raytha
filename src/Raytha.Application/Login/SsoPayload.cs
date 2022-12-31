namespace Raytha.Application.Login;

public record SsoPayload
{
    public string EmailAddress { get; init; }
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public string SsoId { get; set; }
    public DateTime? Exp { get; set; }
    public string Jti { get; set; }
    public dynamic CustomAttributes { get; set; }
}
