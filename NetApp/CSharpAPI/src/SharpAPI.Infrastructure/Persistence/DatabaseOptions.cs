namespace SharpAPI.Infrastructure.Persistence;

/// <summary>Which database engine the app talks to.</summary>
public enum DatabaseProvider
{
    /// <summary>Production: SQL Server or Azure SQL. The schema comes from the scripts in /SQLServerDb.</summary>
    SqlServer,

    /// <summary>
    /// Zero-install local development and integration tests. A file or in-memory DB is
    /// created and seeded with the same Northwind data on startup.
    /// </summary>
    Sqlite
}

/// <summary>
/// Strongly-typed settings bound from the "Database" section of appsettings.json.
/// </summary>
/// <remarks>
/// <para><b>The Options pattern:</b> code asks for <c>IOptions&lt;DatabaseOptions&gt;</c>, not
/// <c>IConfiguration["Database:Provider"]</c> magic strings. It is typed and
/// validated at startup (<c>ValidateOnStart</c>), so a missing connection string crashes
/// the app on deploy, not hours later on the first request ("fail fast").</para>
/// <para>Secrets (passwords) never belong in appsettings.json. Use environment
/// variables, user-secrets, Azure Key Vault or Managed Identity.</para>
/// </remarks>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public DatabaseProvider Provider { get; init; } = DatabaseProvider.SqlServer;

    /// <summary>Name of the entry under "ConnectionStrings".</summary>
    public string ConnectionStringName { get; init; } = "Catalog";

    public int CommandTimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Retries for TRANSIENT SQL errors (failover, throttling, network blips), which are
    /// normal in the cloud. EF Core retries with exponential back-off.
    /// </summary>
    public int MaxRetryCount { get; init; } = 5;

    /// <summary>Create and seed the schema on startup (SQLite only; SQL Server uses the scripts).</summary>
    public bool InitializeOnStartup { get; init; }

    /// <summary>Logs SQL parameter VALUES. Handy when debugging; NEVER enable it in production (PII leak).</summary>
    public bool EnableSensitiveDataLogging { get; init; }
}
