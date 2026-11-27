namespace HealthyFac.Insights.Application.Chats.Tools;

/// <summary>
/// Reads the permissions of the token of the current request. The chat offers only the tools whose permissions the
/// token holds.
/// </summary>
public interface IChatPermissions
{
    /// <summary>
    /// Reads the permissions of the user of the current request.
    /// </summary>
    /// <returns>The permission names, for example <c>machines:read</c>. The list is empty outside a request.</returns>
    IReadOnlyCollection<string> Read();
}
