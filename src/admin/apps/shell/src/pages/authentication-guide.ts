export type SsoSchemeType = "jwt" | "saml";
export type Audience = "users" | "admins";
export type EndpointRole = "start" | "return";

export type SsoContext = {
  /** Website URL from Configuration (or this browser's origin) plus the path base, no trailing slash. */
  baseUrl: string;
  origin: string;
  /** The developer name as the server stores it, or empty while nothing is typed. */
  developerName: string;
  websiteUrlConfigured: boolean;
};

export type GuideEndpoint = {
  audience: Audience;
  role: EndpointRole;
  label: string;
  detail: string;
  url: string;
};

export type GuideValues = {
  endpoints: Record<EndpointRole, Record<Audience, string>>;
  spEntityId: string;
};

export type GuideStep = {
  text: string;
  values?: readonly { label: string; value: (values: GuideValues) => string }[];
};

export type GuideRow = { name: string; requirement: string; detail: string };
export type GuideProblem = { symptom: string; fix: string };
export type GuideSnippet = { id: string; label: string; code: (origin: string) => string };
export type IdpRecipe = { id: string; name: string; steps: readonly GuideStep[]; note?: string };

const DEVELOPER_NAME_PLACEHOLDER = "{developer_name}";

const PATHS: Record<SsoSchemeType, Record<EndpointRole, Record<Audience, string>>> = {
  jwt: {
    start: { users: "/account/login/sso/", admins: "/raytha/login/sso/" },
    return: { users: "/account/login/jwt/", admins: "/raytha/login/jwt/" },
  },
  saml: {
    start: { users: "/account/login/sso/", admins: "/raytha/login/sso/" },
    return: { users: "/account/login/saml/", admins: "/raytha/login/saml/" },
  },
};

const ROLE_COPY: Record<SsoSchemeType, Record<EndpointRole, { label: string; detail: string }>> = {
  jwt: {
    start: {
      label: "Start sign-in",
      detail: "Link here to send someone to your sign-in URL. Add ?returnUrl=/a/local/path to choose where they land.",
    },
    return: {
      label: "Token callback",
      detail: "Your app sends the browser here with ?token=<jwt>. Raytha hands you this URL as raytha_callback_url.",
    },
  },
  saml: {
    start: {
      label: "Start sign-in",
      detail: "Link here to send someone to your IdP with a SAML request. Add ?returnUrl=/a/local/path to choose where they land.",
    },
    return: {
      label: "Assertion consumer service (ACS)",
      detail: "Your IdP posts the SAMLResponse here. Register it as the ACS, Reply, or Single sign-on URL.",
    },
  },
};

/** Mirrors StringExtensions.ToDeveloperName: lowercase, trim, every other character becomes "_". */
export function schemeDeveloperName(raw: string): string {
  return raw.toLowerCase().trim().replace(/[^a-z0-9]/g, "_");
}

/** Mirrors RelativeUrlBuilder.GetBaseUrl: the configured Website URL wins, the request origin is the fallback. */
export function ssoContext(
  websiteUrl: string,
  location: { origin: string; pathname: string },
  developerName: string,
): SsoContext {
  const configured = websiteUrl.trim().replace(/\/+$/, "");
  const pathBase = location.pathname.split("/raytha")[0] ?? "";
  const baseUrl = `${configured || location.origin}${pathBase}`;
  return {
    baseUrl,
    origin: originOf(baseUrl, location.origin),
    developerName: schemeDeveloperName(developerName),
    websiteUrlConfigured: configured.length > 0,
  };
}

function originOf(url: string, fallback: string): string {
  try {
    return new URL(url).origin;
  } catch {
    return fallback;
  }
}

function endpointUrl(context: SsoContext, type: SsoSchemeType, role: EndpointRole, audience: Audience): string {
  const name = context.developerName ? encodeURIComponent(context.developerName) : DEVELOPER_NAME_PLACEHOLDER;
  return `${context.baseUrl}${PATHS[type][role][audience]}${name}`;
}

export function guideEndpoints(context: SsoContext, type: SsoSchemeType): GuideEndpoint[] {
  const endpoints: GuideEndpoint[] = [];
  for (const audience of ["users", "admins"] as const) {
    for (const role of ["start", "return"] as const) {
      endpoints.push({ audience, role, ...ROLE_COPY[type][role], url: endpointUrl(context, type, role, audience) });
    }
  }
  return endpoints;
}

