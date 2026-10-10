using BpDbLayer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

public sealed class ApiFixture : IAsyncLifetime
{
    private const string MigrationAssemblyName = "DbMigration.MsSql";
    private const string LocalDbServer = @"(localdb)\MSSQLLocalDB";
    private const string ConnectionStringEnvironmentVariable = "ConnectionStrings__BetPortalDbConnectionString";
    private static readonly Dictionary<string, string> GoogleEnvironment = new()
    {
        ["Authentication__Google__ClientId"] = "test-client-id",
        ["Authentication__Google__ClientSecret"] = "test-client-secret",
        ["Authentication__Google__FrontendOrigin"] = "http://localhost:4200",
    };
    private readonly Dictionary<string, string?> _previousGoogleEnvironment = new();
    private readonly string _databaseName = $"BetPortal_IntegrationTests_{Guid.NewGuid():N}";
    private readonly string _masterConnectionString = new SqlConnectionStringBuilder
    {
        DataSource = LocalDbServer,
        InitialCatalog = "master",
        IntegratedSecurity = true,
        TrustServerCertificate = true,
        ConnectTimeout = 30,
    }.ConnectionString;
    private string _databaseConnectionString = string.Empty;
    private DbContextOptions<BpDbContext>? _dbContextOptions;
    private ApiFactory? _factory;
    private string? _previousConnectionString;
    private bool _connectionStringEnvironmentOverridden;
    private bool _databaseCreated;

    public async ValueTask InitializeAsync()
    {
        var databaseConnectionString = new SqlConnectionStringBuilder
        {
            DataSource = LocalDbServer,
            InitialCatalog = _databaseName,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            MultipleActiveResultSets = true,
            ConnectTimeout = 30,
        };
        _databaseConnectionString = databaseConnectionString.ConnectionString;

        await CreateDatabaseAsync();

        try
        {
            var migrationDbContextOptions = new DbContextOptionsBuilder<DbMigration.MsSql.BpDbContext>()
                .UseSqlServer(_databaseConnectionString, sql => sql.MigrationsAssembly(MigrationAssemblyName))
                .Options;

            await using (var migrationDbContext = new DbMigration.MsSql.BpDbContext(migrationDbContextOptions))
            {
                await migrationDbContext.Database.MigrateAsync();
            }

            _dbContextOptions = new DbContextOptionsBuilder<BpDbContext>()
                .UseSqlServer(_databaseConnectionString, sql => sql.MigrationsAssembly(MigrationAssemblyName))
                .Options;

            _previousConnectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
            Environment.SetEnvironmentVariable(ConnectionStringEnvironmentVariable, _databaseConnectionString);
            _connectionStringEnvironmentOverridden = true;

            // Program.cs reads Google settings at startup, before WebApplicationFactory in-memory configuration is applied.
            foreach (var (name, value) in GoogleEnvironment)
            {
                _previousGoogleEnvironment[name] = Environment.GetEnvironmentVariable(name);
                Environment.SetEnvironmentVariable(name, value);
            }

            _factory = new ApiFactory(_databaseConnectionString);
            using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                HandleCookies = false,
                AllowAutoRedirect = false,
            });
        }
        catch (Exception initializationException)
        {
            Exception? cleanupException = null;
            try
            {
                if (_factory is not null)
                {
                    await _factory.DisposeAsync();
                    _factory = null;
                }
            }
            catch (Exception exception)
            {
                cleanupException = exception;
            }

            RestoreConnectionStringEnvironment();

            try
            {
                await DropDatabaseAsync();
            }
            catch (Exception exception)
            {
                cleanupException = cleanupException is null
                    ? exception
                    : new AggregateException(cleanupException, exception);
            }

            if (cleanupException is not null)
            {
                throw new AggregateException(
                    "Integration database initialization failed and cleanup also failed.",
                    initializationException,
                    cleanupException);
            }

            throw;
        }
    }

    internal ApiTestScope CreateTestScope()
    {
        if (_factory is null)
        {
            throw new InvalidOperationException("The API fixture has not been initialized.");
        }

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = false,
            AllowAutoRedirect = false,
        });
        return new ApiTestScope(this, client);
    }

    internal BpDbContext CreateDbContext()
    {
        if (_dbContextOptions is null)
        {
            throw new InvalidOperationException("The integration database is not initialized.");
        }

        return new BpDbContext(_dbContextOptions);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_factory is not null)
            {
                await _factory.DisposeAsync();
                _factory = null;
            }
        }
        finally
        {
            RestoreConnectionStringEnvironment();
            await DropDatabaseAsync();
        }
    }

    private void RestoreConnectionStringEnvironment()
    {
        foreach (var (name, value) in _previousGoogleEnvironment)
        {
            Environment.SetEnvironmentVariable(name, value);
        }

        _previousGoogleEnvironment.Clear();

        if (!_connectionStringEnvironmentOverridden)
        {
            return;
        }

        Environment.SetEnvironmentVariable(ConnectionStringEnvironmentVariable, _previousConnectionString);
        _connectionStringEnvironmentOverridden = false;
    }

    private async Task CreateDatabaseAsync()
    {
        await using var connection = new SqlConnection(_masterConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE [{_databaseName}]";
        await command.ExecuteNonQueryAsync();
        _databaseCreated = true;
    }

    private async Task DropDatabaseAsync()
    {
        if (!_databaseCreated)
        {
            return;
        }

        SqlConnection.ClearAllPools();

        await using var connection = new SqlConnection(_masterConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}]";
        await command.ExecuteNonQueryAsync();
        _databaseCreated = false;
    }

    private sealed class ApiFactory(string databaseConnectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:BetPortalDbConnectionString"] = databaseConnectionString,
                });
            });
        }
    }
}