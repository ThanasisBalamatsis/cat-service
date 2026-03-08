using Domain.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Diagnostics.CodeAnalysis;

namespace Infrastructure.Persistence.Configurations;

[ExcludeFromCodeCoverage]
internal sealed class CatFetchJobConfiguration : IEntityTypeConfiguration<CatFetchJob>
{
    public void Configure(EntityTypeBuilder<CatFetchJob> builder)
    {
        builder.ToTable("CatFetchJobs");
        builder.HasKey(j => j.Id);

        builder.Property(j => j.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(j => j.ErrorMessage)
            .HasMaxLength(500);

        builder.Property(j => j.CompletedAt);

        builder.Property(j => j.CatsFetched)
            .IsRequired();

        builder.Property(j => j.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();
    }
}