export function guideValues(context: SsoContext, type: SsoSchemeType, spEntityId: string): GuideValues {
  const byRole = (role: EndpointRole) => ({
    users: endpointUrl(context, type, role, "users"),
    admins: endpointUrl(context, type, role, "admins"),
  });
  return { endpoints: { start: byRole("start"), return: byRole("return") }, spEntityId: spEntityId.trim() };
}

/** A unique, stable URI for the SP entity ID field when it is empty. Any unique string works. */
export function suggestedSpEntityId(context: SsoContext): string {
  return endpointUrl(context, "saml", "return", "users");
}

export const AUDIENCE_LABEL: Record<Audience, string> = { users: "Public users", admins: "Admins" };

export const ACCOUNT_RULES = (identifier: string): readonly string[] => [
  `Raytha looks for the account by ${identifier} within this scheme first, then by email across all accounts. An existing account with the same email, including a password account, is linked to this scheme.`,
  "No match creates a new public user with a random password. SSO never creates an admin or grants admin access: add the admin in Raytha first, with the same email.",
  "Enabled for admins and Enabled for users decide who may sign in, by account type, on either callback. Admin accounts need Enabled for admins; everyone else needs Enabled for users. The URL only decides where they land.",
  "Deactivated accounts are refused.",
  "Every sign-in overwrites the account's email, and its first and last name when they are sent.",
  "groups values are matched against user group developer names, exactly (lowercase, e.g. editors). When at least one matches, the account's groups become exactly the matched set. When none match or the value is missing, groups are left alone.",
];

export const JWT_FLOW: readonly GuideStep[] = [
  {
    text: "Someone clicks this scheme's button (the public login page, or Continue with … on the admin sign-in page), or follows a Start sign-in link.",
    values: [
      { label: "Users", value: (values) => values.endpoints.start.users },
      { label: "Admins", value: (values) => values.endpoints.start.admins },
    ],
  },
  {
    text: "Raytha redirects the browser to your Sign in URL with one query parameter, raytha_callback_url. It is the token callback for that audience, with returnUrl attached when there is one.",
  },
  { text: "Your app signs the person in however it likes, then signs a short-lived JWT with the shared secret." },
  {
    text: "Your app checks that raytha_callback_url is on your Raytha site, appends token=<jwt>, and redirects the browser there. Keep the query string that is already on it.",
    values: [
      { label: "Users", value: (values) => values.endpoints.return.users },
      { label: "Admins", value: (values) => values.endpoints.return.admins },
    ],
  },
  {
    text: "Raytha validates the token, finds or creates the account, and sets its session cookie. It then redirects to returnUrl when that is a local path, otherwise to the home page (users) or the admin dashboard (admins).",
  },
];

export const JWT_FLOW_NOTE =
  "Your app can skip the first two steps and send people straight to a token callback URL with a token. Start sign-in only works for an audience the scheme is enabled for: the users link redirects home and the admin link answers 401 otherwise.";

export const JWT_CLAIMS: readonly GuideRow[] = [
  {
    name: "email",
    requirement: "Required",
    detail: "Must be a valid address. Finds the account when sub doesn't, and becomes the account's email on every sign-in.",
  },
  {
    name: "exp",
    requirement: "Required",
    detail: "Expiry in seconds since the epoch. Raytha allows no clock skew. Keep it to a few minutes.",
  },
  {
    name: "sub",
    requirement: "Recommended",
    detail: "Your stable user id. Raytha matches on it first, within this scheme, so a changed email doesn't create a second account.",
  },
  { name: "given_name", requirement: "Optional", detail: "First name. New accounts without it are named SsoVisitor." },
  { name: "family_name", requirement: "Optional", detail: "Last name. New accounts without it get a random last name." },
  {
    name: "groups",
    requirement: "Optional",
    detail: "A string or an array of user group developer names. See Accounts and groups.",
  },
  {
    name: "jti",
    requirement: "High security",
    detail: "Unique token id. Required and single-use when high security is on, ignored otherwise.",
  },
  { name: "nbf", requirement: "Optional", detail: "Not before. Enforced with no clock skew, so leave it out unless you need it." },
  { name: "iss, aud, iat", requirement: "Ignored", detail: "Raytha does not validate issuer or audience." },
];

export const JWT_EXAMPLE_PAYLOAD = `{
  "sub": "8f14e45f-ceea-467a-9575-7e1d5f2e0c5b",
  "email": "ada@example.com",
  "given_name": "Ada",
  "family_name": "Lovelace",
  "groups": ["editors"],
  "jti": "0d8c3a4e-9a51-4c43-8f7e-6f1b2f0a7c11",
  "exp": 1790000120
}`;

