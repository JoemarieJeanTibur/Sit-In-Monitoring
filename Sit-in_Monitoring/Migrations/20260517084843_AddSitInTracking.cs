using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sit_in_Monitoring.Migrations
{
    /// <inheritdoc />
    public partial class AddSitInTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCurrentlyCheckedIn",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCheckIn",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCheckOut",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionsUsed",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalSessionsAllowed",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SitIns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CheckInTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckOutTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationInMinutes = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SitIns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SitIns_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SitIns_UserId",
                table: "SitIns",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SitIns");

            migrationBuilder.DropColumn(
                name: "IsCurrentlyCheckedIn",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastCheckIn",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastCheckOut",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SessionsUsed",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TotalSessionsAllowed",
                table: "AspNetUsers");
        }
    }
}
