# Tài liệu Đặc tả Yêu cầu Phần mềm - Culinary Blog

**Phiên bản:** 1.2.0  
**Ngày chuẩn hóa:** 16/09/2026  
**Trạng thái:** Approved Baseline - sử dụng xuyên suốt dự án  
**Nguồn:** Chuyển đổi đầy đủ từ `SRS_Culinary_Blog_v1.0.0.pdf`; các nội dung mâu thuẫn đã được chuẩn hóa và đối chiếu thêm với phân tích của nhóm  
**Tài liệu chuẩn duy nhất:** `docs/requirements/SRS.md`

> [!IMPORTANT]
> File này giữ nội dung và cấu trúc của PDF gốc. Chỉ các FR, NFR, data model, API và phụ lục có mâu thuẫn được sửa theo quyết định thống nhất. Không dùng PDF hoặc bản Markdown trung gian làm căn cứ triển khai.

## Quy ước cập nhật

- Mọi thay đổi yêu cầu phải cập nhật file này qua Pull Request và ghi lịch sử thay đổi.
- Dấu `<!-- Trang N -->` dùng để truy vết phần nội dung còn giữ từ PDF; phần chuẩn hóa có thể không còn trùng số dòng của trang nguồn.
- Success response có body dùng `{ "data": ..., "meta": ... }`; error dùng RFC 7807.
- HTTP 400 dành cho request sai cú pháp, 422 cho validation/business rule, 409 cho conflict.

## Lịch sử chuẩn hóa

| Phiên bản | Ngày | Nội dung |
|---|---|---|
| 1.0.0 | 04/06/2026 | Bản PDF ban đầu |
| 1.1.0 | 16/09/2026 | Chuyển toàn bộ PDF sang Markdown; chuẩn hóa 24 mâu thuẫn chính và các khoảng trống kỹ thuật liên quan |
| 1.2.0 | 16/09/2026 | Tích hợp phân tích bổ sung của nhóm: UX phân số cho Quantity, nguồn dữ liệu Nutrition, phạm vi Step phân cấp và vòng đời restore/purge Recipe |

---

<!-- Trang 1 -->

```text
                    GIÁO TRÌNH PHÁT TRIỂN ỨNG DỤNG WEB NÂNG CAO
                           Phiên bản V4 · .NET 10 + Next.js App Router



          TÀI   LIỆU     ĐẶC     TẢ   YÊU     CẦU     PHẦN      MỀM


                      Software Requirements Specification (SRS)

                        Tiêu chuẩn IEEE 830 / ISO/IEC/IEEE 29148:2018



                      Dự   án: Blog  Ẩm   thực  và  Nấu  ăn

                                  Culinary Blog



           Phiên bản tài liệu 1.0.0
           Ngày phát hành  04/06/2026

           Trạng thái      Đã duyệt (Approved)
           Công nghệ Backend .NET 10 Minimal APIs, C#

           Công nghệ Frontend Next.js App Router, TypeScript
           Cơ sở dữ liệu   PostgreSQL 16

           Object Storage  MinIO (S3-Compatible)
           Cache           Redis 7


                  Tài liệu này được biên soạn theo tiêu chuẩn IEEE 830 / ISO/IEC/IEEE 29148:2018.
```

<!-- Trang 2 -->

## Lịch Sử Thay Đổi Tài Liệu

```text
           Phiên                                                 Trạng
                 Ngày     Tác giả / Vai trò Nội dung thay đổi
           bản                                                   thái
                                         Phát hành lần đầu – Bản hoàn
           1.0.0 04/06/2026 Senior BA / Architect                Approved
                                         chỉnh theo IEEE 830 / ISO 29148.
                                         Bổ sung Chương 7 (Data Model), Under
           0.9.0 20/05/2026 Senior BA
                                         Chương 8 (API Spec) và Phụ lục. Review
                                         Hoàn thiện Chương 3 (FR), bổ
           0.8.0 05/05/2026 Senior BA                            Draft
                                         sung FR-FILE, FR-JOB, FR-OBS.
                                         Phác thảo ban đầu: Chương 1–4
           0.5.0 15/04/2026 Senior BA                            Draft
                                         (skeleton).
          Phê duyệt tài liệu: Tài liệu phiên bản 1.0.0 đã được xem xét và phê duyệt bởi Trưởng nhóm
          Kiến trúc Hệ thống (Lead Systems Architect). Mọi thay đổi từ phiên bản 1.0.0 trở đi đều phải
          thông qua quy trình Change Request (CR) và được cập nhật vào bảng này.
```

<!-- Trang 3 -->

## Mục Lục

```text
          LỊCH SỬ THAY ĐỔI TÀI LIỆU .................................................................................................. 2
          MỤC LỤC ................................................................................................................................... 3

          CHƯƠNG 1. GIỚI THIỆU ......................................................................................................... 6
           1.1. Mục đích Tài liệu ............................................................................................................. 6
           1.2. Phạm vi Sản phẩm.......................................................................................................... 6

             1.2.1. Tên và Định danh ..................................................................................................... 6
             1.2.2. Mô tả Sản phẩm ....................................................................................................... 6
             1.2.3. Những gì KHÔNG thuộc phạm vi............................................................................. 7

           1.3. Định nghĩa, Từ viết tắt và Ký hiệu .................................................................................. 7
           1.4. Tài liệu Tham chiếu......................................................................................................... 8
           1.5. Tổng quan Tài liệu ........................................................................................................ 10

          CHƯƠNG 2. MÔ TẢ TỔNG QUAN HỆ THỐNG .................................................................... 11
           2.1. Bối cảnh Sản phẩm....................................................................................................... 11

             2.1.1. Vị trí trong Hệ sinh thái ........................................................................................... 11
             2.1.2. Quan hệ với Hệ thống Ngoài.................................................................................. 11
           2.2. Chức năng Sản phẩm Tổng quát ................................................................................. 12

           2.3. Các Lớp Người dùng và Đặc điểm............................................................................... 12
           2.4. Môi trường Vận hành .................................................................................................... 13
             2.4.1. Môi trường Server (Production) ............................................................................. 13

             2.4.2. Môi trường Phát triển (Development) .................................................................... 13
             2.4.3. Yêu cầu Trình duyệt Client..................................................................................... 14
           2.5. Ràng buộc Thiết kế và Hiện thực ................................................................................. 14

           2.6. Giả định và Phụ thuộc................................................................................................... 15
             2.6.1. Giả định................................................................................................................... 15
             2.6.2. Phụ thuộc Bên ngoài .............................................................................................. 15

          CHƯƠNG 3. YÊU CẦU CHỨC NĂNG CHI TIẾT ................................................................... 17
           3.1. Module Xác thực và Quản lý Người dùng (FR-AUTH) ................................................ 17

             FR-AUTH-001: Đăng ký Tài khoản (User Registration)................................................... 17
             FR-AUTH-002: Đăng nhập bằng Email/Mật khẩu (Local Login) ..................................... 18
             FR-AUTH-003: Đăng nhập bằng Google OAuth 2.0 ....................................................... 19

             FR-AUTH-004: Làm mới Access Token (Token Refresh) ............................................... 20
             FR-AUTH-005: Đăng xuất (Logout / Token Revocation) ................................................. 21
             FR-AUTH-006: Xem Hồ sơ Cá nhân (View Profile)......................................................... 22

             FR-AUTH-007: Cập nhật Hồ sơ Cá nhân (Update Profile) ............................................. 23
           3.2. Module Quản lý Danh mục (FR-CAT) .......................................................................... 23
             FR-CAT-001: Xem Danh sách Danh mục........................................................................ 23

             FR-CAT-002: Xem Chi tiết Danh mục và Công thức ....................................................... 24
             FR-CAT-003: Tạo Danh mục Mới [Admin]....................................................................... 25
```

<!-- Trang 4 -->

```text
             FR-CAT-004: Cập nhật Danh mục [Admin] ..................................................................... 26
             FR-CAT-005: Xóa Danh mục [Admin] .............................................................................. 26
           3.3. Module Quản lý Công thức Nấu ăn (FR-RCP)............................................................. 27

             FR-RCP-001: Xem Danh sách Công thức (Paginated + Filtered + Sorted) ................... 27
             FR-RCP-002: Xem Chi tiết Công thức ............................................................................. 28
             FR-RCP-003: Tạo Công thức Nấu ăn Mới [Author/Admin] ............................................. 29

             FR-RCP-004: Cập nhật Công thức [Author-Owner/Admin] ............................................. 30
             FR-RCP-005: Xuất bản / Hủy Xuất bản Công thức ......................................................... 31

             FR-RCP-006: Lưu trữ Công thức (Archive) ..................................................................... 32
             FR-RCP-007: Xóa Công thức [Author-Owner/Admin] ..................................................... 32
             FR-RCP-008: Quản lý Ảnh Công thức (Upload / Set Primary / Delete) .......................... 33

             FR-RCP-009: Quản lý Nguyên liệu (CRUD RecipeIngredient) ....................................... 34
             FR-RCP-010: Quản lý Các bước Thực hiện (CRUD RecipeStep).................................. 35
           3.4. Module Tìm kiếm và Phân trang (FR-SRCH) ............................................................... 36

             FR-SRCH-001: Tìm kiếm Toàn văn bản (Full-Text Search)............................................ 36
             FR-SRCH-002/003/004: Lọc, Sắp xếp và Phân trang (Tóm tắt) ..................................... 37
           3.5. Module Quản lý Tệp tin (FR-FILE) ............................................................................... 37

           3.6. Module Background Jobs (FR-JOB)............................................................................. 38
           3.7. Module Quan sát Hệ thống (FR-OBS).......................................................................... 39
          4. Yêu cầu Phi Chức năng (NFR)............................................................................................ 40

           4.1. Hiệu năng (NFR-PERF) ................................................................................................ 40
           4.2. Bảo mật (NFR-SEC) ..................................................................................................... 41

           4.3. Khả năng Sử dụng (NFR-USE) .................................................................................... 42
           4.4. Độ tin cậy (NFR-REL) ................................................................................................... 42
           4.5. Khả năng Bảo trì (NFR-MAINT).................................................................................... 43

           4.6. Khả năng Mở rộng (NFR-SCALE) ................................................................................ 44
           4.7. Tối ưu SEO (NFR-SEO) ............................................................................................... 44
          5. Yêu cầu Giao diện Ngoài ..................................................................................................... 46

           5.1. Giao diện Người dùng (UI) ........................................................................................... 46
           5.2. Giao diện Phần mềm – REST API ............................................................................... 47
           5.3. Giao diện Dịch vụ Bên thứ ba....................................................................................... 47

           5.4. Giao diện Phần cứng .................................................................................................... 48
          6. Kiến trúc Hệ thống ............................................................................................................... 50

           6.1. Tổng quan Kiến trúc...................................................................................................... 50
           6.2. Kiến trúc Backend – Clean Architecture....................................................................... 50
           6.3. CQRS + MediatR Pipeline ............................................................................................ 51

           6.4. Mô hình Quan hệ Thực thể (ERD tóm tắt) ................................................................... 52
           6.5. Triển khai – Docker Compose ...................................................................................... 52
          7. Mô hình Dữ liệu ................................................................................................................... 54
```

<!-- Trang 5 -->

```text
           7.1. BaseEntity (Abstract) .................................................................................................... 54
           7.2. Recipe ........................................................................................................................... 54
             7.2.1. RecipeNutrition (Owned Entity — cột trong bảng Recipes) .................................. 56

           7.3. RecipeStep .................................................................................................................... 57
           7.4. RecipeIngredient ........................................................................................................... 57
           7.5. RecipeImage ................................................................................................................. 58

           7.6. Category ........................................................................................................................ 58
           7.7. ApplicationUser (extends IdentityUser) ........................................................................ 58

           7.8. RefreshToken................................................................................................................ 59
          8. Đặc tả REST API ................................................................................................................. 61
           8.1. Authentication Module (/auth) ....................................................................................... 61

           8.2. Categories Module (/categories)................................................................................... 62
           8.3. Recipes Module (/recipes) ............................................................................................ 63
           8.4. Recipe Images (/recipes/{id}/images) ........................................................................... 64

           8.5. Recipe Steps (/recipes/{id}/steps) ................................................................................ 64
           8.6. Recipe Ingredients (/recipes/{id}/ingredients)............................................................... 65
           8.7. Health Check Endpoints ............................................................................................... 65

          Phụ lục A – HTTP Status Codes ............................................................................................. 67
          Phụ lục B – Application Error Codes ....................................................................................... 67
          Phụ lục C – Từ điển Thuật ngữ ............................................................................................... 69
```

<!-- Trang 6 -->

## CHƯƠNG 1. GIỚI THIỆU

### 1.1. Mục đích Tài liệu

```text
          Tài liệu Đặc tả Yêu cầu Phần mềm (Software Requirements Specification – SRS) này được
          biên soạn theo tiêu chuẩn IEEE 830-1998 và ISO/IEC/IEEE 29148:2018 nhằm mô tả đầy đủ,
          chính xác và nhất quán toàn bộ yêu cầu chức năng (Functional Requirements) và yêu cầu phi
          chức năng (Non-Functional Requirements) của dự án ứng dụng web Blog Ẩm thực và Nấu
          ăn (Culinary Blog).
          Tài liệu này phục vụ các đối tượng sau:
            •  Nhóm phát triển Backend (.NET 10/C#): Căn cứ thiết kế API, domain model, và
               business rules.
            •  Nhóm phát triển Frontend (Next.js/TypeScript): Căn cứ thiết kế giao diện, luồng
               người dùng và tích hợp API.

            •  Kỹ sư Kiểm thử (QA/QC): Cơ sở xây dựng test cases, kiểm thử chấp nhận
               (acceptance testing).
            •  Kiến trúc sư Hệ thống: Tham chiếu khi đưa ra quyết định kiến trúc (architecture
               decisions).
            •  Giảng viên và Sinh viên: Tài liệu học thuật mẫu cho dự án thực hành xuyên suốt
               giáo trình.
            •  Stakeholder / Product Owner: Phê duyệt phạm vi và ưu tiên tính năng.

          Phạm vi hiệu lực: Tài liệu này có hiệu lực từ phiên bản 1.0.0 và là tài liệu nền tảng (baseline)
          cho toàn bộ vòng đời phát triển dự án. Mọi thay đổi yêu cầu sau khi tài liệu được phê duyệt
          phải tuân theo quy trình quản lý thay đổi (Change Management Process).
```

### 1.2. Phạm vi Sản phẩm

#### 1.2.1. Tên và Định danh

```text
           Thuộc tính         Giá trị
           Tên sản phẩm       Culinary Blog – Blog Ẩm thực và Nấu ăn

           Định danh dự án    CULINARY-BLOG-V1
           Loại hệ thống      Ứng dụng Web Full-Stack (API-Driven Architecture)

           Phiên bản sản phẩm 1.0.0
           Môi trường đích    Cloud/On-premise (Docker Compose + Nginx)
```

#### 1.2.2. Mô tả Sản phẩm