export const JWT_SIGNING_NOTES: readonly string[] = [
  "Sign with HS256 and the scheme's secret. HS384 needs a secret of at least 48 characters and HS512 at least 64. RS256 and other public-key algorithms fail.",
  "Raytha reads the secret as ASCII and pads it with NUL bytes to 32. HMAC pads keys the same way, so Node and Python sign with the secret as is. .NET refuses keys under 32 bytes, so the C# snippet pads it explicitly.",
  "Use ASCII characters only. Raytha turns any other character into ?, so the two sides stop agreeing.",
  "raytha_callback_url arrives in a query string, so anyone can change it. Only redirect to your Raytha origin, or a crafted link hands a valid token to someone else. Each snippet checks this.",
];

export const JWT_SNIPPETS: readonly GuideSnippet[] = [
  {
    id: "node",
    label: "Node",
    code: (origin) => `// npm install jsonwebtoken
const crypto = require("node:crypto");
const jwt = require("jsonwebtoken");

const RAYTHA_ORIGIN = ${JSON.stringify(origin)};

function raythaRedirectUrl(callbackUrl, user) {
  const url = new URL(callbackUrl);
  if (url.origin !== RAYTHA_ORIGIN) {
    throw new Error("raytha_callback_url is not on the Raytha site");
  }
  const token = jwt.sign(
    {
      sub: user.id,
      email: user.email,
      given_name: user.firstName,
      family_name: user.lastName,
      jti: crypto.randomUUID(),
    },
    process.env.RAYTHA_JWT_SECRET,
    { algorithm: "HS256", expiresIn: "2m" },
  );
  url.searchParams.set("token", token);
  return url.toString();
}

// res.redirect(raythaRedirectUrl(req.query.raytha_callback_url, currentUser));
`,
  },
  {
    id: "csharp",
    label: "C#",
    code: (origin) => `// dotnet add package System.IdentityModel.Tokens.Jwt
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public static class RaythaSignIn
{
    const string RaythaOrigin = ${JSON.stringify(origin)};

    public static string RedirectUrl(string callbackUrl, string userId, string email, string firstName, string lastName)
    {
        var callback = new Uri(callbackUrl);
        if (callback.GetLeftPart(UriPartial.Authority) != RaythaOrigin)
            throw new InvalidOperationException("raytha_callback_url is not on the Raytha site");

        var secret = Environment.GetEnvironmentVariable("RAYTHA_JWT_SECRET")!;
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret.PadRight(32, '\\0')));
        var token = new JwtSecurityToken(
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.GivenName, firstName),
                new Claim(JwtRegisteredClaimNames.FamilyName, lastName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            },
            expires: DateTime.UtcNow.AddMinutes(2),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        var separator = string.IsNullOrEmpty(callback.Query) ? "?" : "&";
        return callbackUrl + separator + "token=" + Uri.EscapeDataString(jwt);
    }
}

// return Redirect(RaythaSignIn.RedirectUrl(Request.Query["raytha_callback_url"], user.Id, ...));
`,
  },
  {
    id: "python",
    label: "Python",
    code: (origin) => `# pip install pyjwt
import os
import time
import uuid
from urllib.parse import parse_qsl, urlencode, urlsplit, urlunsplit

import jwt

RAYTHA_ORIGIN = ${JSON.stringify(origin)}


def raytha_redirect_url(callback_url, user):
    parts = urlsplit(callback_url)
    if f"{parts.scheme}://{parts.netloc}" != RAYTHA_ORIGIN:
        raise ValueError("raytha_callback_url is not on the Raytha site")
    now = int(time.time())
    token = jwt.encode(
        {
            "sub": user["id"],
            "email": user["email"],
            "given_name": user["first_name"],
            "family_name": user["last_name"],
            "jti": str(uuid.uuid4()),
            "exp": now + 120,
        },
        os.environ["RAYTHA_JWT_SECRET"],
        algorithm="HS256",
    )
    query = parse_qsl(parts.query) + [("token", token)]
    return urlunsplit(parts._replace(query=urlencode(query)))


# return redirect(raytha_redirect_url(request.args["raytha_callback_url"], current_user))
`,
  },
];

export const JWT_HIGH_SECURITY: readonly string[] = [
  "Every token must carry a jti claim.",
  "After a successful sign-in Raytha records the jti and refuses it from then on, so a token works exactly once.",
  "Tokens travel in the browser's address bar and end up in history and proxy logs. Turn this on in production and generate a fresh random UUID per token.",
];

