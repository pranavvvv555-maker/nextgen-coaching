# Performance pass — 20 October 2026

Shared loading, scrolling and animation improvements are implemented in the source and verified in a published Release build. These are local measurements, not a guarantee of identical frame rates or loading times on every phone or hosting plan.

## Changes

- Added 480, 800 and 1280px WebP artwork variants and responsive image selection on Home, About, Career Support, Contact and Masterclass, including all career service cards. Transparency and image proportions are preserved. Below-the-fold artwork is lazy loaded; hero artwork retains high priority and reserved dimensions. Image URLs are fingerprinted so new deployments replace browser-cached artwork immediately.
- The animated AI cutouts reuse the same responsive image the browser selected for the hero. They no longer request a separate full-size texture.
- SVG animation paths are sampled once and interpolated during frames. DOM references used by each frame are cached.
- The scroll indicator uses a native scroll timeline where supported. Its fallback caches document dimensions, reads scrolling position once per frame, and updates the navigation shadow only when crossing its threshold.
- Removed the sticky header's expensive background blur. The AI glow animates a static glow layer's opacity rather than recalculating a blur filter.
- Unrelated form updates no longer trigger a full animation setup scan. Observers and controls for removed artwork are cleaned up. Background tabs pause CSS motion; active scenes and ribbons pause outside the visible area, accounting for the sticky header.
- Back to top and Skip to content scroll locally without navigating to the homepage through the document's base URL.
- Brotli/gzip compression applies only to public asset paths. Versioned assets are cached for a year; other static assets revalidate after one hour. Asset hashes are cached with file-change invalidation instead of rereading the entire stylesheet on every page request.
- Repeated translations use a bounded circuit-local cache. English rendering avoids unnecessary normalization and dictionary lookups. Switching languages clears the cache. Restoring unchanged preferences avoids a redundant full-page render; theme changes update only the preference controls instead of rerendering every localized component.

## Measured delivery results

| Asset | Previous uncompressed bytes | Brotli bytes | Reduction |
| --- | ---: | ---: | ---: |
| Shared CSS | 252,720 | 76,776 | 70% |
| Shared motion controller | 36,406 | 12,204 | 66% |
| Motion runtime | 144,870 | 61,088 | 58% |

The 800px set for 21 artwork sources totals 1,375,016 bytes versus 8,717,355 bytes for their original files: 84% less. Actual downloads depend on the image's displayed size, device pixel ratio and browser cache. The homepage AI image is 62,334 bytes at 480px or 128,386 bytes at 800px, compared with the previous 282,014-byte full-size image.

## Verification

- Debug build and Release publish: zero errors and warnings.
- Public asset delivery verified in Development and a temporary Production Release process: Brotli/gzip decode identically to the original, versioned cache headers work, conditional requests return 304, and dynamic pages remain outside public caching and compression.
- All 63 generated image files were successfully served by the published application. Dimensions, aspect ratios and all nine transparent variants were checked. Development diagnostics are absent from Production HTML, even with `?perf`.
- 36 page/viewport/theme checks: Home, Programs, Career Support, About, Contact and Masterclass at 390, 768 and 1440px in light and dark themes. No horizontal page overflow or broken loaded images.
- 15 additional language-navigation checks across five pages in Marathi, Hindi and English, including resetting to English: no horizontal overflow or broken loaded images.
- Program search, enrollment form navigation, service filtering (5 filtered cards, 12 restored), home animation pause/resume, company ribbon pause/resume, FAQ expansion, and local Back to top verified.
- Offscreen artwork pauses and resumes when visible again. The masterclass diagnostic observer recorded zero intersection while the artwork was above the screen.
- Local 15-second animation samples at 390px after correcting transparency: homepage average requestAnimationFrame interval 8.48–9.94ms, p95 8.4–16.2ms, 1–5 intervals over 50ms and one long task. The slower sample overlapped local build activity. The masterclass scrolling sample averaged 8.35ms, p95 8.6ms, with zero intervals over 50ms and zero long tasks. Observed layout shifts were 0 and 0.0001 respectively. These samples demonstrate the limits of a local check; they do not establish a guaranteed frame rate.

These timings were collected in the desktop browser at an emulated mobile viewport and approximately 120Hz. They measure main-thread animation scheduling, not GPU presentation. Physical Android/iOS performance, production network latency, host load and cold starts still need verification on the deployed URL. Existing browser connection errors during deliberate local server restarts are excluded from runtime checks.

## Repeat after deploying

1. Run `node scripts/check-asset-delivery.mjs https://your-site.example` to check compression, cache headers, conditional requests and dynamic response isolation. This performs only read requests and does not create registrations.
2. Test first and repeat visits on an actual phone, including scrolling, opening the mobile menu, searching programs and opening enrollment. Verify the real WebSocket connection through the hosting proxy.
3. Keep the website, API and database close to the intended users and to each other. Maintain the existing private storage and session-key configuration.
4. Free Render services can sleep after 15 minutes of inactivity and take about a minute to wake. Asset optimizations do not remove that host-level wait. See [Render's free service limits](https://render.com/docs/free#spinning-down-on-idle).

Local-only diagnostics: open a development page with `?perf`, then inspect the JSON in `#nextgen-performance`. The probe records a 15-second sample and is never loaded in Production. Regenerate committed images with Pillow using `python scripts/optimize-images.py` when artwork changes. Deployment does not require Python or regenerate images.

Implementation references: [ASP.NET Core compression](https://learn.microsoft.com/en-us/aspnet/core/performance/response-compression?view=aspnetcore-10.0), [animation performance](https://web.dev/articles/animations-guide), [requestAnimationFrame](https://developer.mozilla.org/en-US/docs/Web/API/Window/requestAnimationFrame).
