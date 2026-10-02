#!/usr/bin/env python3
"""Build out a demo Raytha site through the admin API.

Creates content types that cover every field type, a few hundred content items in
mixed states (published, draft, unpublished, trashed, revised), views, media,
users and groups, admins and roles, authentication schemes, functions, a custom
web template, site pages with widgets, navigation menus, webhooks, and a second
theme. Everything goes through /raytha/api, so the data is exactly what the admin
SPA would have written.

Run it against a fresh database:

    python3 tools/seed.py --base-url http://localhost:5200 \\
        --email you@example.com --password 'your-password' --setup

--setup runs first-run setup with those credentials when the database has no
admin yet; without it the script signs in. Seeded admins and users get the same
password, so you can sign in as any of them. Standard library only.
"""

from __future__ import annotations

import argparse
import datetime as dt
import http.cookiejar
import json
import random
import secrets
import struct
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid
import zlib

SEED_MARKER_TYPE = "authors"


class SeedError(Exception):
    pass


# --------------------------------------------------------------------------- HTTP


class Api:
    def __init__(self, base_url: str):
        self.base = base_url.rstrip("/")
        self.jar = http.cookiejar.CookieJar()
        self.opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(self.jar))
        self.calls = 0

    def request(self, method: str, path: str, body=None, raw: bytes | None = None, content_type: str | None = None):
        url = self.base + path
        data = raw
        if body is not None:
            data = json.dumps(body).encode()
            content_type = "application/json"
        elif method in ("POST", "PUT", "PATCH") and raw is None:
            data = b"{}"
            content_type = "application/json"
        req = urllib.request.Request(url, data=data, method=method)
        if content_type:
            req.add_header("Content-Type", content_type)
        req.add_header("Accept", "application/json")
        self.calls += 1
        for attempt in range(5):
            try:
                with self.opener.open(req, timeout=120) as resp:
                    text = resp.read().decode()
                    return json.loads(text) if text else None
            except urllib.error.HTTPError as e:
                text = e.read().decode(errors="replace")
                if e.code == 429 and attempt < 4:
                    time.sleep(int(e.headers.get("Retry-After", "5")))
                    continue
                raise SeedError(f"{method} {path} -> {e.code}: {problem_text(text)}") from None
        raise SeedError(f"{method} {path} -> rate limited")

    def get(self, path):
        return self.request("GET", path)

    def post(self, path, body=None):
        return self.request("POST", path, body)

    def put(self, path, body=None):
        return self.request("PUT", path, body)

    def delete(self, path):
        return self.request("DELETE", path)

    def upload(self, filename: str, mime: str, data: bytes) -> dict:
        boundary = "----raytha-seed-" + secrets.token_hex(8)
        head = (
            f"--{boundary}\r\n"
            f'Content-Disposition: form-data; name="file"; filename="{filename}"\r\n'
            f"Content-Type: {mime}\r\n\r\n"
        ).encode()
        tail = f"\r\n--{boundary}--\r\n".encode()
        result = self.request(
            "POST",
            "/raytha/media-items/upload",
            raw=head + data + tail,
            content_type=f"multipart/form-data; boundary={boundary}",
        )
        if not result or not result.get("success"):
            raise SeedError(f"upload {filename} failed: {result}")
        return result


def problem_text(text: str) -> str:
    try:
        problem = json.loads(text)
    except ValueError:
        return text[:300]
    errors = problem.get("errors") or {}
    messages = [m for values in errors.values() for m in (values if isinstance(values, list) else [values])]
    return "; ".join(messages) or problem.get("detail") or problem.get("title") or text[:300]


def items_of(value):
    return value["items"] if isinstance(value, dict) and "items" in value else value


# --------------------------------------------------------------------------- generated files


def png(width: int, height: int, top: tuple, bottom: tuple, band: tuple) -> bytes:
    rows = bytearray()
    band_start, band_end = int(height * 0.62), int(height * 0.70)
    for y in range(height):
        t = y / max(height - 1, 1)
        if band_start <= y < band_end:
            color = band
        else:
            color = tuple(int(a + (b - a) * t) for a, b in zip(top, bottom))
        rows.append(0)
        rows.extend(bytes(color) * width)

    def chunk(kind: bytes, payload: bytes) -> bytes:
        return struct.pack(">I", len(payload)) + kind + payload + struct.pack(">I", zlib.crc32(kind + payload))

    header = struct.pack(">IIBBBBB", width, height, 8, 2, 0, 0, 0)
    return b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", header) + chunk(b"IDAT", zlib.compress(bytes(rows), 9)) + chunk(b"IEND", b"")


def pdf(title: str, lines: list[str]) -> bytes:
    text = ["BT /F1 20 Tf 72 740 Td (" + pdf_escape(title) + ") Tj ET"]
    y = 700
    for line in lines:
        text.append(f"BT /F1 12 Tf 72 {y} Td (" + pdf_escape(line) + ") Tj ET")
        y -= 18
    stream = "\n".join(text).encode()
    objects = [
        b"<< /Type /Catalog /Pages 2 0 R >>",
        b"<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
        b"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
        b"<< /Length " + str(len(stream)).encode() + b" >>\nstream\n" + stream + b"\nendstream",
        b"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
    ]
    out = bytearray(b"%PDF-1.4\n")
    offsets = []
    for number, body in enumerate(objects, start=1):
        offsets.append(len(out))
        out += f"{number} 0 obj\n".encode() + body + b"\nendobj\n"
    xref = len(out)
    out += f"xref\n0 {len(objects) + 1}\n0000000000 65535 f \n".encode()
    for offset in offsets:
        out += f"{offset:010d} 00000 n \n".encode()
    out += f"trailer\n<< /Size {len(objects) + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n".encode()
    return bytes(out)


def pdf_escape(value: str) -> str:
    return value.replace("\\", "\\\\").replace("(", "\\(").replace(")", "\\)")


PALETTES = [
    ((13, 110, 253), (111, 66, 193), (255, 193, 7)),
    ((25, 135, 84), (32, 201, 151), (255, 255, 255)),
    ((220, 53, 69), (253, 126, 20), (33, 37, 41)),
    ((33, 37, 41), (108, 117, 125), (13, 202, 240)),
    ((102, 16, 242), (214, 51, 132), (255, 243, 205)),
    ((13, 202, 240), (13, 110, 253), (25, 135, 84)),
    ((255, 193, 7), (253, 126, 20), (13, 110, 253)),
    ((52, 58, 64), (13, 110, 253), (248, 249, 250)),
]


# --------------------------------------------------------------------------- text


ADJECTIVES = "quiet bold practical modern hidden honest curious simple rapid gentle clever vivid durable open patient steady".split()
NOUNS = "gardens pipelines kitchens cities rivers libraries workshops harbors markets studios bridges orchards forests archives engines".split()
TOPICS = "design engineering community travel food science culture history music education health climate".split()
VERBS = "rethinking building mapping restoring measuring teaching scaling sketching funding documenting".split()
FIRST_NAMES = "Ada Grace Linus Margaret Alan Katherine Tim Hedy Dennis Frances Ken Barbara Guido Radia Donald Anita Edsger Shafi Mary Whitfield Sophie Rosalind Niklaus Evelyn Carlos Mei Aarav Zainab Mateo Ingrid Kwame Yuki Olga Tariq".split()
LAST_NAMES = "Lovelace Hopper Torvalds Hamilton Turing Johnson Berners-Lee Lamarr Ritchie Allen Thompson Liskov Rossum Perlman Knuth Borg Dijkstra Goldwasser Kenneth Diffie Wilson Franklin Wirth Boyd Mendes Chen Patel Okafor Garcia Larsen Mensah Tanaka Petrova Haddad".split()
COMPANIES = [
    "Northwind Traders", "Contoso Ltd", "Fabrikam Inc", "Tailspin Toys", "Wingtip Cycles", "Litware", "Adventure Works",
    "Proseware", "Lucerne Publishing", "Margie's Travel", "Café Déjà Vu", "Blue Yonder Airlines",
]
SENTENCES = [
    "Small teams ship faster when the feedback loop is measured in minutes, not weeks.",
    "The first version was rough, but it answered the question that mattered.",
    "We kept a written log of every decision, which paid off during the migration.",
    "Nobody expected the old spreadsheet to be the most reliable part of the system.",
    "A clear naming convention saved more time than any framework we tried.",
    "Customers rarely asked for features; they asked for fewer surprises.",
    "The workshop ran long because everyone wanted to try the prototype.",
    "Most of the cost was hidden in the handoffs between departments.",
    "Good defaults made the advanced settings almost unnecessary.",
    "It took three drafts to make the instructions short enough to follow.",
    "We moved the heavy work to the night shift and the dashboards got quiet.",
    "Every photo in this story was taken on an ordinary phone.",
]


