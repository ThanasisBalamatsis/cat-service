using Domain.Cats;
using Domain.Tags;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Diagnostics.CodeAnalysis;

namespace Infrastructure.Persistence.Configurations;

[ExcludeFromCodeCoverage]
internal sealed class CatConfiguration : IEntityTypeConfiguration<Cat>
{
    public void Configure(EntityTypeBuilder<Cat> builder)
    {
        builder.ToTable("Cats");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CatId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Width)
            .IsRequired();

        builder.Property(c => c.Height)
            .IsRequired();

        builder.Property(c => c.ImageUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder
            .HasMany(c => c.Tags)
            .WithMany(t => t.Cats)
            .UsingEntity("Tag_Cat",
                left => left.HasOne(typeof(Tag))
                    .WithMany()
                    .HasForeignKey("TagId")
                    .HasPrincipalKey(nameof(Tag.Id))
                    .HasConstraintName("FK_TagCat_Tag")
                    .OnDelete(DeleteBehavior.Cascade),
                right => right.HasOne(typeof(Cat))
                    .WithMany()
                    .HasForeignKey("CatId")
                    .HasPrincipalKey(nameof(Cat.Id))
                    .HasConstraintName("FK_TagCat_Cat")
                    .OnDelete(DeleteBehavior.Cascade),
                linkBuilder => linkBuilder.HasKey("TagId", "CatId"));

        builder.HasIndex(c => c.CatId)
            .IsUnique()
            .HasDatabaseName("IX_cats_catId");
    }
}
