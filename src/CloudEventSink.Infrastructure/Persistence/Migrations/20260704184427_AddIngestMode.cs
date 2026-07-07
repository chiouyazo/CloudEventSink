using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudEventSink.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIngestMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_events_SourceId_EventId", table: "events");

            migrationBuilder.AddColumn<string>(
                name: "DedupKeyPaths",
                table: "sources",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "Mode",
                table: "sources",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "IgnoreDuplicateById"
            );

            migrationBuilder.AddColumn<string>(
                name: "DedupKey",
                table: "events",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true
            );

            migrationBuilder.Sql("UPDATE events SET \"DedupKey\" = \"EventId\";");

            migrationBuilder.CreateIndex(
                name: "IX_events_SourceId_DedupKey",
                table: "events",
                columns: new[] { "SourceId", "DedupKey" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_events_SourceId_EventId",
                table: "events",
                columns: new[] { "SourceId", "EventId" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_events_SourceId_DedupKey", table: "events");

            migrationBuilder.DropIndex(name: "IX_events_SourceId_EventId", table: "events");

            migrationBuilder.DropColumn(name: "DedupKeyPaths", table: "sources");

            migrationBuilder.DropColumn(name: "Mode", table: "sources");

            migrationBuilder.DropColumn(name: "DedupKey", table: "events");

            migrationBuilder.CreateIndex(
                name: "IX_events_SourceId_EventId",
                table: "events",
                columns: new[] { "SourceId", "EventId" },
                unique: true
            );
        }
    }
}
