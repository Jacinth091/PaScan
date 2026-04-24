using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaScan.Migrations
{
    /// <inheritdoc />
    public partial class SyncWithSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Devices_DeviceRequests_DeviceRequestId",
                table: "Devices");

            migrationBuilder.DropForeignKey(
                name: "FK_QRTokens_Devices_DeviceId",
                table: "QRTokens");

            migrationBuilder.DropIndex(
                name: "IX_Devices_DeviceRequestId",
                table: "Devices");

            migrationBuilder.RenameColumn(
                name: "DeviceRequestId",
                table: "Devices",
                newName: "OriginalRequestId");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ContactNumber",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Students",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MiddleName",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentNumber",
                table: "Students",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "YearLevel",
                table: "Students",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Scanners",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InstalledAt",
                table: "Scanners",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevocationReason",
                table: "QRTokens",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedAt",
                table: "QRTokens",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RevokedBy",
                table: "QRTokens",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "DeviceId",
                table: "GateScanLogs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "Devices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "DeviceRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Courses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Admins",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "Admins",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Admins",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MiddleName",
                table: "Admins",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QRTokens_RevokedBy",
                table: "QRTokens",
                column: "RevokedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_OriginalRequestId",
                table: "Devices",
                column: "OriginalRequestId",
                unique: true,
                filter: "[OriginalRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Admins_CreatedBy",
                table: "Admins",
                column: "CreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Admins_Admins_CreatedBy",
                table: "Admins",
                column: "CreatedBy",
                principalTable: "Admins",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_DeviceRequests_OriginalRequestId",
                table: "Devices",
                column: "OriginalRequestId",
                principalTable: "DeviceRequests",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QRTokens_Admins_RevokedBy",
                table: "QRTokens",
                column: "RevokedBy",
                principalTable: "Admins",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_QRTokens_Devices_DeviceId",
                table: "QRTokens",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Admins_Admins_CreatedBy",
                table: "Admins");

            migrationBuilder.DropForeignKey(
                name: "FK_Devices_DeviceRequests_OriginalRequestId",
                table: "Devices");

            migrationBuilder.DropForeignKey(
                name: "FK_QRTokens_Admins_RevokedBy",
                table: "QRTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_QRTokens_Devices_DeviceId",
                table: "QRTokens");

            migrationBuilder.DropIndex(
                name: "IX_QRTokens_RevokedBy",
                table: "QRTokens");

            migrationBuilder.DropIndex(
                name: "IX_Devices_OriginalRequestId",
                table: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_Admins_CreatedBy",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ContactNumber",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "MiddleName",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "StudentNumber",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "YearLevel",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Scanners");

            migrationBuilder.DropColumn(
                name: "InstalledAt",
                table: "Scanners");

            migrationBuilder.DropColumn(
                name: "RevocationReason",
                table: "QRTokens");

            migrationBuilder.DropColumn(
                name: "RevokedAt",
                table: "QRTokens");

            migrationBuilder.DropColumn(
                name: "RevokedBy",
                table: "QRTokens");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "DeviceRequests");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "MiddleName",
                table: "Admins");

            migrationBuilder.RenameColumn(
                name: "OriginalRequestId",
                table: "Devices",
                newName: "DeviceRequestId");

            migrationBuilder.AlterColumn<Guid>(
                name: "DeviceId",
                table: "GateScanLogs",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DeviceRequestId",
                table: "Devices",
                column: "DeviceRequestId",
                unique: true,
                filter: "[DeviceRequestId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_DeviceRequests_DeviceRequestId",
                table: "Devices",
                column: "DeviceRequestId",
                principalTable: "DeviceRequests",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QRTokens_Devices_DeviceId",
                table: "QRTokens",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
