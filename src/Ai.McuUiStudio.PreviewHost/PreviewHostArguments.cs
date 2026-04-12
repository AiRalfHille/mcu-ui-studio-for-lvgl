namespace Ai.McuUiStudio.PreviewHost;

public static class PreviewHostArguments
{
    public static int ParsePort(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], "--port", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(args[i + 1], out var port))
            {
                return port;
            }
        }

        return 47831;
    }
}
