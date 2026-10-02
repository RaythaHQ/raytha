# Users

An admin manages website member accounts: finds them in a searchable list,
creates one, edits name, email, and groups, suspends or restores access, resets
a password, and deletes the account from its editor's danger zone.

## Sub-features

- `users-list` shows users with name, email, status, last login; search is in the URL.
- `users-create` creates a user from `/raytha/users/new` and opens its editor.
- `users-edit` saves name, email, and user groups.
- `users-suspend` suspends and restores sign-in (`Account access` card).
- `users-reset-password` sets a new password, optionally emailing it.
- `users-delete` deletes from the `Danger zone`, behind a confirm dialog.
- `users-groups` lists and edits user groups under the `User groups` tab.

## How to get to it (user POV)

- Sidebar `People` → `Users` → `/raytha/users`.
- `New user` button on the list → `/raytha/users/new`.
- Name cell in the list → `/raytha/users/<id>`.
- `Search or jump to…` (Ctrl+K) palette.
- Headless: `/raytha/api/admin/users` (needs `MANAGE_USERS`).

## Driving it with browse.sh and curl

Preconditions:

- Baseline from `README.md`; signed in as a super admin.

- **Before.** Record the list. Run
  `curl -s -b runs/cookies-15200.txt "http://127.0.0.1:15200/raytha/api/admin/users?pageSize=50" > evidence/<change>/users-before.json`.
  `totalCount` is the baseline.
- **Create in the UI.** Run `scripts/browse.sh --out evidence/<change> --steps evidence/<change>/create-user.cjs`
  with this module:

  ```js
  module.exports = async ({ page, base, shot, log }) => {
    const email = `ada.${Date.now()}@raytha.local`;
    await page.goto(`${base}/raytha/users`, { waitUntil: "networkidle" });
    await shot("users-before");
    await page.getByRole("link", { name: "New user" }).click();
    await page.waitForURL("**/raytha/users/new");
    await page.getByLabel(/^first name/i).fill("Ada");
    await page.getByLabel(/^last name/i).fill("Lovelace");
    await page.getByLabel(/^email/i).fill(email);
    await shot("new-user-filled");
    await page.getByRole("button", { name: "Create" }).click();
    await page.waitForURL(/\/raytha\/users\/(?!new)[^/]+$/);
    await page.getByText("User created").waitFor();
    await page.getByRole("heading", { name: "Ada Lovelace" }).waitFor();
    await shot("user-created");
    log(`created ${email} at ${page.url().replace(base, "")}`);
    await page.goto(`${base}/raytha/users?search=${encodeURIComponent(email)}`, { waitUntil: "networkidle" });
    await page.getByRole("cell", { name: email }).waitFor();
    await shot("users-after-search");
  };
  ```

  The editor opens at `/raytha/users/<id>` with h1 `Ada Lovelace`, a `User
  created` toast, and cards `Details`, `Account access`, `Impersonate`,
  `Reset password`, `Danger zone`. The searched list shows one row.
- **After.** Re-read the API list: `totalCount` is one higher and the new email
  is present. Confirm the row:
  `docker compose -f tools/compose.yaml exec -T postgres psql -U postgres -d raytha_verify -Atc 'select "EmailAddress","IsAdmin" from "Users" order by "CreationTime"'`
  shows the email with `IsAdmin = f`.
- **Suspend.** On the editor, click `Suspend` in `Account access` (confirm if a
  dialog asks). The header reads `Suspended`, the button becomes `Restore`, and
  `Reset password` says `Restore this account before resetting the password.`
  `GET /raytha/api/admin/users/<id>` returns `isActive: false`.
- **API create.** `POST /raytha/api/admin/users` with
  `{"firstName","lastName","emailAddress","sendEmail":false,"userGroups":[]}`
  answers 201 `{ "id": "<ShortGuid>" }`.

## Gotchas

- Required labels render as `First name *`; match `/^first name/i`.
- Right after create, the editor shows skeletons for a moment. Wait for the
  `Ada Lovelace` heading before asserting or shooting (`shot()` waits for
  skeletons to clear on its own).
- `Send welcome email` is unchecked by default. When checked, the mail lands in
  the shared MailHog; filter by your unique address.
- Users and admins are separate lists. Admins live at `/raytha/settings/admins`
  and `/raytha/api/admin/admins`.
- Delete is only in the editor's `Danger zone` (RFC-0013); there is no row
  delete in the list.
