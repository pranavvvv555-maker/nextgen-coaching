# Mobile layout improvements

The public site keeps its existing design, brand illustrations, programme links, and content. On phones, sections follow a single-column reading order and artwork keeps its proportions. The 12 career features remain three columns on desktop, two on tablet, and one on phones.

## Changes

- Shared header: 44px menu and appearance buttons, readable language selection, larger masterclass link, and a scrollable mobile menu for short landscape screens. Escape closes it and returns keyboard focus; navigating closes it too.
- Inputs and text areas: 16px text on mobile, full available width, and single-column contact fields, avoiding the small text that triggers focus zoom in iOS browsers.
- Home: larger project choices, wrapping FAQ questions, more readable descriptions and programme tiles, proportionate artwork, and roomier footer links.
- Programs: readable search text, touch-sized filters, wrapping prices, and immediate catalogue rendering. The catalogue refresh still uses API data when available; filters run locally against the current catalogue without loading placeholders.
- Career support: one complete feature card per phone row, readable deliverables and descriptions, and larger filter buttons.
- Our story: balanced two-column statistics on phones, readable labels, and fitted logos for reduced-motion layouts.
- Contact and course details: readable fields, wrapping breadcrumbs, consistent card padding, and email addresses that fit small screens.
- Existing animation controls remain available with larger touch targets. Reduced-motion preferences remain respected.

## Verification

Playwright checks use Chromium (Microsoft Edge), mobile viewport/touch emulation, both themes, and English, Marathi, and Hindi. This is browser emulation, not a physical-device or Safari test.

Main pages are checked at 320, 360, 390, 430, 540, 768, 1024, and 1440px. The menu is also checked at 844×390px landscape. Course details, sign-in, sign-up, masterclass, checkout, privacy, and terms receive additional 320px checks.

Checks compare layout width to the requested viewport width to detect unintended mobile auto-scaling. They also cover content containment, field text size, touch targets, filters, project selection, FAQ expansion, navigation, theme persistence, and animation controls. No enquiry or payment was submitted.

Scripts and reports are in `D:/NG website/audit/`:

- `check-mobile-site.cjs` and `mobile-site-checks.json`
- `check-mobile-interactions.cjs` and `mobile-interaction-checks.json`
- `capture-mobile-previews.cjs` and `mobile-viewport-metrics.json`

Previews are actual local browser screenshots, not generated design mockups.
