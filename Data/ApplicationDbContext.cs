using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Receptoria.API.Models;

namespace Receptoria.API.Data;

public class ApplicationDbContext : IdentityDbContext<ReceptoriaUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Recipe> Recipes { get; set; } = default!;
    public DbSet<Step> Steps { get; set; } = default!;
    public DbSet<Rating> Ratings { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Recipe>()
            .HasOne(r => r.Author)
            .WithMany()
            .HasForeignKey(r => r.AuthorId)
            .IsRequired();

        modelBuilder.Entity<Rating>(entity =>
       {
           entity.HasIndex(r => new { r.RecipeId, r.UserId }).IsUnique();
       });
    }
}