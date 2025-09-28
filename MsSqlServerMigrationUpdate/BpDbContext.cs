using Microsoft.EntityFrameworkCore;

namespace MsSqlServerMigrationUpdate {
    public class BpDbContext(DbContextOptions<BpDbContext> options) : DbContext(options)
    {
        
    }
}