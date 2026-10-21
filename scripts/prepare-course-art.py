"""Compress the selected built-in image-generation outputs into course assets.

Usage: python scripts/prepare-course-art.py <generated-image-directory>
Then run scripts/optimize-images.py to rebuild responsive variants.
"""
from pathlib import Path
import json
import sys
from PIL import Image, ImageOps

root = Path(__file__).resolve().parents[1]
brief = json.loads((root / 'docs/course-art.json').read_text(encoding='utf8'))
output = root / 'src/NextGenCoaching.Web/wwwroot/images/courses'
output.mkdir(parents=True, exist_ok=True)
source = Path(sys.argv[1])
for asset in brief['assets']:
    with Image.open(source / asset['source']) as raw:
        image = ImageOps.exif_transpose(raw).convert('RGB')
        image.save(output / (asset['slug'] + '.webp'), 'WEBP', quality=88, method=6)
    print(asset['slug'] + ': saved optimised course artwork')