```text
          Culinary Blog là một nền tảng web cho phép người dùng chia sẻ, khám phá và lưu trữ các
          công thức nấu ăn từ nhiều nền ẩm thực khác nhau. Ứng dụng cung cấp hệ sinh thái hoàn
          chỉnh bao gồm:
            •  Nền tảng chia sẻ công thức: Tác giả (Author) đăng tải công thức với hình ảnh,
               danh sách nguyên liệu chi tiết, hướng dẫn từng bước thực hiện và thông tin dinh
               dưỡng.
```

<!-- Trang 7 -->

```text
            •  Tổ chức nội dung: Phân loại công thức theo danh mục (Category), độ khó
               (Difficulty Level), thời gian chuẩn bị và nấu.
            •  Tìm kiếm thông minh: Full-Text Search tiếng Việt sử dụng PostgreSQL
               tsvector/tsquery với unaccent extension.
            •  Bảo mật đa lớp: Xác thực JWT stateless, phân quyền theo vai trò (RBAC) và theo
               tài nguyên (Resource-Based Authorization), đăng nhập Google OAuth 2.0.
            •  Tối ưu hiệu năng và SEO: Redis distributed cache, Next.js ISR, Open Graph
               Protocol, JSON-LD Schema.org Recipe markup.
            •  Quan sát hệ thống: Structured logging (Serilog), distributed tracing
               (OpenTelemetry), health check endpoints.
```

#### 1.2.3. Những gì KHÔNG thuộc phạm vi

```text
          Các tính năng sau đây nằm ngoài phạm vi phiên bản 1.0.0:

            •  Hệ thống bình luận (Comment System) và đánh giá sao (Rating System).
            •  Tính năng lưu/đánh dấu công thức yêu thích (Bookmark/Favorite).
            •  Thông báo real-time (SignalR/WebSocket).
            •  Ứng dụng di động native (iOS/Android).
            •  Thanh toán / Tính năng thương mại điện tử.
            •  Hệ thống nhắn tin trực tiếp giữa người dùng.
            •  GraphQL API (định hướng sau khóa học).
```

### 1.3. Định nghĩa, Từ viết tắt và Ký hiệu

```text
           Thuật ngữ / Viết tắt Định nghĩa đầy đủ

                              Software Requirements Specification – Đặc tả Yêu cầu Phần
           SRS
                              mềm.
           FR                 Functional Requirement – Yêu cầu chức năng.
           NFR                Non-Functional Requirement – Yêu cầu phi chức năng.
           API                Application Programming Interface – Giao diện lập trình ứng dụng.

                              Representational State Transfer – Kiểu kiến trúc API phổ biến
           REST
                              nhất.
           JWT                JSON Web Token – Chuẩn token xác thực stateless (RFC 7519).
           RBAC               Role-Based Access Control – Kiểm soát truy cập dựa trên vai trò.
                              Command Query Responsibility Segregation – Pattern tách biệt
           CQRS
                              lệnh và truy vấn.
                              Domain-Driven Design – Phương pháp thiết kế phần mềm lấy
           DDD
                              domain làm trung tâm.
                              Object-Relational Mapper – Công cụ ánh xạ object-database (EF
           ORM
                              Core).
           FTS                Full-Text Search – Tìm kiếm toàn văn bản.
                              Incremental Static Regeneration – Kỹ thuật tái tạo trang tĩnh của
           ISR
                              Next.js.
```

<!-- Trang 8 -->

```text
           Thuật ngữ / Viết tắt Định nghĩa đầy đủ
                              Largest Contentful Paint – Core Web Vital đo tốc độ tải nội dung
           LCP
                              lớn nhất.
                              Cumulative Layout Shift – Core Web Vital đo độ ổn định bố cục
           CLS
                              trang.
                              Interaction to Next Paint – Core Web Vital đo thời gian phản hồi
           INP
                              tương tác.
                              Continuous Integration / Continuous Delivery – Tích hợp và triển
           CI/CD
                              khai liên tục.
                              Device-independent pixel unit used in OOXML (1 inch = 1440
           DXA
                              DXA).
           TTL                Time-To-Live – Thời gian sống của dữ liệu trong cache.
           SSR                Server-Side Rendering – Render HTML trên server.
           SSG                Static Site Generation – Tạo trang tĩnh lúc build time.
                              Must Have / Should Have / Could Have / Won't Have – Mô hình
           MoSCoW
                              phân loại ưu tiên.
                              Request For Comments – Tài liệu tiêu chuẩn kỹ thuật (e.g., RFC
           RFC
                              7807).
           ERD                Entity Relationship Diagram – Sơ đồ quan hệ thực thể.
                              Password-Based Key Derivation Function 2 – Thuật toán hash mật
           PBKDF2
                              khẩu an toàn.
           CDN                Content Delivery Network – Mạng phân phối nội dung.
                              Multipurpose Internet Mail Extensions – Chuẩn định dạng tệp trên
           MIME
                              Internet.
                              JavaScript Object Notation for Linked Data – Định dạng dữ liệu có
           JSON-LD
                              cấu trúc cho SEO.
```

### 1.4. Tài liệu Tham chiếu

```text
               Tài liệu / Tiêu
           STT               Nguồn / URL
               chuẩn
               IEEE Std 830-
               1998 –
               Recommended
           1   Practice for  https://ieeexplore.ieee.org/document/720574
               Software
               Requirements
               Specifications
               ISO/IEC/IEEE
               29148:2018 –
           2                 https://www.iso.org/standard/72089.html
               Requirements
               Engineering
               OWASP Top
           3                 https://owasp.org/www-project-top-ten/
               10:2021 – Top 10
```

<!-- Trang 9 -->

```text
               Tài liệu / Tiêu
           STT               Nguồn / URL
               chuẩn
               Web Application
               Security Risks
               RFC 7807 –
           4   Problem Details https://datatracker.ietf.org/doc/html/rfc7807
               for HTTP APIs
               RFC 7519 –
           5   JSON Web Token https://datatracker.ietf.org/doc/html/rfc7519
               (JWT)
               RFC 6749 – The
               OAuth 2.0
           6                 https://datatracker.ietf.org/doc/html/rfc6749
               Authorization
               Framework
               .NET 10 Minimal
           7   APIs – Microsoft https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis
               Learn
               ASP.NET Core
           8   Identity –    https://learn.microsoft.com/aspnet/core/security/authentication/identity
               Microsoft Learn
               Entity Framework
           9   Core 10       https://learn.microsoft.com/ef/core/
               Documentation
               Next.js 15 App
           10  Router        https://nextjs.org/docs
               Documentation

               PostgreSQL 16
           11  Documentation – https://www.postgresql.org/docs/16/textsearch.html
               Full-Text Search
               Redis 7
           12                https://redis.io/docs/
               Documentation
               MinIO S3-
           13  Compatible    https://min.io/docs/
               Object Storage
               Google Web
           14  Vitals – Core Web https://web.dev/explore/learn-core-web-vitals
               Vitals
               Schema.org
           15  Recipe –      https://schema.org/Recipe
               Structured Data
               OpenTelemetry
           16  .NET          https://opentelemetry.io/docs/languages/dotnet/
               Documentation
               Serilog
           17                https://serilog.net/
               Documentation
               Hangfire
           18                https://docs.hangfire.io/
               Documentation
```

<!-- Trang 10 -->

```text
               Tài liệu / Tiêu
           STT               Nguồn / URL
               chuẩn
               FluentValidation
           19                https://docs.fluentvalidation.net/
               Documentation
               Giáo trình Phát
               triển Ứng dụng
           20                N/A (tài liệu nội bộ)
               Web Nâng cao V4
               – Nội bộ
```

### 1.5. Tổng quan Tài liệu

```text
          Tài liệu SRS này được tổ chức thành 8 chương chính và 3 phụ lục, theo cấu trúc từ tổng quan
          đến chi tiết:
            •  Chương 2 – Mô tả Tổng quan: Bối cảnh sản phẩm, chức năng tóm tắt, các lớp
               người dùng, môi trường vận hành và ràng buộc thiết kế.

            •  Chương 3 – Yêu cầu Chức năng: 38 FR được đặc tả chi tiết theo format chuẩn,
               nhóm thành 7 module chức năng.
            •  Chương 4 – Yêu cầu Phi chức năng: Hiệu năng, bảo mật, khả năng sử dụng, độ
               tin cậy, khả năng bảo trì/mở rộng và SEO.
            •  Chương 5 – Giao diện Ngoài: Tích hợp với các hệ thống và dịch vụ ngoài (Google
               OAuth, MinIO, Redis, SendGrid).
            •  Chương 6 – Kiến trúc Hệ thống: Clean Architecture Backend, Next.js App Router
               Frontend, chiến lược caching và deployment.
            •  Chương 7 – Mô hình Dữ liệu: ERD mô tả văn bản và bảng định nghĩa chi tiết từng
               entity/table.
            •  Chương 8 – Đặc tả API REST: Quy ước, chuẩn lỗi RFC 7807, và bảng tổng hợp tất
               cả ~30 endpoint.
            •  Phụ lục A-C: HTTP Status Codes, Application Error Codes, và Từ điển thuật ngữ.
```

<!-- Trang 11 -->

## CHƯƠNG 2. MÔ TẢ TỔNG QUAN HỆ THỐNG

### 2.1. Bối cảnh Sản phẩm

#### 2.1.1. Vị trí trong Hệ sinh thái

```text
          Culinary Blog vận hành theo mô hình API-Driven Architecture, trong đó Backend (.NET 10)
          và Frontend (Next.js) là hai hệ thống độc lập giao tiếp hoàn toàn qua HTTP/JSON RESTful
          API. Không có server-side rendering truyền thống (MVC Razor/Blazor) hay shared view
          engine giữa hai tầng.


          Sơ đồ bối cảnh hệ thống (Context Diagram):


            ┌─────────────────────────────────────────────────────────────────┐
            │               CULINARY BLOG SYSTEM             │
            │                                                │
            │  ┌──────────────────┐ ┌───────────────────────────────┐ │
            │  │ NEXT.JS FRONTEND│◄──────►│ .NET 10 BACKEND API │ │
            │  │ (App Router) │ REST │  (Minimal APIs + Clean Arch) │ │
            │  │ Port: 3000  │ JSON │   Port: 5000          │ │
            │  └──────────────────┘ └──────────────┬────────────────┘ │
            │                                 │              │
            │  ┌──────┐ ┌────────┐ ┌────────┐ ┌────────┐ ┌───────────┐ │
            │  │ Pgsql│ │ Redis │ │ MinIO │ │Hangfire│ │Google Auth│ │
            │  │:5432 │ │:6379 │ │:9000 │ │ Jobs │ │ OAuth2.0 │ │
            │  └──────┘ └────────┘ └────────┘ └────────┘ └───────────┘ │
            └─────────────────────────────────────────────────────────────────┘
                         Hình 2.1. Sơ đồ bối cảnh hệ thống Culinary Blog
```

#### 2.1.2. Quan hệ với Hệ thống Ngoài

```text
           Hệ thống Ngoài Vai trò        Giao thức / Chuẩn Hướng tích hợp
                       Hệ quản trị CSDL quan TCP + Npgsql Driver
           PostgreSQL 16                                Backend → PostgreSQL
                       hệ chính (RDBMS)  (EF Core)
                       Distributed Cache & TCP +
           Redis 7                                      Backend → Redis
                       Session Store     StackExchange.Redis
                       Object Storage cho ảnh HTTP/S3 API +
           MinIO (S3)                                   Backend → MinIO
                       công thức         MinIO .NET SDK
           Google OAuth Đăng nhập bên thứ ba HTTPS + OpenID Client ↔ Google ↔
           2.0         (Identity Provider) Connect      Backend
                       Background Job
           Hangfire                      In-process (.NET) Backend (internal)
                       Processing (embedded)
                       Structured Log
           Serilog / Seq Aggregation     HTTP Sink → Seq Backend → Seq
                       (development)
           OpenTelemetry Distributed Tracing &
                                         OTLP / gRPC    Backend → Collector
           Collector   Metrics (production)
```

<!-- Trang 12 -->

```text
           Hệ thống Ngoài Vai trò        Giao thức / Chuẩn Hướng tích hợp
           Nginx (Reverse SSL termination, load         Client → Nginx →
                                         HTTP/HTTPS
           Proxy)      balancing, static serving        Services
```

### 2.2. Chức năng Sản phẩm Tổng quát

```text
          Culinary Blog cung cấp 7 nhóm chức năng chính, được hiện thực hóa qua 27 Functional
          Requirements chi tiết tại Chương 3:


                                    Số
           Nhóm chức năng   Mã nhóm       Mô tả tóm tắt
                                    FR
           Xác thực & Quản lý             Đăng ký, đăng nhập (email + Google), JWT
                            FR-AUTH 10
           Người dùng                     refresh token, logout, quản lý profile.
                                          CRUD danh mục công thức (Category) – phân
           Quản lý Danh mục FR-CAT  5
                                          quyền Admin.
           Quản lý Công thức nấu          CRUD recipe, publish/archive, quản lý
                            FR-RCP  10
           ăn                             ảnh/bước/nguyên liệu.
                                          Full-Text Search (PostgreSQL), filter, sort,
           Tìm kiếm & Phân trang FR-SRCH 4
                                          offset pagination.
           Quản lý Tệp tin  FR-FILE 2     Upload/Delete ảnh trên MinIO S3-compatible.
                                          Email chào mừng, thumbnail generation,
           Background Jobs  FR-JOB  4
                                          sitemap XML (Hangfire).
                                          Health checks, structured logging, distributed
           Quan sát Hệ thống FR-OBS 3
                                          tracing.
```

### 2.3. Các Lớp Người dùng và Đặc điểm

```text
          Hệ thống định nghĩa 3 loại tác nhân (Actor) với quyền hạn khác nhau:
                                                                  Ưu tiên
           Vai trò    Mô tả         Điều kiện  Quyền hạn chính    phục
                                                                  vụ
                                                                  Cao
                      Người dùng chưa
                                               Xem danh sách & chi tiết (đây là
                      xác thực, truy cập
           Khách (Guest             Không cần tài recipe (Published), xem đại đa
                      ứng dụng mà
           / Anonymous)             khoản      danh mục, tìm kiếm. số
                      không có tài
                                               KHÔNG được tạo/sửa/xóa. người
                      khoản.
                                                                  dùng)
                                               + Tất cả quyền của Guest.
                      Người dùng đã                               Cao
                                               + Tạo/sửa/xóa recipe CỦA
                      đăng ký và xác                              (nhà
           Tác giả                  Có tài khoản & MÌNH. + Upload ảnh, quản
                      thực thành công.                            sản
           (Author)                 JWT hợp lệ lý steps/ingredients. +
                      Được tự động gán                            xuất nội
                                               Publish/Archive recipe của
                      khi đăng ký.                                dung)
                                               mình.
                      Người quản lý hệ         + Tất cả quyền của Author.
           Quản trị viên            Có tài khoản &                Trung
                      thống với quyền          + Quản lý (CRUD) danh
           (Admin)                  role Admin                    bình (số
                      cao nhất. Được           mục. + Sửa/xóa bất kỳ
```

