# Website appearance and language preferences

The ribbon at the top of the public website has a theme button and a native language selector: English, Marathi (mr), and Hindi (hi). Equivalent controls appear in the portal headers.

Preferences are scoped to each Blazor circuit and stored in the visitor's browser under `nextgen.site.preferences.v1`. The head script restores the theme before the stylesheet loads, including on nested routes. With no saved theme, the operating system preference is used. Invalid or unavailable storage does not prevent the controls from working.

Translations are UTF-8 entries in `src/NextGenCoaching.Web/Resources/Translations.json`. Each English source key maps to an array `[Marathi, Hindi]`. Use `@L["English source"]` in a Razor component, or `@L.Format("Text with {0}", value)` for dynamic values. All components receive the localized base class from `_Imports.razor`; layouts use `LocalizedLayoutBase`. A cascading revision updates existing components without reloading pages or losing form drafts.

The public navigation, marketing pages, course catalogue and details, career support, contact and registration forms, account screens, checkout interface, articles, privacy and terms are translated. Technical product names, original logo artwork, names, email addresses and user-supplied content remain as provided. New or externally supplied text without a catalogue entry falls back to its original text. Add entries for new content in both languages.

Dark styles and Devanagari typography are in `wwwroot/css/app.css`. Run `npm run build:css` from the Web project after stylesheet changes. Translations are an embedded resource; rebuild the Web project after changing the catalogue.

Validation covered all 14 public routes in all three languages and both themes, services at six viewport widths, reload persistence, nested course routes, keyboard controls, isolation between visitors, blocked/corrupt browser storage, translated course search, filter state, and preservation of form drafts.
