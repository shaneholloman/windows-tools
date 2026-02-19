# removebg

Removes the background from an image using [rembg](https://github.com/danielgatis/rembg) with the `birefnet-portrait` model.

## Usage

```
removebg <image_file>
```

Output is saved alongside the input with `_nobg` appended to the filename (e.g. `photo_nobg.jpg`).

## Dependencies

Python package: `rembg[gpu]` (installed by `deps.ps1`; falls back to CPU-only `rembg` if GPU install fails).

## Notes

- Uses the `birefnet-portrait` model, which is well-suited for photos of people.
- The model weights are downloaded automatically on first run (~1 GB).
