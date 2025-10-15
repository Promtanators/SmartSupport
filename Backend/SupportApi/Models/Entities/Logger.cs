namespace SupportApi.Models.Entities;

public static class Logger
{
    private static ILogger _logger;

    public static void Initialization(ILogger logger)
    {
        _logger = logger;
    }

    public static void LogInformation(string message, params object[] args)
    {
        _logger.LogInformation(message, args);
    }

    public static void LogWarning(string message, params object[] args)
    {
        _logger.LogWarning(message, args);
    }

    public static void LogError(Exception ex, string message, params object[] args)
    {
        _logger.LogError(ex.Message, message, args);
    }
}