export const JWT_URL_NOTES: readonly string[] = [
  "Sign in URL: Raytha appends raytha_callback_url and keeps any query string already on your URL.",
  "Sign out URL: Raytha never calls it or redirects to it. Signing out of Raytha ends only the Raytha session; the person stays signed in to your app. Site templates can read it from CurrentOrganization.AuthenticationSchemes to offer a sign-out-everywhere link.",
  "Login button text: the label of this scheme's button on the public login page. The admin sign-in page shows Continue with and the scheme label.",
];

const RATE_LIMITED: GuideProblem = {
  symptom: "429 Too Many Requests",
  fix: "Every sign-in path, these included, shares a per-IP limit of AUTH_RATE_LIMIT_PER_MINUTE requests a minute (default 30). The brute-force lockout settings apply only to email and password.",
};

export const JWT_TROUBLESHOOTING: readonly GuideProblem[] = [
  {
    symptom: "Security token has expired.",
    fix: "exp had passed when Raytha checked it. Raytha allows no clock skew: keep both clocks on NTP and give tokens a couple of minutes.",
  },
  {
    symptom: "Invalid security token.",
    fix: "Wrong secret, a non-HMAC algorithm, HS384 or HS512 with a short secret, no exp claim, an nbf in the future, or a token mangled in transit. URL-encode it as a query value.",
  },
  {
    symptom: "'email' is missing from security token / 'email' is not a valid email address",
    fix: "Send an email claim holding one valid address.",
  },
  {
    symptom: "JWT high security enabled: 'jti' attribute is required in security token.",
    fix: "High security is on and the token has no jti.",
  },
  {
    symptom: "Security token already consumed: …",
    fix: "That jti was used before. Mint a new token with a new jti for every sign-in; reloading a callback URL replays it.",
  },
  {
    symptom: "Authentication scheme disabled for administrators. / … for public users.",
    fix: "The account's type isn't enabled on this scheme. Turn on the matching toggle.",
  },
  { symptom: "User has been deactivated.", fix: "Reactivate the account in Raytha." },
  { symptom: "Authentication scheme is disabled", fix: "Neither Enabled for users nor Enabled for admins is on." },
  {
    symptom: "Admin sign-in shows a bare 403 page",
    fix: "The admin callback hides the reason. Send the same token to the users callback to read it: the checks are identical.",
  },
  {
    symptom: "Signed in, but landed on the home page",
    fix: "returnUrl must be a local path such as /members. Absolute URLs are ignored.",
  },
  RATE_LIMITED,
];

export const SAML_FLOW: readonly GuideStep[] = [
  {
    text: "Someone clicks this scheme's button (the public login page, or Continue with … on the admin sign-in page), or follows a Start sign-in link.",
    values: [
      { label: "Users", value: (values) => values.endpoints.start.users },
      { label: "Admins", value: (values) => values.endpoints.start.admins },
    ],
  },
  {
    text: "Raytha redirects to your Sign in URL with SAMLRequest, and RelayState when there is a return path. The request names the ACS URL for that audience, carries the SP entity ID as its Issuer, and asks for the unspecified NameID format.",
  },
  {
    text: "The IdP signs the person in and posts a signed SAMLResponse, with RelayState, to the ACS URL.",
    values: [
      { label: "Users", value: (values) => values.endpoints.return.users },
      { label: "Admins", value: (values) => values.endpoints.return.admins },
    ],
  },
  {
    text: "Raytha verifies the response, finds or creates the account, and sets its session cookie. It redirects to RelayState when that is a local path, otherwise to the home page (users) or the admin dashboard (admins).",
  },
];

export const SAML_FLOW_NOTE =
  "Raytha does not track request ids, so IdP-initiated sign-in works too: launching the app from the IdP's dashboard posts straight to the ACS URL. Set the IdP's default RelayState to a local path to pick the landing page.";

export const SAML_IDP_VALUES: readonly GuideStep[] = [
  {
    text: "ACS URL (also called Reply URL or Single sign-on URL). Register one for every audience you enable.",
    values: [
      { label: "Users", value: (values) => values.endpoints.return.users },
      { label: "Admins", value: (values) => values.endpoints.return.admins },
    ],
  },
  {
    text: "SP entity ID (Audience URI, Identifier). It must match the Service provider entity ID field exactly, because Raytha sends that value as the Issuer.",
    values: [{ label: "Entity ID", value: (values) => values.spEntityId }],
  },
  {
    text: "NameID: any format works, since Raytha asks for unspecified. Pick a value that never changes for the person, such as a persistent id or an email that stays put. It is required.",
  },
  { text: "Binding: HTTP-POST to the ACS URL." },
  {
    text: "Signing: sign the response, the assertion, or both. Okta, Entra, and Google sign by default. Do not encrypt the assertion.",
  },
];