<!-- Trang 13 -->

```text
                                                                  Ưu tiên
           Vai trò    Mô tả         Điều kiện  Quyền hạn chính    phục
                                                                  vụ
                      gán thủ công qua         recipe của bất kỳ Author. + lượng
                      database seeding.        Truy cập Hangfire  ít)
                                               Dashboard. + Xem
                                               structured logs.

          Ghi chú về phân quyền: Hệ thống triển khai 3 tầng phân quyền. (1) Role-Based
          Authorization: phân biệt quyền dựa trên role (Guest/Author/Admin). (2) Resource-Based
          Authorization: Author chỉ sửa/xóa được recipe của chính mình (AuthorId == currentUserId).
          (3) Policy-Based Authorization: Policy "VerifiedAuthor" yêu cầu email đã xác nhận. Admin có
          quyền bypass resource ownership check.
```

### 2.4. Môi trường Vận hành

#### 2.4.1. Môi trường Server (Production)

```text
           Thành phần Yêu cầu tối thiểu Khuyến nghị Ghi chú

                      Linux Ubuntu  Ubuntu 22.04
           Hệ điều hành                         Docker phải được cài đặt
                      22.04 LTS     LTS / Debian 12
                      .NET 10.0 Runtime .NET 10.0.x Cung cấp qua Docker image
           .NET Runtime
                      (aspnet)      latest patch mcr.microsoft.com/dotnet/aspnet:10.0
                      Node.js 20 LTS            Chỉ cần lúc build Next.js; production
           Node.js                  Node.js 22 LTS
                      (build only)              dùng standalone output
                                    PostgreSQL  Extensions: unaccent, pg_trgm bắt
           PostgreSQL PostgreSQL 16.x
                                    16.x        buộc
           Redis      Redis 7.x     Redis 7.2.x Persistent mode với AOF
                      MinIO         MinIO latest Bucket policy: public-read cho recipe
           MinIO
                      RELEASE.2024+ stable      images
                                    Docker Engine
                      Docker Engine             Docker Compose cho local dev và
           Docker                   27.x + Compose
                      24.x                      staging
                                    v2
                                    Nginx 1.26+
           Nginx      Nginx 1.24+               Reverse proxy, SSL termination
                                    (stable)
           RAM        4 GB minimum  8 GB+       RAM cần tăng nếu Redis cache lớn
                                                CPU-intensive: FTS indexing, image
           CPU        2 vCPU minimum 4 vCPU+
                                                processing
                      20 GB SSD
           Disk                     50 GB+ SSD  MinIO object storage tốn nhiều disk
                      minimum
```

#### 2.4.2. Môi trường Phát triển (Development)

```text
           Thành phần      Yêu cầu
           .NET 10 SDK     dotnet SDK 10.0.x (bao gồm CLI và runtime)
```

<!-- Trang 14 -->

```text
           Thành phần      Yêu cầu
           Node.js         Node.js 20+ LTS với npm 10+

                           Docker Desktop 4.x+ (Windows/macOS) hoặc Docker Engine (Linux) –
           Docker Desktop
                           để chạy PostgreSQL, Redis, MinIO local
                           Visual Studio 2022 v17.12+ / Rider 2024+ / VS Code với C# Dev Kit
           IDE / Editor
                           extension
           Git             Git 2.40+ với Git LFS (nếu lưu asset lớn)
           Postman / Scalar Postman hoặc Scalar UI (tích hợp sẵn, chạy tại /scalar) để test API
```

#### 2.4.3. Yêu cầu Trình duyệt Client

```text
           Trình duyệt        Phiên bản tối thiểu Ghi chú
                                             Khuyến nghị chính – tốt nhất cho
           Google Chrome      90+
                                             Developer Tools
           Mozilla Firefox    88+            Hỗ trợ đầy đủ
           Microsoft Edge     90+ (Chromium) Hỗ trợ đầy đủ (Chromium-based)

                                             Hỗ trợ đầy đủ; Safari 13 trở xuống
           Safari             14+ (macOS 11+)
                                             KHÔNG đảm bảo
           Mobile Chrome (Android) 90+       Responsive design, touch-friendly
           Mobile Safari (iOS) iOS 14+       Hỗ trợ đầy đủ
           Internet Explorer  Mọi phiên bản  KHÔNG hỗ trợ (EOL)
```

### 2.5. Ràng buộc Thiết kế và Hiện thực

```text
          Các ràng buộc sau đây là bắt buộc và không thể thương lượng trong suốt quá trình phát triển:


           Mã ràng
                     Loại     Mô tả ràng buộc
           buộc
                              Backend PHẢI tuân thủ Clean Architecture với 4 tầng riêng biệt:
           CONS-001  Kiến trúc Domain, Application, Infrastructure, Presentation. Tầng Domain
                              không được phụ thuộc bất kỳ thư viện ngoài nào.
                              CQRS với MediatR là pattern bắt buộc cho tầng Application. Mỗi
           CONS-002  Pattern  use case được hiện thực dưới dạng Command hoặc Query
                              Handler riêng biệt.
                     Ngôn ngữ / Backend: .NET 10 Minimal APIs (không dùng MVC Controllers).
           CONS-003
                     Framework Frontend: Next.js App Router (không dùng Pages Router).
                              Xác thực PHẢI sử dụng JWT stateless (access token 15 phút,
           CONS-004  Bảo mật  refresh token 7 ngày). Mật khẩu PHẢI được hash với PBKDF2 qua
                              ASP.NET Core Identity.
                              API PHẢI tuân thủ RESTful design. Phản hồi lỗi PHẢI theo RFC
           CONS-005  API Design 7807 (application/problem+json). API versioning qua URL path
                              (/api/v1/).
```

<!-- Trang 15 -->

```text
           Mã ràng
                     Loại     Mô tả ràng buộc
           buộc
                              PostgreSQL là DBMS duy nhất. Migrations qua EF Core Code-
           CONS-006  Database First. Không viết raw SQL trực tiếp (dùng LINQ hoặc Raw SQL có
                              parameterization qua EF Core).
                              Kích thước tệp tải lên tối đa 5 MB. Định dạng chỉ chấp nhận:
           CONS-007  File Upload image/jpeg, image/png, image/webp, image/avif. Kiểm tra MIME
                              type (không chỉ extension).
                              Input validation PHẢI qua FluentValidation kết hợp MediatR
           CONS-008  Validation
                              Pipeline Behavior. Không validation trong Endpoint handler.
                              Ứng dụng PHẢI được đóng gói Docker. Dockerfile multi-stage
           CONS-009  Container build (SDK → aspnet runtime). Docker Compose cho local
                              development.
                              Structured logging với Serilog là bắt buộc. Mọi log entry PHẢI có
           CONS-010  Logging
                              CorrelationId, RequestPath, UserId (khi đã xác thực).
```

### 2.6. Giả định và Phụ thuộc

#### 2.6.1. Giả định

```text
            •  Môi trường development có kết nối Internet để pull Docker images và package
               NuGet/npm.
            •  PostgreSQL, Redis và MinIO được cung cấp qua Docker Compose trong
               development và dưới dạng managed service (hoặc VPS) trong production.
            •  Người dùng cuối có trình duyệt hiện đại và kết nối Internet đủ ổn định để load ảnh từ
               MinIO.
            •  Dữ liệu test (seed) được tạo bằng thư viện Bogus với 50 recipe mẫu và 5 tác giả
               mẫu.
            •  Email service (SendGrid hoặc SMTP) được cấu hình sẵn khi triển khai production để
               gửi email chào mừng.

            •  Giới hạn dữ liệu kỳ vọng (initial scale): ≤ 10,000 công thức, ≤ 5,000 người dùng,
               ≤ 50 danh mục – phù hợp với single-server deployment.
```

#### 2.6.2. Phụ thuộc Bên ngoài

```text
                                    Mức độ ảnh hưởng
           Phụ thuộc   Phiên bản                    Kế hoạch dự phòng
                                    nếu không khả dụng
                                                    Vẫn có đăng nhập
           Google OAuth v2 (OpenID  Cao – Mất chức năng email/password. Hiển thị thông
           2.0 API     Connect)     đăng nhập Google báo "Google login tạm thời
                                                    không khả dụng".
                                                    Fallback về local FileSystem
                       MinIO        Cao – Không
           MinIO / S3                               storage (development only).
                       RELEASE.2024+ upload/xem được ảnh
                                                    Production cần MinIO.
                                                    Hệ thống tiếp tục hoạt động
                                    Trung bình – Mất nhưng mọi request đều query
           Redis       7.x
                                    cache, hiệu năng giảm database. Cache miss graceful
                                                    degradation.
```

<!-- Trang 16 -->

```text
                                    Mức độ ảnh hưởng
           Phụ thuộc   Phiên bản                    Kế hoạch dự phòng
                                    nếu không khả dụng
                                                    Backup định kỳ (pg_dump).
                                    Rất cao – Toàn bộ hệ
           PostgreSQL  16.x                         Readiness probe sẽ fail, Nginx
                                    thống ngừng
                                                    trả 503.
                                                    Fire-and-forget jobs sẽ bị mất;
           Hangfire (in-            Thấp – Background Recurring jobs bỏ qua chu kỳ.
                       v1.8+
           process)                 jobs không chạy Không ảnh hưởng core
                                                    functionality.
```

<!-- Trang 17 -->

## CHƯƠNG 3. YÊU CẦU CHỨC NĂNG CHI TIẾT

```text
          Chương này đặc tả chi tiết 38 Functional Requirements (FR) được nhóm thành 7 module
          chức năng. Mỗi FR được mô tả theo template chuẩn bao gồm: Mã yêu cầu, Tên, Nhóm chức
          năng, Tác nhân, Mức ưu tiên (MoSCoW), Mô tả, Điều kiện tiên quyết, Luồng chính, Luồng
          thay thế/Ngoại lệ, HTTP Endpoint, Kết quả mong đợi và HTTP Status Code.

          Quy ước mức ưu tiên MoSCoW: M (Must Have – Bắt buộc), S (Should Have – Nên có), C
          (Could Have – Có thể có), W (Won't Have – Không trong scope hiện tại).
```

### 3.1. Module Xác thực và Quản lý Người dùng (FR-AUTH)

```text
          Module này quản lý toàn bộ vòng đời xác thực người dùng: từ đăng ký, đăng nhập đa phương
          thức, duy trì phiên làm việc với cơ chế token rotation, đến quản lý hồ sơ cá nhân. Backend
          sử dụng ASP.NET Core Identity kết hợp JWT và OAuth 2.0.
```

#### FR-AUTH-001 - Đăng ký

- `POST /api/v1/auth/register`
- Request: `email`, `password`, `displayName`.
- Email duy nhất, case-insensitive.
- Password tối thiểu 8 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt.
- Tạo `ApplicationUser`, gán role Author; Identity `UserName` nội bộ bằng normalized email.
- Tạo access token và refresh token; gửi email xác nhận bất đồng bộ.
- Response `201` gồm user và token pair.
- Email trùng trả 409; validation trả 422.

#### FR-AUTH-002: Đăng nhập bằng Email/Mật khẩu (Local Login)

```text
           Mã yêu cầu      FR-AUTH-002
           Tên yêu cầu     Đăng nhập bằng Email và Mật khẩu

           Nhóm chức năng  Module Xác thực và Quản lý Người dùng (FR-AUTH)
           Tác nhân        Tác giả đã đăng ký (Author) hoặc Quản trị viên (Admin)

           Mức ưu tiên     M – Must Have (Bắt buộc)
           (MoSCoW)
                           Hệ thống cho phép người dùng đã có tài khoản đăng nhập bằng email
                           và mật khẩu. Mỗi lần đăng nhập thành công tạo ra một cặp access
           Mô tả           token mới (JWT, 15 phút) và refresh token mới (7 ngày). Cơ chế Token
                           Rotation: refresh token cũ KHÔNG bị xóa ngay mà được đánh dấu đã
                           sử dụng (để phát hiện token reuse attack).
                           1. Người dùng đã có tài khoản hợp lệ trong hệ thống. 2. Tài khoản
           Điều kiện tiên quyết chưa bị khóa (LockoutEnabled = false hoặc chưa đến lockout
                           deadline).
```

<!-- Trang 19 -->

```text
                           1. Client gửi POST /api/v1/auth/login với body: { "email": "...",
                           "password": "..." }.
                           2. LoginCommand được dispatch qua MediatR.
                           3. ValidationBehavior kiểm tra email format và password không rỗng.
                           4. LoginCommandHandler tìm user:
                           UserManager.FindByEmailAsync(email).
                           5. Xác minh mật khẩu: UserManager.CheckPasswordAsync(user,
                           password) – so sánh với PBKDF2 hash.
           Luồng chính (Happy
                           6. Kiểm tra tài khoản không bị lockout:
           Path)
                           UserManager.IsLockedOutAsync(user).
                           7. Tạo access token mới: JwtService.GenerateAccessToken(user,
                           roles).
                           8. Tạo refresh token mới: JwtService.GenerateRefreshToken(userId).
                           9. Lưu refresh token mới vào database.
                           10. Ghi nhận đăng nhập thành công:
                           UserManager.ResetAccessFailedCountAsync(user).
                           11. Trả về HTTP 200 OK với AuthResponseDto.
                           A1 – Tài khoản không tồn tại hoặc mật khẩu sai: HTTP 401
                           Unauthorized với message generic "Email hoặc mật khẩu không đúng"
                           (KHÔNG tiết lộ tài khoản có tồn tại hay không – tránh User
                           Enumeration Attack).
           Luồng thay thế /
           Ngoại lệ        A2 – Tài khoản bị lockout: HTTP 423 Locked với thông báo thời gian
                           unlock còn lại.
                           A3 – Vượt quá số lần thử sai (5 lần): AccessFailedCount tăng lên, sau
                           5 lần → tài khoản bị lockout 15 phút (cấu hình qua LockoutOptions).
           HTTP Method &   POST /api/v1/auth/login
           Endpoint
                           Access token và refresh token mới được tạo và trả về. Refresh token
           Kết quả mong đợi
                           được lưu vào database.
                           200 OK – Đăng nhập thành công. 401 Unauthorized – Sai email/mật
           HTTP Status Code trả
                           khẩu. 422 Unprocessable Entity – Dữ liệu không hợp lệ. 423 Locked –
           về
                           Tài khoản bị khóa.
```

#### FR-AUTH-003 - Đăng nhập Google

- Frontend dùng Google Identity Services để nhận ID token.
- `POST /api/v1/auth/google` với `idToken`.
- Backend xác minh signature, issuer, audience, expiry và nonce bằng thư viện Google chính thức.
- Nếu provider key đã liên kết, đăng nhập user tương ứng.
- Nếu email đã tồn tại và Google báo email verified, liên kết login với user đó.
- Nếu chưa tồn tại, tạo user mới, gán Author và lấy display name/avatar từ claim.
- Google token sai hoặc hết hạn trả 401 `AUTH_GOOGLE_TOKEN_INVALID`.
- Hệ thống không dùng Auth.js session song song trong v1.1.