def title_case(words):
    return " ".join(w[:1].upper() + w[1:] for w in words)


def paragraph(rng, count=3):
    return " ".join(rng.sample(SENTENCES, count))


def rich_text(rng, heading: str) -> str:
    items = "".join(f"<li>{s}</li>" for s in rng.sample(SENTENCES, 3))
    return (
        f"<h2>{heading}</h2>"
        f"<p>{paragraph(rng)}</p>"
        f"<blockquote><p>{rng.choice(SENTENCES)}</p></blockquote>"
        f"<ul>{items}</ul>"
        f"<p>Read more on <a href=\"https://raytha.com\">raytha.com</a>. {paragraph(rng, 2)}</p>"
    )


def iso_date(day: dt.date) -> str:
    return day.isoformat()


def slug(value: str) -> str:
    return "".join(c if c.isalnum() else "_" for c in value.lower()).strip("_")


# --------------------------------------------------------------------------- field specs


def choice(label, developer_name=None):
    return {"label": label, "developerName": developer_name or slug(label), "disabled": False}


def field(developer_name, field_type, label, required=False, description="", choices=None, related=None, sub_fields=None):
    return {
        "developerName": developer_name,
        "fieldType": field_type,
        "label": label,
        "isRequired": required,
        "description": description,
        "choices": [choice(c) if isinstance(c, str) else c for c in (choices or [])],
        "related": related,
        "subFields": sub_fields or [],
    }


def sub(developer_name, field_type, label, required=False, choices=None):
    return {
        "developerName": developer_name,
        "label": label,
        "fieldType": field_type,
        "description": None,
        "isRequired": required,
        "defaultValue": None,
        "choices": [choice(c) for c in (choices or [])],
        "subFields": [],
    }


EXPERTISE = ["Design", "Engineering", "Research", "Operations", "Writing", "Photography"]
TAGS = ["How-to", "Opinion", "Case study", "Release notes", "Behind the scenes", "Interview", "Data"]
TOPIC_CHOICES = ["Accessibility", "Performance", "Security", "Content strategy", "Community", "Tooling"]
COLORS = ["Red", "Blue", "Green", "Black", "White", "Sand"]

CONTENT_TYPES = [
    {
        "developerName": "categories",
        "singular": "Category",
        "plural": "Categories",
        "route": "topics/{PrimaryField}",
        "description": "Editorial categories that posts belong to.",
        "primaryLabel": "Name",
        "contentLabel": "Description",
        "fields": [
            field("accent", "color", "Accent color"),
            field("sort_order", "number", "Sort order", description="Lower numbers come first."),
            field("is_visible", "checkbox", "Show in navigation"),
        ],
    },
    {
        "developerName": "authors",
        "singular": "Author",
        "plural": "Authors",
        "route": "authors/{PrimaryField}",
        "description": "People who write for the site.",
        "primaryLabel": "Name",
        "contentLabel": "Biography",
        "fields": [
            field("role", "single_line_text", "Role"),
            field("email", "single_line_text", "Email"),
            field("featured_image", "attachment", "Headshot"),
            field("accent", "color", "Profile color"),
            field("is_staff", "checkbox", "Staff writer"),
            field("joined_on", "date", "Joined"),
            field("expertise", "multiple_select", "Expertise", choices=EXPERTISE),
            field("seniority", "radio", "Seniority", choices=["Junior", "Mid", "Senior", "Principal"]),
            field(
                "social_links",
                "repeater",
                "Social links",
                sub_fields=[
                    sub("network", "dropdown", "Network", choices=["Mastodon", "GitHub", "LinkedIn", "Website"]),
                    sub("url", "single_line_text", "URL", required=True),
                ],
            ),
        ],
    },
    {
        "developerName": "posts",
        "existing": True,
        "fields": [
            field("excerpt", "long_text", "Excerpt"),
            field("author", "one_to_one_relationship", "Author", related="authors"),
            field("category", "one_to_one_relationship", "Category", related="categories"),
            field("format", "dropdown", "Format", choices=["Article", "Interview", "Tutorial", "News", "Review"]),
            field("audience", "radio", "Audience", choices=["Beginner", "Intermediate", "Advanced"]),
            field("tags", "multiple_select", "Tags", choices=TAGS),
            field("is_featured", "checkbox", "Featured"),
            field("published_on", "date", "Published on"),
            field("reading_minutes", "number", "Reading time (minutes)"),
            field("featured_image", "attachment", "Featured image"),
            field("accent", "color", "Accent color"),
            field(
                "sources",
                "repeater",
                "Sources",
                sub_fields=[
                    sub("label", "single_line_text", "Label", required=True),
                    sub("url", "single_line_text", "URL"),
                    sub("accessed_on", "date", "Accessed on"),
                    sub("is_primary", "checkbox", "Primary source"),
                ],
            ),
        ],
    },
    {
        "developerName": "venues",
        "singular": "Venue",
        "plural": "Venues",
        "route": "venues/{PrimaryField}",
        "description": "Places where events happen.",
        "primaryLabel": "Name",
        "contentLabel": "About the venue",
        "fields": [
            field("address", "long_text", "Address"),
            field("city", "dropdown", "City", choices=["Chicago", "Lisbon", "Nairobi", "Osaka", "Toronto", "Warsaw"]),
            field("capacity", "number", "Capacity"),
            field("is_accessible", "checkbox", "Step-free access"),
            field("map_url", "single_line_text", "Map link"),
            field("featured_image", "attachment", "Photo"),
        ],
    },
    {
        "developerName": "events",
        "singular": "Event",
        "plural": "Events",
        "route": "events/{PrimaryField}",
        "description": "Conferences, workshops, and meetups.",
        "primaryLabel": "Event name",
        "contentLabel": "Details",
        "fields": [
            field("summary", "long_text", "Summary", required=True),
            field("starts_on", "date", "Starts on", required=True),
            field("ends_on", "date", "Ends on"),
            field("venue", "one_to_one_relationship", "Venue", related="venues"),
            field("event_type", "radio", "Event type", choices=["Conference", "Workshop", "Meetup", "Webinar"]),
            field("topics", "multiple_select", "Topics", choices=TOPIC_CHOICES),
            field("is_virtual", "checkbox", "Virtual"),
            field("capacity", "number", "Capacity"),
            field("ticket_price", "number", "Ticket price"),
            field("featured_image", "attachment", "Banner"),
            field("brand_color", "color", "Brand color"),
            field(
                "schedule",
                "repeater",
                "Schedule",
                sub_fields=[
                    sub("time", "single_line_text", "Time", required=True),
                    sub("session", "single_line_text", "Session", required=True),
                    sub("speaker", "single_line_text", "Speaker"),
                    sub("minutes", "number", "Length (minutes)"),
                    sub("is_keynote", "checkbox", "Keynote"),
                ],
            ),
        ],
    },
    {
        "developerName": "products",
        "singular": "Product",
        "plural": "Products",
        "route": "shop/{PrimaryField}",
        "description": "Items in the shop.",
        "primaryLabel": "Product name",
        "contentLabel": "Description",
        "fields": [
            field("sku", "single_line_text", "SKU", required=True),
            field("price", "number", "Price", required=True),
            field("stock", "number", "Units in stock"),
            field("department", "dropdown", "Department", choices=["Apparel", "Home", "Outdoor", "Stationery", "Tech"]),
            field("size", "radio", "Size", choices=["S", "M", "L", "XL"]),
            field("colors", "multiple_select", "Available colors", choices=COLORS),
            field("on_sale", "checkbox", "On sale"),
            field("launch_date", "date", "Launch date"),
            field("swatch", "color", "Swatch"),
            field("featured_image", "attachment", "Product photo"),
            field("manual", "attachment", "Manual (PDF)"),
            field(
                "specs",
                "repeater",
                "Specifications",
                sub_fields=[
                    sub("name", "single_line_text", "Name", required=True),
                    sub("value", "single_line_text", "Value"),
                    sub("accent", "color", "Highlight"),
                ],
            ),
        ],
    },
    {
        "developerName": "testimonials",
        "singular": "Testimonial",
        "plural": "Testimonials",
        "route": "testimonials/{PrimaryField}",
        "description": "What customers say.",
        "primaryLabel": "Headline",
        "contentLabel": "Full story",
        "fields": [
            field("quote", "long_text", "Quote", required=True),
            field("person", "single_line_text", "Person"),
            field("company", "single_line_text", "Company"),
            field("rating", "number", "Rating (1-5)"),
            field("product", "one_to_one_relationship", "Product", related="products"),
            field("featured_image", "attachment", "Photo"),
        ],
    },
    {
        "developerName": "faqs",
        "singular": "FAQ",
        "plural": "FAQs",
        "route": "faq/{PrimaryField}",
        "description": "Frequently asked questions.",
        "primaryLabel": "Question",
        "contentLabel": "Answer",
        "fields": [
            field("topic", "dropdown", "Topic", choices=["Accounts", "Billing", "Events", "Shipping", "Privacy"]),
            field("sort_order", "number", "Sort order"),
            field("is_pinned", "checkbox", "Pinned"),
        ],
    },
]


