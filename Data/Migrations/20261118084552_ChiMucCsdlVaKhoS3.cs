using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;

#nullable disable

namespace SuDia.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChiMucCsdlVaKhoS3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bỏ dấu tiếng Việt trong CSDL để tìm "dien bien phu" ra "Điện Biên Phủ". unaccent() không được đánh dấu
            // IMMUTABLE nên bọc trong hàm riêng để dùng được cho cột sinh tự động và chỉ mục.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS unaccent;");
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION f_bo_dau(text) RETURNS text
                LANGUAGE sql IMMUTABLE PARALLEL SAFE STRICT
                AS $$ SELECT lower(public.unaccent('public.unaccent'::regdictionary, replace(replace($1, 'đ', 'd'), 'Đ', 'D'))) $$;
                """);

            migrationBuilder.AddColumn<string>(
                name: "NoiLuu",
                table: "TaiLieuKhoa",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "CucBo");

            migrationBuilder.CreateTable(
                name: "DoanChiMuc",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NguonKhoa = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    KhoaId = table.Column<string>(type: "text", nullable: true),
                    Loai = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Mon = table.Column<string>(type: "text", nullable: false),
                    Nguon = table.Column<string>(type: "text", nullable: false),
                    NoiDung = table.Column<string>(type: "text", nullable: false),
                    Lien = table.Column<string>(type: "text", nullable: true),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    NapLuc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimKiem = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: false, computedColumnSql: "to_tsvector('simple', f_bo_dau(\"Nguon\" || ' ' || \"NoiDung\"))", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoanChiMuc", x => x.Id);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_TaiLieuKhoa_NoiLuu",
                table: "TaiLieuKhoa",
                sql: "\"NoiLuu\" IN ('CucBo', 'S3')");

            migrationBuilder.CreateIndex(
                name: "IX_DoanChiMuc_KhoaId",
                table: "DoanChiMuc",
                column: "KhoaId");

            migrationBuilder.CreateIndex(
                name: "IX_DoanChiMuc_NguonKhoa",
                table: "DoanChiMuc",
                column: "NguonKhoa");

            migrationBuilder.CreateIndex(
                name: "IX_DoanChiMuc_TimKiem",
                table: "DoanChiMuc",
                column: "TimKiem")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoanChiMuc");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TaiLieuKhoa_NoiLuu",
                table: "TaiLieuKhoa");

            migrationBuilder.DropColumn(
                name: "NoiLuu",
                table: "TaiLieuKhoa");

            migrationBuilder.Sql("DROP FUNCTION IF EXISTS f_bo_dau(text);");
        }
    }
}