#### FR-AUTH-004 - Refresh token

- `POST /api/v1/auth/refresh` với raw refresh token.
- Raw token dài 32 random bytes, Base64URL; chỉ SHA-256 hash được lưu.
- Token hợp lệ khi chưa hết hạn, chưa revoked, user active.
- Trong một transaction: revoke token cũ, tạo token mới cùng `FamilyId`, ghi `ReplacedByTokenHash` và phát access token mới.
- Nếu token đã revoked bị dùng lại, revoke toàn bộ token trong family và log security warning.
- Token sai/hết hạn/revoked trả 401.

#### FR-AUTH-005 - Logout

- `POST /api/v1/auth/logout` với `refreshToken`; không yêu cầu access token.
- Hash token và revoke nếu tồn tại.
- Luôn trả 204, kể cả token không tồn tại hoặc đã revoked.
- Logout một thiết bị chỉ revoke token family của phiên đó.

#### FR-AUTH-006 - Xem profile

- `GET /api/v1/auth/me`, Bearer JWT.
- Trả `id`, `email`, `displayName`, `avatarUrl`, `bio`, `roles`, `emailConfirmed`, `createdAt`.
- Không trả password hash, security stamp hoặc token data.

#### FR-AUTH-007 - Cập nhật profile

- `PATCH /api/v1/auth/me` với `displayName?`, `avatarUrl?`, `bio?`.
- `displayName`: 2-100 ký tự; `avatarUrl`: URL hợp lệ; `bio`: tối đa 1000 ký tự.
- Email và username không thay đổi qua endpoint này.

#### FR-AUTH-008 - Xác nhận email

- `POST /api/v1/auth/email/confirm` với `userId`, `token`.
- Token hợp lệ xác nhận email và trả 204; thao tác idempotent.
- Chỉ user có email confirmed hoặc Admin mới được publish Recipe.

#### FR-AUTH-009 - Gửi lại email xác nhận

- `POST /api/v1/auth/email/resend` với `email`.
- Luôn trả 202 với message chung để tránh user enumeration.
- Áp dụng rate limit auth.

#### FR-AUTH-010 - Admin đổi trạng thái user

- `PATCH /api/v1/admin/users/{id}/status` với `isActive`.
- Chỉ Admin.
- Khi chuyển sang inactive, revoke toàn bộ refresh token của user.
- User không được tự vô hiệu hóa tài khoản của chính mình qua endpoint này.

### 3.2. Module Quản lý Danh mục (FR-CAT)

```text
          Module quản lý danh mục (Category) phân loại công thức nấu ăn. Danh mục được tạo và duy
          trì bởi Admin; Author và Guest chỉ có quyền đọc. Mỗi danh mục có Slug duy nhất phục vụ
          URL thân thiện SEO. Danh mục công khai được cache trên Redis theo cache-aside (TTL 30 phút) vì thay đổi ít
          thường xuyên.
```

#### FR-CAT-001: Xem Danh sách Danh mục

```text
           Mã yêu cầu      FR-CAT-001
           Tên yêu cầu     Xem Danh sách Tất cả Danh mục
```

<!-- Trang 24 -->

```text
           Nhóm chức năng  Module Quản lý Danh mục (FR-CAT)
           Tác nhân        Tất cả (Guest / Author / Admin)

           Mức ưu tiên     M – Must Have
           (MoSCoW)
                           Trả về danh sách tất cả danh mục công thức hiện có trong hệ thống,
                           kèm số lượng công thức đã xuất bản (Published) trong mỗi danh mục.
           Mô tả
                           Kết quả được cache với Redis distributed cache (TTL 30 phút) và sắp xếp theo
                           Name tăng dần.
                           1. Ít nhất một danh mục tồn tại trong database (hoặc trả về mảng rỗng).
           Điều kiện tiên quyết
                           2. Không yêu cầu xác thực.
                           1. Client gửi GET /api/v1/categories.
                           2. GetCategoriesQuery dispatch qua MediatR.
                           3. Handler kiểm tra Redis distributed cache với key "categories:all".
                           4. Cache hit: trả về dữ liệu từ cache.
           Luồng chính (Happy
           Path)           5. Cache miss: query database
                           (IUnitOfWork.Categories.GetAllWithRecipeCount()), map sang
                           CategoryDto[].
                           6. Lưu vào Redis distributed cache với TTL 30 phút (absolute expiration).
                           7. Trả về HTTP 200 OK với CategoryDto[].
           Luồng thay thế / A1 – Không có danh mục nào: HTTP 200 OK với mảng rỗng [].
           Ngoại lệ
           HTTP Method &   GET /api/v1/categories
           Endpoint
                           Mảng CategoryDto[] với các field: { id, name, slug, description,
           Kết quả mong đợi
                           recipeCount }. Kết quả được serve từ cache khi có.
           HTTP Status Code trả 200 OK – Thành công (kể cả khi trống).
           về
```

#### FR-CAT-002: Xem Chi tiết Danh mục và Công thức

```text
           Mã yêu cầu      FR-CAT-002
           Tên yêu cầu     Xem Chi tiết Danh mục và Danh sách Công thức thuộc Danh mục

           Nhóm chức năng  Module Quản lý Danh mục (FR-CAT)
           Tác nhân        Tất cả (Guest / Author / Admin)

           Mức ưu tiên     M – Must Have
           (MoSCoW)
                           Trả về thông tin chi tiết của một danh mục cụ thể (theo Slug) kèm danh
                           sách phân trang các công thức đã xuất bản (Published) thuộc danh
           Mô tả
                           mục đó. Guest chỉ thấy Published recipes; Author thấy thêm Draft
                           recipes của chính mình trong danh mục.
                           1. Danh mục với slug tương ứng phải tồn tại. 2. Không yêu cầu xác
           Điều kiện tiên quyết
                           thực.
           Luồng chính (Happy 1. Client gửi GET /api/v1/categories/{slug}?page=1&pageSize=12.
           Path)           2. GetCategoryBySlugQuery dispatch qua MediatR.
```

<!-- Trang 25 -->

```text
                           3. Handler tìm category theo slug:
                           _unitOfWork.Categories.GetBySlugAsync(slug).
                           4. Query recipes thuộc category với Status == Published (+ Draft của
                           currentUser nếu đã đăng nhập).
                           5. Apply pagination (OFFSET-based: SKIP (page-1)*pageSize TAKE
                           pageSize).
                           6. Map sang CategoryDetailDto kèm
                           PagedResult<RecipeSummaryDto>.
                           7. Trả về HTTP 200 OK.

           Luồng thay thế / A1 – Slug không tồn tại: HTTP 404 Not Found với RFC 7807 body.
           Ngoại lệ
           HTTP Method &   GET /api/v1/categories/{slug}?page={n}&pageSize={n}
           Endpoint
                           { category: CategoryDto, recipes: { items: RecipeSummaryDto[],
           Kết quả mong đợi
                           totalCount, page, pageSize, totalPages } }
           HTTP Status Code trả 200 OK – Thành công. 404 Not Found – Slug không tồn tại.
           về
```

#### FR-CAT-003: Tạo Danh mục Mới [Admin]

```text
           Mã yêu cầu      FR-CAT-003
           Tên yêu cầu     Tạo Danh mục Công thức Mới

           Nhóm chức năng  Module Quản lý Danh mục (FR-CAT)
           Tác nhân        Quản trị viên (Admin)

           Mức ưu tiên     M – Must Have
           (MoSCoW)
                           Admin tạo danh mục công thức mới. Slug được tự động sinh từ Name
                           (slugify: chuyển sang chữ thường, bỏ dấu, thay khoảng trắng bằng "-").
           Mô tả           Nếu Slug đã tồn tại, hệ thống thêm suffix số (e.g., "mon-chinh-2"). Sau
                           khi tạo, cache danh mục (Redis distributed cache key "categories:all") bị
                           invalidate.
                           1. Người dùng đang đăng nhập với role Admin. 2. Name chưa tồn tại
           Điều kiện tiên quyết
                           trong database.
                           1. Admin gửi POST /api/v1/categories với Authorization: Bearer
                           {adminJwt} và body: { "name": "...", "description": "..." }.
                           2. RequireAuthorization("Admin") middleware kiểm tra role.
                           3. CreateCategoryCommand dispatch qua MediatR.
                           4. ValidationBehavior: name 2–50 ký tự, không chứa HTML.
                           5. SlugHelper.Generate(name) tạo slug.
           Luồng chính (Happy
                           6. Kiểm tra slug chưa tồn tại. Nếu trùng, thêm "-2", "-3",... cho đến khi
           Path)
                           unique.
                           7. Category.Create(name, slug, description) tạo entity.
                           8. _unitOfWork.Categories.AddAsync(entity).
                           9. _unitOfWork.SaveChangesAsync().
                           10. CacheInvalidationService.Remove("categories:all") – invalidate cache.
                           11. Trả về HTTP 201 Created với CategoryDto và Location header.
```

<!-- Trang 26 -->

```text
           Luồng thay thế / A1 – Thiếu role Admin: HTTP 403 Forbidden.
           Ngoại lệ        A2 – Dữ liệu không hợp lệ: HTTP 422.

           HTTP Method &   POST /api/v1/categories
           Endpoint
                           Danh mục mới được tạo trong database. Cache danh mục bị xóa.
           Kết quả mong đợi
                           Location header trỏ đến /api/v1/categories/{newSlug}.
                           201 Created – Tạo thành công. 403 Forbidden – Không có quyền
           HTTP Status Code trả
                           Admin. 409 Conflict – Name đã tồn tại. 422 Unprocessable Entity – Dữ
           về
                           liệu không hợp lệ.
```

#### FR-CAT-004: Cập nhật Danh mục [Admin]

```text
           Mã yêu cầu      FR-CAT-004
           Tên yêu cầu     Cập nhật Thông tin Danh mục

           Nhóm chức năng  Module Quản lý Danh mục (FR-CAT)
           Tác nhân        Quản trị viên (Admin)

           Mức ưu tiên     M – Must Have
           (MoSCoW)
                           Admin cập nhật Name và/hoặc Description của danh mục. Slug
           Mô tả           KHÔNG thay đổi khi đổi tên (để tránh broken links). Sau khi cập nhật,
                           cache bị invalidate.

           Điều kiện tiên quyết 1. Admin đang đăng nhập. 2. Danh mục với ID tương ứng tồn tại.
                           1. Admin gửi PUT /api/v1/categories/{id} với body: { "name": "...",
                           "description": "..." }.
                           2. Kiểm tra role Admin.
           Luồng chính (Happy
                           3. UpdateCategoryCommand dispatch qua MediatR.
           Path)
                           4. Tìm category theo ID, cập nhật Name và Description.
                           5. Lưu thay đổi, invalidate cache.
                           6. Trả về HTTP 200 OK với CategoryDto đã cập nhật.
           Luồng thay thế / A1 – ID không tồn tại: HTTP 404.
           Ngoại lệ        A2 – Thiếu role Admin: HTTP 403.
           HTTP Method &   PUT /api/v1/categories/{id:guid}
           Endpoint
           Kết quả mong đợi Thông tin danh mục được cập nhật. Cache invalidated.

           HTTP Status Code trả 200 OK – Cập nhật thành công. 403 Forbidden. 404 Not Found. 422
           về              Unprocessable Entity.
```

#### FR-CAT-005 - Xóa Category

- `DELETE /api/v1/categories/{id}`, Admin.
- Soft delete khi không còn Recipe chưa soft-delete thuộc Category, bất kể trạng thái Recipe.
- Nếu còn Recipe, trả 409 `CATEGORY_DELETE_HAS_RECIPES`.

### 3.3. Module Quản lý Công thức Nấu ăn (FR-RCP)

```text
          Module cốt lõi của hệ thống. Recipe là aggregate root chứa các child entity: RecipeStep,
          RecipeIngredient, RecipeImage và Owned Entity RecipeNutrition. Tất cả mutation
          (Create/Update/Delete) đi qua UnitOfWork để đảm bảo tính nhất quán transaction.
          Concurrency được xử lý qua ETag mã hóa từ PostgreSQL xmin để phát hiện lost update khi hai Author
          cùng sửa một recipe.
```

#### FR-RCP-001 - Danh sách Recipe

- `GET /api/v1/recipes`.
- Mặc định public và chỉ trả Published.
- `mine=true` yêu cầu JWT, trả Recipe của user và cho phép filter `status`.
- Admin có thể dùng `authorId`; Author thường không được dùng filter này.
- Public list cache Redis 1 phút; private list không dùng shared cache.

#### FR-RCP-002: Xem Chi tiết Công thức

```text
           Mã yêu   FR-RCP-002
           cầu
           Tên yêu  Xem Chi tiết Công thức Nấu ăn
           cầu

           Nhóm     Module Quản lý Công thức Nấu ăn (FR-RCP)
           chức
           năng
           Tác nhân Tất cả (Guest / Author / Admin)

           Mức ưu   M – Must Have
           tiên
           (MoSCoW)
                    Trả về toàn bộ thông tin chi tiết của một công thức cụ thể, bao gồm: thông tin cơ bản, danh sách nguyên liệu
                    (RecipeIngredient[]) sắp xếp theo SortOrder, các bước thực hiện (RecipeStep[]) sắp xếp theo StepNumber, ảnh
           Mô tả    minh họa (RecipeImage[]), thông tin dinh dưỡng (RecipeNutrition), thông tin danh mục và tác giả. Recipe Draft
                    chỉ được xem bởi tác giả sở hữu hoặc Admin. Endpoint được cache với Redis cache-aside policy "RecipeDetail"
                    (TTL 5 phút) và tagged với "recipes" để hỗ trợ tag-based invalidation.
```

<!-- Trang 29 -->

```text
           Điều kiện 1. Recipe với slug tương ứng tồn tại. 2. Nếu Recipe ở trạng thái Draft/Archived: người yêu cầu phải là tác giả
           tiên quyết hoặc Admin.
                    1. Client gửi GET /api/v1/recipes/{slug}.
                    2. GetRecipeBySlugQuery dispatch qua MediatR.

           Luồng    3. Handler query Recipe với Eager Loading:
           chính    Include(Steps).Include(Ingredients).Include(Images).Include(Category).Include(Author).IncludeOwned(Nutrition).
           (Happy   4. Kiểm tra null → NotFoundException nếu không tìm thấy.
           Path)
                    5. Kiểm tra Status: nếu Draft/Archived → chỉ tác giả hoặc Admin mới được xem (Authorization check).
                    6. Map sang RecipeDetailDto (bao gồm tất cả nested collections).
                    7. Trả về HTTP 200 OK. Lưu cache entry với tags với ["recipes", $"recipe:{slug}"].
           Luồng    A1 – Slug không tồn tại: HTTP 404 Not Found.
           thay thế / A2 – Recipe Draft/Archived, người dùng không có quyền: HTTP 403 Forbidden.
           Ngoại lệ
           HTTP     GET /api/v1/recipes/{slug}
           Method &
           Endpoint
           Kết quả  RecipeDetailDto đầy đủ gồm tất cả nested data (steps, ingredients, images, nutrition, category, author).
           mong đợi
           HTTP     200 OK – Thành công. 403 Forbidden – Không có quyền xem Draft. 404 Not Found – Slug không tồn tại.
           Status
           Code trả
           về
```