# --------------------------------------------------------------------------- seeder


class Seeder:
    def __init__(self, api: Api, rng: random.Random, scale: float, account_password: str, log):
        self.api = api
        self.rng = rng
        self.scale = scale
        self.password = account_password
        self.log = log
        self.types: dict[str, dict] = {}
        self.templates: dict[str, str] = {}
        self.items: dict[str, list[dict]] = {}
        self.images: list[dict] = []
        self.manuals: list[dict] = []
        self.views: dict[str, dict[str, str]] = {}
        self.today = dt.date.today()
        self.counts: dict[str, int] = {}
        self._template_cache: dict[str, dict[str, str]] = {}

    def count(self, key, n=1):
        self.counts[key] = self.counts.get(key, 0) + n

    def n(self, base: int) -> int:
        return max(1, int(round(base * self.scale)))

    # ---------------------------------------------------------------- lookups

    def load_theme(self):
        config = self.api.get("/raytha/api/admin/configuration")
        self.theme_id = config["activeThemeId"]
        templates = items_of(self.api.get(f"/raytha/api/admin/themes/{self.theme_id}/web-templates?pageSize=200"))
        self.templates = {t["developerName"]: t["id"] for t in templates}

    def type_templates(self, developer_name) -> dict[str, str]:
        listed = items_of(self.api.get(f"/raytha/api/admin/content-types/{developer_name}/templates"))
        return {t["developerName"]: t["id"] for t in listed}

    # ---------------------------------------------------------------- media

    def seed_media(self):
        self.log("media")
        names = [
            "harbor-at-dawn", "city-rooftops", "workshop-bench", "market-stalls", "library-stacks", "river-bend",
            "studio-desk", "orchard-rows", "bridge-lights", "forest-trail", "headshot-a", "headshot-b",
            "headshot-c", "headshot-d", "product-shot-a", "product-shot-b",
        ]
        for index, name in enumerate(names):
            top, bottom, band = PALETTES[index % len(PALETTES)]
            data = png(640, 360, top, bottom, band)
            result = self.api.upload(f"{name}.png", "image/png", data)
            self.images.append({"objectKey": result["fields"]["objectKey"], "url": result["url"], "name": name})
            self.count("media")
        for name in ["trail-pack-manual", "desk-lamp-manual", "field-notebook-care-guide"]:
            data = pdf(title_case(name.split("-")), [paragraph(self.rng, 1), paragraph(self.rng, 1), "Printed by the Raytha seeder."])
            result = self.api.upload(f"{name}.pdf", "application/pdf", data)
            self.manuals.append({"objectKey": result["fields"]["objectKey"], "url": result["url"], "name": name})
            self.count("media")
        notes = "Shipping notes\n\n" + "\n".join(SENTENCES[:5]) + "\n"
        self.api.upload("shipping-notes.txt", "text/plain", notes.encode())
        self.count("media")

    def image(self, prefix=None):
        pool = [i for i in self.images if prefix is None or i["name"].startswith(prefix)] or self.images
        return self.rng.choice(pool)

    # ---------------------------------------------------------------- content types

    def seed_content_types(self):
        self.log("content types and fields")
        for spec in CONTENT_TYPES:
            name = spec["developerName"]
            if not spec.get("existing"):
                self.api.post(
                    "/raytha/api/admin/content-types",
                    {
                        "labelPlural": spec["plural"],
                        "labelSingular": spec["singular"],
                        "developerName": name,
                        "description": spec["description"],
                        "defaultRouteTemplate": spec["route"],
                    },
                )
                self.count("content types")
            self.types[name] = self.api.get(f"/raytha/api/admin/content-types/{name}")

        for spec in CONTENT_TYPES:
            name = spec["developerName"]
            content_type = self.types[name]
            if not spec.get("existing"):
                self.relabel_default_fields(name, spec["primaryLabel"], spec["contentLabel"])
            present = {f["developerName"] for f in items_of(self.api.get(f"/raytha/api/admin/content-types/{name}/fields"))}
            for f in spec["fields"]:
                if f["developerName"] in present:
                    continue
                body = {
                    "fieldType": f["fieldType"],
                    "developerName": f["developerName"],
                    "label": f["label"],
                    "isRequired": f["isRequired"],
                    "description": f["description"],
                    "contentTypeId": content_type["id"],
                    "choices": f["choices"],
                    "subFields": f["subFields"],
                }
                if f["related"]:
                    body["relatedContentTypeId"] = self.types[f["related"]]["id"]
                self.api.post(f"/raytha/api/admin/content-types/{name}/fields", body)
                self.count("fields")
            self.types[name] = self.api.get(f"/raytha/api/admin/content-types/{name}")

    def relabel_default_fields(self, content_type, primary_label, content_label):
        fields = items_of(self.api.get(f"/raytha/api/admin/content-types/{content_type}/fields"))
        for f in fields:
            label = {"title": primary_label, "content": content_label}.get(f["developerName"])
            if not label:
                continue
            self.api.put(
                f"/raytha/api/admin/content-types/{content_type}/fields/{f['id']}",
                {
                    "label": label,
                    "isRequired": f["developerName"] == "title",
                    "description": "",
                    "choices": [],
                    "subFields": [],
                },
            )

    # ---------------------------------------------------------------- web template

    def seed_event_template(self):
        self.log("custom web template")
        content = EVENT_TEMPLATE
        created = self.api.post(
            f"/raytha/api/admin/themes/{self.theme_id}/web-templates",
            {
                "themeId": self.theme_id,
                "developerName": "seed_event_detail",
                "label": "Event detail",
                "content": content,
                "isBaseLayout": False,
                "parentTemplateId": self.templates.get("raytha_html_base_layout"),
                "allowAccessForNewContentTypes": False,
                "templateAccessToModelDefinitions": [self.types["events"]["id"]],
            },
        )
        self.templates["seed_event_detail"] = created["id"]
        self.count("web templates")

    # ---------------------------------------------------------------- content items

    def create_item(self, content_type, content, template=None, draft=False):
        templates = self.type_templates_cache(content_type)
        template_id = templates.get(template or "raytha_html_content_item_detail") or next(iter(templates.values()))
        created = self.api.post(
            f"/raytha/api/admin/content-types/{content_type}/items",
            {"saveAsDraft": draft, "templateId": template_id, "content": content},
        )
        record = {"id": created["id"], "content": content, "draft": draft}
        self.items.setdefault(content_type, []).append(record)
        self.count(f"items: {content_type}")
        return record

    def type_templates_cache(self, content_type):
        if content_type not in self._template_cache:
            self._template_cache[content_type] = self.type_templates(content_type)
        return self._template_cache[content_type]

    def seed_items(self):
        rng = self.rng
        self.log("content items")

        for index, (name, color) in enumerate(
            [("Design", "#6f42c1"), ("Engineering", "#0d6efd"), ("Community", "#198754"), ("Field notes", "#fd7e14"),
             ("Releases", "#dc3545"), ("Research", "#20c997"), ("Guides", "#0dcaf0"), ("Archive", "#6c757d")]
        ):
            self.create_item(
                "categories",
                {"title": name, "content": f"<p>Stories about {name.lower()}. {paragraph(rng, 1)}</p>", "accent": color,
                 "sort_order": index + 1, "is_visible": name != "Archive"},
            )

        people = list(zip(FIRST_NAMES, LAST_NAMES))
        rng.shuffle(people)
        for index, (first, last) in enumerate(people[: self.n(12)]):
            full = f"{first} {last}"
            if index == 0:
                full = "Siobhán O'Brien"
            self.create_item(
                "authors",
                {
                    "title": full,
                    "content": f"<p>{full} writes about {rng.choice(TOPICS)} and {rng.choice(TOPICS)}. {paragraph(rng, 2)}</p>",
                    "role": rng.choice(["Staff writer", "Contributor", "Editor", "Photographer", "Guest columnist"]),
                    "email": f"{slug(first)}.{slug(last)}@example.com",
                    "featured_image": self.image("headshot")["objectKey"],
                    "accent": "#%06x" % rng.randrange(0x1000000),
                    "is_staff": rng.random() < 0.6,
                    "joined_on": iso_date(self.today - dt.timedelta(days=rng.randrange(60, 2000))),
                    "expertise": rng.sample([slug(c) for c in EXPERTISE], rng.randint(1, 3)),
                    "seniority": rng.choice(["junior", "mid", "senior", "principal"]),
                    "social_links": [
                        {"network": network, "url": f"https://{network}.example.com/{slug(first)}{slug(last)}"}
                        for network in rng.sample(["mastodon", "github", "linkedin", "website"], rng.randint(0, 3))
                    ],
                },
            )

        cities = ["chicago", "lisbon", "nairobi", "osaka", "toronto", "warsaw"]
        for index in range(self.n(10)):
            name = f"{title_case([rng.choice(ADJECTIVES), rng.choice(NOUNS)])} Hall"
            if index == 0:
                name = "Theatre Café 100% Open"
            city = cities[index % len(cities)]
            self.create_item(
                "venues",
                {
                    "title": name,
                    "content": f"<p>{paragraph(rng, 2)}</p>",
                    "address": f"{rng.randint(1, 999)} {rng.choice(NOUNS).title()} Street\n{city.title()}",
                    "city": city,
                    "capacity": rng.choice([40, 80, 120, 250, 600, 1200]),
                    "is_accessible": rng.random() < 0.75,
                    "map_url": f"https://maps.example.com/?q={urllib.parse.quote(name)}",
                    "featured_image": self.image()["objectKey"],
                },
            )

        departments = ["apparel", "home", "outdoor", "stationery", "tech"]
        product_nouns = ["Trail Pack", "Desk Lamp", "Field Notebook", "Travel Mug", "Rain Shell", "Cable Kit", "Linen Throw",
                         "Wool Beanie", "Pocket Knife", "Planter", "Headphones", "Tote Bag"]
        used = set()
        for index in range(self.n(40)):
            while True:
                name = f"{rng.choice(ADJECTIVES).title()} {rng.choice(product_nouns)}"
                if name not in used:
                    used.add(name)
                    break
            if index == 0:
                name = "Under_score & 50% Off Mug"
            self.create_item(
                "products",
                {
                    "title": name,
                    "content": rich_text(rng, "Why you'll like it"),
                    "sku": f"SKU-{1000 + index:04d}",
                    "price": round(rng.uniform(4, 320), 2),
                    "stock": rng.choice([0, 0, 3, 12, 40, 150, 999]),
                    "department": rng.choice(departments),
                    "size": rng.choice(["s", "m", "l", "xl"]),
                    "colors": rng.sample([slug(c) for c in COLORS], rng.randint(1, 4)),
                    "on_sale": rng.random() < 0.3,
                    "launch_date": iso_date(self.today + dt.timedelta(days=rng.randrange(-700, 90))),
                    "swatch": "#%06x" % rng.randrange(0x1000000),
                    "featured_image": self.image("product")["objectKey"] if rng.random() < 0.5 else self.image()["objectKey"],
                    "manual": rng.choice(self.manuals)["objectKey"] if rng.random() < 0.4 else "",
                    "specs": [
                        {"name": spec_name, "value": spec_value, "accent": rng.choice([None, "#198754", "#dc3545"])}
                        for spec_name, spec_value in rng.sample(
                            [("Weight", f"{rng.randint(100, 2400)} g"), ("Material", rng.choice(["Cotton", "Steel", "Oak", "Recycled PET"])),
                             ("Warranty", f"{rng.randint(1, 5)} years"), ("Origin", rng.choice(["Portugal", "Japan", "Kenya", "Canada"]))],
                            rng.randint(0, 4),
                        )
                    ],
                },
                draft=index % 13 == 5,
            )

        authors = self.items["authors"]
        categories = self.items["categories"]
        formats = ["article", "interview", "tutorial", "news", "review"]
        post_titles = set()
        for index in range(self.n(60)):
            while True:
                title = f"{title_case([rng.choice(VERBS), 'the', rng.choice(ADJECTIVES), rng.choice(NOUNS)])}"
                if title not in post_titles:
                    post_titles.add(title)
                    break
            if index == 0:
                title = "Don't Panic: O'Brien's Guide to 100% Uptime"
            elif index == 1:
                title = "東京 and Zürich: Notes on Localized Content — Ünïcödé Edition"
            elif index == 2:
                title = " ".join(["A Very Long Headline That Keeps Going To Test Truncation In Lists And Cards"] * 2)
            published = self.today - dt.timedelta(days=rng.randrange(0, 900))
            content = {
                "title": title,
                "content": rich_text(rng, title_case([rng.choice(ADJECTIVES), rng.choice(TOPICS)])),
                "excerpt": paragraph(rng, 1),
                "author": rng.choice(authors)["id"],
                "category": rng.choice(categories)["id"] if rng.random() < 0.9 else "",
                "format": rng.choice(formats),
                "audience": rng.choice(["beginner", "intermediate", "advanced"]),
                "tags": rng.sample([slug(t) for t in TAGS], rng.randint(0, 3)),
                "is_featured": rng.random() < 0.2,
                "published_on": iso_date(published),
                "reading_minutes": rng.randint(2, 25),
                "featured_image": self.image()["objectKey"] if rng.random() < 0.85 else "",
                "accent": "#%06x" % rng.randrange(0x1000000),
                "sources": [
                    {
                        "label": f"{rng.choice(COMPANIES)} report",
                        "url": f"https://example.com/reports/{rng.randint(100, 999)}",
                        "accessed_on": iso_date(published - dt.timedelta(days=rng.randint(1, 30))),
                        "is_primary": i == 0,
                    }
                    for i in range(rng.randint(0, 3))
                ],
            }
            self.create_item("posts", content, draft=index % 11 == 7)

        venues = self.items["venues"]
        for index in range(self.n(30)):
            starts = self.today + dt.timedelta(days=rng.randrange(-240, 240))
            kind = rng.choice(["conference", "workshop", "meetup", "webinar"])
            name = f"{title_case([rng.choice(ADJECTIVES), rng.choice(TOPICS)])} {kind.title()} {starts.year}"
            if index == 0:
                name = "Community Meetup: \"What's Next?\""
            virtual = kind == "webinar" or rng.random() < 0.15
            sessions = []
            for slot in range(rng.randint(1, 5)):
                first, last = rng.choice(people)
                sessions.append(
                    {
                        "time": f"{9 + slot}:00",
                        "session": title_case([rng.choice(VERBS), rng.choice(NOUNS)]),
                        "speaker": f"{first} {last}",
                        "minutes": rng.choice([15, 30, 45, 60]),
                        "is_keynote": slot == 0,
                    }
                )
            self.create_item(
                "events",
                {
                    "title": name,
                    "content": rich_text(rng, "What to expect"),
                    "summary": paragraph(rng, 1),
                    "starts_on": iso_date(starts),
                    "ends_on": iso_date(starts + dt.timedelta(days=rng.choice([0, 0, 1, 2]))),
                    "venue": "" if virtual else rng.choice(venues)["id"],
                    "event_type": kind,
                    "topics": rng.sample([slug(t) for t in TOPIC_CHOICES], rng.randint(1, 3)),
                    "is_virtual": virtual,
                    "capacity": rng.choice([25, 50, 100, 300, 1000]),
                    "ticket_price": rng.choice([0, 0, 15, 49, 199, 499.5]),
                    "featured_image": self.image()["objectKey"],
                    "brand_color": "#%06x" % rng.randrange(0x1000000),
                    "schedule": sessions,
                },
                template="seed_event_detail",
                draft=index % 9 == 4,
            )

        products = [p for p in self.items["products"] if not p["draft"]]
        for index in range(self.n(20)):
            first, last = rng.choice(people)
            product = rng.choice(products)
            self.create_item(
                "testimonials",
                {
                    "title": rng.choice(["Exactly what we needed", "Worth every penny", "Our team loves it",
                                         "Better than expected", "Five stars from the whole office"]) + f" ({index + 1})",
                    "content": f"<p>{paragraph(rng, 3)}</p>",
                    "quote": paragraph(rng, 1),
                    "person": f"{first} {last}",
                    "company": rng.choice(COMPANIES),
                    "rating": rng.choice([3, 4, 4, 5, 5, 5]),
                    "product": product["id"],
                    "featured_image": self.image("headshot")["objectKey"],
                },
            )

        faq_pairs = [
            ("How do I reset my password?", "accounts"), ("Can I change my email address?", "accounts"),
            ("Which payment methods do you accept?", "billing"), ("How do refunds work?", "billing"),
            ("Are events recorded?", "events"), ("Can I transfer my ticket?", "events"),
            ("How long does shipping take?", "shipping"), ("Do you ship internationally?", "shipping"),
            ("What data do you store about me?", "privacy"), ("How do I delete my account?", "privacy"),
            ("Is there a student discount?", "billing"), ("What's your accessibility policy?", "events"),
            ("Can I get an invoice with my company's details?", "billing"), ("Where is my order?", "shipping"),
            ("Do you use cookies?", "privacy"), ("How do I join the community forum?", "accounts"),
        ]
        for index, (question, topic) in enumerate(faq_pairs[: self.n(16)]):
            self.create_item(
                "faqs",
                {"title": question, "content": f"<p>{paragraph(rng, 2)}</p>", "topic": topic,
                 "sort_order": index + 1, "is_pinned": index < 3},
            )

    def seed_item_states(self):
        self.log("item states: revisions, unpublished, trash")
        rng = self.rng
        for content_type in ["posts", "products", "events"]:
            published = [i for i in self.items[content_type] if not i["draft"]]
            rng.shuffle(published)
            for record in published[:4]:
                for revision in range(2):
                    content = dict(record["content"])
                    content["title"] = f"{record['content']['title']} (rev {revision + 1})" if revision == 0 else record["content"]["title"]
                    self.api.put(
                        f"/raytha/api/admin/content-types/{content_type}/items/{record['id']}",
                        {"saveAsDraft": False, "content": content},
                    )
                    self.count("revisions")
            for record in published[4:6]:
                content = dict(record["content"])
                content["title"] = content["title"] + " (unsaved draft)"
                self.api.put(
                    f"/raytha/api/admin/content-types/{content_type}/items/{record['id']}",
                    {"saveAsDraft": True, "content": content},
                )
                self.count("published items with pending drafts")
            for record in published[6:8]:
                self.api.post(f"/raytha/api/admin/content-types/{content_type}/items/{record['id']}/unpublish")
                self.count("unpublished items")
            for record in published[8:10]:
                self.api.delete(f"/raytha/api/admin/content-types/{content_type}/items/{record['id']}")
                self.count("trashed items")

    # ---------------------------------------------------------------- views

    def seed_views(self):
        self.log("views")
        list_templates = {name: self.type_templates_cache(name).get("raytha_html_content_item_list") for name in self.types}
        plans = {
            "posts": [
                ("Featured posts", "featured", ["PrimaryField", "author", "category", "published_on", "is_featured"],
                 [("published_on", "desc")], [("is_featured", "true", None)], "featured"),
                ("Tutorials", "tutorials", ["PrimaryField", "author", "audience", "reading_minutes", "tags"],
                 [("reading_minutes", "asc")], [("format", "eq", "tutorial")], "tutorials"),
                ("Long reads", "long_reads", ["PrimaryField", "reading_minutes", "published_on"],
                 [("reading_minutes", "desc")], [("reading_minutes", "ge", "15")], None),
                ("Missing image", "missing_image", ["PrimaryField", "featured_image", "LastModificationTime"],
                 [], [("featured_image", "empty", None)], None),
            ],
            "events": [
                ("Upcoming", "upcoming", ["PrimaryField", "starts_on", "venue", "event_type", "ticket_price"],
                 [("starts_on", "asc")], [("starts_on", "ge", iso_date(self.today))], "upcoming"),
                ("Free and virtual", "free_virtual", ["PrimaryField", "starts_on", "is_virtual", "ticket_price"],
                 [("starts_on", "desc")], [("ticket_price", "eq", "0"), ("is_virtual", "true", None)], None),
            ],
            "products": [
                ("On sale", "on_sale", ["PrimaryField", "sku", "price", "stock", "swatch", "colors"],
                 [("price", "asc")], [("on_sale", "true", None)], "sale"),
                ("Out of stock", "out_of_stock", ["PrimaryField", "sku", "stock", "department"],
                 [("PrimaryField", "asc")], [("stock", "eq", "0")], None),
                ("Premium", "premium", ["PrimaryField", "price", "department", "launch_date"],
                 [("price", "desc")], [("price", "gt", "150")], None),
            ],
            "authors": [
                ("Staff", "staff", ["PrimaryField", "role", "email", "is_staff", "joined_on", "expertise"],
                 [("joined_on", "asc")], [("is_staff", "true", None)], None),
            ],
            "testimonials": [
                ("Five stars", "five_stars", ["PrimaryField", "person", "company", "rating", "product"],
                 [("CreationTime", "desc")], [("rating", "eq", "5")], "reviews"),
            ],
            "faqs": [
                ("Billing", "billing", ["PrimaryField", "topic", "sort_order", "is_pinned"],
                 [("sort_order", "asc")], [("topic", "eq", "billing")], None),
            ],
        }
        for content_type, views in plans.items():
            ct = self.types[content_type]
            for label, developer_name, columns, sort, conditions, route in views:
                created = self.api.post(
                    f"/raytha/api/admin/content-types/{content_type}/views",
                    {"contentTypeId": ct["id"], "label": label, "developerName": developer_name, "description": f"Seeded view: {label}."},
                )
                view_id = created["id"]
                base = f"/raytha/api/admin/content-types/{content_type}/views/{view_id}"
                current = self.api.get(base)
                for column in current.get("columns") or []:
                    if column not in columns:
                        self.api.put(f"{base}/columns", {"developerName": column, "showColumn": False})
                for column in columns:
                    if column not in (current.get("columns") or []):
                        self.api.put(f"{base}/columns", {"developerName": column, "showColumn": True})
                for position, column in enumerate(columns):
                    self.api.post(f"{base}/columns/reorder", {"developerName": column, "newFieldOrder": position + 1})
                for column, direction in sort:
                    self.api.put(f"{base}/sort", {"developerName": column, "showColumn": True, "orderByDirection": direction})
                if conditions:
                    self.api.put(f"{base}/filter", {"filter": filter_rows(conditions)})
                if route:
                    self.api.put(
                        f"{base}/public-settings",
                        {
                            "isPublished": True,
                            "routePath": f"{content_type}/{route}",
                            "templateId": list_templates[content_type],
                            "defaultNumberOfItemsPerPage": 12,
                            "maxNumberOfItemsPerPage": 100,
                            "ignoreClientFilterAndSortQueryParams": False,
                        },
                    )
                self.api.post(f"{base}/favorite", {"setAsFavorite": developer_name in ("featured", "upcoming", "on_sale")})
                self.views.setdefault(content_type, {})[developer_name] = view_id
                self.count("views")

        for content_type in ["categories", "authors", "venues", "events", "products", "testimonials", "faqs"]:
            default = next(
                (v for v in items_of(self.api.get(f"/raytha/api/admin/content-types/{content_type}/views?pageSize=100"))
                 if v["developerName"] == content_type),
                None,
            )
            if default and list_templates.get(content_type):
                self.api.put(
                    f"/raytha/api/admin/content-types/{content_type}/views/{default['id']}/public-settings",
                    {
                        "isPublished": True,
                        "routePath": content_type,
                        "templateId": list_templates[content_type],
                        "defaultNumberOfItemsPerPage": 12,
                        "maxNumberOfItemsPerPage": 100,
                        "ignoreClientFilterAndSortQueryParams": False,
                    },
                )
                self.views.setdefault(content_type, {})[content_type] = default["id"]

    # ---------------------------------------------------------------- people

    def seed_people(self):
        self.log("user groups and users")
        groups = {}
        for label in ["Members", "Newsletter subscribers", "Beta testers", "Event speakers", "VIP customers"]:
            created = self.api.post("/raytha/api/admin/user-groups", {"label": label, "developerName": slug(label)})
            groups[slug(label)] = created["id"]
            self.count("user groups")

        rng = self.rng
        people = [(f, l) for f in FIRST_NAMES for l in LAST_NAMES]
        rng.shuffle(people)
        self.user_emails = []
        users = []
        for index, (first, last) in enumerate(people[: self.n(40)]):
            email = f"{slug(first)}.{slug(last)}{index}@example.com"
            created = self.api.post(
                "/raytha/api/admin/users",
                {
                    "firstName": first,
                    "lastName": last,
                    "emailAddress": email,
                    "userGroups": rng.sample(list(groups.values()), rng.randint(0, 3)),
                    "sendEmail": False,
                },
            )
            users.append(created["id"])
            self.user_emails.append(email)
            self.count("users")
        for user_id in users[:5]:
            self.api.post(
                f"/raytha/api/admin/users/{user_id}/reset-password",
                {"newPassword": self.password, "confirmNewPassword": self.password, "sendEmail": False},
            )
        for user_id in users[-3:]:
            self.api.post(f"/raytha/api/admin/users/{user_id}/suspend")
            self.count("suspended users")

        self.log("roles and admins")
        type_ids = {name: t["id"] for name, t in self.types.items()}
        roles = {
            "editor": ("Editor", [], {type_ids[n]: ["read", "edit"] for n in ["posts", "authors", "categories", "events", "venues"]}),
            "shop_manager": ("Shop manager", ["media_items"], {type_ids[n]: ["read", "edit", "config"] for n in ["products", "testimonials"]}),
            "designer": ("Designer", ["templates", "site_pages", "media_items"], {type_ids["posts"]: ["read"]}),
            "community_manager": ("Community manager", ["users"], {type_ids["faqs"]: ["read", "edit"]}),
            "auditor": ("Auditor", ["audit_logs"], {name_id: ["read"] for name_id in type_ids.values()}),
        }
        role_ids = {r["developerName"]: r["id"] for r in items_of(self.api.get("/raytha/api/admin/roles?pageSize=100"))}
        for developer_name, (label, system, content_permissions) in roles.items():
            if developer_name in role_ids:
                continue
            created = self.api.post(
                "/raytha/api/admin/roles",
                {"label": label, "developerName": developer_name, "systemPermissions": system,
                 "contentTypePermissions": content_permissions},
            )
            role_ids[developer_name] = created["id"]
            self.count("roles")

        self.admin_emails = []
        admins = [
            ("Elena", "Editor", ["editor"]), ("Sam", "Shopkeeper", ["shop_manager"]), ("Dana", "Designer", ["designer"]),
            ("Chris", "Community", ["community_manager", "editor"]), ("Audrey", "Auditor", ["auditor"]),
            ("Former", "Contractor", ["editor"]),
        ]
        admin_ids = []
        for first, last, role_names in admins:
            email = f"{first.lower()}.{last.lower()}@example.com"
            created = self.api.post(
                "/raytha/api/admin/admins",
                {"firstName": first, "lastName": last, "emailAddress": email,
                 "roles": [role_ids[r] for r in role_names], "sendEmail": False},
            )
            admin_ids.append(created["id"])
            self.admin_emails.append(email)
            self.api.post(
                f"/raytha/api/admin/admins/{created['id']}/reset-password",
                {"newPassword": self.password, "confirmNewPassword": self.password, "sendEmail": False},
            )
            self.count("admins")
        self.api.post(f"/raytha/api/admin/admins/{admin_ids[-1]}/suspend")
        self.count("suspended admins")

        me = self.api.get("/raytha/api/auth/me")
        self.api.post(f"/raytha/api/admin/admins/{me['id']}/api-keys", {})
        self.count("api keys")

    # ---------------------------------------------------------------- auth schemes

    def seed_auth_schemes(self):
        self.log("authentication schemes")
        schemes = items_of(self.api.get("/raytha/api/admin/authentication-schemes"))
        magic = next((s for s in schemes if s["developerName"] == "magic_link"), None)
        if magic:
            self.api.put(
                f"/raytha/api/admin/authentication-schemes/{magic['id']}",
                scheme_request(magic, is_enabled_for_users=True),
            )
        self.api.post(
            "/raytha/api/admin/authentication-schemes",
            {
                "label": "Partner portal",
                "developerName": "partner_portal",
                "authenticationSchemeType": "jwt",
                "loginButtonText": "Sign in with the partner portal",
                "signInUrl": "https://partners.example.com/sso/raytha",
                "signOutUrl": "https://partners.example.com/sso/logout",
                "isEnabledForUsers": True,
                "isEnabledForAdmins": False,
                "jwtSecretKey": secrets.token_urlsafe(48),
                "jwtUseHighSecurity": True,
                "samlCertificate": None,
                "samlIdpEntityId": None,
                "magicLinkExpiresInSeconds": 900,
                "bruteForceProtectionMaxFailedAttempts": 10,
                "bruteForceProtectionWindowInSeconds": 60,
            },
        )
        self.count("authentication schemes")
        self.api.post(
            "/raytha/api/admin/authentication-schemes",
            {
                "label": "Staff directory (disabled)",
                "developerName": "staff_directory",
                "authenticationSchemeType": "jwt",
                "loginButtonText": "Staff single sign-on",
                "signInUrl": "https://sso.example.com/raytha",
                "signOutUrl": "",
                "isEnabledForUsers": False,
                "isEnabledForAdmins": False,
                "jwtSecretKey": secrets.token_urlsafe(48),
                "jwtUseHighSecurity": False,
                "samlCertificate": None,
                "samlIdpEntityId": None,
                "magicLinkExpiresInSeconds": 900,
                "bruteForceProtectionMaxFailedAttempts": 10,
                "bruteForceProtectionWindowInSeconds": 60,
            },
        )
        self.count("authentication schemes")

    # ---------------------------------------------------------------- functions

    def seed_functions(self):
        self.log("functions")
        functions = [
            ("Status API", "status_api", "http_request", True, None, FUNCTION_STATUS),
            ("llms.txt", "llms_txt", "http_request", True, "llms.txt", FUNCTION_LLMS),
            ("Greeting", "greeting", "liquid_template", True, None, FUNCTION_GREETING),
            ("Log new content", "log_new_content", "content_item_created", True, None, FUNCTION_ON_CREATE),
            ("Notify on update (paused)", "notify_on_update", "content_item_updated", False, None, FUNCTION_ON_UPDATE),
        ]
        for name, developer_name, trigger, active, route, code in functions:
            body = {"name": name, "developerName": developer_name, "triggerType": trigger, "isActive": active, "code": code}
            if route:
                body["routePath"] = route
            self.api.post("/raytha/api/admin/functions", body)
            self.count("functions")

    # ---------------------------------------------------------------- site pages

    def seed_site_pages(self):
        self.log("site pages")
        rng = self.rng
        views = self.views

        def w(widget_type, settings, row=0, column=0, span=12, css=""):
            return {"widgetType": widget_type, "settingsJson": json.dumps(settings), "row": row, "column": column,
                    "columnSpan": span, "cssClass": css, "htmlId": "", "customAttributes": ""}

        def card(title, image, url, column):
            return w("card", {"title": title, "description": f"<p>{paragraph(rng, 1)}</p>", "imageUrl": image["url"],
                              "imageAlt": title, "buttonText": "Learn more", "buttonUrl": url, "buttonStyle": "outline-primary"},
                     row=0, column=column, span=4)

        pages = [
            ("About us", "raytha_html_page_multi", False, {
                "hero": [w("hero", {"headline": "We build calm software", "subheadline": paragraph(rng, 1),
                                    "backgroundImage": self.image()["url"], "textColor": "#ffffff", "buttonText": "Meet the team",
                                    "buttonUrl": "/authors", "buttonStyle": "light", "alignment": "left", "minHeight": 420})],
                "features": [card("Editorial", self.image(), "/posts", 0), card("Events", self.image(), "/events", 4),
                             card("Shop", self.image("product"), "/products", 8)],
                "content": [w("imagetext", {"imageUrl": self.image()["url"], "imageAlt": "Our studio", "headline": "Our story",
                                            "content": rich_text(rng, "Where we started"), "imagePosition": "right",
                                            "buttonText": "Read the blog", "buttonUrl": "/posts", "buttonStyle": "primary"}),
                            w("wysiwyg", {"content": rich_text(rng, "Values"), "padding": "medium"}, row=1)],
                "cta": [w("cta", {"headline": "Want to write for us?", "content": "<p>We pay contributors and edit kindly.</p>",
                                  "buttonText": "Get in touch", "buttonUrl": "/contact", "buttonStyle": "light",
                                  "backgroundColor": "#198754", "textColor": "#ffffff", "alignment": "center"})],
            }),
            ("Pricing", "raytha_html_page_fullwidth", False, {
                "main": [
                    w("hero", {"headline": "Simple pricing", "subheadline": "Pick a plan. Change any time.",
                               "backgroundColor": "#212529", "textColor": "#ffffff", "alignment": "center", "minHeight": 280}),
                    card("Starter · $0", self.image(), "/contact", 0) | {"row": 1},
                    card("Team · $29", self.image(), "/contact", 4) | {"row": 1},
                    card("Enterprise · Let's talk", self.image(), "/contact", 8) | {"row": 1},
                    w("faq", {"headline": "Billing questions", "expandFirst": True,
                              "items": [{"question": q, "answer": f"<p>{paragraph(rng, 1)}</p>"} for q, t in FAQ_SAMPLE]}, row=2),
                ],
            }),
            ("Contact", "raytha_html_page_sidebar", False, {
                "main": [w("wysiwyg", {"content": "<h2>Say hello</h2><p>Email <a href=\"mailto:hello@example.com\">hello@example.com</a> "
                                                  "or visit one of our venues.</p>", "padding": "large"}),
                         w("embed", {"embedType": "iframe", "iframeUrl": "https://www.openstreetmap.org/export/embed.html?bbox=-9.15,38.70,-9.12,38.72",
                                     "aspectRatio": "16x9", "maxWidth": 900, "caption": "Our Lisbon office"}, row=1)],
                "sidebar": [w("card", {"title": "Office hours", "description": "<p>Mon–Fri, 9:00–17:00</p>",
                                       "buttonText": "FAQ", "buttonUrl": "/faqs", "buttonStyle": "secondary"})],
            }),
            ("Events calendar", "raytha_html_page_fullwidth", False, {
                "main": [w("hero", {"headline": "Upcoming events", "subheadline": "Workshops, meetups, and one big conference.",
                                    "backgroundColor": "#6f42c1", "textColor": "#ffffff", "alignment": "center", "minHeight": 260}),
                         w("contentlist", {"headline": "Next up", "contentType": "events", "viewId": views["events"]["upcoming"],
                                           "pageSize": 6, "displayStyle": "cards", "showImage": True, "showDate": True,
                                           "showExcerpt": True, "linkText": "All events", "linkUrl": "/events"}, row=1)],
            }),
            ("Shop highlights", "raytha_html_page_multi", False, {
                "hero": [w("hero", {"headline": "The shop", "subheadline": "Durable things for everyday work.",
                                    "backgroundImage": self.image("product")["url"], "textColor": "#ffffff",
                                    "buttonText": "Browse products", "buttonUrl": "/products", "buttonStyle": "light", "minHeight": 360})],
                "content": [w("contentlist", {"headline": "On sale now", "contentType": "products", "viewId": views["products"]["on_sale"],
                                              "pageSize": 8, "displayStyle": "list", "showImage": True, "showDate": False,
                                              "showExcerpt": True, "linkText": "See every deal", "linkUrl": "/products/sale"}),
                            w("contentlist", {"headline": "What customers say", "contentType": "testimonials",
                                              "pageSize": 3, "displayStyle": "compact", "showImage": False}, row=1)],
            }),
            ("Holiday campaign (draft)", "raytha_html_page_fullwidth", True, {
                "main": [w("cta", {"headline": "Coming soon", "content": "<p>Unpublished draft page.</p>", "buttonText": "Notify me",
                                   "buttonUrl": "/contact", "backgroundColor": "#dc3545", "textColor": "#ffffff", "alignment": "center"})],
            }),
        ]
        self.page_routes = {}
        for title, template, draft, sections in pages:
            created = self.api.post(
                "/raytha/api/admin/site-pages",
                {"title": title, "saveAsDraft": True, "templateId": self.templates[template]},
            )
            page_id = created["id"]
            for section, widgets in sections.items():
                self.api.put(f"/raytha/api/admin/site-pages/{page_id}/widgets", {"sectionName": section, "widgets": widgets})
                self.count("widgets", len(widgets))
            if not draft:
                self.api.post(f"/raytha/api/admin/site-pages/{page_id}/publish")
            page = self.api.get(f"/raytha/api/admin/site-pages/{page_id}")
            self.page_routes[title] = page["routePath"]
            self.count("site pages")

    # ---------------------------------------------------------------- menus

    def seed_menus(self):
        self.log("navigation menus")
        menus = items_of(self.api.get("/raytha/api/admin/navigation-menus"))
        main = next(m for m in menus if m.get("isMainMenu"))
        base = f"/raytha/api/admin/navigation-menus/{main['id']}/items"
        for item in self.api.get(base) or []:
            self.api.delete(f"{base}/{item['id']}")

        def add(menu_id, label, url, parent=None, new_tab=False, disabled=False, css=None):
            created = self.api.post(
                f"/raytha/api/admin/navigation-menus/{menu_id}/items",
                {"navigationMenuId": menu_id, "label": label, "url": url, "isDisabled": disabled, "openInNewTab": new_tab,
                 "cssClassName": css, "parentNavigationMenuItemId": parent},
            )
            self.count("menu items")
            return created["id"]

        route = self.page_routes
        m = main["id"]
        add(m, "Home", "/")
        blog = add(m, "Blog", "/posts")
        add(m, "Featured", "/posts/featured", parent=blog)
        add(m, "Tutorials", "/posts/tutorials", parent=blog)
        add(m, "Authors", "/authors", parent=blog)
        add(m, "Topics", "/categories", parent=blog)
        events = add(m, "Events", f"/{route['Events calendar']}")
        add(m, "All events", "/events", parent=events)
        add(m, "Venues", "/venues", parent=events)
        shop = add(m, "Shop", f"/{route['Shop highlights']}")
        add(m, "All products", "/products", parent=shop)
        add(m, "On sale", "/products/sale", parent=shop, css="text-danger")
        add(m, "Reviews", "/testimonials/reviews", parent=shop)
        add(m, "About", f"/{route['About us']}")
        add(m, "Pricing", f"/{route['Pricing']}")
        add(m, "Contact", f"/{route['Contact']}")

        footer = self.api.post("/raytha/api/admin/navigation-menus", {"label": "Footer", "developerName": "footer"})["id"]
        self.count("menus")
        add(footer, "FAQ", "/faqs")
        add(footer, "Status", "/raytha/functions/execute/status_api")
        add(footer, "llms.txt", "/llms.txt")
        add(footer, "Raytha on GitHub", "https://github.com/RaythaHQ/raytha", new_tab=True)
        add(footer, "Careers (closed)", "/careers", disabled=True)

        social = self.api.post("/raytha/api/admin/navigation-menus", {"label": "Social", "developerName": "social"})["id"]
        self.count("menus")
        for label in ["Mastodon", "GitHub", "LinkedIn"]:
            add(social, label, f"https://{label.lower()}.example.com/raytha", new_tab=True)

    # ---------------------------------------------------------------- webhooks, themes

    def seed_webhooks(self):
        self.log("webhooks")
        self.api.post(
            "/raytha/api/admin/webhooks",
            {"name": "Search index", "url": "https://search.example.invalid/hooks/raytha",
             "description": "Reindexes content when it changes. The host does not resolve, so deliveries fail and retry.",
             "isActive": True, "subscribedEvents": ["content_item.created", "content_item.updated", "content_item.deleted"],
             "maxAttempts": 3, "timeoutSeconds": 10},
        )
        self.api.post(
            "/raytha/api/admin/webhooks",
            {"name": "CRM sync (paused)", "url": "https://crm.example.invalid/raytha",
             "description": "Paused integration.", "isActive": False,
             "subscribedEvents": ["user.created", "user.updated", "user.deleted", "user.active_changed"],
             "maxAttempts": 5, "timeoutSeconds": 30},
        )
        self.count("webhooks", 2)

    def seed_theme_copy(self):
        self.log("second theme")
        self.api.post(
            f"/raytha/api/admin/themes/{self.theme_id}/duplicate",
            {"title": "Seasonal theme (inactive)", "developerName": "seasonal_theme", "description": "A copy of the default theme to experiment with."},
        )
        self.count("themes")

    # ---------------------------------------------------------------- run

    def run(self):
        self.load_theme()
        self.seed_media()
        self.seed_content_types()
        self.seed_event_template()
        self.seed_items()
        self.seed_views()
        self.seed_item_states()
        self.seed_people()
        self.seed_auth_schemes()
        self.seed_functions()
        self.seed_site_pages()
        self.seed_menus()
        self.seed_webhooks()
        self.seed_theme_copy()


