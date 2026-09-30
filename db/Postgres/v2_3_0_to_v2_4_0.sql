START TRANSACTION;
INSERT INTO "EmailTemplateRevisions" ("Id", "Subject", "Content", "Cc", "Bcc", "EmailTemplateId", "CreationTime")
SELECT gen_random_uuid(), "Subject", "Content", "Cc", "Bcc", "Id", now()
FROM "EmailTemplates"
WHERE "DeveloperName" = 'raytha_email_login_beginloginwithmagiclink'
  AND strpos(coalesce("Content", ''), 'Target.Code') = 0;

UPDATE "EmailTemplates" SET "Content" = '<p>Hello {{ Target.FirstName }},</p>

<p>Use this one-time code to login to the {{ CurrentOrganization.OrganizationName }} website.</p>

<p style="font-size: 28px; font-weight: bold; letter-spacing: 6px;">{{ Target.Code }}</p>

<p>The code expires after {{ Target.MagicLinkExpiresInSeconds }} seconds. If you did not request it, you can ignore this email.</p>

<p>Thank you,<br/>
  {{ CurrentOrganization.OrganizationName }}</p>', "LastModificationTime" = now()
WHERE "DeveloperName" = 'raytha_email_login_beginloginwithmagiclink'
  AND strpos(coalesce("Content", ''), 'Target.Code') = 0;

INSERT INTO "WebTemplateRevisions" ("Id", "Label", "Content", "WebTemplateId", "AllowAccessForNewContentTypes", "CreationTime")
SELECT gen_random_uuid(), "Label", "Content", "Id", "AllowAccessForNewContentTypes", now()
FROM "WebTemplates"
WHERE "DeveloperName" = 'raytha_html_login_magiclinksent'
  AND strpos(coalesce("Content", ''), 'magic-link/complete') = 0;

UPDATE "WebTemplates" SET "Content" = '<h3>Enter your code</h3>
<p>We emailed you a one-time code. Enter it below to login.</p>
{% if Target.ValidationFailures["__ValidationSummary"] %}
  <div class="alert alert-danger">
    {{ Target.ValidationFailures["__ValidationSummary"] }}
  </div>
{% endif %}
<form
  action="{{ PathBase }}/account/login/magic-link/complete?returnUrl={{ Target.ReturnUrl }}"
  method="post">
  <div class="mb-3">
    <label for="email" class="form-label">Email address</label>
    <input
      id="email"
      type="email"
      name="EmailAddress"
      value="{{ Target.EmailAddress }}"
      class="form-control">
  </div>
  <div class="mb-3">
    <label for="code" class="form-label">One-time code</label>
    <input
      id="code"
      type="text"
      name="Code"
      inputmode="numeric"
      autocomplete="one-time-code"
      class="form-control">
  </div>
  <div>
    <button type="submit" class="btn btn-primary w-100">Login</button>
  </div>
  <input
    name="__RequestVerificationToken"
    type="hidden"
    value="{{ Target.RequestVerificationToken }}" />
</form>', "LastModificationTime" = now()
WHERE "DeveloperName" = 'raytha_html_login_magiclinksent'
  AND strpos(coalesce("Content", ''), 'magic-link/complete') = 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260930140823_v2_4_0', '10.0.11');

COMMIT;

