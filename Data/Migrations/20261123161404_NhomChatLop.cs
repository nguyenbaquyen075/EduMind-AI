using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SuDia.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class NhomChatLop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TraLoiBaiDang");

            migrationBuilder.DropTable(
                name: "BaiDang");

            migrationBuilder.CreateTable(
                name: "TinNhanLop",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LopId = table.Column<int>(type: "integer", nullable: false),
                    NguoiDungId = table.Column<string>(type: "text", nullable: false),
                    NoiDung = table.Column<string>(type: "text", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TinNhanLop", x => x.Id);
                    table.CheckConstraint("CK_TinNhanLop_NoiDung", "length(btrim(\"NoiDung\")) BETWEEN 1 AND 2000");
                    table.ForeignKey(
                        name: "FK_TinNhanLop_AspNetUsers_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TinNhanLop_Lop_LopId",
                        column: x => x.LopId,
                        principalTable: "Lop",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TinNhanLop_LopId_Id",
                table: "TinNhanLop",
                columns: new[] { "LopId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_TinNhanLop_NguoiDungId_NgayTao",
                table: "TinNhanLop",
                columns: new[] { "NguoiDungId", "NgayTao" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TinNhanLop");

            migrationBuilder.CreateTable(
                name: "BaiDang",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LopId = table.Column<int>(type: "integer", nullable: false),
                    NguoiDungId = table.Column<string>(type: "text", nullable: false),
                    CapNhatLuc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    DaDong = table.Column<bool>(type: "boolean", nullable: false),
                    Ghim = table.Column<bool>(type: "boolean", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NoiDung = table.Column<string>(type: "text", nullable: false),
                    TieuDe = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaiDang", x => x.Id);
                    table.CheckConstraint("CK_BaiDang_NoiDung", "length(btrim(\"NoiDung\")) BETWEEN 1 AND 4000");
                    table.CheckConstraint("CK_BaiDang_TieuDe", "length(btrim(\"TieuDe\")) BETWEEN 1 AND 200");
                    table.ForeignKey(
                        name: "FK_BaiDang_AspNetUsers_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BaiDang_Lop_LopId",
                        column: x => x.LopId,
                        principalTable: "Lop",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TraLoiBaiDang",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BaiDangId = table.Column<int>(type: "integer", nullable: false),
                    NguoiDungId = table.Column<string>(type: "text", nullable: false),
                    LaDapAn = table.Column<bool>(type: "boolean", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NoiDung = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraLoiBaiDang", x => x.Id);
                    table.CheckConstraint("CK_TraLoiBaiDang_NoiDung", "length(btrim(\"NoiDung\")) BETWEEN 1 AND 4000");
                    table.ForeignKey(
                        name: "FK_TraLoiBaiDang_AspNetUsers_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TraLoiBaiDang_BaiDang_BaiDangId",
                        column: x => x.BaiDangId,
                        principalTable: "BaiDang",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BaiDang_LopId_Ghim_NgayTao",
                table: "BaiDang",
                columns: new[] { "LopId", "Ghim", "NgayTao" });

            migrationBuilder.CreateIndex(
                name: "IX_BaiDang_NguoiDungId_NgayTao",
                table: "BaiDang",
                columns: new[] { "NguoiDungId", "NgayTao" });

            migrationBuilder.CreateIndex(
                name: "IX_TraLoiBaiDang_BaiDangId_NgayTao",
                table: "TraLoiBaiDang",
                columns: new[] { "BaiDangId", "NgayTao" });

            migrationBuilder.CreateIndex(
                name: "IX_TraLoiBaiDang_NguoiDungId_NgayTao",
                table: "TraLoiBaiDang",
                columns: new[] { "NguoiDungId", "NgayTao" });
        }
    }
}
