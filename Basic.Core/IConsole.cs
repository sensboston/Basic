namespace Basic.Core;

public interface IConsole
{
    void Write(string text);
    void WriteLine(string text);
    void WriteLine();
    string? ReadLine();
    void Clear();
    string ReadKey();  // Returns pressed key or empty string if none available
}

public class SystemConsole : IConsole
{
    public void Write(string text) => Console.Write(text);
    public void WriteLine(string text) => Console.WriteLine(text);
    public void WriteLine() => Console.WriteLine();
    public string? ReadLine() => Console.ReadLine();

    public void Clear()
    {
        try
        {
            Console.Clear();
        }
        catch (IOException)
        {
            // Console.Clear() fails if not running in interactive console
        }
    }

    public string ReadKey()
    {
        try
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);
                return key.KeyChar.ToString();
            }
        }
        catch (InvalidOperationException)
        {
            // Not running in interactive console
        }
        return "";
    }
}
