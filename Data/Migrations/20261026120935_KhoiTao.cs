using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SuDia.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class KhoiTao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    HoTen = table.Column<string>(type: "text", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TienDoBai",
                columns: table => new
                {
                    NguoiDungId = table.Column<string>(type: "text", nullable: false),
                    BaiId = table.Column<int>(type: "integer", nullable: false),
                    TrangThai = table.Column<int>(type: "integer", nullable: false),
                    CapNhat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TienDoBai", x => new { x.NguoiDungId, x.BaiId });
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BaiLam",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NguoiDungId = table.Column<string>(type: "text", nullable: false),
                    MaDe = table.Column<int>(type: "integer", nullable: false),
                    LaDeThi = table.Column<bool>(type: "boolean", nullable: false),
                    TenDe = table.Column<string>(type: "text", nullable: false),
                    SoCau = table.Column<int>(type: "integer", nullable: false),
                    SoDung = table.Column<int>(type: "integer", nullable: false),
                    NopLuc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaiLam", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaiLam_AspNetUsers_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Khoa",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Ten = table.Column<string>(type: "text", nullable: false),
                    KyHieu = table.Column<string>(type: "text", nullable: false),
                    MoTa = table.Column<string>(type: "text", nullable: false),
                    Anh = table.Column<string>(type: "text", nullable: false),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    GiangVienId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Khoa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Khoa_AspNetUsers_GiangVienId",
                        column: x => x.GiangVienId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CauTraLoi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BaiLamId = table.Column<int>(type: "integer", nullable: false),
                    CauHoiId = table.Column<int>(type: "integer", nullable: false),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    DaChon = table.Column<string>(type: "text", nullable: true),
                    Dung = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CauTraLoi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CauTraLoi_BaiLam_BaiLamId",
                        column: x => x.BaiLamId,
                        principalTable: "BaiLam",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChuDe",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KhoaId = table.Column<string>(type: "text", nullable: false),
                    Ten = table.Column<string>(type: "text", nullable: false),
                    ThuTu = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChuDe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChuDe_Khoa_KhoaId",
                        column: x => x.KhoaId,
                        principalTable: "Khoa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeThi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    KhoaId = table.Column<string>(type: "text", nullable: false),
                    Ten = table.Column<string>(type: "text", nullable: false),
                    SoPhut = table.Column<int>(type: "integer", nullable: false),
                    DangMo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeThi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeThi_Khoa_KhoaId",
                        column: x => x.KhoaId,
                        principalTable: "Khoa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GhiDanh",
                columns: table => new
                {
                    NguoiDungId = table.Column<string>(type: "text", nullable: false),
                    KhoaId = table.Column<string>(type: "text", nullable: false),
                    HanDen = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GhiDanh", x => new { x.NguoiDungId, x.KhoaId });
                    table.ForeignKey(
                        name: "FK_GhiDanh_AspNetUsers_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GhiDanh_Khoa_KhoaId",
                        column: x => x.KhoaId,
                        principalTable: "Khoa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bai",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    ChuDeId = table.Column<int>(type: "integer", nullable: false),
                    So = table.Column<int>(type: "integer", nullable: false),
                    Ten = table.Column<string>(type: "text", nullable: false),
                    SoMuc = table.Column<int>(type: "integer", nullable: false),
                    SoPhut = table.Column<int>(type: "integer", nullable: false),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    HocThu = table.Column<bool>(type: "boolean", nullable: false),
                    DaXuatBan = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bai", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bai_ChuDe_ChuDeId",
                        column: x => x.ChuDeId,
                        principalTable: "ChuDe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BuoiHoc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BaiId = table.Column<int>(type: "integer", nullable: false),
                    Link = table.Column<string>(type: "text", nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuoiHoc", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuoiHoc_Bai_BaiId",
                        column: x => x.BaiId,
                        principalTable: "Bai",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CauHoi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BaiId = table.Column<int>(type: "integer", nullable: false),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    NoiDung = table.Column<string>(type: "text", nullable: false),
                    PhuongAn = table.Column<List<string>>(type: "text[]", nullable: false),
                    DapAnDung = table.Column<string>(type: "text", nullable: false),
                    GiaiThich = table.Column<string>(type: "text", nullable: false),
                    Muc = table.Column<int>(type: "integer", nullable: true),
                    NguonHienThi = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CauHoi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CauHoi_Bai_BaiId",
                        column: x => x.BaiId,
                        principalTable: "Bai",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Muc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BaiId = table.Column<int>(type: "integer", nullable: false),
                    So = table.Column<int>(type: "integer", nullable: false),
                    Ten = table.Column<string>(type: "text", nullable: false),
                    TieuDe = table.Column<string>(type: "text", nullable: false),
                    SoPhut = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Muc", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Muc_Bai_BaiId",
                        column: x => x.BaiId,
                        principalTable: "Bai",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeThiCau",
                columns: table => new
                {
                    DeThiId = table.Column<int>(type: "integer", nullable: false),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    CauHoiId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeThiCau", x => new { x.DeThiId, x.ThuTu });
                    table.ForeignKey(
                        name: "FK_DeThiCau_CauHoi_CauHoiId",
                        column: x => x.CauHoiId,
                        principalTable: "CauHoi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeThiCau_DeThi_DeThiId",
                        column: x => x.DeThiId,
                        principalTable: "DeThi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Phan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MucId = table.Column<int>(type: "integer", nullable: false),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    MaNeo = table.Column<string>(type: "text", nullable: false),
                    TieuDe = table.Column<string>(type: "text", nullable: false),
                    DoanVan = table.Column<List<string>>(type: "text[]", nullable: false),
                    LaGhiNho = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Phan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Phan_Muc_MucId",
                        column: x => x.MucId,
                        principalTable: "Muc",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MocThoiGian",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PhanId = table.Column<int>(type: "integer", nullable: false),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    Ten = table.Column<string>(type: "text", nullable: false),
                    ThoiGian = table.Column<string>(type: "text", nullable: false),
                    NoiDung = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MocThoiGian", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MocThoiGian_Phan_PhanId",
                        column: x => x.PhanId,
                        principalTable: "Phan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bai_ChuDeId",
                table: "Bai",
                column: "ChuDeId");

            migrationBuilder.CreateIndex(
                name: "IX_BaiLam_NguoiDungId_NopLuc",
                table: "BaiLam",
                columns: new[] { "NguoiDungId", "NopLuc" });

            migrationBuilder.CreateIndex(
                name: "IX_BuoiHoc_BaiId",
                table: "BuoiHoc",
                column: "BaiId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CauHoi_BaiId",
                table: "CauHoi",
                column: "BaiId");

            migrationBuilder.CreateIndex(
                name: "IX_CauTraLoi_BaiLamId",
                table: "CauTraLoi",
                column: "BaiLamId");

            migrationBuilder.CreateIndex(
                name: "IX_ChuDe_KhoaId",
                table: "ChuDe",
                column: "KhoaId");

            migrationBuilder.CreateIndex(
                name: "IX_DeThi_KhoaId",
                table: "DeThi",
                column: "KhoaId");

            migrationBuilder.CreateIndex(
                name: "IX_DeThiCau_CauHoiId",
                table: "DeThiCau",
                column: "CauHoiId");

            migrationBuilder.CreateIndex(
                name: "IX_GhiDanh_KhoaId",
                table: "GhiDanh",
                column: "KhoaId");

            migrationBuilder.CreateIndex(
                name: "IX_Khoa_GiangVienId",
                table: "Khoa",
                column: "GiangVienId");

            migrationBuilder.CreateIndex(
                name: "IX_MocThoiGian_PhanId",
                table: "MocThoiGian",
                column: "PhanId");

            migrationBuilder.CreateIndex(
                name: "IX_Muc_BaiId",
                table: "Muc",
                column: "BaiId");

            migrationBuilder.CreateIndex(
                name: "IX_Phan_MucId",
                table: "Phan",
                column: "MucId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "BuoiHoc");

            migrationBuilder.DropTable(
                name: "CauTraLoi");

            migrationBuilder.DropTable(
                name: "DeThiCau");

            migrationBuilder.DropTable(
                name: "GhiDanh");

            migrationBuilder.DropTable(
                name: "MocThoiGian");

            migrationBuilder.DropTable(
                name: "TienDoBai");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "BaiLam");

            migrationBuilder.DropTable(
                name: "CauHoi");

            migrationBuilder.DropTable(
                name: "DeThi");

            migrationBuilder.DropTable(
                name: "Phan");

            migrationBuilder.DropTable(
                name: "Muc");

            migrationBuilder.DropTable(
                name: "Bai");

            migrationBuilder.DropTable(
                name: "ChuDe");

            migrationBuilder.DropTable(
                name: "Khoa");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
