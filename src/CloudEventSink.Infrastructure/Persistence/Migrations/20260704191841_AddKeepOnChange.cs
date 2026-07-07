using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudEventSink.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKeepOnChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GroupKey",
                table: "events",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_events_SourceId_GroupKey_ReceivedAtUtc",
                table: "events",
                columns: new[] { "SourceId", "GroupKey", "ReceivedAtUtc" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_events_SourceId_GroupKey_ReceivedAtUtc",
                table: "events"
            );

            migrationBuilder.DropColumn(name: "GroupKey", table: "events");
        }
    }
}