#### FR-RCP-003 - Tạo Recipe

- `POST /api/v1/recipes`, Author/Admin.
- Request: `title`, `description`, `categoryId`, `prepTime`, `cookTime`, `servings`, `difficulty`, `nutrition?`.
- Trong MVP, Nutrition chỉ do Author nhập thủ công; hệ thống không tự suy ra dinh dưỡng từ Ingredient.
- Server luôn gán `nutrition.source=Manual`. Client không được tự gửi hoặc thay đổi `source`.
- Giao diện phải hiển thị lưu ý: “Thông tin dinh dưỡng do tác giả cung cấp và chỉ mang tính tham khảo.”
- `title` 3-200; `description` 10-2000; `prepTime > 0`; `cookTime >= 0`; `servings > 0`.
- Category phải tồn tại và chưa xóa.
- Recipe mới ở Draft.
- Sinh slug duy nhất bằng hậu tố `-1`, `-2`... trong transaction khi cần.
- Hướng dẫn chi tiết chỉ lưu bằng RecipeStep; không có trường Instructions legacy.

#### FR-RCP-004 - Cập nhật Recipe

- `PUT /api/v1/recipes/{id}`, Owner/Admin, yêu cầu `If-Match`.
- Cập nhật các field cơ bản và nutrition.
- Draft đổi title có thể đổi slug; slug cũ được ghi RecipeSlugHistory.
- Published hoặc Archived không đổi slug.
- Thành công trả Recipe mới và ETag mới; conflict trả 409.

#### FR-RCP-005 - Publish/Unpublish

- `PATCH /api/v1/recipes/{id}/publish` và `/unpublish`, Owner/Admin, yêu cầu `If-Match`.
- Publish yêu cầu:
  - Author đã xác nhận email hoặc là Admin.
  - Ít nhất 1 Ingredient và 1 Step.
  - Các field cơ bản hợp lệ và Category đang active.
- Publish đặt `Status=Published`, `PublishedAt` nếu là lần publish đầu.
- Unpublish đặt `Status=Draft`, giữ `PublishedAt` làm lịch sử.
- Thao tác idempotent; vi phạm điều kiện trả 422.

#### FR-RCP-006: Lưu trữ Công thức (Archive)

```text
           Mã yêu cầu      FR-RCP-006

           Tên yêu cầu     Lưu trữ Công thức (Archive / Unarchive)
           Nhóm chức năng  Module Quản lý Công thức Nấu ăn (FR-RCP)

           Tác nhân        Tác giả sở hữu / Quản trị viên (Admin)
           Mức ưu tiên     S – Should Have
           (MoSCoW)

                           Chuyển Recipe sang trạng thái Archived. Recipe Archived không hiển
                           thị trong danh sách công khai nhưng không bị xóa khỏi database (soft
           Mô tả
                           hide). Hữu ích để ẩn recipe cũ không còn phù hợp mà không mất dữ
                           liệu.
           Điều kiện tiên quyết 1. Recipe tồn tại, người dùng có quyền.
                           1. Author gửi PATCH /api/v1/recipes/{id}/archive.
                           2. Kiểm tra authorization.
           Luồng chính (Happy
                           3. recipe.Archive() → Status = Archived.
           Path)
                           4. SaveChangesAsync(), invalidate cache.
                           5. HTTP 200 OK.
           Luồng thay thế / A1 – ID không tồn tại: HTTP 404. A2 – Không có quyền: HTTP 403.
           Ngoại lệ
           HTTP Method &   PATCH /api/v1/recipes/{id:guid}/archive
           Endpoint
           Kết quả mong đợi Status = Archived. Recipe không còn xuất hiện trong public listing.
           HTTP Status Code trả 200 OK. 403 Forbidden. 404 Not Found.
           về
```

#### FR-RCP-007 - Xóa Recipe

- `DELETE /api/v1/recipes/{id}`, Owner/Admin, yêu cầu `If-Match`.
- Đặt `IsDeleted=true`, `DeletedAt=now`; không xóa child hoặc file ngay.
- Invalidate Recipe, Search và Category cache.
- Background purge xóa bản ghi con và file MinIO sau 30 ngày; job phải idempotent.
- Trả 204.
- `GET /api/v1/admin/recipes/trash`: Admin xem danh sách Recipe đã xóa mềm, phân trang theo `DeletedAt` giảm dần.
- `POST /api/v1/admin/recipes/{id}/restore`: Admin khôi phục trong thời hạn 30 ngày bằng cách đặt `IsDeleted=false`, `DeletedAt=null`; invalidate cache và trả 200.
- `DELETE /api/v1/admin/recipes/{id}/purge`: Admin xóa vật lý ngay một Recipe đã soft-delete, toàn bộ child record và file MinIO; trả 204 và ghi audit log.
- Author không được tự restore hoặc purge. Purge thủ công và purge job phải dùng cùng một application service để bảo đảm hành vi nhất quán.

#### FR-RCP-008 - Ảnh Recipe

- `POST /recipes/{id}/images`: multipart `file`, `altText?`, `isPrimary?`; trả 201 `RecipeImageDto`.
- `PATCH /recipes/{id}/images/{imageId}`: `altText?`, `isPrimary?`, `orderIndex?`.
- `DELETE /recipes/{id}/images/{imageId}`: soft delete metadata và lập lịch xóa file sau retention phù hợp.
- Tối đa 5 MB; loại JPEG, PNG, WebP, AVIF.
- Kiểm tra MIME và magic bytes bằng image decoding library; JPEG/PNG signature, WebP RIFF+WEBP, AVIF ISO BMFF `avif`/`avis`.
- Ảnh đầu tiên mặc định primary. Khi đặt primary, tất cả ảnh khác được bỏ primary trong cùng transaction.
- Khi xóa ảnh primary, ảnh có `orderIndex` nhỏ nhất còn lại trở thành primary.
- MinIO unavailable trả 503; file sai trả 422 nếu request hợp cấu trúc nhưng nội dung file không hợp lệ.

#### FR-RCP-009 - Ingredient

- POST/PUT/DELETE `/recipes/{id}/ingredients/{ingredientId?}`.
- Owner/Admin; yêu cầu `If-Match` của Recipe.
- Fields: `name` 1-200, `quantity?`, `unit?` tối đa 50, `notes?` tối đa 500, `orderIndex?`.
- Nếu quantity có giá trị thì phải lớn hơn 0. Quantity và unit có thể null để biểu diễn “vừa đủ”.
- API chỉ nhận `quantity` dưới dạng JSON number tương ứng `decimal(10,3)`; chuỗi như `"1/2"` bị từ chối với 422.
- Frontend được phép cho người dùng nhập số thập phân, hỗn số hoặc phân số thông dụng; trước khi gửi API phải chuẩn hóa thành decimal.
- Khi hiển thị, frontend có thể định dạng decimal thành phân số bếp phổ biến nhưng không làm thay đổi giá trị lưu trữ.

#### FR-RCP-010 - Step

- POST/PUT/DELETE `/recipes/{id}/steps/{stepId?}`.
- Fields: `title?` tối đa 200, `description` 1-2000, `timerMinutes? >= 0`, `imageUrl?`.
- Server cấp StepNumber liên tục khi thêm.
- Khi xóa, server renumber trong transaction.
- `PUT /recipes/{id}/steps/reorder` nhận `stepIds` theo thứ tự mới; danh sách phải chứa đúng toàn bộ step active của Recipe.
- Phiên bản 1.2 chỉ hỗ trợ danh sách bước phẳng; không có `ParentStepId`, sub-step hoặc số bước dạng `3.1`.
- Step phân cấp thuộc backlog Phase 2. Khi được phê duyệt, có thể bổ sung `ParentStepId` nullable bằng migration không phá vỡ dữ liệu hiện tại.

### 3.4. Module Tìm kiếm và Phân trang (FR-SRCH) - Chuẩn hóa

#### FR-SRCH-001 - Full-text search

- `GET /api/v1/recipes/search?q=...`.
- Query dài 2-100 ký tự; chỉ tìm Published.
- PostgreSQL `SearchVector` được duy trì bằng trigger từ Title và Description.
- Chuẩn hóa tìm kiếm bằng `unaccent`; sử dụng cấu hình `simple`, GIN cho tsvector và `pg_trgm` cho fuzzy fallback.
- Xếp hạng theo relevance, sau đó theo PublishedAt giảm dần.
- Cache Redis 1 phút, vary theo toàn bộ query/filter/page/sort.

#### FR-SRCH-002/003/004 - Filter, sort, pagination

- Filter kết hợp bằng AND.
- Sort whitelist: `createdAt`, `publishedAt`, `title`, `prepTime`, `cookTime`, `servings`, `relevance` đối với search.
- Không tìm thấy dữ liệu trả 200 với mảng rỗng.

### 3.5. Module Quản lý Tệp tin (FR-FILE)

<!-- Trang 38 -->

```text
          Module xử lý tất cả thao tác với file binary trên hệ thống lưu trữ đối tượng (Object Storage)
          MinIO S3-compatible. Abstraction layer IFileStorageService cho phép swap implementation
          (MinIO ↔ AWS S3 ↔ local filesystem) mà không cần thay đổi Application Layer.

           Mã FR  Tên      Mô tả                        Ràng buộc kỹ thuật

                           IFileStorageService.UploadAsync(IFormFile, Max size: 5MB. MIME:
           FR-             folder, ct) → string (public URL). Tạo unique JPEG/PNG/WebP/AVIF.
                  Upload File
           FILE-           filename = {folder}/{Guid.NewGuid()}{ext} để Magic bytes validation.
                  lên MinIO
           001             ngăn path traversal. Preserve MIME type Bucket: "culinary-blog".
                           gốc.                         Policy: public-read.
                                                        Nếu object không tồn tại
                           IFileStorageService.DeleteAsync(fileUrl, ct).
                                                        trên MinIO → không
           FR-             Trích xuất object name từ URL, gọi
                  Xóa File                              throw exception
           FILE-           RemoveObjectAsync(). Thường được gọi từ
                  khỏi MinIO                            (idempotent). Lỗi kết nối
           002             Hangfire background job (fire-and-forget)
                                                        MinIO → Hangfire retry
                           sau khi xóa recipe.
                                                        tối đa 3 lần.
```

### 3.6. Module Background Jobs (FR-JOB)

```text
          Module xử lý các tác vụ nền không đồng bộ sử dụng Hangfire. Hangfire chạy in-process trong
          .NET API và sử dụng PostgreSQL làm persistent storage cho job queue. Dashboard quản lý
          jobs tại /hangfire (chỉ Admin). Hỗ trợ 3 loại job: Fire-and-forget (chạy ngay), Delayed (chạy
          sau N giây/phút) và Recurring (lịch cron).
           Mã FR  Tên Job   Loại     Trigger           Mô tả         Retry Policy
                                                                     Tự động retry
                                                       Gửi email HTML
                                                                     3 lần với
                                                       chào mừng đến
                                                                     exponential
                                                       địa chỉ email vừa
                                                                     backoff (1
           FR-                       Sau FR-AUTH-001 thành đăng ký. Email
                  Welcome   Fire-and-                                phút, 5 phút,
           JOB-                      công              template bao gồm:
                  Email Job forget                                   30 phút). Sau 3
           001                       (BackgroundJob.Enqueue) tên người dùng,
                                                                     lần fail →
                                                       link kích hoạt email
                                                                     chuyển sang
                                                       (nếu cần), link đến
                                                                     Failed state,
                                                       ứng dụng.
                                                                     log error.
                                                       Tạo thumbnail
                                                       (300x300px) và
                                                       medium image  Retry 3 lần.
           FR-    Image Resize                         (800x600px) từ Nếu fail: ảnh
                            Fire-and- Sau FR-RCP-008 upload
           JOB-   / Thumbnail                          ảnh gốc. Lưu cả 3 gốc vẫn hiển
                            forget   ảnh thành công
           002    Job                                  phiên bản lên thị, chỉ thiếu
                                                       MinIO. Cập nhật thumbnail.
                                                       URLs vào
                                                       database.
                                                       Tạo file
                                                       sitemap.xml chứa
                                                                     Retry 2 lần nếu
                                                       URL tất cả
           FR-    Sitemap                                            fail. Log kết
                                     Hàng ngày lúc 02:00 AM Published recipes,
           JOB-   Generation Recurring                               quả (số URL
                                     UTC (cron: "0 2 * * *") categories và
           003    Job                                                trong sitemap)
                                                       pages tĩnh. Upload
                                                                     qua Serilog.
                                                       sitemap.xml lên
                                                       MinIO hoặc lưu
```

<!-- Trang 39 -->

```text
           Mã FR  Tên Job   Loại     Trigger           Mô tả         Retry Policy
                                                       vào wwwroot. Gửi
                                                       thông báo đến
                                                       Google Search
                                                       Console (ping).
```

#### FR-JOB-004 - Purge soft-deleted content

- Chạy hằng ngày.
- Chỉ quét Recipe có `IsDeleted=true` và `DeletedAt <= now - 30 days`; Recipe đã được restore không đủ điều kiện purge.
- Xóa vật lý Recipe, child record và file đã soft-delete quá 30 ngày.
- Dùng chung application service với endpoint Admin purge để quy tắc cascade, xóa file và audit không bị khác nhau.
- Phải idempotent và log đầy đủ số record/file đã xử lý.

### 3.7. Module Quan sát Hệ thống (FR-OBS)

```text
          Module cung cấp khả năng quan sát (Observability) toàn diện theo ba trụ cột: Logging
          (Serilog), Metrics (OpenTelemetry), và Distributed Tracing (OpenTelemetry). Đây là yêu cầu
          bắt buộc cho production deployment.


           Mã FR  Tên       Mô tả                 Kỹ thuật / Công cụ
                            Hệ thống cung cấp 3 endpoint
                            health check với mục đích khác IHealthCheck,
                            nhau: • GET /health – tổng hợp AspNetCore.HealthChecks.NpgSql,
                            tất cả components (database, AspNetCore.HealthChecks.Redis,
           FR-
                  Health Check Redis, MinIO). • GET AspNetCore.HealthChecks.Minio.
           OBS-
                  Endpoints /health/live – Liveness probe Liveness chỉ trả healthy. Readiness
           001
                            (chỉ kiểm tra process còn fail khi DB/Redis down →
                            sống). • GET /health/ready – Kubernetes/Nginx ngừng route
                            Readiness probe (kiểm tra kết traffic.
                            nối database và Redis).
                            Mọi HTTP request được log
                            với: CorrelationId (X-
                                                  Serilog + CorrelationIdMiddleware.
                            Correlation-ID header), HTTP
                                                  Sinks: Console (structured JSON),
                            method/path/status, elapsed
           FR-                                    File (rolling daily), Seq
                  Structured time (ms), UserId (khi đã xác
           OBS-                                   (development). Log levels: Debug
                  Logging   thực). MediatR Pipeline
           002                                    (development), Information
                            Behavior (LoggingBehavior) log
                                                  (production), Warning/Error (luôn
                            tất cả Commands/Queries vào.
                                                  luôn).
                            Performance alert khi request >
                            500ms.
                            OpenTelemetry instrumentation
                            cho: HTTP request traces
                            (ActivitySource), EF Core OpenTelemetry .NET SDK, OTLP
                            database operation traces, exporter. Activity.TraceId được
           FR-    Distributed
                            custom business metrics include trong structured log (log
           OBS-   Tracing &
                            (recipe created/published correlation với trace). Metrics:
           003    Metrics
                            count). Traces được export đến request count, duration histogram,
                            Seq (development) hoặc error rate.
                            Jaeger/Grafana Tempo
                            (production).
```

