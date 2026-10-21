using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PdfToolkit;

/// <summary>Combines PDFs and images, in order, into one new PDF.</summary>
internal static class Merger
{
    private static readonly HashSet<string> ImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png" };

    // US Letter in points (1 pt = 1/72 inch). Change to 595 x 842 for A4.
    private const double PageShortSide = 612;
    private const double PageLongSide = 792;
    private const double Margin = 18; // 1/4 inch

    public static bool IsPdf(string path) =>
        Path.GetExtension(path).Equals(".pdf", StringComparison.OrdinalIgnoreCase);

    public static bool IsImage(string path) =>
        ImageExtensions.Contains(Path.GetExtension(path));

    public static void Merge(IReadOnlyList<string> inputs, string outputPath)
    {
        using var output = new PdfDocument();
        output.Info.Title = Path.GetFileNameWithoutExtension(outputPath);
        output.Info.Creator = "PDF Toolkit";

        foreach (var path in inputs)
        {
            if (IsPdf(path)) AppendPdf(output, path);
            else if (IsImage(path)) AppendImage(output, path);
            else throw new NotSupportedException($"Unsupported file type: {Path.GetFileName(path)}");
        }

        output.Save(outputPath);
    }

    private static void AppendPdf(PdfDocument output, string path)
    {
        PdfDocument source;
        try
        {
            source = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        }
        catch (Exception ex)
        {
            // Password-protected and damaged PDFs end up here.
            throw new InvalidOperationException(
                $"Couldn't open \"{Path.GetFileName(path)}\". It may be password-protected or damaged.\n({ex.Message})", ex);
        }

        using (source)
        {
            foreach (var page in source.Pages)
                output.AddPage(page);
        }
    }

    /// <summary>Puts one image on its own Letter page, scaled to fit and centred.</summary>
    private static void AppendImage(PdfDocument output, string path)
    {
        using var image = XImage.FromFile(path);

        bool landscape = image.PointWidth > image.PointHeight;
        var page = output.AddPage();
        page.Width = XUnit.FromPoint(landscape ? PageLongSide : PageShortSide);
        page.Height = XUnit.FromPoint(landscape ? PageShortSide : PageLongSide);

        double boxW = page.Width.Point - 2 * Margin;
        double boxH = page.Height.Point - 2 * Margin;
        double scale = Math.Min(boxW / image.PointWidth, boxH / image.PointHeight);
        scale = Math.Min(scale, 1.0); // never blow small images up past their real size

        double w = image.PointWidth * scale;
        double h = image.PointHeight * scale;
        double x = (page.Width.Point - w) / 2;
        double y = (page.Height.Point - h) / 2;

        using var gfx = XGraphics.FromPdfPage(page);
        gfx.DrawImage(image, x, y, w, h);
    }

    /// <summary>"Merged.pdf" next to the first file, or "Merged (2).pdf" etc. if taken.</summary>
    public static string NextFreeOutputPath(string firstInput)
    {
        var folder = Path.GetDirectoryName(firstInput)!;
        var candidate = Path.Combine(folder, "Merged.pdf");
        for (int n = 2; File.Exists(candidate); n++)
            candidate = Path.Combine(folder, $"Merged ({n}).pdf");
        return candidate;
    }
}
