using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NextGenCoaching.Core.Persistence;

/// <summary>
/// Used only by the <c>dotnet ef</c> tooling at design time to construct an
/// <see cref="AppDbContext"/> without booting the web host. It mirrors the options that affect
/// the generated schema — chiefly the snake_case convention and the migrations assembly — so
/// migrations match what the app builds at runtime.
///
/// The connection string is irrelevant for <c>migrations add</c> (no DB connection is made) and
/// is used as-is for <c>database update</c>. Override it with the
/// <c>ConnectionStrings__DefaultConnection</c> environment variable when generating against a
/// specific database.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                 ?? "Host=localhost;Port=5432;Database=nextgen_coaching;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(cs, npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }
}
