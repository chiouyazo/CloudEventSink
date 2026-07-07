using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudEventSink.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectionSpec : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SpecJson",
                table: "schema_projections",
                type: "jsonb",
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "SpecJson", table: "schema_projections");
        }
    }
}
