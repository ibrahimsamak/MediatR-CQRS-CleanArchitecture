
namespace OrderFlow.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Infrastructure.Persistence.Outbox;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasMaxLength(500);
        builder.Property(x => x.Content).HasColumnType("nvarchar(max)");
        // Index to let the dispatcher poll unprocessed rows cheaply.
        builder.HasIndex(x => x.ProcessedOnUtc);
    }
}
