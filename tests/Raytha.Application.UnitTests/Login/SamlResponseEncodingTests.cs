using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using FluentAssertions;
using Raytha.Application.Login.Commands;

namespace Raytha.Application.UnitTests.Login;

public class SamlResponseEncodingTests
{
    [Test]
    public void Base64Response_PreservesNonAsciiNameId()
    {
        const string xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <samlp:Response xmlns:samlp="urn:oasis:names:tc:SAML:2.0:protocol" xmlns:saml="urn:oasis:names:tc:SAML:2.0:assertion">
              <saml:Assertion>
                <saml:Subject>
                  <saml:NameID>José</saml:NameID>
                </saml:Subject>
              </saml:Assertion>
            </samlp:Response>
            """;

        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(xml));
        var response = new LoginWithSaml.SAMLResponse(payload, CertificateChars());

        response.NameID.Should().Be("José");
    }

    private static string CertificateChars()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=raytha-test",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );
        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1)
        );
        var bytes = certificate.Export(X509ContentType.Cert);
        return new string(bytes.Select(b => (char)b).ToArray());
    }
}
