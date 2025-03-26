using DRM.Data;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    //h
    public DbSet<AudioFile> AudioFiles { get; set; }
    public DbSet<VideoFile> VideoFiles { get; set; }
    public DbSet<PdfFile> PdfFiles { get; set; }
    public DbSet<AssignUser> AssignUsers { get; set; } // Added AssignUser table

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

        modelBuilder.Entity<AssignUser>(entity =>
        {
            entity.ToTable("AssignUsers");

            entity.HasOne(a => a.User)
                  .WithMany()
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.VideoFile)
                  .WithMany()
                  .HasForeignKey(a => a.VideoId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(a => a.PdfFile)
                  .WithMany()
                  .HasForeignKey(a => a.PdfId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(a => a.AudioFile)
                  .WithMany()
                  .HasForeignKey(a => a.AudioId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
