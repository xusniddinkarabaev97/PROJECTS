using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GzsBilling.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "filling_stations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_filling_stations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ApiToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SslCertificateThumbprint = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SslCertificatePfxBase64 = table.Column<string>(type: "text", nullable: true),
                    WhiteIpAddresses = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SslCertificateExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "schetfakturalar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SystemCommission = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NetDistributionAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CalculationJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsAuthorized = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsPaid = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schetfakturalar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sverka_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReconciliationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IssueType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Details = table.Column<string>(type: "jsonb", nullable: false),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sverka_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "system_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "jsonb", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "manager"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "dispensers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FillingStationId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FuelType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "AI-92"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dispensers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dispensers_filling_stations_FillingStationId",
                        column: x => x.FillingStationId,
                        principalTable: "filling_stations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stakeholders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FillingStationId = table.Column<int>(type: "integer", nullable: false),
                    PaymentId = table.Column<int>(type: "integer", nullable: false),
                    BankAccount = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SharePercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stakeholders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stakeholders_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tranzaktsiyalar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalSum = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FillingStationId = table.Column<int>(type: "integer", nullable: false),
                    DispenserId = table.Column<int>(type: "integer", nullable: true),
                    CardType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PaymentId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tranzaktsiyalar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tranzaktsiyalar_dispensers_DispenserId",
                        column: x => x.DispenserId,
                        principalTable: "dispensers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tranzaktsiyalar_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "disbursement_tarixi",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StakeholderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BankReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TranzaksiyaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_disbursement_tarixi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_disbursement_tarixi_stakeholders_StakeholderId",
                        column: x => x.StakeholderId,
                        principalTable: "stakeholders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_disbursement_tarixi_tranzaktsiyalar_TranzaksiyaId",
                        column: x => x.TranzaksiyaId,
                        principalTable: "tranzaktsiyalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_disbursement_tarixi_StakeholderId",
                table: "disbursement_tarixi",
                column: "StakeholderId");

            migrationBuilder.CreateIndex(
                name: "IX_disbursement_tarixi_TranzaksiyaId",
                table: "disbursement_tarixi",
                column: "TranzaksiyaId");

            migrationBuilder.CreateIndex(
                name: "IX_dispensers_FillingStationId",
                table: "dispensers",
                column: "FillingStationId");

            migrationBuilder.CreateIndex(
                name: "IX_filling_stations_Name",
                table: "filling_stations",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_payments_Name",
                table: "payments",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_schetfakturalar_InvoiceDate",
                table: "schetfakturalar",
                column: "InvoiceDate");

            migrationBuilder.CreateIndex(
                name: "IX_stakeholders_FillingStationId_PaymentId",
                table: "stakeholders",
                columns: new[] { "FillingStationId", "PaymentId" });

            migrationBuilder.CreateIndex(
                name: "IX_stakeholders_PaymentId",
                table: "stakeholders",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_sverka_logs_ReconciliationDate",
                table: "sverka_logs",
                column: "ReconciliationDate");

            migrationBuilder.CreateIndex(
                name: "IX_system_settings_Category",
                table: "system_settings",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_system_settings_Key",
                table: "system_settings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tranzaktsiyalar_CreatedAt",
                table: "tranzaktsiyalar",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_tranzaktsiyalar_DispenserId",
                table: "tranzaktsiyalar",
                column: "DispenserId");

            migrationBuilder.CreateIndex(
                name: "IX_tranzaktsiyalar_IdempotencyKey",
                table: "tranzaktsiyalar",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tranzaktsiyalar_PaymentId",
                table: "tranzaktsiyalar",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_tranzaktsiyalar_Status",
                table: "tranzaktsiyalar",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_users_Username",
                table: "users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "disbursement_tarixi");

            migrationBuilder.DropTable(
                name: "schetfakturalar");

            migrationBuilder.DropTable(
                name: "sverka_logs");

            migrationBuilder.DropTable(
                name: "system_settings");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "stakeholders");

            migrationBuilder.DropTable(
                name: "tranzaktsiyalar");

            migrationBuilder.DropTable(
                name: "dispensers");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "filling_stations");
        }
    }
}