<!-- Trang 40 -->

## 4. Yêu cầu Phi Chức năng (NFR)

```text
          Phần này mô tả các thuộc tính chất lượng hệ thống theo mô hình ISO/IEC 25010 (FURPS+).
          Mỗi yêu cầu phi chức năng được gán mã định danh, mức ưu tiên và tiêu chí đo lường định
          lượng cụ thể. Các NFR này ràng buộc thiết kế kiến trúc và lựa chọn công nghệ toàn bộ hệ
          thống.


           Mã NFR    Danh mục            Số yêu    Ưu tiên
                                         cầu

           NFR-PERF  Hiệu năng (Performance) 5     Cao
           NFR-SEC   Bảo mật (Security)  6         Rất cao

           NFR-USE   Khả năng sử dụng    4         Trung
                     (Usability)                   bình
           NFR-REL   Độ tin cậy (Reliability) 3    Cao

           NFR-      Khả năng bảo trì    4         Trung
           MAINT     (Maintainability)             bình

           NFR-      Khả năng mở rộng    3         Cao
           SCALE     (Scalability)
           NFR-SEO   Tối ưu SEO (SEO)    4         Cao
```

### 4.1. Hiệu năng (NFR-PERF) - Chuẩn hóa

- Cache-warm GET: p50 <= 150 ms.
- Tất cả API: p95 <= 500 ms, p99 <= 1000 ms trong tải mục tiêu.
- Hỗ trợ ít nhất 100 concurrent users trên 2 vCPU/4 GB RAM.
- Không có N+1 query; query chậm trên 100 ms được log.
- Frontend: LCP <= 2.5 s, CLS <= 0.1, INP <= 200 ms, first-load JS <= 200 KB gzip.
- Redis policy:
  - Category list/detail: 30 phút.
  - Public Recipe list: 1 phút.
  - Public Recipe detail: 5 phút.
  - Search: 1 phút.
  - Không shared-cache response private/authenticated.
- Recipe create/update/publish/unpublish/archive/delete và child mutations invalidate tags `recipes`, `recipe:{id}`, `recipe-slug:{slug}`, `search`, `categories` khi recipeCount có thể thay đổi.

### 4.2. Bảo mật (NFR-SEC) - Chuẩn hóa

- Password dùng ASP.NET Core Identity PBKDF2-HMACSHA512, iteration >= 100,000.
- JWT HS256, TTL 15 phút, claims tối thiểu `sub`, `email`, `roles`, `jti`.
- Signing key lấy từ secret store; không commit Git; rotation theo ADR vận hành.
- Refresh token 256-bit, hash SHA-256, TTL 7 ngày, rotation và family reuse detection.
- Auth rate limit: 10 request/phút/IP; API chung 100/phút/IP; upload 5/phút/IP.
- HTTPS TLS 1.2+, HSTS production; CORS allowlist, không wildcard production.
- Input validation tại Application layer; output encode ở frontend; CSP production.
- File upload kiểm tra size trước khi buffer, MIME, magic bytes và decode thực tế.
- Write operation log UserId, resource ID, action, timestamp và result.

### 4.3. Khả năng Sử dụng (NFR-USE)

```text
           NFR-USE-001       Giao diện hiển thị chính xác trên tất cả breakpoints: • Mobile:
           Responsive Design 320px – 767px (single column, touch-friendly). • Tablet: 768px
                             – 1199px (2-column grid). • Desktop: ≥ 1200px (full layout).
                             Framework: Tailwind CSS utility-first. Không sử dụng CSS
                             framework override. Kiểm thử: Chrome DevTools responsive
                             mode + BrowserStack (iOS, Android).
           NFR-USE-002       Tuân thủ WCAG 2.1 Level AA: • Semantic HTML5: <article>,
           Accessibility (a11y) <nav>, <main>, <aside>. • ARIA attributes: aria-label, aria-
                             expanded, role trên interactive elements. • Keyboard
                             navigation: tất cả chức năng dùng được bằng bàn phím (Tab,
                             Enter, Escape). • Color contrast ratio ≥ 4.5:1 (text) và ≥ 3:1 (UI
                             components). • Screen reader: test với NVDA (Windows) và
                             VoiceOver (macOS/iOS).

           NFR-USE-003 Error Thông báo lỗi phải rõ ràng và actionable: • API: trả về RFC
           Messages          7807 Problem Details (type, title, status, detail, errors{}). •
                             Frontend: hiển thị ngay bên cạnh field lỗi (React Hook Form
                             inline validation). • Server errors (5xx): hiển thị thông báo thân
                             thiện, không lộ stack trace. • I18n-ready: error messages sử
                             dụng error code (không hardcode tiếng Việt/Anh).
           NFR-USE-004       Mọi async operation phải có visual feedback: • Loading
           Loading States    skeleton: hiển thị trong khi fetch data (không blank screen). •
                             Optimistic update: UI cập nhật ngay, rollback nếu API fail. •
                             Toast notification: xác nhận thành công/thất bại sau write
                             operation. • Progress indicator: upload ảnh hiển thị progress
                             bar (%) realtime.
```

### 4.4. Độ tin cậy (NFR-REL)

<!-- Trang 43 -->

```text
           NFR-REL-001 Uptime Hệ thống có uptime ≥ 99.5% (≈ 3.65 giờ downtime/năm). •
           SLA               Maintenance window: công bố trước 48 giờ qua banner thông
                             báo. • Health check: /health/ready probe mỗi 10 giây
                             (Kubernetes readiness probe). • Monitoring: Uptime Robot /
                             Better Uptime gửi alert khi down > 1 phút.
           NFR-REL-002 Error Hệ thống xử lý lỗi gracefully, không crash toàn bộ: • Global
           Handling & Resilience Exception Handler Middleware: bắt tất cả unhandled
                             exceptions → trả 500 Problem Details + log. • Database
                             connection pool: tự reconnect, timeout 30s. • Redis failover:
                             nếu Redis down → fallback database (không cache), không
                             throw exception. • Hangfire retry: mỗi job tối đa 3 retry với
                             exponential backoff. • Circuit Breaker: (tùy chọn nâng cao)
                             Polly cho external HTTP calls.

           NFR-REL-003 Data  Dữ liệu không bị mất trong trường hợp restart hoặc crash: •
           Durability        PostgreSQL WAL (Write-Ahead Logging): đảm bảo ACID. •
                             Backup: pg_dump tự động hàng ngày lúc 03:00 AM, lưu 30
                             ngày. • MinIO: dữ liệu file trên volume persistent (không
                             ephemeral container storage). • Refresh tokens: lưu DB
                             (không Redis) để survive restart. • Soft delete: Recipe được
                             đánh dấu IsDeleted thay vì xóa vật lý (có thể khôi phục).
```

### 4.5. Khả năng Bảo trì (NFR-MAINT)

```text
           NFR-MAINT-001 Code Toàn bộ code phải pass static analysis trước khi merge: •
           Quality           .NET: SonarAnalyzer, StyleCop, EditorConfig (indent, naming
                             conventions). • TypeScript/React: ESLint (Airbnb ruleset),
                             Prettier. • Không có compiler warnings trong build CI. • Code
                             review: ít nhất 1 reviewer phê duyệt Pull Request.

           NFR-MAINT-002 Test Độ phủ test tối thiểu: • Unit tests: ≥ 80% line coverage
           Coverage          (Application layer commands, queries, validators). • Integration
                             tests: tất cả API endpoints có ít nhất 1 happy path + 1 error
                             case. • E2E tests: 5 critical user flows (register, login, create
                             recipe, publish, search). Tool: xUnit (backend), Jest + Testing
                             Library (frontend), Playwright (E2E).
           NFR-MAINT-003     Tài liệu kỹ thuật bắt buộc: • README.md: hướng dẫn setup
           Documentation     dev environment (Docker Compose) trong < 5 phút. • API
                             documentation: tự động sinh từ XML comments +
                             Scalar/Swagger UI tại /scalar. • Architecture Decision Records
                             (ADR): ghi lại mọi quyết định kiến trúc quan trọng. •
                             CHANGELOG.md: cập nhật mỗi release (theo Keep a
                             Changelog + SemVer).
           NFR-MAINT-004     Tuân thủ nghiêm ngặt dependency rules của Clean
           Clean Architecture Architecture: • Domain layer: KHÔNG dependency vào bất kỳ
           Compliance        layer nào khác. Không có nuget packages ngoài
                             FluentValidation. • Application layer: chỉ depend vào Domain.
                             KHÔNG reference Infrastructure. • Infrastructure layer: depend
                             vào Application (implements interfaces). • Vi phạm: được phát
                             hiện qua ArchUnit.NET tests hoặc custom Architecture test
```

<!-- Trang 44 -->

```text
                             project. • CQRS: Commands thay đổi state, Queries đọc data
                             — không trộn lẫn.
```

### 4.6. Khả năng Mở rộng (NFR-SCALE)

```text
           NFR-SCALE-001     API được thiết kế stateless để hỗ trợ horizontal scaling: • JWT
           Stateless Backend authentication (không session server-side). • Distributed cache
                             (Redis, không in-memory IMemoryCache) cho mọi shared
                             state. • Distributed lock (RedLock) cho các tác vụ singleton
                             (sitemap generation). • Hangfire: chạy với multiple workers
                             (IBackgroundJobServer), PostgreSQL làm shared queue.

           NFR-SCALE-002     Chiến lược database scaling: • Connection pooling: Npgsql
           Database Scaling  built-in pool (max 100 connections/instance). • Read replica
                             (tùy chọn): EF Core split queries + IQueryable routing qua
                             IDbContextFactory. • Index strategy: B-tree cho equality/range,
                             GIN cho full-text search (tsvector). • Table partitioning: (nâng
                             cao) partition Recipe by CreatedAt khi > 1 triệu rows.
           NFR-SCALE-003     Hạ tầng có thể scale theo chiều ngang: • Docker: mỗi service
           Infrastructure Scaling là container riêng biệt (API, Postgres, Redis, MinIO, Nginx). •
                             Nginx: load balancer upstream pool cho nhiều API instances. •
                             MinIO: Distributed Mode (4+ nodes) cho production storage
                             scaling. • CDN: static assets (Next.js _next/static) được serve
                             qua CDN (Cloudflare).
```

### 4.7. Tối ưu SEO (NFR-SEO) - Chuẩn hóa

- Published Recipe có JSON-LD Schema.org Recipe.
- Draft/Archived/Deleted có `noindex` và không nằm trong sitemap.
- URL Recipe `/recipes/{slug}`, Category `/categories/{slug}`.
- Slug Published bất biến; slug Draft đổi sẽ lưu lịch sử và redirect 301.
- Canonical URL, Open Graph, Twitter card và meta description đầy đủ.

## 5. Yêu cầu Giao diện Ngoài

```text
          Chương này mô tả tất cả giao diện giữa hệ thống Culinary Blog với các thực thể bên ngoài:
          người dùng cuối, phần cứng, phần mềm bên thứ ba và giao tiếp mạng. Mọi giao tiếp đều qua
          HTTPS (TLS 1.2+) trong môi trường production.
```

### 5.1. Giao diện Người dùng (UI)

```text
          Hệ thống cung cấp giao diện web duy nhất trên nền Next.js App Router, hoạt động như Single
          Page Application (SPA) với Server-Side Rendering (SSR) và Incremental Static Regeneration
          (ISR).


           Màn hình / Route    Mô tả           Loại Rendering Yêu cầu Auth
           /                   Trang chủ: danh ISR           Không
                               sách recipe nổi bật (revalidate=3600)
                               + categories

           /recipes            Danh sách tất cả SSR (dynamic) Không
                               recipes với
                               filter/sort/search
           /recipes/[slug]     Chi tiết recipe: ISR          Không
                               ingredients, steps, (revalidate=300)
                               nutrition, JSON-LD

           /categories         Danh sách category ISR        Không
                                               (revalidate=3600)

           /categories/[slug]  Danh sách recipe ISR          Không
                               theo category   (revalidate=600)
           /auth/login         Form đăng nhập  CSR           Không (redirect
                               (email/password +             nếu đã login)
                               Google OAuth
                               button)

           /auth/register      Form đăng ký tài CSR          Không
                               khoản mới
           /dashboard          Trang tổng quan CSR           Bắt buộc
                               của Author/Admin              (Author/Admin)

           /dashboard/recipes  Quản lý danh sách CSR         Bắt buộc
                               recipe của user
           /dashboard/recipes/new Form tạo recipe mới CSR    Bắt buộc
                               (multi-step wizard)           (Author/Admin)

           /dashboard/recipes/[id]/edit Form chỉnh sửa CSR   Bắt buộc
                               recipe                        (Owner/Admin)

           /dashboard/categories Quản lý categories CSR      Bắt buộc
                               (chỉ Admin)                   (Admin)
```

<!-- Trang 47 -->

```text
           Màn hình / Route    Mô tả           Loại Rendering Yêu cầu Auth

           /profile            Xem và chỉnh sửa CSR          Bắt buộc
                               thông tin cá nhân
           /search             Trang kết quả full- SSR       Không
                               text search
```

### 5.2. Giao diện Phần mềm – REST API

```text
          Backend cung cấp RESTful API theo chuẩn JSON. Toàn bộ endpoints được tiền tố /api/v1.
          Xem chi tiết tại Chương 8.


           Giao thức         HTTP/1.1 và HTTP/2 qua HTTPS (TLS 1.2+). Nginx
                             termination SSL.
           Base URL (dev)    http://localhost:5000/api/v1

           Base URL (prod)   https://api.culinaryblog.com/api/v1
           Content-Type      application/json; charset=utf-8 (request và response).
                             Multipart/form-data cho file upload endpoints.

           Authentication    Bearer Token trong Authorization header: Authorization:
                             Bearer <access_token>. Refresh token: trong request body
                             (không dùng cookie để tránh CSRF).

           Response Format   Success: { "data": {...}, "meta": { "page":1, "pageSize":10,
                             "total":100 } } Error: RFC 7807 Problem Details { "type", "title",
                             "status", "detail", "errors":{} }
           Versioning        URL Path versioning: /api/v1/. Khi có breaking changes →
                             /api/v2/ (v1 được duy trì tối thiểu 6 tháng).

           CORS Headers      Access-Control-Allow-Origin: <configured-origins> Access-
                             Control-Allow-Methods: GET, POST, PUT, PATCH, DELETE,
                             OPTIONS Access-Control-Allow-Headers: Content-Type,
                             Authorization, X-Correlation-ID
           Rate Limit Headers X-RateLimit-Limit: 100 X-RateLimit-Remaining: 87 X-
                             RateLimit-Reset: 1700000000 (Unix timestamp) Retry-After: 30
                             (seconds, khi 429)

           Correlation ID    X-Correlation-ID header: sinh tự động nếu không có trong
                             request, trả về trong response. Gán vào tất cả log entries
                             (Serilog MDC).
```

