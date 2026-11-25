using PdfSharp.Fonts;

namespace PdfAnchorStamp;

public static class FontSetup
{
    public static void EnsureDefaults()
    {
        if (GlobalFontSettings.FontResolver is not null)
        {
            return;
        }

        if (OperatingSystem.IsWindows())
        {
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;
        }
    }
}
