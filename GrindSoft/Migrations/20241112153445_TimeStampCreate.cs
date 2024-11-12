using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrindSoft.Migrations
{
    /// <inheritdoc />
    public partial class TimeStampCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessedMessageIds",
                table: "Sessions");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastProcessedMessageTimestamp",
                table: "Sessions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastProcessedMessageTimestamp",
                table: "Sessions");

            migrationBuilder.AddColumn<string>(
                name: "ProcessedMessageIds",
                table: "Sessions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
