"""Export the specialist catalogue's native SVG scenes as lightweight image URLs.

The interactive website renders the same SVG inline to reuse its offscreen-aware
animations. These standalone files supply course-image metadata and previews.
"""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
source = (root / 'src/NextGenCoaching.Web/Components/Shared/CourseIllustration.razor').read_text(encoding='utf8')
output = root / 'src/NextGenCoaching.Web/wwwroot/images/courses'
output.mkdir(exist_ok=True)
common = source[source.index('        <circle cx="180"'):source.index('        @switch')]
palette = {
    'advanced-ai': ('#fff6ef', '#f9dfd1', '#d49b89'),
    'machine-learning': ('#f0faf5', '#d9f0e5', '#6eb59b'),
    'generative-ai': ('#f8f4fd', '#e9dff7', '#ad96cc'),
    'agentic-ai': ('#f2f7ff', '#dfeaf9', '#86a9d0'),
    'databricks': ('#fff9ee', '#f3e7c9', '#c3ab83'),
}
for slug, (background, wash, accent) in palette.items():
    match = re.search(r'case "' + slug + r'":\s*([\s\S]*?)\s*break;', source)
    if not match:
        raise ValueError('Missing scene: ' + slug)
    scene = common + match.group(1)
    scene = scene.replace('var(--art-wash, #eef2ff)', wash).replace('var(--art-accent, #4263f5)', accent)
    svg = f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 360 200" width="360" height="200"><rect width="360" height="200" fill="{background}"/>{scene}</svg>\n'
    ET.fromstring(svg)
    path = output / (slug + '.svg')
    path.write_text(svg, encoding='utf8')
    print(f'{path.name}: {path.stat().st_size:,} bytes')
