using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raytha.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class v2_4_0 : Migration
    {
        // Frozen copies of the 2.0 magic-link templates; the default template files may change later,
        // this migration must not.
        private const string MagicLinkEmailContent = """
            <p>Hello {{ Target.FirstName }},</p>

            <p>Use this one-time code to login to the {{ CurrentOrganization.OrganizationName }} website.</p>

            <p style="font-size: 28px; font-weight: bold; letter-spacing: 6px;">{{ Target.Code }}</p>

            <p>The code expires after {{ Target.MagicLinkExpiresInSeconds }} seconds. If you did not request it, you can ignore this email.</p>

            <p>Thank you,<br/>
              {{ CurrentOrganization.OrganizationName }}</p>
            """;

        private const string MagicLinkSentPageContent = """
            <h3>Enter your code</h3>
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
            </form>
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 2.0 replaced the magic-link URL with a one-time code, but 1.5 databases still hold the
            // old email (which renders an empty link) and a sent page with nowhere to enter the code.
            // Any copy without the code is broken, customized or not, so replace it and keep the old
            // content as a revision the admin can restore from.
            migrationBuilder.Sql(
                """
                INSERT INTO "EmailTemplateRevisions" ("Id", "Subject", "Content", "Cc", "Bcc", "EmailTemplateId", "CreationTime")
                SELECT gen_random_uuid(), "Subject", "Content", "Cc", "Bcc", "Id", now()
                FROM "EmailTemplates"
                WHERE "DeveloperName" = 'raytha_email_login_beginloginwithmagiclink'
                  AND strpos(coalesce("Content", ''), 'Target.Code') = 0;

                UPDATE "EmailTemplates" SET "Content" = @content, "LastModificationTime" = now()
                WHERE "DeveloperName" = 'raytha_email_login_beginloginwithmagiclink'
                  AND strpos(coalesce("Content", ''), 'Target.Code') = 0;
                """.Replace("@content", SqlLiteral(MagicLinkEmailContent))
            );

            migrationBuilder.Sql(
                """
                INSERT INTO "WebTemplateRevisions" ("Id", "Label", "Content", "WebTemplateId", "AllowAccessForNewContentTypes", "CreationTime")
                SELECT gen_random_uuid(), "Label", "Content", "Id", "AllowAccessForNewContentTypes", now()
                FROM "WebTemplates"
                WHERE "DeveloperName" = 'raytha_html_login_magiclinksent'
                  AND strpos(coalesce("Content", ''), 'magic-link/complete') = 0;

                UPDATE "WebTemplates" SET "Content" = @content, "LastModificationTime" = now()
                WHERE "DeveloperName" = 'raytha_html_login_magiclinksent'
                  AND strpos(coalesce("Content", ''), 'magic-link/complete') = 0;
                """.Replace("@content", SqlLiteral(MagicLinkSentPageContent))
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) { }

        private static string SqlLiteral(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