### 5.3. Giao diện Dịch vụ Bên thứ ba - Chuẩn hóa

| Dịch vụ | Mục đích | Giao thức/SDK | Cấu hình chính |
|---|---|---|---|
| Google Identity Services | Đăng nhập/đăng ký Google | Frontend nhận ID token; backend xác minh signature, issuer, audience, expiry và nonce; backend phát JWT/refresh token của hệ thống | `GoogleClientId`; không dùng Auth.js session song song |
| MinIO/S3 | Lưu ảnh Recipe | AWS SDK for .NET, endpoint override cho MinIO | Endpoint, AccessKey, SecretKey, BucketName |
| Hangfire | Background jobs | Hangfire.AspNetCore + PostgreSQL storage | Schema `hangfire`; dashboard chỉ Admin |
| Serilog/Seq | Structured logging | Console JSON, rolling file, Seq development | `Seq__ServerUrl` |
| OpenTelemetry | Trace và metric | OTLP exporter | `OTEL_EXPORTER_OTLP_ENDPOINT` |
| SMTP | Welcome/confirmation email | MailKit với TLS | Host, Port, Username, Password |
| Google Search | Cập nhật sitemap | HTTP ping hoặc Search Console API theo cấu hình | Sitemap URL |

Frontend gửi `idToken` đến `POST /api/v1/auth/google`; không dùng callback `/auth/google/callback`. Google token sai/hết hạn trả 401.

### 5.4. Giao diện Phần cứng

```text
          Hệ thống là web application, không giao tiếp trực tiếp với phần cứng chuyên biệt. Yêu cầu
          phần cứng tối thiểu cho server:

           Thành phần     Development (local)  Production (minimum)

           CPU            2 cores (Intel/AMD/ARM64 2 vCPU (VPS/Cloud instance,
                          — Apple M-series được hỗ x86_64)
                          trợ qua Docker)
           RAM            8 GB (chạy Docker    4 GB (API + dependencies riêng lẻ)
                          Compose đầy đủ: API + PG
                          + Redis + MinIO + Seq)

           Storage        20 GB SSD (cho Docker 50 GB SSD (production data
                          images + database data + growth)
                          MinIO volumes)

           Network        Kết nối internet (npm/nuget Bandwidth ≥ 1 Gbps, IP tĩnh
                          packages, Google OAuth)
```

<!-- Trang 49 -->

```text
           Thành phần     Development (local)  Production (minimum)

           Browser Client Chrome 112+, Firefox Tương tự — không hỗ trợ IE11
                          113+, Safari 16+, Edge
                          112+ (ES2020+)
```

<!-- Trang 50 -->

## 6. Kiến trúc Hệ thống

```text
          Chương này mô tả tổng quan kiến trúc phần mềm của hệ thống Culinary Blog. Hệ thống được
          thiết kế theo mô hình Client-Server với hai tầng riêng biệt: Frontend (Next.js) và Backend
          (.NET 10 Minimal API), giao tiếp qua REST API. Backend tuân thủ nguyên tắc Clean
          Architecture kết hợp CQRS pattern.
```

### 6.1. Tổng quan Kiến trúc

```text
           Tầng          Technology        Vai trò            Giao tiếp với
           Client        Browser           Người dùng tương tác Next.js App
           (Browser/Mobile) (Chrome/Firefox/Safari) qua giao diện web

           Frontend      Next.js 14+ App   Rendering UI, route Backend
                         Router, TypeScript, management, client-side REST API
                         Tailwind CSS, JWT client-side state. SSR/ISR cho SEO.
                         v5, TanStack Query,
                         React Hook Form +
                         Zod
           Nginx Reverse Nginx Alpine (Docker) SSL termination, load Frontend
           Proxy                           balancing, static file :3000,
                                           caching, rate limiting Backend API
                                           basic.             :5000

           Backend API   ASP.NET Core .NET Business logic,    PostgreSQL,
                         10 Minimal API    authentication, data Redis, MinIO,
                                           access, background jobs. Email
           Cache Layer   Redis 7           Distributed cache cho Backend API
                                           recipe/category/search
                                           results. Rate limiting
                                           counters.

           Object Storage MinIO (S3-compatible) Lưu file ảnh: original, Backend API
                                           medium (800×600),  (via
                                           thumbnail (300×300). AWSSDK.S3)
           Database      PostgreSQL 16     Persistent relational data Backend API
                                           storage. Full-text search (via EF Core)
                                           via tsvector.

           Observability Serilog + Seq,    Logging, metrics,  Backend API
                         OpenTelemetry +   distributed tracing.
                         Grafana/Jaeger
```

### 6.2. Kiến trúc Backend – Clean Architecture

```text
          Backend tuân thủ Clean Architecture (Robert C. Martin) với nguyên tắc Dependency Rule:
          dependency chỉ đi vào trong (hướng Domain). Không bao giờ có reference từ
          Domain/Application ra Infrastructure.
```

<!-- Trang 51 -->

```text
           Domain Layer           Nhân lõi hệ thống. Chứa: • Entities: Recipe, Category,
           (CulinaryBlog.Domain)  ApplicationUser, RecipeStep, RecipeIngredient,
                                  RecipeImage. • Value Objects: Slug, EmailAddress. •
                                  Owned Entities: RecipeNutrition. • Domain Events
                                  (optional): RecipePublishedEvent. • Enums:
                                  RecipeDifficulty, RecipeStatus. • Interfaces:
                                  IRepository<T>, IRecipeRepository,
                                  ICategoryRepository. • Không có NuGet dependencies
                                  (chỉ .NET BCL).
           Application Layer      Orchestration Layer. Chứa: • Commands (CQRS write):
           (CulinaryBlog.Application) CreateRecipeCommand, PublishRecipeCommand,
                                  LoginCommand... • Queries (CQRS read):
                                  GetRecipesQuery, GetRecipeBySlugQuery... •
                                  Handlers (MediatR IRequestHandler): xử lý logic
                                  business cho mỗi command/query. • DTOs / Response
                                  models: RecipeDto, UserDto, PagedResult<T>. •
                                  Validators (FluentValidation): validation rules cho mỗi
                                  command. • Pipeline Behaviors: ValidationBehavior,
                                  LoggingBehavior, CachingBehavior,
                                  PerformanceBehavior. • Service interfaces:
                                  IEmailService, IJwtService, IFileStorageService,
                                  ICurrentUser.

           Infrastructure Layer   Implements application interfaces. Chứa: • EF Core:
           (CulinaryBlog.Infrastructure) CulinaryBlogDbContext, configurations, migrations,
                                  repositories. • Repository implementations:
                                  RecipeRepository (LINQ + EF Core + FTS),
                                  CategoryRepository. • JWT Service: JwtService
                                  (System.IdentityModel.Tokens.Jwt). • File Storage:
                                  MinioFileStorageService (AWSSDK.S3). • Email:
                                  MailKitEmailService. • Cache: RedisCacheService
                                  (StackExchange.Redis). • Hangfire job registrations. •
                                  EF Core Interceptors: AuditInterceptor (auto set
                                  CreatedAt/UpdatedAt).
           Presentation Layer     HTTP interface. Chứa: • Minimal API Endpoint Groups:
           (CulinaryBlog.API)     AuthEndpoints, RecipesEndpoints,
                                  CategoriesEndpoints. • Middleware:
                                  GlobalExceptionMiddleware, CorrelationIdMiddleware,
                                  RateLimitingMiddleware. • DI Configuration: Program.cs
                                  + Extension methods (AddApplication,
                                  AddInfrastructure, AddPresentation). • OpenAPI: Scalar
                                  UI tại /scalar, XML documentation comments. •
                                  Authentication: JWT Bearer; Google ID token được backend
                                  xác minh trước khi phát token của hệ thống.
```

### 6.3. CQRS + MediatR Pipeline

```text
          CQRS (Command Query Responsibility Segregation) tách biệt read và write models. Mỗi
          request đi qua MediatR Pipeline Behaviors theo thứ tự:
```

<!-- Trang 52 -->

```text
           Thứ  Pipeline Behavior   Trách nhiệm        Áp dụng cho
           tự

           1    LoggingBehavior     Log request type,  Tất cả Commands và
                                    parameters, elapsed Queries
                                    time. Cảnh báo nếu >
                                    500ms.
           2    ValidationBehavior  Chạy FluentValidation Tất cả Commands và
                                    validators đã đăng ký. Queries có Validator
                                    Throw
                                    ValidationException nếu
                                    có lỗi.

           3    CachingBehavior     Kiểm tra Redis cache Queries implements
                                    trước khi xử lý.   ICacheable (GET
                                    Implements ICacheable endpoints)
                                    interface trên Query.
           4    Handler             Thực thi business logic: Tất cả (bắt buộc)
                (IRequestHandler)   gọi repositories, raise
                                    domain events, tạo
                                    response DTO.

           5    CacheInvalidationBehavior Xóa cache liên quan sau Commands thay đổi
                                    khi Command thành  data
                                    công. Implements   (Create/Update/Delete)
                                    ICacheInvalidator.
```

### 6.4. Mô hình Quan hệ Thực thể (ERD tóm tắt) - Chuẩn hóa

- Recipe thuộc một Category và một ApplicationUser; có nhiều RecipeStep, RecipeIngredient, RecipeImage và RecipeSlugHistory; RecipeNutrition là owned fields.
- ApplicationUser có nhiều Recipe và RefreshToken.
- RefreshToken có `FamilyId` để rotation/reuse detection.
- Domain content entities dùng audit fields và soft delete khi phù hợp; ApplicationUser và RefreshToken có cấu trúc riêng.
- Recipe dùng PostgreSQL `xmin` làm concurrency token/ETag, không dùng `RowVersion bytea`.

### 6.5. Triển khai – Docker Compose

```text
          Toàn bộ hệ thống được containerized với Docker Compose. Development dùng docker-
          compose.yml, Production dùng docker-compose.prod.yml với optimized build + secrets
          management.
```

<!-- Trang 53 -->

```text
           Service   Image           Port          Volume / Dependency
                                     (host:container)

           nginx     nginx:alpine    80:80, 443:443 Depends: api, frontend
                                                   Volume: ./nginx/nginx.conf,
                                                   ./ssl/
           api       culinaryblog-api 5000:8080    Depends: postgres, redis,
                     (Dockerfile)                  minio Env file: .env.production

           frontend  culinaryblog-web 3000:3000    Depends: api
                     (Dockerfile)
           postgres  postgres:16-alpine 5432:5432  Volume:
                                                   pgdata:/var/lib/postgresql/data
                                                   Env: POSTGRES_DB, USER,
                                                   PASSWORD

           redis     redis:7-alpine  6379:6379     Volume: redisdata:/data
                                                   Command: redis-server --
                                                   appendonly yes
           minio     minio/minio:latest 9000:9000, Volume: miniodata:/data
                                     9001:9001     Command: server /data --
                                     (Console)     console-address :9001

           seq       datalust/seq:latest 5341:80   Volume: seqdata:/data Dev
                                                   only — không deploy
                                                   production

           mailhog   mailhog/mailhog 8025:8025 (UI), Dev only — test email
                                     1025:1025
                                     (SMTP)
```

<!-- Trang 54 -->

## 7. Mô hình dữ liệu chuẩn hóa

### 7.1. Quy ước chung

Domain content entities có:

- `Id uuid` primary key.
- `CreatedAt timestamptz not null`.
- `UpdatedAt timestamptz null`.
- `IsDeleted boolean not null default false`.
- `DeletedAt timestamptz null`.
- PostgreSQL `xmin` dùng làm concurrency token, không tạo `RowVersion bytea`.

ApplicationUser và RefreshToken không bắt buộc kế thừa base entity.

### 7.2. Recipe

| Column | Kiểu/ràng buộc |
|---|---|
| Id | uuid PK |
| Title | varchar(200), not null |
| Slug | varchar(220), unique cho record chưa xóa |
| Description | text, not null, tối đa 2000 |
| PrepTime | integer > 0 |
| CookTime | integer >= 0 |
| Servings | integer > 0 |
| Difficulty | smallint: Easy, Medium, Hard, Expert |
| Status | smallint: Draft, Published, Archived |
| CategoryId | uuid FK RESTRICT |
| AuthorId | varchar(450) FK AspNetUsers |
| SearchVector | tsvector, trigger-maintained, GIN index |
| PublishedAt | timestamptz nullable |
| Nutrition_* | decimal nullable owned fields |
| Nutrition_Source | smallint not null default 0; `0=Manual`, `1=AutoCalculated` được dành cho Phase 2 |
| Audit fields | theo mục 7.1 |

Không có cột `Instructions`; RecipeStep là nguồn hướng dẫn.

Trong MVP, server chỉ tạo `Nutrition_Source=Manual`. Chế độ `AutoCalculated` chưa được triển khai vì hệ thống chưa có bảng nguyên liệu chuẩn hóa, dữ liệu dinh dưỡng tham chiếu và bộ chuyển đổi đơn vị.

### 7.3. RecipeStep

- `RecipeId` FK; `StepNumber > 0`; unique `(RecipeId, StepNumber)` cho record active.
- `Title varchar(200) null`.
- `Description text not null`.
- `TimerMinutes integer null check >=0`.
- `ImageUrl varchar(500) null`.
- Không có `ParentStepId` trong v1.2; mô hình dữ liệu hiện tại là flat list. Sub-step là backlog Phase 2.

### 7.4. RecipeIngredient

- `RecipeId` FK.
- `Name varchar(200) not null`.
- `Quantity decimal(10,3) null check >0 when not null`.
- `Unit varchar(50) null`, `Notes varchar(500) null`, `OrderIndex integer default 0`.
- Database và API chỉ lưu decimal. Biểu diễn phân số là trách nhiệm của frontend và không tạo thêm cột chuỗi trong database.

### 7.5. RecipeImage

- `RecipeId` FK.
- `OriginalUrl not null`, `MediumUrl null`, `ThumbnailUrl null`.
- `AltText varchar(200) null`, `IsPrimary boolean`, `OrderIndex integer`.
- Partial unique index bảo đảm tối đa một active primary image mỗi Recipe.

### 7.6. Category

- `Name varchar(100)`, `Slug varchar(120)`, `Description text?`, `ImageUrl varchar(500)?`, `OrderIndex integer`.
- Partial unique index trên normalized Name và Slug với `IsDeleted=false`.