def filter_rows(conditions):
    root = str(uuid.uuid4())
    rows = [{"id": root, "parentId": None, "type": "filter_condition_group", "groupOperator": "AND",
             "field": None, "conditionOperator": "", "value": None}]
    for field_name, operator, value in conditions:
        rows.append({"id": str(uuid.uuid4()), "parentId": root, "type": "filter_condition", "groupOperator": "AND",
                     "field": field_name, "conditionOperator": operator, "value": value})
    return rows


def scheme_request(scheme, is_enabled_for_users):
    return {
        "label": scheme["label"],
        "developerName": scheme["developerName"],
        "authenticationSchemeType": scheme["authenticationSchemeType"]["developerName"],
        "loginButtonText": scheme.get("loginButtonText"),
        "signInUrl": scheme.get("signInUrl"),
        "signOutUrl": scheme.get("signOutUrl"),
        "isEnabledForUsers": is_enabled_for_users,
        "isEnabledForAdmins": scheme["isEnabledForAdmins"],
        "jwtSecretKey": None,
        "jwtUseHighSecurity": scheme.get("jwtUseHighSecurity", False),
        "samlCertificate": None,
        "samlIdpEntityId": None,
        "magicLinkExpiresInSeconds": scheme.get("magicLinkExpiresInSeconds") or 900,
        "bruteForceProtectionMaxFailedAttempts": scheme.get("bruteForceProtectionMaxFailedAttempts") or 10,
        "bruteForceProtectionWindowInSeconds": scheme.get("bruteForceProtectionWindowInSeconds") or 60,
    }


