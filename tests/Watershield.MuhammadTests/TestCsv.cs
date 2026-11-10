namespace Watershield.MuhammadTests;

/// <summary>Writes CSV text to a temp file that is deleted when disposed.</summary>
internal sealed class TestCsv : IDisposable
{
    public TestCsv(string content)
    {
        Path = System.IO.Path.GetTempFileName();
        File.WriteAllText(Path, content);
    }

    public string Path { get; }

    public void Dispose() => File.Delete(Path);
}
