namespace Watershield.MuhammadTests;

// little helper so each test gets its own temp csv and cleans it up after
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
