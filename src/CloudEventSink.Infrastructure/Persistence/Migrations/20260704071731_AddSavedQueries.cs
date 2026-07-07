using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudEventSink.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedQueries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "query_folders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<string>(
                        type: "character varying(256)",
                        maxLength: 256,
                        nullable: false
                    ),
                    CreatedAtUtc = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_query_folders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_query_folders_query_folders_ParentId",
                        column: x => x.ParentId,
                        principalTable: "query_folders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "saved_queries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    Description = table.Column<string>(
                        type: "character varying(1000)",
                        maxLength: 1000,
                        nullable: true
                    ),
                    FolderId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Mode = table.Column<string>(
                        type: "character varying(16)",
                        maxLength: 16,
                        nullable: false
                    ),
                    Sql = table.Column<string>(type: "text", nullable: true),
                    ModelJson = table.Column<string>(type: "jsonb", nullable: true),
                    RenderConfigJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedBy = table.Column<string>(
                        type: "character varying(256)",
                        maxLength: 256,
                        nullable: false
                    ),
                    CreatedAtUtc = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_queries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_saved_queries_query_folders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "query_folders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_query_folders_ParentId",
                table: "query_folders",
                column: "ParentId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_saved_queries_CreatedBy",
                table: "saved_queries",
                column: "CreatedBy"
            );

            migrationBuilder.CreateIndex(
                name: "IX_saved_queries_FolderId",
                table: "saved_queries",
                column: "FolderId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "saved_queries");

            migrationBuilder.DropTable(name: "query_folders");
        }
    }
}
