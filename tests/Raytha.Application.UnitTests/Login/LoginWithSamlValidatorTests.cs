using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using FluentAssertions;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using NUnit.Framework;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Login.Commands;
using Raytha.Domain.Entities;

namespace Raytha.Application.UnitTests.Login;

[TestFixture]
public class LoginWithSamlValidatorTests
{
    private const string Email = "person@example.com";

    private RSA _rsa = null!;
    private X509Certificate2 _certificate = null!;
    private AuthenticationScheme _scheme = null!;
    private List<User> _users = null!;

    [OneTimeSetUp]
    public void CreateSigningCertificate()
    {
        _rsa = RSA.Create(2048);
        _certificate = new CertificateRequest("CN=idp.test", _rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
    }

    [OneTimeTearDown]
    public void DisposeSigningCertificate()
    {
        _certificate.Dispose();
        _rsa.Dispose();
    }

    [SetUp]
    public void SetUp()
    {
        _scheme = new AuthenticationScheme
        {
            Id = Guid.NewGuid(),
            Label = "Customer SSO",
            DeveloperName = "customer_sso",
            IsEnabledForUsers = true,
            IsEnabledForAdmins = false,
            SamlCertificate = _certificate.ExportCertificatePem(),
        };
        _users = [];
    }

    [Test]
    public void Admin_with_unlinked_name_id_cannot_sign_in_through_a_users_only_scheme()
    {
        _users.Add(new User { Id = Guid.NewGuid(), EmailAddress = Email, IsAdmin = true, IsActive = true });

        var result = Validate(SignedResponse(nameId: "new-idp-subject"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Authentication scheme disabled for administrators.");
    }

    [Test]
    public void Deactivated_user_with_unlinked_name_id_is_refused()
    {
        _users.Add(new User { Id = Guid.NewGuid(), EmailAddress = Email, IsActive = false });

        var result = Validate(SignedResponse(nameId: "new-idp-subject"));

        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "User has been deactivated.");
    }

    [Test]
    public void Active_user_with_unlinked_name_id_is_accepted()
    {
        _users.Add(new User { Id = Guid.NewGuid(), EmailAddress = Email, IsActive = true });

        Validate(SignedResponse(nameId: "new-idp-subject")).IsValid.Should().BeTrue();
    }

    private FluentValidation.Results.ValidationResult Validate(string samlResponse)
    {
        var db = new Mock<IRaythaDbContext>();
        db.Setup(x => x.AuthenticationSchemes)
            .Returns(new List<AuthenticationScheme> { _scheme }.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Users).Returns(_users.AsQueryable().BuildMockDbSet().Object);

        return new LoginWithSaml.Validator(db.Object).Validate(
            new LoginWithSaml.Command { SAMLResponse = samlResponse, DeveloperName = _scheme.DeveloperName! }
        );
    }

    private string SignedResponse(string nameId)
    {
        var now = DateTime.UtcNow;
        var xml =
            "<samlp:Response xmlns:samlp=\"urn:oasis:names:tc:SAML:2.0:protocol\" xmlns:saml=\"urn:oasis:names:tc:SAML:2.0:assertion\" "
            + $"ID=\"_r1\" Version=\"2.0\" IssueInstant=\"{now:yyyy-MM-ddTHH:mm:ssZ}\">"
            + "<saml:Issuer>https://idp.test</saml:Issuer>"
            + "<samlp:Status><samlp:StatusCode Value=\"urn:oasis:names:tc:SAML:2.0:status:Success\"/></samlp:Status>"
            + $"<saml:Assertion ID=\"_a1\" Version=\"2.0\" IssueInstant=\"{now:yyyy-MM-ddTHH:mm:ssZ}\">"
            + "<saml:Issuer>https://idp.test</saml:Issuer>"
            + $"<saml:Subject><saml:NameID>{nameId}</saml:NameID></saml:Subject>"
            + $"<saml:Conditions NotBefore=\"{now.AddMinutes(-1):yyyy-MM-ddTHH:mm:ssZ}\" NotOnOrAfter=\"{now.AddMinutes(5):yyyy-MM-ddTHH:mm:ssZ}\"/>"
            + $"<saml:AttributeStatement><saml:Attribute Name=\"email\"><saml:AttributeValue>{Email}</saml:AttributeValue></saml:Attribute></saml:AttributeStatement>"
            + "</saml:Assertion></samlp:Response>";

        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.LoadXml(xml);
        var ns = new XmlNamespaceManager(doc.NameTable);
        ns.AddNamespace("saml", "urn:oasis:names:tc:SAML:2.0:assertion");
        var assertion = (XmlElement)doc.SelectSingleNode("//saml:Assertion", ns)!;

        var signed = new SignedXml(doc) { SigningKey = _rsa };
        var reference = new Reference("#_a1");
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigExcC14NTransform());
        signed.AddReference(reference);
        signed.SignedInfo!.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
        signed.KeyInfo = new KeyInfo();
        signed.KeyInfo.AddClause(new KeyInfoX509Data(_certificate));
        signed.ComputeSignature();
        assertion.InsertAfter(doc.ImportNode(signed.GetXml(), true), assertion.SelectSingleNode("saml:Issuer", ns)!);

        return Convert.ToBase64String(Encoding.ASCII.GetBytes(doc.OuterXml));
    }
}
