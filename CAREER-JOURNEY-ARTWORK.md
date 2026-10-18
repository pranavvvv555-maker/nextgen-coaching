# NextGen career journey artwork

The Our Story hero uses the approved career-journey concept and tagline: **NextGen — Big dreams. Brighter futures.**

The four figures are 5,000 students placed, ₹35 LPA highest CTC, 200+ hiring partners, and 4.6 / 5 alumni rating, as provided by the user. This redesign changes presentation; it does not introduce a new source of placement data.

## Saved assets and method

- Generated using the **built-in image generation tool**, editing the selected reference into a transparent illustration.
- Runtime image: `src/NextGenCoaching.Web/wwwroot/images/career-journey.webp` (244,932 bytes).
- Original PNG: `src/NextGenCoaching.Web/wwwroot/images/career-journey.png` (1254 × 1254).
- PNG generation output preserved at `C:/Users/prana/.codex/generated_images/01a11cf9-2705-7362-babc-bdff498bf41b/exec-7c7a6c1f-9236-4acc-b78c-7e19a7b7f480.png`.
- Pillow was used only for WebP format conversion and compression.

## Animation and layout

`CareerJourneyScene.razor` places SVG flow paths over the transparent artwork. The local Motion runtime uses a ten-second timeline: learning packets travel from SQL across the pathway, the AI chip responds with light, then output packets and an ascending arrow travel to the achievement badge. Glowing milestone rings, floating artwork, sparkles, active stage labels, and spring entrances complete the motion.

Playback pauses when hidden or offscreen. The keyboard-accessible pause button preserves the frame; reduced-motion mode shows the complete static illustration with all three milestones highlighted. Controllers clean up after internal navigation. Hero copy, CTAs, controls, labels, and statistics support English, Marathi, and Hindi. Scoped light/dark styling keeps headings and pastel/tinted metrics readable.

## Generation prompt

Edit the reference into a standalone premium 3D career journey illustration for NextGen's website. Extract ONLY the right-hand winding ascending pathway sculpture with the young male student wearing a backpack, friendly female industry mentor at laptop, blue SQL database and books/cloud at the bottom left, coral AI chip halfway up, and lime green final platform with a gold graduation cap and achievement medal at the top right. Preserve the gorgeous glossy polished coral, electric blue, lavender and lime 3D style of the reference. Recompose those parts to fill a balanced square image, all elements fully within canvas, sculpture climbing diagonally bottom-left to upper-right. Include two small floating glass analytics cards. Make the winding road clearly connected through all stages. No website headings, paragraphs, navigation, buttons, numbers, stats tiles, no backdrop landscape, no large background panels, no dotted luminous trail (we will animate that in code). Only SQL and AI labels on corresponding objects may remain. Transparent background around the sculpture, clean silhouette, no floor extending outside the scene; beautiful lit 3D surfaces usable on both pale ivory and deep navy backgrounds.

## Verification

Build: .NET and compiled Tailwind CSS. Browser checks: 36 theme/language/viewport combinations, requested figures, translation, animation sequence, climbing arrow, keyboard pause/resume, reduced-motion mode, offscreen pause, approach anchor and internal navigation. Screenshots and browser results are saved under `D:/NG website/audit/career-journey-*`.
