namespace PdfAnchorStamp;

public static class AnchorStamper
{
    public static int Run(string inputPath, string outputPath, string marker, StampSpec spec, FindOptions? options = null)
    {
        var anchors = AnchorFinder.Find(inputPath, marker, options);
        return PdfStamper.Stamp(inputPath, outputPath, anchors, spec);
    }
}
