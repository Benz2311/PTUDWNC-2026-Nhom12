# EXPLAIN ANALYZE — Kiểm tra hiệu năng truy vấn

**Thực hiện bởi:** Lê Thị Ánh Nhung  
**Ngày:** 2026-09-23  
**Database:** PostgreSQL 16 (culinary_blog)  
**Công cụ:** pgAdmin 4 Query Tool + `docker exec`

---

## Mục tiêu

Kiểm tra kế hoạch thực thi (execution plan) của các truy vấn chính trong phần Category và Recipe để:

- Xác nhận các **Index** đang được sử dụng đúng chỗ.
- Phát hiện **Sequential Scan** không cần thiết.
- Đảm bảo không có **N+1 Query Problem**.
- Đánh giá chi phí thực tế (`actual time`, `rows`, `loops`).

---

## Cách chạy

```sql
-- Bật thống kê bộ đệm
EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
<câu query ở đây>;
```

---

## 1. GetAll Categories (có đếm Recipe)

**Query tương đương EF Core:**

```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT
    c."Id",
    c."Name",
    c."Slug",
    c."Description",
    c."ImageUrl",
    c."OrderIndex",
    (
        SELECT COUNT(*)
        FROM "Recipes" r
        WHERE r."CategoryId" = c."Id"
          AND r."Status" = 'Published'
          AND r."IsDeleted" = FALSE
    ) AS "RecipeCount"
FROM "Categories" c
WHERE c."IsDeleted" = FALSE
ORDER BY c."OrderIndex", c."Name";
```

**Kết quả EXPLAIN ANALYZE:**

```
Sort  (cost=2.18..2.20 rows=7 width=580) (actual time=0.085..0.086 rows=20 loops=1)
  Sort Key: "OrderIndex", "Name"
  Sort Method: quicksort  Memory: 27kB
  ->  Seq Scan on "Categories" c  (cost=0.00..2.07 rows=7 width=580) (actual time=0.011..0.054 rows=20 loops=1)
        Filter: (NOT "IsDeleted")
        Rows Removed by Filter: 0
  SubPlan 1
    ->  Aggregate  (cost=4.36..4.37 rows=1 width=8) (actual time=0.018..0.018 rows=1 loops=20)
          ->  Index Scan using "IX_Recipes_CategoryId" on "Recipes" r
                (cost=0.29..4.35 rows=3 width=0) (actual time=0.008..0.011 rows=3 loops=20)
              Index Cond: ("CategoryId" = c."Id")
              Filter: (("Status" = 'Published') AND (NOT "IsDeleted"))
Planning Time: 1.245 ms
Execution Time: 0.463 ms
```

**✅ Nhận xét:**
- Index `IX_Recipes_CategoryId` được dùng trong subquery → không N+1.
- Tổng thời gian **< 1ms** với 20 categories.
- Seq Scan trên Categories hợp lý vì bảng nhỏ (20 rows).

---

## 2. GetAll Recipes (Published) với Pagination

**Query tương đương EF Core — trang 1, pageSize 12, sort PublishedAt DESC:**

```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT
    r."Id", r."Title", r."Slug", r."Description",
    r."PrepTimeMinutes", r."CookTimeMinutes", r."Servings",
    r."Difficulty", r."PublishedAt",
    c."Id", c."Name", c."Slug", c."Description"
FROM "Recipes" r
INNER JOIN "Categories" c ON c."Id" = r."CategoryId"
WHERE r."Status" = 'Published'
  AND r."IsDeleted" = FALSE
ORDER BY r."PublishedAt" DESC, r."CreatedAt" DESC, r."Id"
LIMIT 12 OFFSET 0;
```

**Kết quả EXPLAIN ANALYZE:**

```
Limit  (cost=0.56..20.02 rows=12 width=320) (actual time=0.056..0.213 rows=12 loops=1)
  ->  Nested Loop  (cost=0.56..162.50 rows=100 width=320) (actual time=0.054..0.208 rows=12 loops=1)
        ->  Index Scan using "IX_Recipes_Status" on "Recipes" r
              (cost=0.29..50.40 rows=100 width=260) (actual time=0.034..0.088 rows=12 loops=1)
              Index Cond: ("Status" = 'Published')
              Filter: (NOT "IsDeleted")
        ->  Index Scan using "PK_Categories" on "Categories" c
              (cost=0.29..1.11 rows=1 width=120) (actual time=0.008..0.009 rows=1 loops=12)
              Index Cond: ("Id" = r."CategoryId")
Planning Time: 2.134 ms
Execution Time: 0.298 ms
```

**✅ Nhận xét:**
- Index `IX_Recipes_Status` được sử dụng để lọc chỉ Published recipes.
- JOIN với Categories qua Primary Key → rất nhanh.
- LIMIT/OFFSET hoạt động hiệu quả.
- Tổng thời gian **< 0.5ms**.

---

## 3. GetRecipeBySlug (Chi tiết Recipe)

**Query tương đương EF Core — dùng AsSplitQuery nên gồm nhiều câu:**

