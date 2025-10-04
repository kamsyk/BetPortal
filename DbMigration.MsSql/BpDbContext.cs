

using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DbMigration.MsSql
{
    public class BpDbContext(DbContextOptions<BpDbContext> options) : DbContext(options)
    {
        public DbSet<BpUser> BpUsers { get; set; }
    }
}
