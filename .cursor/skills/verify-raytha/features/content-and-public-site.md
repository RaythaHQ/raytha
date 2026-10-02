# Content items and the public site

An editor creates a content item of a content type (a post), saves it as a
draft or publishes it, and the published item renders on the public site at
its route through the content type's Liquid template.

## Sub-features

- `content-list` lists items of a type, with views, search, and paging in the URL.
- `content-create` creates an item from `/raytha/content/<type>/new`.
- `content-publish` publishes; `content-draft` saves a draft without changing the live version.
- `content-unpublish`, `content-discard-draft`, `content-revisions` (revert).
- `content-trash` deletes to trash; restore or delete permanently from trash.
- `public-render` serves the published item at its `routePath` on the public site.
- `content-types` creates types and fields at `/raytha/content-types`.

## How to get to it (user POV)

- Sidebar `Content` → `Posts` → `/raytha/content/posts`.
- `/raytha/content/posts/new` from the list's create button.
- Item editor at `/raytha/content/posts/items/<id>` with `Save draft` and
  `Publish`.
- Public URL `/<routePath>`, e.g. `/2026/10/Probe-post-1790870728`.
- Headless: `/raytha/api/admin/content-types/posts/items`.

## Driving it with browse.sh and curl

Preconditions:

- Baseline from `README.md`. Get the detail template id:
  `curl -s -b runs/cookies-15200.txt http://127.0.0.1:15200/raytha/api/admin/content-types/posts/templates`
  → the item with developerName `raytha_html_content_item_detail`.

- **Create and publish in the UI.** Steps module:

  ```js
  module.exports = async ({ page, base, shot, log }) => {
    const title = `UI post ${Date.now()}`;
    await page.goto(`${base}/raytha/content/posts/new`, { waitUntil: "networkidle" });
    await page.getByLabel(/^title/i).fill(title);
    await page.getByRole("button", { name: "Publish" }).click();
    await page.getByText("Saved", { exact: true }).waitFor();
    await page.waitForURL(/\/raytha\/content\/posts\/items\/[^/]+$/);
    await page.getByText("Published", { exact: true }).first().waitFor();
    await shot("post-published");
    log(`published ${title} at ${page.url().replace(base, "")}`);
  };
  ```

  The new page has h1 `New Post`; after publishing, the URL is
  `/raytha/content/posts/items/<id>`, the h1 is the title, and a `Published`
  badge shows.
- **Create via API.** `POST /raytha/api/admin/content-types/posts/items` with
  `{"saveAsDraft":false,"templateId":"<detail id>","content":{"title":"Probe post <ts>","content":"<p>probe body</p>"}}`
  answers 201 `{ "id": … }`.
- **Read back.** `GET /raytha/api/admin/content-types/posts/items/<id>` shows
  `isPublished: true` and `routePath` like `2026/10/Probe-post-<ts>`.
- **Public render.** `curl -s http://127.0.0.1:15200/<routePath>` (no cookies)
  answers 200 `text/html` containing the title. Save it as
  `evidence/<change>/public.html`, or screenshot it with `browse.sh`.
- **Draft does not leak.** Edit the title and click `Save draft`. The public
  URL still shows the old title; the admin item has `isDraft: true`. Publish
  and the public page changes.

## Gotchas

- The route comes from the type's route template (`{CurrentYear}/{CurrentMonth}/{PrimaryField}`
  for posts), so it changes with the date and title. Read `routePath` back; do
  not construct it.
- Public requests must be anonymous to prove what visitors see; drop the jar.
- A fresh database's only type is `posts`. Create other types through
  `/raytha/content-types/new` or `POST /raytha/api/admin/content-types`, or use
  the seeded database (`SKILL.md` → Seeded data).
- Content field keys are field developer names (`title`, `content`), not labels.
- Attachments store relative media URLs; REST API v1 returns them absolute.
