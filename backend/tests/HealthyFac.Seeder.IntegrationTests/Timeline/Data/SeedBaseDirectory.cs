using HealthyFac.Seeder.IntegrationTests.People.Data;

namespace HealthyFac.Seeder.IntegrationTests.Timeline.Data;

public static class SeedBaseDirectory
{
    private const string ServicesFolder = "services";

    /// <summary>Creates a base directory with the service settings and a given seed folder.</summary>
    /// <param name="seedFolder">The folder that becomes <c>seed/demo</c>.</param>
    /// <returns>The path of the new base directory.</returns>
    public static string Create(string seedFolder)
    {
        var target = Directory.CreateTempSubdirectory().FullName;
        Copy(Path.Combine(AppContext.BaseDirectory, ServicesFolder), Path.Combine(target, ServicesFolder));
        Copy(seedFolder, Path.Combine(target, PeopleTestData.SeedFolder));

        return target;
    }

    private static void Copy(string source, string target)
    {
        foreach (var directory in Directory.GetDirectories(source, searchPattern: "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(source, target));
        }

        Directory.CreateDirectory(target);

        foreach (var file in Directory.GetFiles(source, searchPattern: "*", SearchOption.AllDirectories))
        {
            File.Copy(file, file.Replace(source, target));
        }
    }
}
