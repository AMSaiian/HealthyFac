namespace HealthyFac.Seeder.Reset;

public static class ResetConstants
{
    public const string NamesParameter = "names";
    public const string OwnParameter = "own";

    public const string BusySessionSql =
        "select datname from pg_stat_activity where datname = any(@names) and pid <> pg_backend_pid() "
        + "and application_name <> @own and backend_type = 'client backend' "
        + "order by datname limit 1";
}
