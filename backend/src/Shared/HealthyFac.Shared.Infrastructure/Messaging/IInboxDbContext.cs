using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.Infrastructure.Messaging;

/// <summary>
/// Marks a context that maps the inbox table, so a consumer can store a received message before it handles it.
/// </summary>
public interface IInboxDbContext
{
    DbSet<InboxMessage> InboxMessages { get; }
}
