using System.Diagnostics;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace HealthyFac.Shared.Infrastructure.Persistence;

/// <summary>
/// Provides the tracing setup that every service context shares.
/// </summary>
public static class NpgsqlTracingExtensions
{
    extension(NpgsqlDbContextOptionsBuilder npgsql)
    {
        /// <summary>
        /// Records an SQL span only when an activity is running. A client span without a parent is an idle poll
        /// (e.g., the claim query of the inbox and outbox processors) or other background noise with no business
        /// work, and each one would become a root trace. SQL inside an HTTP request, a use case, or a
        /// <c>process</c> span keeps its span. The claim query of a row that is then processed also gets no span,
        /// because it runs before <c>process</c> starts: the <c>process</c> span covers the handling. A physical
        /// connection open (<c>CONNECT ...</c>) is not a command, so the filters do not cover it. It is rare. The
        /// filters read <see cref="Activity.Current"/> when Npgsql starts the command, so it is the activity of
        /// the caller, not an Npgsql activity.
        /// </summary>
        /// <returns>The same builder, so calls can chain.</returns>
        public NpgsqlDbContextOptionsBuilder ConfigureParentOnlyTracing() =>
            npgsql.ConfigureDataSource(dataSource => dataSource.ConfigureTracing(tracing => tracing
                .ConfigureCommandFilter(_ => Activity.Current is not null)
                .ConfigureBatchFilter(_ => Activity.Current is not null)));
    }
}
