using DRM.Data;
using DRM.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    //h
    public DbSet<AudioFile> AudioFiles { get; set; }
    public DbSet<VideoFile> VideoFiles { get; set; }
    public DbSet<PdfFile> PdfFiles { get; set; }

    public DbSet<Student> Students { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Required for Identity tables

        modelBuilder.Entity<AudioFile>(entity =>
        {
            entity.ToTable("AudioFiles");
            entity.Property(e => e.Lock).HasDefaultValue(false); // Default to false
        });

        modelBuilder.Entity<VideoFile>(entity =>
        {
            entity.ToTable("VideoFiles");
            entity.Property(e => e.Lock).HasDefaultValue(false); // Default to false
        });

        modelBuilder.Entity<PdfFile>(entity =>
        {
            entity.ToTable("PdfFiles");
            entity.Property(e => e.Lock).HasDefaultValue(false); // Default to false
        });

      

    }
}
