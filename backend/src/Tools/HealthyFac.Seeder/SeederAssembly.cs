using System.Reflection;

namespace HealthyFac.Seeder;

public static class SeederAssembly
{
    public static readonly Assembly Instance = typeof(SeederAssembly).Assembly;
}
