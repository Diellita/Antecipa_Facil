using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WebApi.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            var conn = "Host=localhost;Port=5432;Database=antecipafacil;Username=postgres;Password=postgres";
            optionsBuilder.UseNpgsql(conn);

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
