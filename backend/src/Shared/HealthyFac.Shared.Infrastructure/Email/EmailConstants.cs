namespace HealthyFac.Shared.Infrastructure.Email;

public static class EmailConstants
{
    public const int DefaultPort = 587;

    public static class Timeout
    {
        public const int DefaultSeconds = 30;
        public const int MinSeconds = 1;
        public const int MaxSeconds = 300;
    }
}
