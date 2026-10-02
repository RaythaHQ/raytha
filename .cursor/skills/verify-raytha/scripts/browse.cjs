// Drive the admin SPA in headless Chromium against the verification host.
// Run through scripts/browse.sh, which puts Playwright on NODE_PATH.
//
//   browse.sh --out DIR [--port N] [--steps FILE.cjs] [/raytha/path ...]
//
// Signs in through /raytha/login like a user, then either screenshots each path
// or runs a steps module:
//
//   module.exports = async ({ page, base, shot, log, settle }) => { ... };
//
// Writes DIR/NN-<name>.png and .aria.txt per shot, DIR/browse.log (navigation, titles, headings), and
// DIR/console.log (browser console errors and failed /raytha/api responses).
const fs = require("node:fs");
const path = require("node:path");
const { chromium } = require("playwright");

function parseArgs(argv) {
  const opts = { port: "15200", out: null, steps: null, paths: [] };
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a === "--port") opts.port = argv[++i];
    else if (a === "--out") opts.out = argv[++i];
    else if (a === "--steps") opts.steps = argv[++i];
    else opts.paths.push(a);
  }
  if (!opts.out || (!opts.steps && opts.paths.length === 0)) {
    console.error("usage: browse.sh --out DIR [--port N] [--steps FILE.cjs] [/raytha/path ...]");
    process.exit(2);
  }
  if (opts.port === "5200") {
    console.error("refusing: 5200 is the developer's instance");
    process.exit(2);
  }
  return opts;
}

const slug = (p) => p.replace(/^\/+/, "").replace(/[^a-z0-9]+/gi, "-").replace(/-+$/, "") || "root";

(async () => {
  const opts = parseArgs(process.argv.slice(2));
  const base = `http://127.0.0.1:${opts.port}`;
  const out = path.resolve(opts.out);
  fs.mkdirSync(out, { recursive: true });
  const logFile = path.join(out, "browse.log");
  const consoleFile = path.join(out, "console.log");
  fs.writeFileSync(logFile, "");
  fs.writeFileSync(consoleFile, "");
  const log = (line) => {
    fs.appendFileSync(logFile, line + "\n");
    console.log(line);
  };

  const browser = await chromium.launch();
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await context.newPage();
  page.on("console", (m) => {
    if (m.type() === "error") fs.appendFileSync(consoleFile, `console.error ${m.text()}\n`);
  });
  page.on("pageerror", (e) => fs.appendFileSync(consoleFile, `pageerror ${e.message}\n`));
  page.on("response", (r) => {
    if (r.url().includes("/raytha/api/") && r.status() >= 400) {
      fs.appendFileSync(consoleFile, `${r.status()} ${r.request().method()} ${r.url()}\n`);
    }
  });

  let n = 0;
  // SPA navigations never reach a new load state, so wait for the @raytha/ui
  // Skeleton placeholders to go instead.
  const settle = async () => {
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.locator(".animate-pulse").first().waitFor({ state: "detached", timeout: 10000 }).catch(() => {});
  };

  const shot = async (name) => {
    await settle();
    n += 1;
    const file = path.join(out, `${String(n).padStart(2, "0")}-${slug(name)}.png`);
    await page.screenshot({ path: file, fullPage: true });
    const aria = await page.locator("body").ariaSnapshot().catch((e) => `ariaSnapshot failed: ${e.message}`);
    fs.writeFileSync(file.replace(/\.png$/, ".aria.txt"), aria + "\n");
    const heading = await page.locator("h1").first().textContent({ timeout: 2000 }).catch(() => null);
    log(`shot ${path.basename(file)} url=${page.url().replace(base, "")} title=${JSON.stringify(await page.title())} h1=${JSON.stringify(heading?.trim() ?? null)}`);
    return file;
  };

  const email = process.env.VERIFY_EMAIL || "admin@raytha.local";
  const password = process.env.VERIFY_PASSWORD || "Verify123$";
  await page.goto(`${base}/raytha/login`, { waitUntil: "networkidle" });
  await page.getByLabel(/email/i).first().fill(email);
  await page.getByLabel(/password/i).first().fill(password);
  await Promise.all([
    page.waitForURL((u) => !u.pathname.startsWith("/raytha/login"), { timeout: 15000 }),
    page.getByRole("button", { name: /^sign in$/i }).click(),
  ]).catch(async (e) => {
    await shot("login-failed");
    throw new Error(`sign-in did not leave /raytha/login: ${e.message}`);
  });
  await page.waitForLoadState("networkidle");
  log(`signed in as ${email}, landed on ${page.url().replace(base, "")}`);

  try {
    if (opts.steps) {
      const steps = require(path.resolve(opts.steps));
      await steps({ page, base, shot, log, settle });
    } else {
      for (const p of opts.paths) {
        await page.goto(base + p, { waitUntil: "networkidle" });
        await shot(p);
      }
    }
  } catch (e) {
    await shot("error").catch(() => {});
    log(`FAILED ${e.message}`);
    process.exitCode = 1;
  } finally {
    await browser.close();
  }
})();
