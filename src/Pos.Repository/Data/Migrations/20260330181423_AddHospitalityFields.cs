using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHospitalityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HospitalityResponsibleName",
                table: "OrdersDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHospitality",
                table: "OrdersDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsStaffMeal",
                table: "OrdersDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StaffMealEmployeeName",
                table: "OrdersDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HospitalityReason",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HospitalityResponsibleId",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HospitalityResponsibleName",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHospitality",
                table: "Orders",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsStaffMeal",
                table: "Orders",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StaffMealEmployeeId",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StaffMealEmployeeName",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HospitalityResponsibleName",
                table: "OrdersDetails");

            migrationBuilder.DropColumn(
                name: "IsHospitality",
                table: "OrdersDetails");

            migrationBuilder.DropColumn(
                name: "IsStaffMeal",
                table: "OrdersDetails");

            migrationBuilder.DropColumn(
                name: "StaffMealEmployeeName",
                table: "OrdersDetails");

            migrationBuilder.DropColumn(
                name: "HospitalityReason",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "HospitalityResponsibleId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "HospitalityResponsibleName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsHospitality",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsStaffMeal",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StaffMealEmployeeId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StaffMealEmployeeName",
                table: "Orders");
        }
    }
}
