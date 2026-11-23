using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SuDia.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class TinThongBaoTrongWeb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TinThongBao",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NguoiDungId = table.Column<string>(type: "text", nullable: false),
                    Loai = table.Column<string>(type: "text", nullable: false),
                    TieuDe = table.Column<string>(type: "text", nullable: false),
                    NoiDung = table.Column<string>(type: "text", nullable: false),
                    Link = table.Column<string>(type: "text", nullable: false),
                    KhoaDuyNhat = table.Column<string>(type: "text", nullable: true),
                    DaDoc = table.Column<bool>(type: "boolean", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TinThongBao", x => x.Id);
                    table.CheckConstraint("CK_TinThongBao_TieuDe", "length(btrim(\"TieuDe\")) BETWEEN 1 AND 200");
                    table.ForeignKey(
                        name: "FK_TinThongBao_AspNetUsers_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TinThongBao_NguoiDungId_DaDoc_NgayTao",
                table: "TinThongBao",
                columns: new[] { "NguoiDungId", "DaDoc", "NgayTao" });

            migrationBuilder.CreateIndex(
                name: "IX_TinThongBao_NguoiDungId_KhoaDuyNhat",
                table: "TinThongBao",
                columns: new[] { "NguoiDungId", "KhoaDuyNhat" },
                unique: true,
                filter: "\"KhoaDuyNhat\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TinThongBao");
        }
    }
}
