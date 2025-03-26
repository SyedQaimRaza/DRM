using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DRM.Migrations
{
    /// <inheritdoc />
    public partial class Third_Content : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AudioFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EncryptedContent = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Length = table.Column<TimeSpan>(type: "time", nullable: true),
                    DateOfUpload = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Lock = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudioFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PdfFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EncryptedContent = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateOfUpload = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Lock = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PdfFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VideoFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EncryptedContent = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Length = table.Column<TimeSpan>(type: "time", nullable: true),
                    DateOfUpload = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Lock = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssignUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VideoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PdfId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AudioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignUsers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssignUsers_AudioFiles_AudioId",
                        column: x => x.AudioId,
                        principalTable: "AudioFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AssignUsers_PdfFiles_PdfId",
                        column: x => x.PdfId,
                        principalTable: "PdfFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AssignUsers_VideoFiles_VideoId",
                        column: x => x.VideoId,
                        principalTable: "VideoFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssignUsers_AudioId",
                table: "AssignUsers",
                column: "AudioId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignUsers_PdfId",
                table: "AssignUsers",
                column: "PdfId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignUsers_UserId",
                table: "AssignUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignUsers_VideoId",
                table: "AssignUsers",
                column: "VideoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignUsers");

            migrationBuilder.DropTable(
                name: "AudioFiles");

            migrationBuilder.DropTable(
                name: "PdfFiles");

            migrationBuilder.DropTable(
                name: "VideoFiles");
        }
    }
}
