namespace HealthyFac.Testing.Exceptions;

public static class ExceptionCapture
{
    public static async Task<Exception?> CaptureAsync(Func<Task> action)
    {
        try
        {
            await action();

            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
}