FAQ_SAMPLE = [
    ("Can I switch plans later?", "billing"),
    ("Do you offer annual billing?", "billing"),
    ("What happens when a trial ends?", "billing"),
]

FUNCTION_STATUS = """/**
 * GET /raytha/functions/execute/status_api returns a small JSON status document.
 */
function get(query) {
    const posts = API_V1.GetContentItems("posts", "", "", "", "CreationTime desc", 1, 1).Result;
    return new JsonResult({
        ok: true,
        latestPost: posts.Items.length > 0 ? posts.Items[0].PrimaryField : null,
        totalPosts: posts.TotalCount,
        query: query,
    });
}

function post(payload, query) {
    return new JsonResult({ received: payload });
}
"""

FUNCTION_LLMS = """/**
 * Answers at /llms.txt with a plain-text site summary.
 */
function get(query) {
    const lines = [
        "# Northwind Journal",
        "",
        "> A demo Raytha site with posts, events, and a small shop.",
        "",
        "- /posts: articles and tutorials",
        "- /events: upcoming events",
        "- /products: the shop",
    ];
    return new TextResult(lines.join("\\n"));
}
"""

FUNCTION_GREETING = """/**
 * {{ raytha_function("greeting", "greet", name="World") }}
 */
function greet(args) {
    return "Hello, " + (args.name || "friend") + "!";
}
"""

