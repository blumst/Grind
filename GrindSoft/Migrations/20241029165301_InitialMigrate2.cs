using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrindSoft.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigrate2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AuthorId",
                table: "Sessions",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<int>(
                name: "DelayBetweenMessages",
                table: "Sessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LastProcessedMessageId",
                table: "Sessions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MessageCount",
                table: "Sessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DelayBetweenMessages",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "LastProcessedMessageId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "MessageCount",
                table: "Sessions");

            migrationBuilder.AlterColumn<string>(
                name: "AuthorId",
                table: "Sessions",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
