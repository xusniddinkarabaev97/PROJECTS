using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GzsBilling.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStationColumnToTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "Transactions");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:hstore", ",,");

            migrationBuilder.AddColumn<Guid>(
                name: "ColumnId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColumnName",
                table: "Transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StationId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StationName",
                table: "Transactions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColumnId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ColumnName",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "StationId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "StationName",
                table: "Transactions");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:hstore", ",,");

            migrationBuilder.AddColumn<Dictionary<string, string>>(
                name: "Metadata",
                table: "Transactions",
                type: "hstore",
                nullable: false);
        }
    }
}