FUNCTION_ON_CREATE = """/**
 * Runs in the background after any content item is created.
 */
function run(payload) {
    console.log("Created " + payload.ContentType.DeveloperName + ": " + payload.PrimaryField);
}
"""

FUNCTION_ON_UPDATE = """/**
 * Paused: would notify an external system on every update.
 */
function run(payload) {
    // HttpClient.Post("https://example.invalid/hooks", null, { id: payload.Id });
}
"""

EVENT_TEMPLATE = """{% assign e = Target.PublishedContent %}
<div class="py-5" style="border-top: 6px solid {{ e.brand_color.Text | default: '#0d6efd' }}">
  <div class="container">
    <div class="row">
      <div class="col-lg-9 mx-auto">
        <p class="text-uppercase small text-muted mb-1">{{ e.event_type.Text }}{% if e.is_virtual.Value %} · Virtual{% endif %}</p>
        <h1 class="display-6 fw-bold">{{ Target.PrimaryField }}</h1>
        <p class="lead">{{ e.summary.Text }}</p>
        <ul class="list-inline text-secondary">
          <li class="list-inline-item"><i class="bi bi-calendar3"></i> {{ e.starts_on.Value | date: "%B %e, %Y" }}{% if e.ends_on.Text != e.starts_on.Text %} to {{ e.ends_on.Value | date: "%B %e, %Y" }}{% endif %}</li>
          {% if e.venue.PrimaryField %}<li class="list-inline-item"><i class="bi bi-geo-alt"></i> <a href="{{ PathBase }}/{{ e.venue.RoutePath }}">{{ e.venue.PrimaryField }}</a></li>{% endif %}
          <li class="list-inline-item"><i class="bi bi-people"></i> {{ e.capacity.Text }} seats</li>
          <li class="list-inline-item"><i class="bi bi-ticket"></i> {% if e.ticket_price.Value == 0 %}Free{% else %}{{ e.ticket_price.Text }}{% endif %}</li>
        </ul>
        {% if e.featured_image.HasValue %}
        <img class="img-fluid rounded mb-4" src="{{ e.featured_image.Value | attachment_public_url }}" alt="{{ Target.PrimaryField }}">
        {% endif %}
        <p>{% for topic in e.topics.Value %}<span class="badge text-bg-light border me-1">{{ topic }}</span>{% endfor %}</p>
        <article class="lh-lg">{{ e.content.Text }}</article>
        <h2 class="h4 mt-5">Schedule</h2>
        <table class="table">
          <thead><tr><th>Time</th><th>Session</th><th>Speaker</th><th>Minutes</th></tr></thead>
          <tbody>
          {% for slot in e.schedule.Value %}
            <tr><td>{{ slot.time }}</td><td>{{ slot.session }}{% if slot.is_keynote %} <span class="badge text-bg-warning">Keynote</span>{% endif %}</td><td>{{ slot.speaker }}</td><td>{{ slot.minutes }}</td></tr>
          {% endfor %}
          </tbody>
        </table>
        <p class="mt-4">{{ raytha_function("greeting", "greet", name="attendee") }}</p>
        <a class="btn btn-outline-primary" href="{{ PathBase }}/events">All events</a>
      </div>
    </div>
  </div>
</div>
"""


