using System.Text.Json;
using HealthyFac.Seeder.Moments;
using HealthyFac.Shared.Infrastructure.Messaging;

namespace HealthyFac.Seeder.SeedFiles;

/// <summary>
/// The folder of the seed files. It reads each file with the JSON options of the API (camelCase, the
/// <c>Enumeration&lt;T&gt;</c> values, the text input rules) and the relative moments. A member that the type does not
/// declare, a missing required member, and a wrong type fail with the file path and the JSON path.
/// </summary>
public sealed class SeedFolder
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    private readonly string _path;

    public SeedFolder(string path)
    {
        _path = path;
    }

    /// <summary>Reads one JSON file of the folder.</summary>
    /// <typeparam name="T">The type of the file content.</typeparam>
    /// <param name="relativePath">The path of the file inside the folder.</param>
    /// <returns>The content.</returns>
    /// <exception cref="SeedFileException">The file is missing or does not match <typeparamref name="T"/>.</exception>
    public T Read<T>(string relativePath)
    {
        var path = Path.Combine(_path, relativePath);
        if (!File.Exists(path))
        {
            throw new SeedFileException(SeedFileErrors.FileMissing(path));
        }

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options)
                ?? throw new SeedFileException(
                    SeedFileErrors.FileInvalid(path, jsonPath: null, reason: "The file is null."));
        }
        catch (JsonException exception)
        {
            throw new SeedFileException(SeedFileErrors.FileInvalid(path, exception.Path, exception.Message), exception);
        }
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(MessagingJsonOptions.Instance)
        {
            UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
        };
        options.Converters.Add(new RelativeMomentJsonConverter());
        options.MakeReadOnly();

        return options;
    }
}
