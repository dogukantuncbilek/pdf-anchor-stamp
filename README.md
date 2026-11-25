# pdf-anchor-stamp

[![ci](https://github.com/dogukantuncbilek/pdf-anchor-stamp/actions/workflows/ci.yml/badge.svg)](https://github.com/dogukantuncbilek/pdf-anchor-stamp/actions/workflows/ci.yml)

Find a text marker inside a PDF and stamp an image or text at that exact spot.

The problem it solves: a report engine (SSRS, Crystal, a Liquid/HTML template, anything) produces a PDF, and later a process has to put a signature, an "APPROVED" stamp or a QR code on it, at a position that moves with the layout. Instead of guessing coordinates, the template carries a marker such as `{{SIGN_HERE}}`, visible or invisible (white, tiny font). This tool finds the marker's bounding box and draws the stamp relative to it.

- Marker search with [PdfPig](https://github.com/UglyToad/PdfPig): exact or case-insensitive, whole word or substring, multi-word markers, page filter, first match only.
- Drawing with [PDFsharp 6.2](https://docs.pdfsharp.net/) (MIT): image stamps with aspect-ratio scaling, text stamps with font/size/colour, placement on/above/below/left/right of the marker plus offsets.
- Library (`PdfAnchorStamp`) and a small CLI. .NET 8, no native dependencies.

## Quick start

```bash
dotnet run --project src/PdfAnchorStamp.Cli -- make-sample --out sample.pdf
dotnet run --project src/PdfAnchorStamp.Cli -- find  --pdf sample.pdf --marker "{{SIGN_HERE}}"
dotnet run --project src/PdfAnchorStamp.Cli -- stamp --pdf sample.pdf --out signed.pdf --marker "{{SIGN_HERE}}" --image samples/stamp.png --width 120
dotnet run --project src/PdfAnchorStamp.Cli -- stamp --pdf sample.pdf --out approved.pdf --marker "{{STAMP}}" --text "APPROVED" --font-size 18 --color "#1A7F37" --place Above
```

`find` prints one JSON object per anchor, in PDF points with the origin at the bottom-left of the page:

```json
{"PageNumber":1,"Text":"{{SIGN_HERE}}","Left":160,"Bottom":342,"Width":78.17,"Height":8.01,"PageWidth":595,"PageHeight":842,"Right":238.17,"Top":350.01,"TopFromPageTop":491.99}
```

## Library

```csharp
using PdfAnchorStamp;

var anchors = AnchorFinder.Find("workorder.pdf", "{{SIGN_HERE}}", new FindOptions { IgnoreCase = true });

var spec = new StampSpec
{
    ImagePath = "stamp.png",
    Width = 120,
    Placement = Placement.Above,
    OffsetY = 4
};

var count = PdfStamper.Stamp("workorder.pdf", "workorder-signed.pdf", anchors, spec);
```

`AnchorStamper.Run(input, output, marker, spec, options)` does both steps in one call and returns the number of stamps written. When no anchor is found nothing is written and the result is `0`.

## CLI reference

```
find        --pdf in.pdf --marker "{{SIGN_HERE}}" [--ignore-case] [--contains] [--first-only] [--pages 1,2]
stamp       --pdf in.pdf --out out.pdf --marker "{{SIGN_HERE}}" (--image stamp.png | --text "APPROVED")
            [--width pt] [--height pt] [--dx pt] [--dy pt] [--place OnMarker|Above|Below|Right|Left]
            [--font Arial] [--font-size 12] [--color #RRGGBB] [--ignore-case] [--contains] [--first-only] [--pages 1,2]
make-sample [--out sample.pdf]
```

Exit codes: `0` ok, `1` usage, `2` error, `3` no anchor found.

## Invisible markers

Put the marker in the template as white text in a very small font (for example 2 to 4 pt). It is not visible on screen or paper, but it is still text inside the content stream, so PdfPig finds it. The stamp is drawn on top; the marker itself stays in the file. If the marker must not be extractable afterwards, flatten the page to an image as a separate step.

## Fonts

Text stamps and `make-sample` need a font. On Windows the library enables PDFsharp's Windows font lookup automatically. On Linux or macOS set `PdfSharp.Fonts.GlobalFontSettings.FontResolver` to a resolver that serves your own TTF files before calling the stamper; image stamps need no font at all.

## Notes

- Coordinates follow PDF conventions: points (1/72 inch), origin bottom-left. `Anchor.TopFromPageTop` is provided for top-left based drawing.
- PDFsharp is pinned to 6.2.x, the last MIT-licensed line; 6.5 switched to AGPL with a licence key.
- Rotated pages and markers split across lines are not handled yet.

## License

MIT
