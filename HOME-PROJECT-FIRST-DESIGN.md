# Project-first homepage (option 2)

The homepage order is: existing NextGen SQL/AI hero, featured interactive project previews, static skills strip, compact six-program catalogue, online classroom section, FAQ and shared footer.

The page uses the existing six course slugs and catalogue, without new course categories from the mockup. Local course summaries render immediately; a four-second cancellation deadline bounds the live catalogue refresh. The large background chooser, initial-avatar row, marquee and fallback testimonials are removed from this homepage layout.

## Artwork

- Original: `src/NextGenCoaching.Web/wwwroot/images/home-online-classroom.png`
- Optimized: `src/NextGenCoaching.Web/wwwroot/images/home-online-classroom.webp` (149,984 bytes)
- Generated with the built-in image_gen tool, using the official NextGen logo as a reference.
- This is an illustrative online-learning scene, not a portrait or testimonial of a named instructor or student.

### Final generation prompt

Use case: stylized-concept. Standalone homepage online-learning illustration for NextGen. Create a premium friendly glossy 3D scene matching a colorful data science education website: a young adult Indian woman learner in a lavender hoodie wearing headphones at a laptop, participating in a live online class. A prominent floating rounded video-call window beside her shows a friendly Indian male mentor with glasses and a blue shirt explaining a small SQL/data chart. Physical laptop, tidy books, small plant, one coral chat bubble and soft blue cloud motif. NextGen official logo from the reference is clearly but tastefully printed on the learner's laptop lid or a visible notebook, naturally following perspective. Minimal pale lavender/ivory studio background, cobalt/lavender/coral accents, soft lighting and polished 3D materials. Wide landscape 4:3 framing with all heads and laptop visible. NO website UI, no invented claims, no readable text except 'NEXTGEN' and small 'SQL' if needed. No speech bubble sentences. Compact cohesive illustration suitable for a homepage pan-India online-learning panel.
Course-detail requests also use a four-second cancellation deadline. If the API is unavailable, each known course slug receives its corresponding catalogue details; unknown slugs return not-found instead of the flagship course.
