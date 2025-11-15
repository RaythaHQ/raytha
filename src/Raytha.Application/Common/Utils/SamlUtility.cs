using System.Xml;

namespace Raytha.Application.Common.Utils;

public static class SamlUtility
{
    public static string GetSamlRequestAsBase64(string acsUrl, string entityId)
    {
        string id = "_" + System.Guid.NewGuid().ToString();
        string issue_instant = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

        using (StringWriter sw = new StringWriter())
        {
            XmlWriterSettings xws = new XmlWriterSettings();
            xws.OmitXmlDeclaration = true;

            using (XmlWriter xw = XmlWriter.Create(sw, xws))
            {
                xw.WriteStartElement(
                    "samlp",
                    "AuthnRequest",
                    "urn:oasis:names:tc:SAML:2.0:protocol"
                );
                xw.WriteAttributeString("ID", id);
                xw.WriteAttributeString("Version", "2.0");
                xw.WriteAttributeString("IssueInstant", issue_instant);
                xw.WriteAttributeString(
                    "ProtocolBinding",
                    "urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST"
                );
                xw.WriteAttributeString("AssertionConsumerServiceURL", acsUrl);

                xw.WriteStartElement("saml", "Issuer", "urn:oasis:names:tc:SAML:2.0:assertion");
                xw.WriteString(entityId);
                xw.WriteEndElement();

                xw.WriteStartElement(
                    "samlp",
                    "NameIDPolicy",
                    "urn:oasis:names:tc:SAML:2.0:protocol"
                );
                xw.WriteAttributeString(
                    "Format",
                    "urn:oasis:names:tc:SAML:2.0:nameid-format:unspecified"
                );
                xw.WriteAttributeString("AllowCreate", "true");
                xw.WriteEndElement();

                xw.WriteStartElement(
                    "samlp",
                    "RequestedAuthnContext",
                    "urn:oasis:names:tc:SAML:2.0:protocol"
                );
                xw.WriteAttributeString("Comparison", "exact");

                xw.WriteStartElement(
                    "saml",
                    "AuthnContextClassRef",
                    "urn:oasis:names:tc:SAML:2.0:assertion"
                );
                xw.WriteString("urn:oasis:names:tc:SAML:2.0:ac:classes:PasswordProtectedTransport");
                xw.WriteEndElement();

                xw.WriteEndElement(); // RequestedAuthnContext

                xw.WriteEndElement();
            }

            byte[] toEncodeAsBytes = System.Text.Encoding.UTF8.GetBytes(sw.ToString());
            return System.Convert.ToBase64String(toEncodeAsBytes);
        }
    }
}
