using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace MsSqlServerMigrationUpdate
{
    internal class BpDbContextFactory : IDesignTimeDbContextFactory<BpDbContext> {
        public BpDbContext CreateDbContext(string[] args) {
            // 1) Jednoduše: natvrdo spojovací řetězec (rychlé pro běh migrací)
            var connectionString =
                "Server=localhost;Initial Catalog=BetPortal;Integrated Security=True;TrustServerCertificate=true;";

            // 2) Nebo si ho načti z env proměnné:
            // var connectionString = Environment.GetEnvironmentVariable("BPDB_CS") ?? "...fallback...";

            var options = new DbContextOptionsBuilder<BpDbContext>()
                .UseSqlServer(connectionString)  // nebo Npgsql/MySQL dle vašeho provideru
                .Options;

            return new BpDbContext(options);
        }
    }
}
