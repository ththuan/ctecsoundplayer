/* CTEC Sound Player service worker.
   - App shell and code use network-first so new deployments appear quickly.
   - Fonts, images and audio use cache-first.
   - Audio downloaded with "Tải offline" is stored by the app in tsound-audio-v1
     and is served by the caches.match() lookup below. */
const VERSION = "ctec-v2";
const SHELL_CACHE = `${VERSION}-shell`;
const RUNTIME_CACHE = `${VERSION}-runtime`;

const SHELL = [
  "./",
  "./index.html",
  "./app.css",
  "./app.js",
  "./catalog.json",
  "./background.json",
  "./manifest.webmanifest",
  "./assets/logo.png",
  "./assets/icon-64.png",
  "./assets/icon-192.png",
  "./assets/icon-512.png",
];

self.addEventListener("install", (event) => {
  event.waitUntil(
    (async () => {
      const cache = await caches.open(SHELL_CACHE);
      await Promise.all(
        SHELL.map(async (url) => {
          try {
            const response = await fetch(url, { cache: "reload" });
            if (response.ok) await cache.put(url, response);
          } catch {
            /* optional asset unavailable */
          }
        }),
      );
      await self.skipWaiting();
    })(),
  );
});

self.addEventListener("activate", (event) => {
  event.waitUntil(
    (async () => {
      const keys = await caches.keys();
      await Promise.all(
        keys
          .filter(
            (key) =>
              key.startsWith("ctec-") &&
              key !== SHELL_CACHE &&
              key !== RUNTIME_CACHE,
          )
          .map((key) => caches.delete(key)),
      );
      await self.clients.claim();
    })(),
  );
});

self.addEventListener("fetch", (event) => {
  const request = event.request;
  if (request.method !== "GET") return;

  const url = new URL(request.url);
  if (url.origin !== self.location.origin) return;

  // Page navigation: always try the network first, fall back to the app shell.
  if (request.mode === "navigate") {
    event.respondWith(
      (async () => {
        try {
          const response = await fetch(request);
          if (response.ok) {
            const copy = response.clone();
            const cache = await caches.open(SHELL_CACHE);
            await cache.put("./index.html", copy);
          }
          return response;
        } catch {
          return (await caches.match("./index.html")) || Response.error();
        }
      })(),
    );
    return;
  }

  // Audio: serve from the offline cache when present; never duplicate it here.
  const isAudio =
    request.destination === "audio" ||
    /\/audio\//i.test(url.pathname) ||
    /\.(mp3|m4a|wav|aac|ogg|flac)$/i.test(url.pathname);

  if (isAudio) {
    event.respondWith(caches.match(request).then((hit) => hit || fetch(request)));
    return;
  }

  // Code and metadata: network-first so a new deployment is picked up quickly.
  const isCode =
    /\.(html|js|css|json|webmanifest)$/i.test(url.pathname) ||
    url.pathname.endsWith("/");

  if (isCode) {
    event.respondWith(
      (async () => {
        try {
          const response = await fetch(request);
          if (response.ok) {
            const copy = response.clone();
            const cache = await caches.open(RUNTIME_CACHE);
            await cache.put(request, copy);
          }
          return response;
        } catch {
          return (await caches.match(request)) || Response.error();
        }
      })(),
    );
    return;
  }

  // Fonts, images and icons: cache-first.
  const isStatic = /\.(woff2?|png|jpe?g|svg|webp|ico)$/i.test(url.pathname);
  if (isStatic) {
    event.respondWith(
      caches.match(request).then(
        (hit) =>
          hit ||
          fetch(request).then((response) => {
            if (response.ok) {
              const copy = response.clone();
              caches
                .open(RUNTIME_CACHE)
                .then((cache) => cache.put(request, copy));
            }
            return response;
          }),
      ),
    );
    return;
  }

  event.respondWith(fetch(request).catch(() => caches.match(request)));
});
