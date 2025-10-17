using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Configuration;

namespace DbMigration.MySql
{
    internal class BpDbContextFactory : IDesignTimeDbContextFactory<BpDbContext>
    {
        public BpDbContext CreateDbContext(string[] args)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["MySqlConnectionString"]?.ConnectionString;

            var options = new DbContextOptionsBuilder<BpDbContext>()
                .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)))
                .Options;

            return new BpDbContext(options);
        }
    }
}
