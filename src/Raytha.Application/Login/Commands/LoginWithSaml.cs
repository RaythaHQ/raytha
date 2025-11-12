using System.Linq.Dynamic.Core;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text.Json.Serialization;
using System.Xml;
using CSharpVitamins;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Security;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;

namespace Raytha.Application.Login.Commands;

public class LoginWithSaml
{
    public record Command : LoggableRequest<CommandResponseDto<LoginDto>>
    {
        [JsonIgnore]
        public string SAMLResponse { get; init; } = null!;
        public string DeveloperName { get; set; } = null!;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.SAMLResponse).NotEmpty();
            RuleFor(x => x.DeveloperName).NotEmpty();
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        var developerName = request.DeveloperName.ToDeveloperName();
                        var authScheme = db.AuthenticationSchemes.FirstOrDefault(p =>
                            p.DeveloperName == developerName
                        );

                        if (authScheme == null)
                        {
                            throw new NotFoundException(
                                "Auth scheme",
                                $"{request.DeveloperName} was not found"
                            );
                        }

                        if (!authScheme.IsEnabledForUsers && !authScheme.IsEnabledForAdmins)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Authentication scheme is disabled"
                            );
                            return;
                        }

                        var payload = new SAMLResponse(
                            request.SAMLResponse,
                            authScheme.SamlCertificate
                        );

                        if (!payload.IsValid)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Failed authentication."
                            );
                            return;
                        }

                        if (string.IsNullOrEmpty(payload.EmailAddress))
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Missing 'email' attribute from saml assertion payload."
                            );
                            return;
                        }

                        var email = payload.EmailAddress.ToLower().Trim();

                        if (!email.IsValidEmailAddress())
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                $"'email' is not a valid email address"
                            );
                            return;
                        }

                        User entity = null;
                        if (!string.IsNullOrEmpty(payload.NameID))
                        {
                            entity = db.Users.FirstOrDefault(p =>
                                p.SsoId == payload.NameID
                                && p.AuthenticationSchemeId == authScheme.Id
                            );
                        }
                        else
                        {
                            entity = db.Users.FirstOrDefault(p =>
                                p.EmailAddress.ToLower() == email
                            );
                        }

                        if (entity != null)
                        {
                            if (entity.IsAdmin && !authScheme.IsEnabledForAdmins)
                            {
                                context.AddFailure(
                                    Constants.VALIDATION_SUMMARY,
                                    $"Authentication scheme disabled for administrators."
                                );
                                return;
                            }

                            if (!entity.IsAdmin && !authScheme.IsEnabledForUsers)
                            {
                                context.AddFailure(
                                    Constants.VALIDATION_SUMMARY,
                                    $"Authentication scheme disabled for public users."
                                );
                                return;
                            }

                            if (!entity.IsActive)
                            {
                                context.AddFailure(
                                    Constants.VALIDATION_SUMMARY,
                                    $"User has been deactivated."
                                );
                                return;
                            }
                        }
                    }
                );
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<LoginDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<CommandResponseDto<LoginDto>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var authScheme = _db.AuthenticationSchemes.First(p =>
                p.DeveloperName == request.DeveloperName.ToDeveloperName()
            );
            var payload = new SAMLResponse(request.SAMLResponse, authScheme.SamlCertificate);

            string nameID = payload.NameID;
            string email = payload.EmailAddress;
            string givenName = payload.FirstName;
            string familyName = payload.LastName;
            List<string> userGroups = payload.UserGroups.ToList();

            User entity = null;

            if (!string.IsNullOrWhiteSpace(nameID))
            {
                entity = _db
                    .Users.Include(p => p.UserGroups)
                    .FirstOrDefault(p =>
                        p.SsoId == nameID && p.AuthenticationSchemeId == authScheme.Id
                    );
            }

            if (entity == null)
            {
                var cleanedEmail = email.Trim().ToLower();
                entity = _db
                    .Users.Include(p => p.UserGroups)
                    .FirstOrDefault(p => p.EmailAddress.ToLower() == cleanedEmail);
            }

            ICollection<UserGroup> foundUserGroups = null;
            if (userGroups.Any())
            {
                foundUserGroups = _db
                    .UserGroups.Where(p => userGroups.Any(c => c == p.DeveloperName))
                    .ToList();
            }

            bool firstTime = false;
            if (entity == null)
            {
                firstTime = true;
                var id = Guid.NewGuid();
                ShortGuid shortGuid = id;
                var salt = PasswordUtility.RandomSalt();
                entity = new User
                {
                    Id = id,
                    EmailAddress = payload.EmailAddress.Trim(),
                    FirstName = payload.FirstName.IfNullOrEmpty("SsoVisitor"),
                    LastName = payload.LastName.IfNullOrEmpty(shortGuid),
                    IsActive = true,
                    Salt = salt,
                    PasswordHash = PasswordUtility.Hash(PasswordUtility.RandomPassword(12), salt),
                    UserGroups = foundUserGroups,
                };
            }
            else
            {
                entity.SsoId = nameID.IfNullOrEmpty(entity.SsoId);
                entity.FirstName = givenName.IfNullOrEmpty(entity.FirstName);
                entity.LastName = familyName.IfNullOrEmpty(entity.LastName);
                entity.EmailAddress = email.Trim();

                if (foundUserGroups != null && foundUserGroups.Any())
                {
                    foreach (var ug in entity.UserGroups)
                    {
                        if (!foundUserGroups.Any(p => p.DeveloperName == ug.DeveloperName))
                        {
                            entity.UserGroups.Remove(ug);
                        }
                    }

                    foreach (var ug in foundUserGroups)
                    {
                        if (!entity.UserGroups.Any(p => p.DeveloperName == ug.DeveloperName))
                        {
                            entity.UserGroups.Add(ug);
                        }
                    }
                }
            }

            entity.LastLoggedInTime = DateTime.UtcNow;
            entity.AuthenticationSchemeId = authScheme.Id;

            if (firstTime)
                _db.Users.Add(entity);

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<LoginDto>(
                new LoginDto
                {
                    Id = entity.Id,
                    FirstName = entity.FirstName,
                    LastName = entity.LastName,
                    EmailAddress = entity.EmailAddress,
                    LastModificationTime = entity.LastModificationTime,
                    AuthenticationScheme = authScheme.DeveloperName,
                    SsoId = entity.SsoId,
                    IsAdmin = entity.IsAdmin,
                }
            );
        }
    }

    public class SAMLResponse
    {
        private XmlDocument xmlDoc;
        private Certificate certificate;

        public SAMLResponse(string xmlPayload, string x509cert, bool isBase64payload = true)
        {
            certificate = new Certificate();
            certificate.LoadCertificate(x509cert);

            xmlDoc = new XmlDocument();
            xmlDoc.PreserveWhitespace = true;
            xmlDoc.XmlResolver = null;
            if (isBase64payload)
            {
                System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
                xmlPayload = enc.GetString(Convert.FromBase64String(xmlPayload));
            }
            xmlDoc.LoadXml(xmlPayload);
        }

        public string NameID
        {
            get
            {
                var manager = GetNamespaceManager();
                XmlNode node = xmlDoc.SelectSingleNode(
                    "/samlp:Response/saml:Assertion/saml:Subject/saml:NameID",
                    manager
                );
                return node.InnerText;
            }
        }

        public string EmailAddress => GetSingleAttribute(JwtRegisteredClaimNames.Email);
        public string FirstName => GetSingleAttribute(JwtRegisteredClaimNames.GivenName);
        public string LastName => GetSingleAttribute(JwtRegisteredClaimNames.FamilyName);
        public string[] UserGroups => GetArrayAttribute(RaythaClaimTypes.UserGroups);

        public bool IsValid
        {
            get
            {
                var manager = GetNamespaceManager();

                // Enforce single assertion rule to prevent injection attacks
                XmlNodeList allAssertions = xmlDoc.SelectNodes("//saml:Assertion", manager);
                if (allAssertions == null || allAssertions.Count != 1)
                {
                    return false; // Reject: zero assertions or multiple assertions
                }

                // Verify signature exists
                XmlNodeList nodeList = xmlDoc.SelectNodes("//ds:Signature", manager);
                if (nodeList == null || nodeList.Count == 0)
                {
                    return false; // No signature found
                }

                bool status = true;

                SignedXml signedXml = new SignedXml(xmlDoc);
                signedXml.LoadXml((XmlElement)nodeList[0]);

                status &= signedXml.CheckSignature(certificate.cert, true);

                var notBefore = NotBefore();
                status &= !notBefore.HasValue || (notBefore <= DateTime.Now);

                var notOnOrAfter = NotOnOrAfter();
                status &= !notOnOrAfter.HasValue || (notOnOrAfter > DateTime.Now);

                return status;
            }
        }

        private string GetSingleAttribute(string attr)
        {
            var manager = GetNamespaceManager();
            XmlNode node = xmlDoc.SelectSingleNode(
                $"/samlp:Response/saml:Assertion/saml:AttributeStatement/saml:Attribute[@Name='{attr}']/saml:AttributeValue",
                manager
            );
            return node == null ? null : node.InnerText;
        }

        private DateTime? NotBefore()
        {
            XmlNamespaceManager manager = new XmlNamespaceManager(xmlDoc.NameTable);
            manager.AddNamespace("saml", "urn:oasis:names:tc:SAML:2.0:assertion");
            manager.AddNamespace("samlp", "urn:oasis:names:tc:SAML:2.0:protocol");

            var nodes = xmlDoc.SelectNodes(
                "/samlp:Response/saml:Assertion/saml:Conditions",
                manager
            );
            string value = null;
            if (
                nodes != null
                && nodes.Count > 0
                && nodes[0] != null
                && nodes[0].Attributes != null
                && nodes[0].Attributes["NotBefore"] != null
            )
            {
                value = nodes[0].Attributes["NotBefore"].Value;
            }
            return value != null ? DateTime.Parse(value) : (DateTime?)null;
        }

        private DateTime? NotOnOrAfter()
        {
            XmlNamespaceManager manager = new XmlNamespaceManager(xmlDoc.NameTable);
            manager.AddNamespace("saml", "urn:oasis:names:tc:SAML:2.0:assertion");
            manager.AddNamespace("samlp", "urn:oasis:names:tc:SAML:2.0:protocol");

            var nodes = xmlDoc.SelectNodes(
                "/samlp:Response/saml:Assertion/saml:Conditions",
                manager
            );
            string value = null;
            if (
                nodes != null
                && nodes.Count > 0
                && nodes[0] != null
                && nodes[0].Attributes != null
                && nodes[0].Attributes["NotOnOrAfter"] != null
            )
            {
                value = nodes[0].Attributes["NotOnOrAfter"].Value;
            }
            return value != null ? DateTime.Parse(value) : (DateTime?)null;
        }

        private string[] GetArrayAttribute(string attr)
        {
            var manager = GetNamespaceManager();
            XmlNodeList nodes = xmlDoc.SelectNodes(
                $"/samlp:Response/saml:Assertion/saml:AttributeStatement/saml:Attribute[@Name='{attr}']/saml:AttributeValue",
                manager
            );
            if (nodes == null)
                return null;

            List<string> groups = new List<string>();
            foreach (XmlNode node in nodes)
            {
                groups.Add(node.InnerText);
            }

            return groups.ToArray();
        }

        private XmlNamespaceManager GetNamespaceManager()
        {
            XmlNamespaceManager manager = new XmlNamespaceManager(xmlDoc.NameTable);
            manager.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);
            manager.AddNamespace("saml", "urn:oasis:names:tc:SAML:2.0:assertion");
            manager.AddNamespace("samlp", "urn:oasis:names:tc:SAML:2.0:protocol");
            return manager;
        }
    }

    public class Certificate
    {
        public X509Certificate2 cert;

        public void LoadCertificate(string certificate)
        {
            cert = new X509Certificate2(StringToByteArray(certificate));
        }

        private byte[] StringToByteArray(string st)
        {
            byte[] bytes = new byte[st.Length];
            for (int i = 0; i < st.Length; i++)
            {
                bytes[i] = (byte)st[i];
            }
            return bytes;
        }
    }
}