export const SAML_BRING_BACK: readonly string[] = [
  "The IdP's SAML single sign-on URL goes in Sign in URL.",
  "The IdP's signing certificate goes in SAML certificate.",
];

export const SAML_ATTRIBUTES: readonly GuideRow[] = [
  {
    name: "NameID",
    requirement: "Required",
    detail: "The Subject's NameID. Raytha stores it on the account and matches on it first, within this scheme.",
  },
  {
    name: "email",
    requirement: "Required",
    detail: "Must be a valid address. Finds the account when NameID doesn't, and becomes the account's email on every sign-in.",
  },
  { name: "given_name", requirement: "Optional", detail: "First name. New accounts without it are named SsoVisitor." },
  { name: "family_name", requirement: "Optional", detail: "Last name. New accounts without it get a random last name." },
  {
    name: "groups",
    requirement: "Optional",
    detail: "One AttributeValue per user group developer name. See Accounts and groups.",
  },
];

export const SAML_ATTRIBUTE_NOTE =
  "Raytha matches the attribute Name exactly and ignores NameFormat. A claim URI such as http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress, or Email with a capital E, is not read.";

export const SAML_VERIFICATION: readonly string[] = [
  "The response holds exactly one plain Assertion. An encrypted assertion counts as none.",
  "It carries an XML signature. Raytha checks the first signature in the document against the certificate.",
  "The assertion's NotBefore and NotOnOrAfter hold against the server clock, with no skew allowance.",
  "Raytha does not check Audience, Recipient, Destination, InResponseTo, or the response Issuer.",
];

export const SAML_CERTIFICATE_NOTES: readonly string[] = [
  "Paste the certificate as PEM text: the whole file, from -----BEGIN CERTIFICATE----- through -----END CERTIFICATE-----. The bare base64 body without those lines does not load.",
  "The editor trims surrounding whitespace when you save, because leading spaces also stop the certificate loading.",
  "Okta calls it X.509 Certificate, Entra calls it Certificate (Base64), and Google offers it as Certificate. Each download is already PEM.",
  "When the IdP rotates its signing certificate, paste the new one. Responses signed with the new key fail until you do.",
];

const SP_ENTITY_ID_VALUE = { label: "Entity ID", value: (values: GuideValues) => values.spEntityId };
const USERS_ACS_VALUE = { label: "Users ACS", value: (values: GuideValues) => values.endpoints.return.users };
const ADMINS_ACS_VALUE = { label: "Admins ACS", value: (values: GuideValues) => values.endpoints.return.admins };

