"""
img-to-svg - convert a raster image to SVG.

Engines:
  vtracer       (default) fast local vectorizer, works on any image
  starvector-1b AI model, best for icons/logos/diagrams, needs ~3 GB VRAM
  starvector-8b AI model, best quality for icons/logos/diagrams, needs ~16 GB VRAM

Vtracer presets (--preset, only used with vtracer engine):
  poster  (default) logos, icons, flat colour illustrations
  photo             photographs, gradients, fine detail
  bw                black and white line art / sketches

Usage:
  img-to-svg logo.png
  img-to-svg photo.webp --preset photo
  img-to-svg icon.png --engine starvector-8b
  img-to-svg icon.png out.svg --engine starvector-1b --max-length 2000
"""
import sys
import os
import argparse
import tempfile


VTRACER_PRESETS = {
    "poster": dict(
        colormode="color",
        color_precision=8,
        layer_difference=16,
        filter_speckle=4,
        corner_threshold=60,
        length_threshold=4,
        max_iterations=10,
        splice_threshold=45,
        path_precision=3,
    ),
    "photo": dict(
        colormode="color",
        color_precision=8,
        layer_difference=48,
        filter_speckle=10,
        corner_threshold=180,
        length_threshold=4,
        max_iterations=10,
        splice_threshold=45,
        path_precision=3,
    ),
    "bw": dict(
        colormode="binary",
        color_precision=8,
        layer_difference=16,
        filter_speckle=4,
        corner_threshold=60,
        length_threshold=4,
        max_iterations=10,
        splice_threshold=45,
        path_precision=3,
    ),
}

# vtracer handles these natively; everything else gets pre-converted via Pillow
VTRACER_NATIVE_EXTS = {".png", ".jpg", ".jpeg"}


def run_vtracer(input_path, output_path, preset):
    try:
        import vtracer
    except ImportError:
        print("Error: vtracer is not installed. Run: pip install vtracer")
        sys.exit(1)

    try:
        from PIL import Image
    except ImportError:
        print("Error: Pillow is not installed. Run: pip install Pillow")
        sys.exit(1)

    params = VTRACER_PRESETS[preset]
    ext = os.path.splitext(input_path)[1].lower()

    tmp_path = None
    vtracer_input = input_path

    if ext not in VTRACER_NATIVE_EXTS:
        print(f"  Note:   {ext} pre-converted to PNG (vtracer only reads PNG/JPEG natively)")
        img = Image.open(input_path).convert("RGB")
        tmp_fd, tmp_path = tempfile.mkstemp(suffix=".png")
        os.close(tmp_fd)
        img.save(tmp_path, format="PNG")
        vtracer_input = tmp_path

    try:
        vtracer.convert_image_to_svg_py(vtracer_input, output_path, **params)
    finally:
        if tmp_path and os.path.exists(tmp_path):
            os.remove(tmp_path)


def run_starvector(input_path, output_path, model_size, max_length):
    model_name = f"starvector/starvector-{model_size}-im2svg"
    vram_note = "~16 GB VRAM" if model_size == "8b" else "~3 GB VRAM"

    print(f"  Engine: StarVector {model_size.upper()}  ({vram_note})")
    print(f"  Note:   Best for icons, logos, and diagrams - not photos")

    try:
        import torch
    except ImportError:
        print("Error: PyTorch is not installed. Run deps.ps1 to set up StarVector.")
        sys.exit(1)

    try:
        from starvector.model.starvector_arch import StarVectorForCausalLM
    except ImportError:
        print("Error: starvector package not found.")
        print("Run deps.ps1 to clone and install the star-vector repo.")
        sys.exit(1)

    try:
        from PIL import Image
    except ImportError:
        print("Error: Pillow is not installed.")
        sys.exit(1)

    if not torch.cuda.is_available():
        print("Error: CUDA GPU required for StarVector. No CUDA device detected.")
        sys.exit(1)

    vram_gb = torch.cuda.get_device_properties(0).total_memory / 1024**3
    print(f"  GPU:    {torch.cuda.get_device_name(0)}  ({vram_gb:.0f} GB)")
    print()
    print(f"  Loading model from HuggingFace (downloads ~17 GB on first run)...")

    starvector = StarVectorForCausalLM.from_pretrained(model_name, torch_dtype=torch.float16)
    starvector.cuda()
    starvector.eval()

    image_pil = Image.open(input_path).convert("RGB")
    image = starvector.process_images([image_pil])[0].cuda()
    batch = {"image": image}

    print(f"  Generating SVG (max_length={max_length})...")
    with torch.no_grad():
        raw_svg = starvector.generate_im2svg(batch, max_length=max_length)[0]

    # Extract the <svg>...</svg> block from the model output
    if "<svg" in raw_svg:
        raw_svg = raw_svg[raw_svg.index("<svg"):]
    if "</svg>" in raw_svg:
        raw_svg = raw_svg[: raw_svg.rindex("</svg>") + len("</svg>")]

    with open(output_path, "w", encoding="utf-8") as f:
        f.write(raw_svg)


def main():
    parser = argparse.ArgumentParser(
        description="Convert a raster image to SVG",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Engines:
  vtracer       (default) fast, works on any image
  starvector-1b AI model for icons/logos/diagrams (~3 GB VRAM)
  starvector-8b AI model, higher quality          (~16 GB VRAM)

Vtracer presets (--preset):
  poster  (default)  logos, icons, flat colour
  photo              photographs and gradients
  bw                 black and white line art

Examples:
  img-to-svg logo.png
  img-to-svg photo.webp --preset photo
  img-to-svg icon.png --engine starvector-8b
""",
    )
    parser.add_argument("input", help="Input image file")
    parser.add_argument("output", nargs="?", help="Output SVG (default: same name, .svg extension)")
    parser.add_argument(
        "--engine",
        choices=["vtracer", "starvector-1b", "starvector-8b"],
        default="vtracer",
        help="Conversion engine (default: vtracer)",
    )
    parser.add_argument(
        "--preset",
        choices=["poster", "photo", "bw"],
        default="poster",
        help="vtracer preset (default: poster, ignored for starvector engines)",
    )
    parser.add_argument(
        "--max-length",
        type=int,
        default=4000,
        help="Max SVG token length for starvector (default: 4000)",
    )
    args = parser.parse_args()

    input_path = args.input
    if not os.path.exists(input_path):
        print(f"Error: File not found: {input_path}")
        sys.exit(1)

    output_path = args.output or (os.path.splitext(input_path)[0] + ".svg")

    if args.engine == "vtracer":
        print(f"Converting to SVG  [vtracer / {args.preset} preset]")
    else:
        print(f"Converting to SVG")
    print(f"  Input:  {input_path}")
    print(f"  Output: {output_path}")

    if args.engine == "vtracer":
        run_vtracer(input_path, output_path, args.preset)
    else:
        model_size = args.engine.split("-")[1]  # "1b" or "8b"
        run_starvector(input_path, output_path, model_size, args.max_length)

    size = os.path.getsize(output_path)
    print()
    print(f"Done!  {output_path}  ({size:,} bytes)")


if __name__ == "__main__":
    main()
