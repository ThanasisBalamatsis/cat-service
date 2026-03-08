using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeImageBinaryToImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Image",
                table: "Cats");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Cats",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Cats");

            migrationBuilder.AddColumn<byte[]>(
                name: "Image",
                table: "Cats",
                type: "VARBINARY(MAX)",
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
