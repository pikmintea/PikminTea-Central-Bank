using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PTCB.Migrations
{
    public partial class AddBalanceToUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SumSumBalance",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SumSumBalance",
                table: "AspNetUsers");
        }
    }
}
