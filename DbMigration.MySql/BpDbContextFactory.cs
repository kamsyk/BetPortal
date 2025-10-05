using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DbMigration.MySql
{
    internal class BpDbContextFactory : IDesignTimeDbContextFactory<BpDbContext>
    {
        public BpDbContext CreateDbContext(string[] args)
        {
            var connectionString = "server=localhost;port=3306;database=betportal;user=root;password=yourpassword;";

            var options = new DbContextOptionsBuilder<BpDbContext>()
                .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)))
                .Options;

            return new BpDbContext(options);
        }
    }
}
