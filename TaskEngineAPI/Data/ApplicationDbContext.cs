using Microsoft.EntityFrameworkCore;
using TaskEngineAPI.DTO;


namespace TaskEngineAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Role> Roles { get; set; }
    }
}
