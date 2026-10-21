using System.Runtime.InteropServices;

namespace PdfToolkit;

/// <summary>Small Windows API helpers, so we don't need WinForms or WPF.</summary>
internal static partial class Native
{
    private const uint MB_OK = 0x0;
    private const uint MB_ICONERROR = 0x10;
    private const uint MB_ICONINFORMATION = 0x40;

    [LibraryImport("user32.dll", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    // The same "natural" comparison File Explorer uses to sort names.
    [LibraryImport("shlwapi.dll", EntryPoint = "StrCmpLogicalW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int StrCmpLogical(string a, string b);

    public static void ShowError(string title, string message) =>
        MessageBox(IntPtr.Zero, message, title, MB_OK | MB_ICONERROR);

    public static void ShowInfo(string title, string message) =>
        MessageBox(IntPtr.Zero, message, title, MB_OK | MB_ICONINFORMATION);

    public static int CompareNaturally(string a, string b) =>
        StrCmpLogical(Path.GetFileName(a), Path.GetFileName(b));
}
