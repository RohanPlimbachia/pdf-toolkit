using System.Diagnostics;

namespace PdfToolkit;

/// <summary>
/// Entry point. Explorer launches one copy of this program PER selected file,
/// so the first copy to start becomes the "collector": it gathers the paths
/// from all the other copies (see SingleInstance) and then merges them once.
/// </summary>
internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Native.ShowInfo("PDF Toolkit",
                "Select PDFs or images in File Explorer, right-click, and choose \"Merge into PDF\".");
            return 0;
        }

        // Some copies just hand their file to the collector and exit.
        var files = SingleInstance.CollectFiles(args);
        if (files is null) return 0;

        try
        {
            var inputs = files
                .Select(Path.GetFullPath)
                .Where(File.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Sort the way Explorer does ("page 2" before "page 10").
            inputs.Sort(Native.CompareNaturally);

            if (inputs.Count == 0)
            {
                Native.ShowError("PDF Toolkit", "None of the selected files could be found.");
                return 1;
            }

            if (inputs.Count == 1 && Merger.IsPdf(inputs[0]))
            {
                Native.ShowInfo("PDF Toolkit", "Select two or more files to merge.");
                return 0;
            }

            var output = Merger.NextFreeOutputPath(inputs[0]);
            Merger.Merge(inputs, output);

            // Open File Explorer with the new PDF selected.
            Process.Start("explorer.exe", $"/select,\"{output}\"");
            return 0;
        }
        catch (Exception ex)
        {
            Native.ShowError("PDF Toolkit — merge failed", ex.Message);
            return 1;
        }
    }
}
