﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrindSoft.Migrations
{
    /// <inheritdoc />
    public partial class Mode2Create : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TargetBotUsername",
                table: "Sessions",
                newName: "TargetUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TargetUserId",
                table: "Sessions",
                newName: "TargetBotUsername");
        }
    }
}