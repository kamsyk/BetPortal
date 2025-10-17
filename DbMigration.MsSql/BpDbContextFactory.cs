using System.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DbMigration.MsSql
{
    internal class BpDbContextFactory : IDesignTimeDbContextFactory<BpDbContext>
    {
        public BpDbContext CreateDbContext(string[] args)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["MsSqlConnectionString"]?.ConnectionString;

            var options = new DbContextOptionsBuilder<BpDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            return new BpDbContext(options);
        }
    }
}