### 7.7. ApplicationUser

- Kế thừa `IdentityUser<string>`.
- Custom fields: `DisplayName varchar(100)`, `AvatarUrl varchar(500)?`, `Bio text?`, `IsActive boolean default true`, `CreatedAt timestamptz`.
- Identity fields tiếp tục quản lý email confirmation, lockout và concurrency stamp.

### 7.8. RefreshToken

| Column | Ràng buộc |
|---|---|
| Id | uuid PK |
| UserId | FK AspNetUsers, cascade |
| FamilyId | uuid not null, indexed |
| TokenHash | char(64), unique |
| ExpiresAt | timestamptz not null |
| RevokedAt | timestamptz null |
| ReplacedByTokenHash | char(64) null |
| RevocationReason | varchar(100) null |
| CreatedAt | timestamptz not null |
| CreatedByIp | varchar(45) null |
| RevokedByIp | varchar(45) null |

`IsRevoked` là giá trị suy ra từ `RevokedAt`, không phải cột.

### 7.9. RecipeSlugHistory

- `Id uuid PK`, `RecipeId uuid FK cascade`, `Slug varchar(220)`, `CreatedAt timestamptz`.
- Slug lịch sử phải unique để xác định redirect duy nhất.

## 8. Đặc tả REST API - Chuẩn hóa

### 8.1. Auth/User

| Method | Path | Auth |
|---|---|---|
| POST | `/auth/register` | Public |
| POST | `/auth/login` | Public |
| POST | `/auth/google` | Public |
| POST | `/auth/refresh` | Refresh token |
| POST | `/auth/logout` | Refresh token, không cần access token |
| POST | `/auth/email/confirm` | Public token |
| POST | `/auth/email/resend` | Public |
| GET | `/auth/me` | Bearer |
| PATCH | `/auth/me` | Bearer |
| PATCH | `/admin/users/{id}/status` | Admin |

### 8.2. Category

| Method | Path | Auth |
|---|---|---|
| GET | `/categories` | Public |
| GET | `/categories/{slug}` | Public |
| POST | `/categories` | Admin |
| PUT | `/categories/{id}` | Admin |
| DELETE | `/categories/{id}` | Admin |

### 8.3. Recipe và child resource

| Method | Path | Auth |
|---|---|---|
| GET | `/recipes` | Public; JWT khi `mine=true` |
| GET | `/recipes/search` | Public |
| GET | `/recipes/{slug}` | Public Published; Owner/Admin private |
| POST | `/recipes` | Author/Admin |
| PUT | `/recipes/{id}` | Owner/Admin + If-Match |
| PATCH | `/recipes/{id}/publish` | Verified Owner/Admin + If-Match |
| PATCH | `/recipes/{id}/unpublish` | Owner/Admin + If-Match |
| PATCH | `/recipes/{id}/archive` | Owner/Admin + If-Match |
| PATCH | `/recipes/{id}/unarchive` | Owner/Admin + If-Match |
| DELETE | `/recipes/{id}` | Owner/Admin + If-Match |
| GET | `/admin/recipes/trash` | Admin |
| POST | `/admin/recipes/{id}/restore` | Admin |
| DELETE | `/admin/recipes/{id}/purge` | Admin |
| POST | `/recipes/{id}/images` | Owner/Admin |
| PATCH | `/recipes/{id}/images/{imageId}` | Owner/Admin |
| DELETE | `/recipes/{id}/images/{imageId}` | Owner/Admin |
| POST | `/recipes/{id}/ingredients` | Owner/Admin |
| PUT | `/recipes/{id}/ingredients/{ingredientId}` | Owner/Admin |
| DELETE | `/recipes/{id}/ingredients/{ingredientId}` | Owner/Admin |
| POST | `/recipes/{id}/steps` | Owner/Admin |
| PUT | `/recipes/{id}/steps/{stepId}` | Owner/Admin |
| PUT | `/recipes/{id}/steps/reorder` | Owner/Admin |
| DELETE | `/recipes/{id}/steps/{stepId}` | Owner/Admin |

Quy ước bổ sung:

- `nutrition.source` là read-only và luôn bằng `Manual` trong MVP.
- `quantity` của Ingredient là JSON number; frontend phải chuyển phân số thành decimal trước khi gửi.
- Step DTO không có `parentStepId` hoặc `subSteps` trong v1.2.
- Admin trash/restore/purge phải truy vấn Recipe bằng cơ chế bỏ qua global query filter một cách có kiểm soát trong Infrastructure layer.

### 8.4. Health

| Method | Path | Ý nghĩa |
|---|---|---|
| GET | `/health` | Tổng hợp dependency |
| GET | `/health/live` | Process còn sống |
| GET | `/health/ready` | Sẵn sàng nhận traffic |

## Phụ lục A - HTTP Status Codes chuẩn hóa

| Code | Ngữ cảnh |
|---|---|
| 200 | Đọc/cập nhật thành công |
| 201 | Tạo resource thành công |
| 202 | Tác vụ bất đồng bộ đã được chấp nhận |
| 204 | Delete/logout idempotent, không có body |
| 400 | JSON, query, multipart hoặc cú pháp request sai |
| 401 | Credential/access/refresh/Google token sai hoặc hết hạn |
| 403 | Không đủ role, ownership hoặc email confirmation |
| 404 | Resource không tồn tại hoặc đã soft-delete |
| 409 | Unique/state/optimistic concurrency conflict |
| 422 | Field validation hoặc business rule không đạt |
| 423 | Tài khoản bị lockout tạm thời |
| 429 | Vượt rate limit |
| 500 | Lỗi không xử lý được |
| 503 | Dependency bắt buộc không sẵn sàng |

## Phụ lục B - Application Error Codes chuẩn hóa

| Error code | HTTP | Ý nghĩa |
|---|---:|---|
| `MALFORMED_REQUEST` | 400 | Request sai cấu trúc/cú pháp |
| `VALIDATION_ERROR` | 422 | Validation/nghiệp vụ không đạt |
| `AUTH_EMAIL_EXISTS` | 409 | Email đã đăng ký |
| `AUTH_INVALID_CREDENTIALS` | 401 | Credential sai |
| `AUTH_TOKEN_INVALID` | 401 | Access token sai |
| `AUTH_TOKEN_EXPIRED` | 401 | Access token hết hạn |
| `AUTH_REFRESH_TOKEN_EXPIRED` | 401 | Refresh token hết hạn |
| `AUTH_REFRESH_TOKEN_REVOKED` | 401 | Refresh token bị revoke/reuse |
| `AUTH_GOOGLE_TOKEN_INVALID` | 401 | Google ID token sai/hết hạn |
| `AUTH_ACCOUNT_DISABLED` | 403 | User bị vô hiệu hóa |
| `AUTH_EMAIL_NOT_CONFIRMED` | 403 | Email chưa xác nhận khi publish |
| `RECIPE_NOT_FOUND` | 404 | Recipe không tồn tại/đã xóa |
| `RECIPE_FORBIDDEN` | 403 | Không phải owner/Admin |
| `RECIPE_PUBLISH_INCOMPLETE` | 422 | Thiếu Ingredient hoặc Step |
| `RECIPE_CONCURRENCY_CONFLICT` | 409 | ETag/xmin không khớp |
| `RECIPE_NOT_DELETED` | 409 | Restore/purge được gọi cho Recipe chưa soft-delete |
| `RECIPE_RESTORE_WINDOW_EXPIRED` | 409 | Recipe đã quá thời hạn khôi phục 30 ngày |
| `CATEGORY_NOT_FOUND` | 404 | Category không tồn tại |
| `CATEGORY_NAME_EXISTS` | 409 | Tên/slug đã tồn tại |
| `CATEGORY_DELETE_HAS_RECIPES` | 409 | Category còn Recipe active |
| `FILE_SIZE_EXCEEDED` | 422 | File lớn hơn 5 MB |
| `FILE_MIME_INVALID` | 422 | File không phải JPEG/PNG/WebP/AVIF hợp lệ |
| `RATE_LIMIT_EXCEEDED` | 429 | Vượt rate limit |

## Phụ lục C – Từ điển Thuật ngữ

```text
           Thuật ngữ         Viết  Định nghĩa
                             tắt

           Access Token      AT    JSON Web Token (JWT) dùng để xác thực API
                                   request. TTL = 15 phút. Ký bằng HS256.
           Application Error Code AEC Mã lỗi tùy chỉnh dạng SCREAMING_SNAKE_CASE
                                   trong trường "type" của RFC 7807 Problem Details.

           Archive           —     Trạng thái Recipe khi bị ẩn khỏi public listing nhưng
                                   không bị xóa. RecipeStatus.Archived.

           Author            —     Role người dùng mặc định sau khi đăng ký. Có thể
                                   tạo/quản lý recipe của mình.
           Background Job    —     Tác vụ xử lý bất đồng bộ chạy ngoài HTTP request
                                   cycle, quản lý bởi Hangfire.

           Clean Architecture CA   Kiến trúc phần mềm của Robert C. Martin tách biệt
                                   concerns theo layers (Domain, Application,
                                   Infrastructure, Presentation). Dependency chỉ đi vào
                                   trong (hướng Domain).
           Command Query     CQRS  Pattern tách biệt write model (Commands) và read
           Responsibility          model (Queries) để tối ưu từng luồng riêng.
           Segregation

           Content Delivery  CDN   Mạng phân phối nội dung tĩnh (ảnh, JS, CSS) từ
           Network                 server gần người dùng nhất.
```

<!-- Trang 70 -->

```text
           Thuật ngữ         Viết  Định nghĩa
                             tắt

           Core Web Vitals   CWV   Chỉ số đo lường UX của Google: LCP (tải trang), CLS
                                   (ổn định layout), INP (phản hồi tương tác).
           CQRS              -     Xem Command Query Responsibility Segregation

           Docker Compose    —     Công cụ định nghĩa và chạy multi-container Docker
                                   application qua file YAML.
           Draft             —     Trạng thái mặc định của Recipe khi mới tạo. Chỉ
                                   Author/Admin thấy.

           Full-Text Search  FTS   Tìm kiếm ngôn ngữ tự nhiên trong PostgreSQL qua
                                   tsvector/tsquery + unaccent extension.
           Hangfire          —     Thư viện .NET xử lý background jobs: fire-and-forget,
                                   delayed, recurring.

           HTTP Status Code  —     Mã phản hồi HTTP chuẩn (RFC 7231) cho biết kết
                                   quả xử lý request (2xx: thành công, 4xx: client error,
                                   5xx: server error).

           Incremental Static ISR  Tính năng Next.js tái sinh (regenerate) trang tĩnh theo
           Regeneration            chu kỳ (revalidate interval) thay vì build lại toàn bộ.
           JSON Web Token    JWT   Chuẩn mở (RFC 7519) định nghĩa cách truyền thông
                                   tin an toàn giữa các bên dưới dạng JSON object
                                   được ký.

           MediatR           —     Thư viện .NET triển khai Mediator pattern. Dispatch
                                   Commands/Queries qua Handler có pipeline
                                   behaviors.
           MinIO             —     Object storage server mã nguồn mở tương thích
                                   Amazon S3 API. Dùng để lưu trữ ảnh.

           Non-Functional    NFR   Yêu cầu chất lượng hệ thống: hiệu năng, bảo mật, độ
           Requirement             tin cậy, khả năng bảo trì...
           Nginx             —     Web server hiệu năng cao, dùng làm reverse proxy,
                                   load balancer và SSL termination.

           OpenTelemetry     OTEL  Framework quan sát hệ thống phân tán: distributed
                                   tracing, metrics, logs.
           Optimistic Concurrency — Kỹ thuật xử lý concurrent writes bằng ETag/xmin —
                                   không lock DB, phát hiện conflict khi save.

           Published         —     Trạng thái Recipe khi được công bố công khai.
                                   RecipeStatus.Published.

           Rate Limiting     —     Giới hạn số lượng request từ một IP trong khoảng
                                   thời gian nhất định để ngăn brute force/DDoS.
           Refresh Token     RT    Token dài hạn (7 ngày) dùng để lấy Access Token
                                   mới mà không cần đăng nhập lại.

           Refresh Token     —     Mỗi lần dùng Refresh Token để refresh → token cũ bị
           Rotation                revoke, cấp token mới (bảo mật cao hơn).
```

<!-- Trang 71 -->

```text
           Thuật ngữ         Viết  Định nghĩa
                             tắt

           Reuse Detection   —     Cơ chế phát hiện khi Refresh Token đã bị revoke
                                   được dùng lại → revoke toàn bộ token family của
                                   user.
           Slug              —     Chuỗi URL-friendly, dạng chữ-thường-gạch-nối, duy
                                   nhất, dùng để định danh Recipe/Category trên URL.

           Soft Delete       —     Đánh dấu IsDeleted=true thay vì xóa vật lý khỏi
                                   database. Dữ liệu có thể khôi phục.
           Software          SRS   Tài liệu đặc tả yêu cầu phần mềm theo IEEE 830 /
           Requirements            ISO/IEC/IEEE 29148.
           Specification

           TanStack Query    —     Thư viện React quản lý server state: caching,
                                   background refetch, optimistic updates.
           tsvector / tsquery —    Kiểu dữ liệu PostgreSQL cho full-text search. tsvector
                                   là chỉ mục đã xử lý, tsquery là biểu thức tìm kiếm.

           Unit of Work      UoW   Pattern đảm bảo nhiều operations được thực hiện
                                   trong một transaction duy nhất.
```

## Phụ lục D - Kết quả tích hợp phân tích bổ sung của nhóm

| Đề xuất được đối chiếu | Kết luận tích hợp | Lý do |
|---|---|---|
| Soft delete và purge sau 30 ngày | Chấp nhận; bổ sung Admin trash, restore và purge | Hoàn chỉnh vòng đời two-phase delete và tạo khả năng khôi phục thực tế |
| `sortBy` + `sortOrder` | Đã có trong baseline, giữ nguyên | Binding rõ ràng và thống nhất REST API |
| Decimal và UX phân số | Chấp nhận theo hướng phân lớp | Database/API giữ decimal để tính toán; frontend chịu trách nhiệm parse/format phân số |
| Nutrition nhập tay hay tự động | Chốt Manual-only cho MVP; dành enum AutoCalculated cho Phase 2 | Chưa có ingredient master data, nutrition reference và unit conversion engine |
| Step phẳng hay phân cấp | Giữ flat-list; không thêm `ParentStepId` trong v1.2 | Sub-step ngoài phạm vi MVP; cột nullable có thể bổ sung sau bằng migration không breaking |
| Redis và TTL Category 30 phút | Đã có trong baseline, giữ nguyên | Bảo đảm stateless backend và cache nhất quán khi scale ngang |
| Envelope `{data, meta}` | Đã có trong baseline, giữ nguyên | Một hợp đồng response duy nhất cho frontend/backend |

Các nội dung trong tài liệu phân tích bổ sung chỉ được coi là quyết định dự án khi đã xuất hiện trong `docs/requirements/SRS.md`. Tài liệu phân tích riêng không thay thế SRS này.
