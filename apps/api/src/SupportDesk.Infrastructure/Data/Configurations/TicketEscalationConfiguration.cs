using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Infrastructure.Data.Configurations;

public sealed class TicketEscalationConfiguration : IEntityTypeConfiguration<TicketEscalation>
{
    public void Configure(EntityTypeBuilder<TicketEscalation> builder)
    {
        builder.ToTable("TicketEscalations", table =>
        {
            table.HasCheckConstraint("CK_TicketEscalations_FromPriority", "[FromPriority] BETWEEN 1 AND 4");
            table.HasCheckConstraint("CK_TicketEscalations_ToPriority", "[ToPriority] BETWEEN 1 AND 4");
        });

        builder.HasKey(escalation => escalation.Id);

        builder.Property(escalation => escalation.FromPriority).HasConversion<int>();
        builder.Property(escalation => escalation.ToPriority).HasConversion<int>();
        builder.Property(escalation => escalation.TicketId).IsRequired();
        builder.Property(escalation => escalation.FromAgentId).IsRequired(false);
        builder.Property(escalation => escalation.ToAgentId).IsRequired(false);
        builder.Property(escalation => escalation.Reason).HasMaxLength(500).IsRequired();
        builder.Property(escalation => escalation.EscalatedBy).HasMaxLength(100).IsRequired();
        builder.Property(escalation => escalation.FromDueAtUtc).HasColumnType("datetime2(3)").IsRequired(false);
        builder.Property(escalation => escalation.ToDueAtUtc).HasColumnType("datetime2(3)").IsRequired(false);
        builder.Property(escalation => escalation.EscalatedAtUtc).HasColumnType("datetime2(3)");

        builder.HasOne<Ticket>()
            .WithMany()
            .HasForeignKey(escalation => escalation.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Agent>()
            .WithMany()
            .HasForeignKey(escalation => escalation.FromAgentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Agent>()
            .WithMany()
            .HasForeignKey(escalation => escalation.ToAgentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Supports each ticket's newest-first escalation history without a separate sort index.
        builder.HasIndex(escalation => new { escalation.TicketId, escalation.EscalatedAtUtc })
            .HasDatabaseName("IX_TicketEscalations_TicketId_EscalatedAtUtc");

        builder.HasIndex(escalation => escalation.FromAgentId)
            .HasDatabaseName("IX_TicketEscalations_FromAgentId");

        builder.HasIndex(escalation => escalation.ToAgentId)
            .HasDatabaseName("IX_TicketEscalations_ToAgentId");
    }
}
