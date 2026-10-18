# Contact advisor artwork

The selected Contact page uses a new standalone illustration generated with the built-in image_gen tool. Text, links, form controls, floating labels and motion controls are rendered separately by the website.

- Original asset: `src/NextGenCoaching.Web/wwwroot/images/contact-advisor.png`
- Optimized website asset: `src/NextGenCoaching.Web/wwwroot/images/contact-advisor.webp` (134,674 bytes)
- Page: `src/NextGenCoaching.Web/Components/Pages/Contact.razor`
- The original company phone and email details are retained. The physical training hub section was replaced by pan-India online learning copy.

## Final generation prompt

Use case: stylized-concept. Asset type: website contact hero illustration, standalone image only, NO website UI, NO words, NO letters, NO logos, NO watermark. Create a beautiful glossy high-end 3D animated-film-style scene for an Indian Data Science, AI and cloud education brand: a friendly young Indian female academic advisor with wavy dark hair, blue blouse and a lanyard seated at a laptop, speaking warmly with a young adult Indian female student in a lavender sweater who is listening attentively. Desk with small white mug and a tidy stack of books in blue and lavender, no visible text anywhere. Two characters in close medium framing, complete heads, upper bodies, hands, laptop and desk visible. Soft pale lavender and ivory minimal studio backdrop, subtle blurred foliage on far edges, soft daylight. Cobalt blue, coral, lavender and ivory palette. Keep top fifth mostly empty for separately implemented floating bubbles. Wide landscape composition around 4:3, subjects fill frame without cropping faces or laptop. Use real depth, smooth materials, expressive natural gestures, warm welcoming mood. NO embedded chat bubbles, NO floating technology icons because these will be added separately in code.

## Branding revision

The live page now uses `src/NextGenCoaching.Web/wwwroot/images/contact-advisor-branded.webp`; the lossless edit is saved as `contact-advisor-branded.png`. Both originals remain available. Edited with the built-in image_gen tool using the original illustration as edit target and `nextgen-official-logo.jpeg` as the brand reference.

### Final edit prompt

Use case: precise-object-edit. Image 1 is the edit target: the existing NextGen advisor and student hero illustration. Image 2 is the official NextGen logo reference, its blue and dark NG mark above NEXTGEN wordmark. Make a precise branding update to image 1. Add a clearly visible authentic official NG / NEXTGEN logo decal centered on the visible back of the silver laptop. Add the same official logo neatly printed on the front of the white mug at lower left. Make the advisor's lanyard a clearly visible blue NextGen branded lanyard, with repeated small legible NEXTGEN lettering. Give her a realistic white employee ID badge with a small portrait, the NextGen logo, and the short role text 'ADVISOR'. Move the badge attachment slightly higher on her torso so the badge is visible above the desk and beside the laptop, without covering her hand or face. Preserve the exact two characters, faces, expressions, hairstyles, clothing, body poses, laptop shape, mug shape, desk, books, background, colors, lighting and overall framing. Do not add any website interface, chat bubbles or unrelated text. Branding should be crisp, realistically printed on the physical surfaces, properly sized and naturally follow perspective. Keep both people unchanged. Output the updated standalone illustration.