export const SAML_IDP_RECIPES: readonly IdpRecipe[] = [
  {
    id: "okta",
    name: "Okta",
    steps: [
      { text: "Admin Console → Applications → Applications → Create App Integration → SAML 2.0." },
      {
        text: "Single sign-on URL: the ACS URL. Keep Use this for Recipient URL and Destination URL checked. To enable both audiences, add the other ACS URL under Show Advanced Settings → Other Requestable SSO URLs.",
        values: [USERS_ACS_VALUE, ADMINS_ACS_VALUE],
      },
      { text: "Audience URI (SP Entity ID): the SP entity ID.", values: [SP_ENTITY_ID_VALUE] },
      { text: "Name ID format: Unspecified or EmailAddress. Application username: Okta username or Email." },
      {
        text: "Attribute Statements: email = user.email, given_name = user.firstName, family_name = user.lastName.",
      },
      {
        text: "Optional Group Attribute Statement: name groups, with a filter that selects Okta groups named exactly like Raytha user group developer names.",
      },
      { text: "Leave Response and Assertion Signature on Signed and Assertion Encryption on Unencrypted." },
      {
        text: "On the app's Sign On tab, open View SAML setup instructions. Identity Provider Single Sign-On URL goes in Sign in URL; X.509 Certificate goes in SAML certificate.",
      },
      { text: "Assign people or groups to the app on the Assignments tab." },
    ],
  },
  {
    id: "entra",
    name: "Microsoft Entra ID",
    steps: [
      {
        text: "Entra admin center → Enterprise applications → New application → Create your own application → Integrate any other application you don't find in the gallery.",
      },
      { text: "Single sign-on → SAML → Basic SAML Configuration → Edit." },
      { text: "Identifier (Entity ID): the SP entity ID.", values: [SP_ENTITY_ID_VALUE] },
      {
        text: "Reply URL (Assertion Consumer Service URL): the ACS URL. Add both when you enable both audiences.",
        values: [USERS_ACS_VALUE, ADMINS_ACS_VALUE],
      },
      {
        text: "Attributes & Claims → Add new claim, each with an empty Namespace: email = user.mail, given_name = user.givenname, family_name = user.surname. The default claims use long URIs that Raytha does not read.",
      },
      {
        text: "Optional groups: Entra sends group object ids, which never match a developer name. Instead add a claim named groups with source user.assignedroles, and give the app roles values equal to Raytha user group developer names.",
      },
      {
        text: "SAML Certificates: download Certificate (Base64) and paste the whole file into SAML certificate. Set up …: copy Login URL into Sign in URL.",
      },
      { text: "Users and groups: assign who may sign in." },
    ],
    note: "AADSTS75011 means Raytha's request asked for password authentication and the person's Entra session used another method (MFA, certificate, passwordless). Launching the app from My Apps is IdP-initiated, sends no request, and avoids it.",
  },
  {
    id: "google",
    name: "Google Workspace",
    steps: [
      { text: "Admin console → Apps → Web and mobile apps → Add app → Add custom SAML app." },
      {
        text: "Google Identity Provider details: SSO URL goes in Sign in URL; download Certificate and paste it into SAML certificate.",
      },
      {
        text: "Service provider details, ACS URL: Google accepts one ACS URL per app. To serve both audiences, create two Raytha schemes, each with its own Google app and entity ID.",
        values: [USERS_ACS_VALUE, ADMINS_ACS_VALUE],
      },
      { text: "Entity ID: the SP entity ID.", values: [SP_ENTITY_ID_VALUE] },
      { text: "Name ID format: EMAIL or UNSPECIFIED. Name ID: Basic Information › Primary email." },
      {
        text: "Attribute mapping: Primary email → email, First name → given_name, Last name → family_name. Optional Group membership → groups, with Google groups named like Raytha user group developer names.",
      },
      { text: "User access: turn the app on for the organizational units or groups that should sign in." },
    ],
  },
];

export const SAML_TROUBLESHOOTING: readonly GuideProblem[] = [
  {
    symptom: "Failed authentication.",
    fix: "The signature didn't verify with the pasted certificate (wrong or rotated), the response is unsigned, it holds an encrypted or second assertion, or it is outside NotBefore/NotOnOrAfter. Check the server clock.",
  },
  {
    symptom: "Missing 'email' attribute from saml assertion payload.",
    fix: "Add an attribute whose Name is exactly email.",
  },
  { symptom: "'email' is not a valid email address", fix: "The email attribute must hold one valid address." },
  {
    symptom: "An error page instead of a message",
    fix: "The certificate text can't be parsed, or the assertion has no NameID. Paste the full PEM and send a NameID.",
  },
  {
    symptom: "The IdP can't find the application (Entra AADSTS700016)",
    fix: "The Service provider entity ID field doesn't match the IdP's Entity ID, Identifier, or Audience URI.",
  },
  {
    symptom: "The IdP rejects the reply or ACS URL (Entra AADSTS50011)",
    fix: "Register the exact ACS URLs above. Raytha builds the admin one from the Website URL in Configuration and the user one from the address the browser used.",
  },
  {
    symptom: "The IdP says the request is malformed",
    fix: "Raytha base64-encodes its request without DEFLATE compression. If your IdP insists on compressed redirects, set Sign in URL to the IdP's app launch URL. That sign-in is IdP-initiated, which Raytha accepts.",
  },
  {
    symptom: "Authentication scheme disabled for administrators. / … for public users.",
    fix: "The account's type isn't enabled on this scheme. Turn on the matching toggle.",
  },
  { symptom: "User has been deactivated.", fix: "Reactivate the account in Raytha." },
  {
    symptom: "Admin sign-in shows a bare 403 page",
    fix: "The admin ACS hides the reason. Sign in through the users Start sign-in link to read it: the checks are identical. That needs Enabled for users on for the test.",
  },
  {
    symptom: "Groups don't change",
    fix: "Values must equal user group developer names exactly. Entra sends object ids unless you map app roles.",
  },
  RATE_LIMITED,
];
