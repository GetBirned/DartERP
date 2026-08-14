using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DartERP.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDisposition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dispositions",
                columns: table => new
                {
                    DispositionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SerializedItemId = table.Column<int>(type: "int", nullable: false),
                    DispositionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dispositions", x => x.DispositionId);
                    table.ForeignKey(
                        name: "FK_Dispositions_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Dispositions_SerializedItems_SerializedItemId",
                        column: x => x.SerializedItemId,
                        principalTable: "SerializedItems",
                        principalColumn: "SerializedItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dispositions_CustomerId",
                table: "Dispositions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Dispositions_SerializedItemId",
                table: "Dispositions",
                column: "SerializedItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Dispositions");
        }
    }
}
