using System.Globalization;
using System.Text.Json;
using PdfAnchorStamp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

return Run(args);

static int Run(string[] args)
{
    if (args.Length == 0 || args[0] is "-h" or "--help")
    {
        PrintUsage();
        return 1;
    }

    var opts = ParseOptions(args.Skip(1));
    try
    {
        return args[0] switch
        {
            "find" => Find(opts),
            "stamp" => Stamp(opts),
            "make-sample" => MakeSample(opts),
            _ => Unknown(args[0])
        };
    }
    catch (Exception ex) when (ex is ArgumentException or FileNotFoundException or InvalidOperationException)
    {
        Console.Error.WriteLine($"error: {ex.Message}");
        return 2;
    }
}

static int Find(Dictionary<string, string> o)
{
    var anchors = AnchorFinder.Find(Require(o, "pdf"), Require(o, "marker"), FindOptionsFrom(o));
    foreach (var a in anchors)
    {
        Console.WriteLine(JsonSerializer.Serialize(a));
    }

    Console.Error.WriteLine($"{anchors.Count} anchor(s) found.");
    return anchors.Count == 0 ? 3 : 0;
}

static int Stamp(Dictionary<string, string> o)
{
    var spec = new StampSpec
    {
        ImagePath = o.GetValueOrDefault("image"),
        Text = o.GetValueOrDefault("text"),
        FontFamily = o.GetValueOrDefault("font") ?? "Arial",
        FontSize = Double(o, "font-size") ?? 12,
        Color = o.GetValueOrDefault("color") ?? "#000000",
        Width = Double(o, "width"),
        Height = Double(o, "height"),
        OffsetX = Double(o, "dx") ?? 0,
        OffsetY = Double(o, "dy") ?? 0,
        Placement = Enum.Parse<Placement>(o.GetValueOrDefault("place") ?? "OnMarker", ignoreCase: true)
    };

    var count = AnchorStamper.Run(Require(o, "pdf"), Require(o, "out"), Require(o, "marker"), spec, FindOptionsFrom(o));
    Console.Error.WriteLine($"{count} stamp(s) written to {o["out"]}.");
    return count == 0 ? 3 : 0;
}

static int MakeSample(Dictionary<string, string> o)
{
    var path = o.GetValueOrDefault("out") ?? "sample.pdf";
    FontSetup.EnsureDefaults();
    using var document = new PdfDocument();
    var page = document.AddPage();
    using var gfx = XGraphics.FromPdfPage(page);
    var title = new XFont("Arial", 20);
    var body = new XFont("Arial", 11);
    var hidden = new XFont("Arial", 4);

    gfx.DrawString("Work Order #2026-0142", title, XBrushes.Black, new XPoint(60, 80));
    gfx.DrawString("Customer approval required before the vehicle leaves the workshop.", body, XBrushes.Black, new XPoint(60, 120));
    gfx.DrawString("Visible marker:", body, XBrushes.Black, new XPoint(60, 500));
    gfx.DrawString("{{SIGN_HERE}}", body, XBrushes.Gray, new XPoint(160, 500));
    gfx.DrawString("Invisible marker is on this line, white text at 4pt:", body, XBrushes.Black, new XPoint(60, 600));
    gfx.DrawString("{{STAMP}}", hidden, XBrushes.White, new XPoint(360, 600));
    document.Save(path);
    Console.Error.WriteLine($"Sample written to {path}.");
    return 0;
}

static FindOptions FindOptionsFrom(Dictionary<string, string> o) => new()
{
    IgnoreCase = o.ContainsKey("ignore-case"),
    WordContains = o.ContainsKey("contains"),
    FirstOnly = o.ContainsKey("first-only"),
    Pages = o.TryGetValue("pages", out var pages)
        ? pages.Split(',').Select(p => int.Parse(p, CultureInfo.InvariantCulture)).ToHashSet()
        : null
};

static Dictionary<string, string> ParseOptions(IEnumerable<string> args)
{
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    var list = args.ToList();
    for (var i = 0; i < list.Count; i++)
    {
        if (!list[i].StartsWith("--", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unexpected argument '{list[i]}'.");
        }

        var key = list[i][2..];
        var hasValue = i + 1 < list.Count && !list[i + 1].StartsWith("--", StringComparison.Ordinal);
        result[key] = hasValue ? list[++i] : "true";
    }

    return result;
}

static string Require(Dictionary<string, string> o, string key) =>
    o.TryGetValue(key, out var value) ? value : throw new ArgumentException($"--{key} is required.");

static double? Double(Dictionary<string, string> o, string key) =>
    o.TryGetValue(key, out var value) ? double.Parse(value, CultureInfo.InvariantCulture) : null;

static int Unknown(string command)
{
    Console.Error.WriteLine($"Unknown command '{command}'.");
    PrintUsage();
    return 1;
}

static void PrintUsage()
{
    Console.Error.WriteLine("""
        pdf-anchor-stamp

        Commands:
          find        --pdf in.pdf --marker "{{SIGN_HERE}}" [--ignore-case] [--contains] [--first-only] [--pages 1,2]
          stamp       --pdf in.pdf --out out.pdf --marker "{{SIGN_HERE}}" (--image stamp.png | --text "APPROVED")
                      [--width pt] [--height pt] [--dx pt] [--dy pt] [--place OnMarker|Above|Below|Right|Left]
                      [--font Arial] [--font-size 12] [--color #RRGGBB] [--ignore-case] [--contains] [--first-only] [--pages 1,2]
          make-sample [--out sample.pdf]

        Coordinates are PDF points (1/72 inch). Exit codes: 0 ok, 1 usage, 2 error, 3 no anchor found.
        """);
}
