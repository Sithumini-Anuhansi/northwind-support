using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SupportDesk.Infrastructure.Data;

#nullable disable

namespace SupportDesk.Infrastructure.Data.Migrations;

[DbContext(typeof(SupportDbContext))]
[Migration("20261016000000_AddTicketEscalations")]
public sealed class AddTicketEscalations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TicketEscalations",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                TicketId = table.Column<int>(type: "int", nullable: false),
                FromPriority = table.Column<int>(type: "int", nullable: false),
                ToPriority = table.Column<int>(type: "int", nullable: false),
                FromAgentId = table.Column<int>(type: "int", nullable: true),
                ToAgentId = table.Column<int>(type: "int", nullable: true),
                FromDueAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                ToDueAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                EscalatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                EscalatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TicketEscalations", escalation => escalation.Id);
                table.CheckConstraint("CK_TicketEscalations_FromPriority", "[FromPriority] BETWEEN 1 AND 4");
                table.CheckConstraint("CK_TicketEscalations_ToPriority", "[ToPriority] BETWEEN 1 AND 4");
                table.ForeignKey(
                    "FK_TicketEscalations_Agents_FromAgentId",
                    escalation => escalation.FromAgentId,
                    "Agents",
                    "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_TicketEscalations_Agents_ToAgentId",
                    escalation => escalation.ToAgentId,
                    "Agents",
                    "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_TicketEscalations_Tickets_TicketId",
                    escalation => escalation.TicketId,
                    "Tickets",
                    "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_TicketEscalations_TicketId_EscalatedAtUtc",
            table: "TicketEscalations",
            columns: new[] { "TicketId", "EscalatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_TicketEscalations_FromAgentId",
            table: "TicketEscalations",
            column: "FromAgentId");

        migrationBuilder.CreateIndex(
            name: "IX_TicketEscalations_ToAgentId",
            table: "TicketEscalations",
            column: "ToAgentId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "TicketEscalations");
}
