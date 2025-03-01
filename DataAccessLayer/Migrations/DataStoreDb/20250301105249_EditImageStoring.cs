using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations.DataStoreDb
{
    /// <inheritdoc />
    public partial class EditImageStoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_url",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "image_url",
                table: "HardQuestions");

            migrationBuilder.AddColumn<byte[]>(
                name: "image_data",
                table: "Questions",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "image_data",
                table: "HardQuestions",
                type: "varbinary(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_data",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "image_data",
                table: "HardQuestions");

            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "Questions",
                type: "varchar(255)",
                unicode: false,
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "HardQuestions",
                type: "varchar(255)",
                unicode: false,
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }
    }
}
