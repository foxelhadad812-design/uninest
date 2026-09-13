using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UniNest.Infrastructure;

public sealed class UniNestDbContextFactory : IDesignTimeDbContextFactory<UniNestDbContext>
{
    public UniNestDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UniNestDbContext>();
        optionsBuilder.UseSqlServer("Server=MOHAMED-ELHADDA;Database=UniNestDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true");

        return new UniNestDbContext(optionsBuilder.Options);
    }
}
