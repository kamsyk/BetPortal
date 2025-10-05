using BpDbLayer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DbMigration.MySql
{
    internal class BpDbContextFactory : IDesignTimeDbContextFactory<BpDbContext>
    {
        public BpDbContext CreateDbContext(string[] args)
        {
            var connectionString = string.Empty;

            var options = new DbContextOptionsBuilder<BpDbContext>()
                .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)))
                .Options;

            return new BpDbContext(options);
        }
    }
}
