using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniNest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixTermsAcceptanceUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TermsAcceptances_UserId_TermsDocumentId",
                table: "TermsAcceptances");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "TermsAcceptances",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TermsAcceptances_UserId_TermsDocumentId",
                table: "TermsAcceptances",
                columns: new[] { "UserId", "TermsDocumentId" },
                unique: true,
                filter: "[DeletedAt] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TermsAcceptances_UserId_TermsDocumentId",
                table: "TermsAcceptances");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "TermsAcceptances");

            migrationBuilder.CreateIndex(
                name: "IX_TermsAcceptances_UserId_TermsDocumentId",
                table: "TermsAcceptances",
                columns: new[] { "UserId", "TermsDocumentId" },
                unique: true);
        }
    }
}
