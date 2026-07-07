using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Generator.Domain.Migrations.Project
{
    /// <inheritdoc />
    public partial class RemovingService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropTable(
                name: "ServiceLayers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceLayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Control = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceLayers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RelatedEntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    ServiceLayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Control = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Services_Entities_RelatedEntityId",
                        column: x => x.RelatedEntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Services_ServiceLayers_ServiceLayerId",
                        column: x => x.ServiceLayerId,
                        principalTable: "ServiceLayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ServiceLayers",
                columns: new[] { "Id", "Control", "Name" },
                values: new object[,]
                {
                    { 1, false, "Core" },
                    { 2, false, "Model" },
                    { 3, false, "DataAccess" },
                    { 4, false, "Business" },
                    { 5, false, "Presentation" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Services_RelatedEntityId",
                table: "Services",
                column: "RelatedEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_ServiceLayerId_RelatedEntityId",
                table: "Services",
                columns: new[] { "ServiceLayerId", "RelatedEntityId" },
                unique: true);
        }
    }
}
