import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react";
import path from "node:path";
import type { Connect, Plugin } from "vite";
import { defineConfig } from "vite";

/**
 * `base` must be `/raytha/` so hashed assets resolve to `/raytha/assets/…`.
 * Vite then only mounts the HTML at `/raytha/`; rewrite bare `/raytha` to that
 * path so the address bar can stay `/raytha`.
 */
function adminBarePath(): Plugin {
  return {
    name: "raytha-admin-bare-path",
    configureServer(server) {
      const rewrite: Connect.NextHandleFunction = (req, _res, next) => {
        if (req.url === "/raytha" || req.url?.startsWith("/raytha?")) {
          req.url = `/raytha/${req.url.slice("/raytha".length)}`;
        }
        next();
      };
      server.middlewares.use(rewrite);
    },
  };
}

// Set by ViteDevServerHostedService. The browser talks to Kestrel (:5200), which
// proxies /raytha to this process. HMR must use that port, and Vite must accept
// the Tailscale Host header Kestrel forwards.
const behindHost = process.env.RAYTHA_DEV_PROXY === "1";

export default defineConfig({
  base: "/raytha/",
  plugins: [adminBarePath(), react(), tailwindcss()],
  resolve: {
    alias: {
      "@": path.resolve(import.meta.dirname, "./src"),
      "@raytha/rich-text-editor": path.resolve(import.meta.dirname, "./src/components/rich-text-editor.tsx"),
    },
  },
  server: {
    host: "127.0.0.1",
    port: 5203,
    strictPort: true,
    allowedHosts: true,
    hmr: behindHost ? { protocol: "ws", clientPort: 5200 } : undefined,
    fs: {
      allow: ["../.."],
    },
    // Mirrors AdminSpaExtensions.ServerPathPrefixes on the host: everything else under
    // /raytha is a client route. `/raytha/login` itself is the SPA sign-in page.
    proxy: {
      "/raytha/api": "http://localhost:5200",
      "/raytha/media-items": "http://localhost:5200",
      "/raytha/functions/execute": "http://localhost:5200",
      "/raytha/themes/export": "http://localhost:5200",
      "/raytha/login/sso": "http://localhost:5200",
      "/raytha/login/jwt": "http://localhost:5200",
      "/raytha/login/saml": "http://localhost:5200",
      "/raytha/login/magic-link/complete": "http://localhost:5200",
      "/raytha/login/forgot-password/complete": "http://localhost:5200",
      "/raytha/login-redirect": "http://localhost:5200",
      "/raytha/logout": "http://localhost:5200",
      "/raytha/error": "http://localhost:5200",
      "/_static-files": "http://localhost:5200",
    },
  },
  build: {
    outDir: "../../../Raytha.Web/wwwroot/raytha",
    emptyOutDir: true,
  },
});