# --------------------------------------------------------------------------- entry point


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--base-url", default="http://localhost:5200")
    parser.add_argument("--email", required=True, help="Super admin email (created by --setup on a fresh database).")
    parser.add_argument("--password", required=True, help="Super admin password; seeded accounts get it too.")
    parser.add_argument("--setup", action="store_true", help="Run first-run setup when the database has no admin yet.")
    parser.add_argument("--organization", default="Northwind Journal", help="Organization name used by --setup.")
    parser.add_argument("--scale", type=float, default=1.0, help="Multiplier for item and user counts.")
    parser.add_argument("--random-seed", type=int, default=2026, help="Seed for the generated data.")
    args = parser.parse_args(argv)

    started = time.time()
    api = Api(args.base_url)

    def log(step):
        print(f"[{time.time() - started:6.1f}s] {step}", flush=True)

    status = api.get("/raytha/api/auth/setup")
    if status.get("required"):
        if not args.setup:
            raise SeedError("This database has no admin yet. Re-run with --setup to create one.")
        log("first-run setup")
        api.post(
            "/raytha/api/auth/setup",
            {"firstName": "Site", "lastName": "Owner", "email": args.email, "password": args.password,
             "organizationName": args.organization, "smtpHost": "localhost", "smtpPort": 1025},
        )
    else:
        log("sign in")
        api.post("/raytha/api/auth/login", {"email": args.email, "password": args.password, "rememberMe": True})

    existing = {t["developerName"] for t in items_of(api.get("/raytha/api/admin/content-types?pageSize=200"))}
    if SEED_MARKER_TYPE in existing:
        raise SeedError(f"Content type '{SEED_MARKER_TYPE}' already exists; this instance looks seeded. Start from a fresh database.")

    seeder = Seeder(api, random.Random(args.random_seed), args.scale, args.password, log)
    seeder.run()

    log("done")
    print()
    for key in sorted(seeder.counts):
        print(f"  {key:<40} {seeder.counts[key]}")
    print(f"\n  {api.calls} API calls in {time.time() - started:.1f}s")
    print(f"  Admins (password: same as --password): {', '.join(seeder.admin_emails[:-1])}")
    print(f"  Suspended admin: {seeder.admin_emails[-1]}")
    print(f"  Public users with that password: {', '.join(seeder.user_emails[:5])}")
    print(f"  Site: {args.base_url}/   Admin: {args.base_url}/raytha")


if __name__ == "__main__":
    try:
        main()
    except SeedError as error:
        print(f"seed failed: {error}", file=sys.stderr)
        sys.exit(1)
