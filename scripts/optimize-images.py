"""Regenerate committed responsive artwork with Pillow: python scripts/optimize-images.py.

Original artwork is retained. Containers deploy the generated files without a Python dependency.
"""
from pathlib import Path
from PIL import Image, ImageOps

root = Path(__file__).resolve().parents[1] / "src/NextGenCoaching.Web/wwwroot/images"
output = root / "optimized"
output.mkdir(exist_ok=True)
heroes = ["career-journey", "colour-pop-ai", "contact-advisor-branded",
          "home-online-classroom", "learning-universe", "masterclass-collage"]
sources = [root / f"{name}.webp" for name in heroes] + sorted((root / "career").glob("*.jpg"))
total_before = total_after = 0
for source in sources:
    with Image.open(source) as original:
        image = ImageOps.exif_transpose(original)
        image = image.convert("RGBA" if "A" in image.getbands() else "RGB")
        for width in (480, 800, 1280):
            height = round(image.height * width / image.width)
            resized = image.resize((width, height), Image.Resampling.LANCZOS)
            path = output / f"{source.stem}-{width}.webp"
            resized.save(path, "WEBP", quality=84, method=6)
            if resized.mode == "RGBA" and resized.getchannel("A").getextrema()[0] < 255:
                with Image.open(path) as generated:
                    assert "A" in generated.getbands(), f"Lost transparency: {path.name}"
            if width == 800:
                total_before += source.stat().st_size
                total_after += path.stat().st_size
print(f"Generated {len(sources) * 3} responsive images. 800px set: {total_before:,} -> {total_after:,} bytes ({1-total_after/total_before:.0%} less).")
