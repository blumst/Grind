using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrindSoft.Migrations
{
    /// <inheritdoc />
    public partial class messageids : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProcessedMessageIds",
                table: "Sessions",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessedMessageIds",
                table: "Sessions");
        }
    }
}