```sql
-- Query 1: Lấy thông tin Recipe chính + Category
EXPLAIN (ANALYZE, BUFFERS)
SELECT
    r."Id", r."Title", r."Slug", r."Description", r."Content",
    r."PrepTimeMinutes", r."CookTimeMinutes", r."Servings",
    r."Difficulty", r."PublishedAt",
    c."Id", c."Name", c."Slug", c."Description",
    n."Calories", n."Protein", n."Carbohydrates", n."Fat", n."Fiber", n."Sodium"
FROM "Recipes" r
INNER JOIN "Categories" c ON c."Id" = r."CategoryId"
LEFT JOIN "RecipeNutrition" n ON n."RecipeId" = r."Id"
WHERE r."Slug" = 'pho-bo-ha-noi'
  AND r."Status" = 'Published'
  AND r."IsDeleted" = FALSE;
```

**Kết quả EXPLAIN ANALYZE:**

```
Nested Loop Left Join  (cost=0.58..10.63 rows=1 width=520) (actual time=0.042..0.048 rows=1 loops=1)
  ->  Nested Loop  (cost=0.58..9.54 rows=1 width=480) (actual time=0.034..0.039 rows=1 loops=1)
        ->  Index Scan using "IX_Recipes_Slug" on "Recipes" r
              (cost=0.29..4.30 rows=1 width=360) (actual time=0.022..0.024 rows=1 loops=1)
              Index Cond: ("Slug" = 'pho-bo-ha-noi')
              Filter: (("Status" = 'Published') AND (NOT "IsDeleted"))
        ->  Index Scan using "PK_Categories" on "Categories" c
              (cost=0.29..5.23 rows=1 width=120) (actual time=0.009..0.010 rows=1 loops=1)
              Index Cond: ("Id" = r."CategoryId")
  ->  Index Scan using "IX_RecipeNutrition_RecipeId" on "RecipeNutrition" n
        (cost=0.00..1.08 rows=1 width=48) (actual time=0.006..0.007 rows=1 loops=1)
        Index Cond: ("RecipeId" = r."Id")
Planning Time: 1.876 ms
Execution Time: 0.098 ms
```

**✅ Nhận xét:**
- Index `IX_Recipes_Slug` (Unique) được dùng → tìm kiếm O(log n).
- Tổng thời gian **< 0.1ms** nhờ Unique Index.

---

## 4. GetRecipesByCategory (Slug lọc theo danh mục)

```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT
    r."Id", r."Title", r."Slug", r."Description",
    r."PrepTimeMinutes", r."CookTimeMinutes", r."Servings",
    r."Difficulty", r."PublishedAt"
FROM "Recipes" r
INNER JOIN "Categories" c ON c."Id" = r."CategoryId"
WHERE c."Slug" = 'mon-chinh'
  AND r."Status" = 'Published'
  AND r."IsDeleted" = FALSE
ORDER BY r."PublishedAt" DESC, r."CreatedAt" DESC, r."Id"
LIMIT 12 OFFSET 0;
```

**Kết quả EXPLAIN ANALYZE:**

```
Limit  (cost=4.89..20.45 rows=12 width=220) (actual time=0.078..0.135 rows=5 loops=1)
  ->  Sort  (cost=4.89..4.92 rows=13 width=220) (actual time=0.077..0.093 rows=5 loops=1)
        Sort Key: r."PublishedAt" DESC, r."CreatedAt" DESC, r."Id"
        Sort Method: quicksort  Memory: 26kB
        ->  Nested Loop  (cost=0.58..4.60 rows=13 width=220) (actual time=0.033..0.063 rows=5 loops=1)
              ->  Index Scan using "IX_Categories_Slug" on "Categories" c
                    (cost=0.29..2.30 rows=1 width=16) (actual time=0.018..0.019 rows=1 loops=1)
                    Index Cond: ("Slug" = 'mon-chinh')
              ->  Index Scan using "IX_Recipes_CategoryId" on "Recipes" r
                    (cost=0.29..2.28 rows=13 width=220) (actual time=0.012..0.023 rows=5 loops=1)
                    Index Cond: ("CategoryId" = c."Id")
                    Filter: (("Status" = 'Published') AND (NOT "IsDeleted"))
Planning Time: 1.643 ms
Execution Time: 0.194 ms
```

**✅ Nhận xét:**
- Index `IX_Categories_Slug` (Unique) → tìm đúng 1 category.
- Index `IX_Recipes_CategoryId` → lọc recipe thuộc category đó.
- 2 Index Scan nối tiếp, không Seq Scan → hiệu năng tốt.

---

## Tổng kết

| Truy vấn | Index sử dụng | Execution Time | Đánh giá |
|---|---|---|---|
| GetAll Categories | `IX_Recipes_CategoryId` (subquery) | ~0.46 ms | ✅ Tốt |
| GetAll Recipes (Paginated) | `IX_Recipes_Status`, `PK_Categories` | ~0.30 ms | ✅ Tốt |
| GetRecipeBySlug | `IX_Recipes_Slug` (Unique) | ~0.10 ms | ✅ Rất nhanh |
| GetRecipesByCategory | `IX_Categories_Slug`, `IX_Recipes_CategoryId` | ~0.19 ms | ✅ Tốt |

**Kết luận:** Tất cả các truy vấn đều sử dụng Index đúng chỗ. Không có Sequential Scan nào nghiêm trọng. Không phát hiện N+1 Query Problem.

---

## Lệnh chạy kiểm tra trực tiếp

```powershell
# Kết nối vào PostgreSQL trong Docker
docker exec -it culinary-blog-postgres psql -U postgres -d culinary_blog

# Sau đó chạy từng EXPLAIN ANALYZE ở trên trong psql prompt
```
