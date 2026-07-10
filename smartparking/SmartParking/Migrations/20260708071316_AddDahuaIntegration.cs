using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SmartParking.Migrations
{
    /// <inheritdoc />
    public partial class AddDahuaIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DahuaDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ChannelId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ApiBaseUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    DeviceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BarrierChannel = table.Column<int>(type: "integer", nullable: true),
                    StationId = table.Column<int>(type: "integer", nullable: true),
                    CompanyId = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DahuaDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DahuaDevices_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DahuaDevices_stations_StationId",
                        column: x => x.StationId,
                        principalTable: "stations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DahuaSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServerUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    WebhookSecret = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    HourlyRate = table.Column<decimal>(type: "numeric", nullable: false),
                    GracePeriodMinutes = table.Column<int>(type: "integer", nullable: false),
                    MaxDailyRate = table.Column<decimal>(type: "numeric", nullable: true),
                    AutoOpenForWhitelist = table.Column<bool>(type: "boolean", nullable: false),
                    BarrierControlEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CompanyId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DahuaSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DahuaSettings_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlateNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PlateCountry = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    OwnerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CompanyId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleLists_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DahuaEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PlateNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PlateCountry = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Confidence = table.Column<int>(type: "integer", nullable: true),
                    SnapshotUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ChannelId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ChannelName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EventTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RawPayload = table.Column<string>(type: "text", nullable: true),
                    ProcessStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DahuaDeviceId = table.Column<int>(type: "integer", nullable: true),
                    ParkingSessionId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DahuaEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DahuaEvents_DahuaDevices_DahuaDeviceId",
                        column: x => x.DahuaDeviceId,
                        principalTable: "DahuaDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ParkingSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlateNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EntryEventId = table.Column<int>(type: "integer", nullable: true),
                    ExitEventId = table.Column<int>(type: "integer", nullable: true),
                    EntryTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExitTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    ParkingFee = table.Column<decimal>(type: "numeric", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EntryBarrierOpened = table.Column<bool>(type: "boolean", nullable: false),
                    ExitBarrierOpened = table.Column<bool>(type: "boolean", nullable: false),
                    EntrySnapshotUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExitSnapshotUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeviceId = table.Column<int>(type: "integer", nullable: true),
                    StationId = table.Column<int>(type: "integer", nullable: true),
                    ClientId = table.Column<int>(type: "integer", nullable: true),
                    TransactionId = table.Column<int>(type: "integer", nullable: true),
                    VehicleCategory = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParkingSessions_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ParkingSessions_DahuaDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "DahuaDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ParkingSessions_DahuaEvents_EntryEventId",
                        column: x => x.EntryEventId,
                        principalTable: "DahuaEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ParkingSessions_DahuaEvents_ExitEventId",
                        column: x => x.ExitEventId,
                        principalTable: "DahuaEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ParkingSessions_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ParkingSessions_stations_StationId",
                        column: x => x.StationId,
                        principalTable: "stations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DahuaDevices_CompanyId",
                table: "DahuaDevices",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_DahuaDevices_StationId",
                table: "DahuaDevices",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_DahuaEvents_DahuaDeviceId",
                table: "DahuaEvents",
                column: "DahuaDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DahuaEvents_ParkingSessionId",
                table: "DahuaEvents",
                column: "ParkingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_DahuaSettings_CompanyId",
                table: "DahuaSettings",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_ClientId",
                table: "ParkingSessions",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_DeviceId",
                table: "ParkingSessions",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_EntryEventId",
                table: "ParkingSessions",
                column: "EntryEventId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_ExitEventId",
                table: "ParkingSessions",
                column: "ExitEventId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_StationId",
                table: "ParkingSessions",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_TransactionId",
                table: "ParkingSessions",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleLists_CompanyId",
                table: "VehicleLists",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_DahuaEvents_ParkingSessions_ParkingSessionId",
                table: "DahuaEvents",
                column: "ParkingSessionId",
                principalTable: "ParkingSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DahuaEvents_DahuaDevices_DahuaDeviceId",
                table: "DahuaEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_ParkingSessions_DahuaDevices_DeviceId",
                table: "ParkingSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_DahuaEvents_ParkingSessions_ParkingSessionId",
                table: "DahuaEvents");

            migrationBuilder.DropTable(
                name: "DahuaSettings");

            migrationBuilder.DropTable(
                name: "VehicleLists");

            migrationBuilder.DropTable(
                name: "DahuaDevices");

            migrationBuilder.DropTable(
                name: "ParkingSessions");

            migrationBuilder.DropTable(
                name: "DahuaEvents");
        }
    }
}
