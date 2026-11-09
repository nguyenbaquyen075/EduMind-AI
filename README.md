<div align="center">

# 🎓 EduMind-AI — Sử Địa

**Nền tảng học trực tuyến có trợ lý AI cho môn Lịch sử và Địa lí**

Quản lý khoá học · Soạn bài bằng AI · Giao bài & chấm điểm · Hỏi đáp theo tài liệu (RAG)

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-MVC-5C2D91)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-EF_Core_8-4169E1?logo=postgresql&logoColor=white)
![Claude](https://img.shields.io/badge/Claude-RAG_Q%26A-D97757)
![Gemini](https://img.shields.io/badge/Gemini-Soạn_bài-4285F4)

</div>

---

## Mục lục

1. [Giới thiệu](#1-giới-thiệu)
2. [Tính năng nổi bật](#2-tính-năng-nổi-bật)
3. [Vai trò người dùng](#3-vai-trò-người-dùng)
4. [Kiến trúc hệ thống](#4-kiến-trúc-hệ-thống)
5. [Các luồng hoạt động](#5-các-luồng-hoạt-động)
6. [Công nghệ sử dụng](#6-công-nghệ-sử-dụng)
7. [Mô hình dữ liệu](#7-mô-hình-dữ-liệu)
8. [Cấu trúc thư mục](#8-cấu-trúc-thư-mục)
9. [Cài đặt và chạy](#9-cài-đặt-và-chạy)
10. [Cấu hình AI và thanh toán](#10-cấu-hình-ai-và-thanh-toán)
11. [Bảo mật](#11-bảo-mật)
12. [Kiểm thử](#12-kiểm-thử)
13. [Quy trình Git](#13-quy-trình-git)
14. [Hạn chế và hướng phát triển](#14-hạn-chế-và-hướng-phát-triển)

---

## 1. Giới thiệu

**Sử Địa** là ứng dụng web hỗ trợ dạy và học, hiện có nội dung mẫu cho **Lịch sử 12** và **Địa lí 12**. Hệ thống phục vụ trọn vòng đời của một khoá học:

- **Giảng viên** soạn bài (nhập văn bản, Word, PowerPoint; AI hỗ trợ chia mục), giao bài tập, tạo đề kiểm tra có đồng hồ, chấm tự luận và theo dõi tiến độ học viên.
- **Học viên** đọc bài theo mục lục, làm bài tập/kiểm tra, xem điểm, đáp án, lời giải và đặt câu hỏi cho trợ lý AI.
- **Quản trị viên** quản lý người dùng, khoá học, ghi danh, lịch dạy, báo cáo và doanh thu.

Hai chức năng AI hoạt động độc lập:

| Chức năng | Nhà cung cấp | Mục đích |
| --- | --- | --- |
| Soạn bài | Google Gemini | Chia văn bản thành các mục, phần của bài học để giảng viên rà soát |
| Hỏi đáp | Anthropic Claude + RAG | Trả lời câu hỏi của học viên **dựa trên tài liệu khoá**, kèm nguồn |

> Dự án gọi AI qua API, **không** huấn luyện mô hình riêng. Khi chưa có khoá API, trợ lý chạy ở chế độ xem thử.

## 2. Tính năng nổi bật

**Học tập**
- Duyệt khoá → chủ đề → bài; đọc theo mục lục, theo dõi tiến độ.
- Học thử bài đầu; nội dung đầy đủ theo ghi danh còn hạn.
- Ba loại câu hỏi: trắc nghiệm 4 phương án, đúng/sai 4 ý, tự luận.
- Chấm tự động trên máy chủ; đáp án và lời giải chỉ hiện **sau khi nộp**.
- Bắt buộc trả lời đủ câu mới được nộp (đề có đồng hồ vẫn tự nộp khi hết giờ).
- Đề kiểm tra có **đồng hồ do máy chủ giữ**: tải lại trang không đặt lại thời gian.

**Giảng dạy**
- Soạn bài từ văn bản, `.docx`, `.pptx` với bản xem thử để sửa trước khi lưu.
- Hai cách chia mục: **AI (Gemini)** hoặc **theo ký hiệu đề mục** (`#`, `I.`, `1.1.`, `a)`…) hoạt động offline.
- Form giao bài/kiểm tra 3 bước: thông tin chung → soạn câu hỏi → xem lại & xuất bản.
- Chấm tự luận, xem bài nộp, theo dõi điểm từng học viên; lịch dạy kèm link Google Meet.

**Quản trị & kinh doanh**
- Người dùng, vai trò, khoá/mở tài khoản; phân công giảng viên cho khoá.
- Ghi danh có hạn học; gói học và đơn hàng, tích hợp cổng thanh toán PayOS.
- Báo cáo theo ngày/khoá, xuất CSV.

## 3. Vai trò người dùng

| Vai trò | Khu vực | Quyền chính |
| --- | --- | --- |
| Học viên | `/Home`, `/BaiTap` | Học, làm bài, xem điểm, hỏi AI trong phạm vi đã ghi danh |
| Giảng viên | `/GiangVien` | Soạn bài, giao bài, tạo đề, chấm điểm — **chỉ khoá được phân công** |
| Admin | `/Admin` | Toàn bộ khoá học, người dùng, ghi danh, báo cáo, cấu hình trợ lý |

Đăng ký mới luôn tạo tài khoản **học viên** và không tự mở khoá học.

## 4. Kiến trúc hệ thống

Mô hình **ASP.NET Core MVC** một máy chủ ứng dụng, dữ liệu trong PostgreSQL, AI gọi qua HTTP.

```mermaid
flowchart LR
    subgraph Client["Trình duyệt"]
        UI["Razor HTML + CSS + JS"]
    end

    subgraph App["Máy chủ ASP.NET Core 8"]
        MW["Middleware<br/>Identity · Anti-forgery · Rate limit"]
        CTRL["Controllers / Areas<br/>Home · GiangVien · Admin"]
        SVC["Services<br/>Học tập · Soạn bài · Trợ lý · Thanh toán"]
        EF["EF Core<br/>AppDbContext"]
        CACHE[("IMemoryCache<br/>bản xem thử")]
        KB[("TaiLieu/*.md<br/>Prompts/")]
    end

    DB[("PostgreSQL")]
    GEM["Google Gemini API"]
    CLA["Anthropic Claude API"]
    PAY["PayOS"]

    UI -->|HTTPS| MW --> CTRL --> SVC
    SVC --> EF --> DB
    SVC --> CACHE
    SVC -->|BM25 tìm đoạn| KB
    SVC -->|soạn bài| GEM
    SVC -->|hỏi đáp| CLA
    SVC <-->|đơn hàng · webhook| PAY
    CTRL -->|Razor View| UI
```

Luồng trang web thông thường: **Trình duyệt → Controller → Service/DbContext → ViewModel → Razor HTML**. Các API tương tác (trợ lý, bắt đầu làm bài, xem thử) trả JSON cho JavaScript.

## 5. Các luồng hoạt động

### 5.1. Học viên: từ đăng ký đến làm bài

```mermaid
flowchart TD
    A([Đăng ký tài khoản]) --> B[Vai trò Học viên]
    B --> C[Xem danh sách khoá]
    C --> D{Đã ghi danh<br/>còn hạn?}
    D -- Chưa --> E[Học thử bài đầu / mua gói qua PayOS]
    E --> F[Admin hoặc webhook mở ghi danh]
    D -- Rồi --> G[Đọc bài theo mục lục]
    F --> G
    G --> H[Ghi nhận tiến độ]
    G --> I[Làm bài tập / kiểm tra]
    I --> J[Nộp bài]
    J --> K[Máy chủ chấm trắc nghiệm + đúng/sai]
    K --> L[Hiện điểm, đáp án, lời giải]
    J --> M[Tự luận chờ giảng viên chấm]
    G --> N[Hỏi AI về nội dung khoá]
```

### 5.2. Làm bài kiểm tra có đồng hồ

```mermaid
sequenceDiagram
    actor HV as Học viên
    participant W as Trình duyệt
    participant S as Máy chủ
    participant DB as PostgreSQL

    HV->>W: Bấm "Bắt đầu làm bài"
    W->>S: POST bắt đầu
    S->>DB: Lưu LuotLamKiemTra (giờ bắt đầu)
    S-->>W: Hiện câu hỏi + giờ hết hạn
    Note over W: Tải lại trang không reset đồng hồ<br/>(giờ do máy chủ lưu)
    HV->>W: Làm bài, hoặc hết giờ thì tự nộp
    W->>S: POST nộp bài
    S->>DB: Kiểm tra có lượt bắt đầu, chưa nộp, chưa quá hạn
    alt Trong hạn (+10 giây đệm)
        S->>DB: Lưu BaiNopGiao + điểm tự động
    else Quá hạn
        S->>DB: Ghi bài rỗng, không nhận đáp án muộn
    end
    S-->>W: Kết quả
```

Mỗi học viên chỉ có **một bài nộp** cho mỗi bài tập (ràng buộc duy nhất `bài tập – học viên`).

### 5.3. Giảng viên: soạn bài từ tài liệu

```mermaid
flowchart TD
    A([Chọn khoá / chủ đề, nhập tên + số bài]) --> B{Nguồn nội dung}
    B -->|Dán văn bản| D
    B -->|Tải .docx / .pptx| C[Đọc ZIP/XML trên máy chủ<br/>tối đa 20 MB, 30.000 ký tự]
    C --> D[Nội dung văn bản]
    D --> E{Cách chia mục}
    E -->|AI| F[Gemini nhận diện cấu trúc]
    E -->|Theo đề mục| G[Tách bằng #, I., 1.1., a ...]
    F -. lỗi / hết hạn mức .-> G
    F --> H
    G --> H[Bản xem thử<br/>IMemoryCache, tối đa 2 giờ]
    H --> I[Giảng viên rà soát và sửa]
    I --> J[Xác nhận lưu vào PostgreSQL]
    J --> K[Bài ở trạng thái nháp<br/>→ bật xuất bản / học thử]
```

Việc đọc Word/PowerPoint **không gọi AI**; văn bản chỉ được gửi sang Gemini khi giảng viên chủ động chọn nút AI. Không chạy macro, không cần cài Office.

### 5.4. Giao bài và chấm điểm

```mermaid
flowchart LR
    A[Thông tin chung<br/>tên · bài học · hạn · thời lượng] --> B[Soạn câu hỏi<br/>TN · Đúng/Sai · Tự luận]
    B --> C[Xem lại tổng điểm]
    C --> D{Lưu}
    D -->|Nháp| E[(BaiTapGiao)]
    D -->|Xuất bản| E
    E --> F[Học viên nộp]
    F --> G[Chấm tự động]
    G --> H[Giảng viên chấm tự luận]
    H --> I[Điểm cuối + báo cáo]
```

Bài tập đã có bài nộp, hoặc đề đã có lượt làm, sẽ **bị khoá sửa/xoá** để bảo toàn kết quả.

### 5.5. Trợ lý hỏi đáp (RAG)

```mermaid
flowchart TD
    Q([Câu hỏi của học viên]) --> R[Viết lại câu hỏi nối tiếp<br/>dùng tối đa 3 lượt gần nhất]
    R --> S[Tìm đoạn liên quan<br/>BM25 trên văn bản bỏ dấu]
    S --> T[(TaiLieu/*.md<br/>cắt theo mục)]
    T --> U[Gọi Claude với prompt hệ thống + đoạn tìm được]
    U --> V[Kiểm tra số liệu trong câu trả lời]
    V --> W[AI chấm độ bám nguồn]
    W --> X([Trả lời kèm nguồn])
```

- API `POST /api/tro-ly/hoi` có chống CSRF và giới hạn **10 câu/phút**.
- Nguồn hiện là file Markdown trong `TaiLieu/`; nội dung bài soạn trong database **chưa tự đồng bộ** thành nguồn cho trợ lý.
- Lịch sử chat chưa lưu bền, chỉ gửi kèm ngữ cảnh gần nhất từ trình duyệt.

## 6. Công nghệ sử dụng

| Lớp | Công nghệ |
| --- | --- |
| Ngôn ngữ / nền tảng | C#, .NET 8 |
| Web | ASP.NET Core MVC, Areas, Razor Views |
| Giao diện | HTML, CSS thuần theo từng trang, JavaScript thuần (không framework) |
| Xác thực & phân quyền | ASP.NET Core Identity, cookie, phân quyền theo vai trò, khoá tài khoản sau 5 lần sai |
| Cơ sở dữ liệu | PostgreSQL 14+, Entity Framework Core 8, Npgsql, migrations |
| AI soạn bài | Google Gemini API (`HttpClient`) |
| AI hỏi đáp | Anthropic SDK (Claude) + BM25 tự cài đặt |
| Nhập tài liệu | `ZipArchive` + XML parser (`.docx`, `.pptx`) |
| Thanh toán | PayOS (tạo đơn, webhook) |
| Bộ nhớ đệm | `IMemoryCache` cho bản xem thử theo tài khoản |
| Kiểm thử | Script Python (HTTP + `psql`), Node.js kiểm tra cú pháp JS |

## 7. Mô hình dữ liệu

```mermaid
erDiagram
    NguoiDung ||--o{ GhiDanh : "có"
    Khoa ||--o{ GhiDanh : "mở cho"
    Khoa ||--o{ ChuDe : "gồm"
    ChuDe ||--o{ Bai : "gồm"
    Bai ||--o{ Muc : "gồm"
    Muc ||--o{ Phan : "gồm"
    Bai ||--o{ BuoiHoc : "lịch dạy"
    NguoiDung ||--o{ TienDoBai : "theo dõi"
    Bai ||--o{ TienDoBai : ""
    Bai ||--o{ BaiTapGiao : "giao"
    BaiTapGiao ||--o{ BaiNopGiao : "nhận"
    NguoiDung ||--o{ BaiNopGiao : "nộp"
    BaiTapGiao ||--o{ LuotLamKiemTra : "bắt đầu"
    NguoiDung ||--o{ DonHang : "mua"
```

| Nhóm | Bảng |
| --- | --- |
| Tài khoản | `NguoiDung` + các bảng Identity |
| Nội dung | `Khoa → ChuDe → Bai → Muc → Phan → MocThoiGian` |
| Quyền học | `GhiDanh` (có hạn hoặc không giới hạn) |
| Tiến độ | `TienDoBai` |
| Bài tập & đề mới | `BaiTapGiao` (câu hỏi lưu dạng JSON), `BaiNopGiao`, `LuotLamKiemTra` |
| Luyện tập cũ | `CauHoi`, `DeThiCau`, `BaiLam`, `CauTraLoi` |
| Lịch & bán hàng | `BuoiHoc`, `GoiHoc`, `DonHang` |

## 8. Cấu trúc thư mục

```text
SuDia.Web/
├── Program.cs                 # DI, Identity, DB, routing, rate limit, nạp dữ liệu đầu
├── Controllers/               # Trang học viên, tài khoản, làm bài, thanh toán, API trợ lý
├── Areas/
│   ├── GiangVien/             # Soạn nội dung, giao bài, đề thi, chấm điểm, lịch dạy
│   └── Admin/                 # Người dùng, khoá, ghi danh, báo cáo, doanh thu
├── Data/                      # Thực thể, AppDbContext, khởi tạo, dữ liệu mẫu, Migrations/
├── Models/                    # ViewModel, biểu mẫu, mô hình câu hỏi
├── Services/
│   ├── TroLy/                 # Kho tài liệu (BM25), prompt, kiểm tra câu trả lời
│   ├── RagAiService.cs        # Trợ lý Claude
│   ├── GeminiSoanNoiDungAiService.cs
│   ├── DocTaiLieuOffice.cs    # Đọc .docx/.pptx
│   ├── ChiaDeMucVanBan.cs     # Chia mục ngoại tuyến
│   └── PayOSService.cs, DonHangService.cs
├── Views/  wwwroot/           # Razor, CSS, JS, ảnh
├── TaiLieu/                   # Nguồn tri thức cho trợ lý (Markdown)
├── Prompts/                   # Prompt hệ thống
├── KiemThu/                   # Script kiểm thử + bộ câu hỏi AI
├── scripts/                   # Cấu hình khoá AI/PayOS (zsh)
└── docs/                      # Tài liệu chi tiết dự án
```

## 9. Cài đặt và chạy

**Yêu cầu:** .NET 8 SDK, PostgreSQL, Git. (Tuỳ chọn: Python 3 + `psql` để chạy kiểm thử.)

```sh
git clone https://github.com/nguyenbaquyen075/EduMind-AI.git
cd EduMind-AI
cp appsettings.Development.example.json appsettings.Development.json
# sửa ConnectionStrings:SuDia trong appsettings.Development.json

createdb sudia_dev
dotnet restore
dotnet dev-certs https --trust
dotnet run --launch-profile SuDia.Web
```

Mở **https://localhost:7180**. Lần đầu, ứng dụng tự chạy migration, tạo vai trò và nạp nội dung mẫu. Ở môi trường Development có sẵn ba tài khoản thử:

| Email | Vai trò |
| --- | --- |
| `admin@sudia.vn` | Admin |
| `giangvien@sudia.vn` | Giảng viên |
| `hocvien@sudia.vn` | Học viên |

Mật khẩu đặt qua `TaiKhoanMau:MatKhau` (mặc định chỉ dùng cho Development).

**Tạo lại toàn bộ dữ liệu mẫu đầy đủ** (xoá dữ liệu Development hiện có):

```sh
dotnet run -- --du-lieu-mau --xac-nhan-xoa-du-lieu-mau
```

Lệnh chỉ chạy ở Development và bắt buộc có cờ xác nhận.

## 10. Cấu hình AI và thanh toán

Khoá bí mật **không** nằm trong mã nguồn. Dùng .NET user-secrets (Development) hoặc biến môi trường (triển khai).

```sh
zsh scripts/cau-hinh-ai.zsh      # Gemini (hỏi khoá bằng ô nhập ẩn)
zsh scripts/cau-hinh-payos.zsh   # PayOS
```

| Dịch vụ | Biến môi trường | Mục cấu hình |
| --- | --- | --- |
| Gemini (soạn bài) | `GEMINI_API_KEY` | `GeminiSoanBai` |
| Claude (hỏi đáp) | `ANTHROPIC_API_KEY` | `TroLy` |
| PayOS | `PayOS__ClientId`… | `PayOS` |

Gemini và Claude cấu hình **riêng**. Khoá API có hạn mức và chi phí theo tài khoản nhà cung cấp.

## 11. Bảo mật

- Chống CSRF (anti-forgery) cho form và API; giới hạn tần suất API trợ lý.
- Khoá tài khoản sau 5 lần đăng nhập sai.
- Chấm điểm, kiểm tra hạn nộp và giờ làm bài **ở máy chủ**; không tin dữ liệu từ trình duyệt.
- Đáp án/lời giải không xuất hiện trong trang làm bài trước khi nộp.
- Giảng viên chỉ thao tác trên khoá được phân công.
- Đọc Office giới hạn dung lượng và dữ liệu giải nén, không thực thi macro.
- Khoá API, cấu hình cá nhân (`appsettings.Development.json`, user-secrets) bị loại khỏi Git.

## 12. Kiểm thử

```sh
dotnet build

SUDIA_TEST_URL=https://localhost:7180 python3 KiemThu/kiem-tra-giao-bai-theo-buoi.py
SUDIA_TEST_URL=https://localhost:7180 python3 KiemThu/kiem-tra-form-kiem-tra.py
SUDIA_TEST_URL=https://localhost:7180 python3 KiemThu/kiem-tra-nhap-office.py
```

Các script dùng tài khoản demo, tạo dữ liệu có mã riêng và dọn lại sau khi chạy; **chỉ dùng ở Development**. Chúng bao quát: lưu nháp/xuất bản, ba loại câu hỏi, chấm tự động và tự luận, đồng hồ kiểm tra, khoá sửa đề đã có người làm, ẩn đáp án trước khi nộp, đọc Word/PowerPoint, chặn file sai định dạng. `KiemThu/bo-cau-hoi.json` là bộ câu hỏi kiểm thử chất lượng trợ lý.

## 13. Quy trình Git

```text
feat/… · fix/… · style/… · chore/… · docs/…   (tách từ dev)
        │  commit theo từng phần việc (Conventional Commits)
        ▼
       dev   ← gộp bằng merge commit, build & kiểm tra
        │
        ▼  chỉ khi chủ dự án duyệt
       main
```

## 14. Hạn chế và hướng phát triển

| Hiện trạng | Hướng xử lý |
| --- | --- |
| Bản xem thử mất khi khởi động lại máy chủ | Lưu nháp bền trong database |
| Nhập Office chưa OCR, chưa giữ ảnh/bảng/đánh số | OCR, giữ ảnh, ánh xạ file nhiều bài |
| Gọi AI trong yêu cầu web (chờ đồng bộ) | Tác vụ nền + trạng thái tiến độ |
| Nguồn trợ lý tách khỏi bài soạn | Đồng bộ nội dung đã duyệt thành nguồn tìm kiếm; cân nhắc pgvector |
| Chưa lưu lịch sử hội thoại | Lưu theo tài khoản |
| Chưa có “đội miễn phí” | Mô hình đội, thành viên, quyền truy cập |
| Chưa có hướng dẫn triển khai/sao lưu/giám sát | Cấu hình triển khai, CI/CD, kiểm thử tải |

---

## Tài liệu chi tiết

- [Tài liệu dự án đầy đủ](docs/tai-lieu-du-an.md)
- [Kế hoạch giảng viên và Admin](docs/ke-hoach-giang-vien-admin.md) · [Nâng cấp Admin](docs/nang-cap-admin.md)
- [Nhập Word/PowerPoint](docs/nghien-cuu-nhap-word-powerpoint.md) · [Bàn giao giao bài tập](docs/ban-giao-giao-bai-tap.md)
