using System.IO.Pipes;

namespace PdfToolkit;

/// <summary>
/// Turns "Explorer started N copies of the app" into "one copy with N files".
///
/// The first copy grabs a named mutex and listens on a named pipe. Every later
/// copy fails to grab the mutex, sends its file path(s) down the pipe, and exits.
/// The first copy stops listening once no new path has arrived for a short
/// quiet period, then returns the full list.
/// </summary>
internal static class SingleInstance
{
    private const string MutexName = @"Local\PdfToolkit.Merge";
    private const string PipeName = "PdfToolkit.Merge";
    private static readonly TimeSpan QuietPeriod = TimeSpan.FromMilliseconds(800);
    private const int ClientConnectTimeoutMs = 5000;

    // Kept in a static field so the mutex lives as long as the process.
    private static Mutex? _mutex;

    /// <returns>
    /// All collected file paths if this copy is the collector;
    /// null if it handed its paths to the collector and should just exit.
    /// </returns>
    public static List<string>? CollectFiles(string[] args)
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out bool isFirst);

        if (!isFirst)
        {
            SendToCollector(args);
            return null;
        }

        var files = new List<string>(args);
        ListenForMore(files);
        return files;
    }

    private static void ListenForMore(List<string> files)
    {
        while (true)
        {
            using var server = new NamedPipeServerStream(
                PipeName, PipeDirection.In, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            using var cts = new CancellationTokenSource(QuietPeriod);
            try
            {
                server.WaitForConnectionAsync(cts.Token).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                return; // Nobody else showed up in time: we have every file.
            }

            using var reader = new StreamReader(server);
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                if (!string.IsNullOrWhiteSpace(line)) files.Add(line);
            }
        }
    }

    private static void SendToCollector(string[] args)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(ClientConnectTimeoutMs);
            using var writer = new StreamWriter(client);
            foreach (var path in args) writer.WriteLine(path);
        }
        catch (Exception ex)
        {
            Native.ShowError("PDF Toolkit",
                $"Couldn't hand this file to the merge window:\n{string.Join("\n", args)}\n\n{ex.Message}");
        }
    }
}
