using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DbMigration.MsSql
{
    internal class BpDbContextFactory : IDesignTimeDbContextFactory<BpDbContext>
    {
        public BpDbContext CreateDbContext(string[] args)
        {
            var connectionString =
                "Server=localhost;Initial Catalog=BetPortal;Integrated Security=True;TrustServerCertificate=true;";

            var options = new DbContextOptionsBuilder<BpDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            return new BpDbContext(options);
        }
    }
}
