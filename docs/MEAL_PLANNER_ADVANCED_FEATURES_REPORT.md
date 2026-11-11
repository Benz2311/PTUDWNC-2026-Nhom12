# Báo cáo chi tiết: Meal Planner và các tính năng nâng cao

## 1. Mục tiêu và phạm vi

Meal Planner giúp người dùng sắp xếp món ăn theo ngày và khung bữa, tự tổng hợp nguyên liệu cần mua, rồi đánh dấu các nguyên liệu đã mua. Những tính năng mở rộng bổ sung kế hoạch có tên, khẩu phần cho từng món, nhiều shopping list, ghi nhận bữa đã dùng, tổng hợp dinh dưỡng, xem trước định lượng Recipe theo servings và xuất Recipe để in.

Báo cáo này giải thích mỗi chức năng theo các giai đoạn để một yêu cầu đi từ giao diện tới dữ liệu và quay lại. Nội dung được đối chiếu với mã nguồn:

- Giao diện hiện hành: [`frontend/app/meal-planner/page.tsx`](../frontend/app/meal-planner/page.tsx).
- API client của giao diện: [`frontend/lib/api/meal-plan.ts`](../frontend/lib/api/meal-plan.ts).
- API lịch và shopping list legacy: [`MealPlanEndpoints.cs`](../backend/src/CulinaryBlog.Api/Endpoints/MealPlanEndpoints.cs).
- API plan/list có tên v2: [`PlanningEndpoints.cs`](../backend/src/CulinaryBlog.Api/Endpoints/PlanningEndpoints.cs).
- API mục tiêu và tổng hợp dinh dưỡng: [`SocialNutritionEndpoints.cs`](../backend/src/CulinaryBlog.Api/Endpoints/SocialNutritionEndpoints.cs).
- API scale và export Recipe: [`RecipeEndpoints.cs`](../backend/src/CulinaryBlog.Api/Endpoints/Recipes/RecipeEndpoints.cs).

### Điểm cần phân biệt: giao diện đang dùng legacy, không phải v2

Trang `/meal-planner` hiện gọi `/api/v1/me/meal-plan` và `/api/v1/me/shopping-list`. Các route này lưu dữ liệu theo `MealPlanEntry` và các `ShoppingListItem` legacy. Backend đồng thời có route `/api/v1/me/meal-plans` và `/api/v1/me/shopping-lists` với entity v2 có tên là `MealPlan`, `MealPlanItem`, `ShoppingList`.

Hai bộ API cùng tồn tại nhưng không tự đồng bộ với nhau. Vì vậy, một món được thêm trên trang Meal Planner hiện tại không tự trở thành `MealPlanItem` v2; tính năng servings, consumed và nutrition gắn với plan v2 cũng chưa được tích hợp vào giao diện đó.

## 2. Các đối tượng và quan hệ dữ liệu

| Đối tượng | Vai trò | Dữ liệu quan trọng |
|---|---|---|
| `MealPlanEntry` (legacy) | Một món trong một slot ngày/bữa của một user | `UserId`, `RecipeId`, `PlannedDate`, `MealType` |
| `MealPlan` (v2) | Kế hoạch có tên và khoảng ngày | `UserId`, `Title`, `StartDate`, `EndDate` |
| `MealPlanItem` (v2) | Món cụ thể thuộc plan | `MealPlanId`, `RecipeId`, `PlanDate`, `MealType`, `Servings`, `Note`, `Consumed`, `ConsumedAt` |
| `ShoppingList` (v2) | Danh sách đi chợ có tên, thuộc user | `UserId`, `Name`, thời gian tạo/cập nhật |
| `ShoppingListItem` | Một nguyên liệu trong danh sách hoặc mục legacy | `UserId`, `ShoppingListId` có thể null, `RecipeId` có thể null, `Name`, `Quantity`, `Unit`, `IsChecked` |
| `UserNutritionGoal` | Hồ sơ và mục tiêu dinh dưỡng của user | Tuổi, giới tính, chiều cao, cân nặng, mức hoạt động, mục tiêu và calories/macros mục tiêu |
| `Recipe` / `RecipeIngredient` | Nguồn món và nguyên liệu để lên lịch, tạo danh sách, scale | Số khẩu phần chuẩn, nguyên liệu, lượng/đơn vị và dữ liệu dinh dưỡng |

Ở legacy, database đặt unique index cho `(UserId, PlannedDate, MealType)`, nên một user có tối đa một món cho mỗi slot ngày/bữa. Ở v2, ràng buộc tương đương là `(MealPlanId, PlanDate, MealType)`, nghĩa là mỗi plan có tối đa một món trong cùng slot. Các đối tượng cá nhân đều truy vấn dựa trên User ID lấy từ JWT.

## 3. Luồng Meal Planner hiện hành

### 3.1 Mở trang, kiểm tra đăng nhập và tải dữ liệu

Mặc định, trang khởi tạo khoảng ngày từ hôm nay đến sáu ngày sau. Người dùng được phép đổi ngày bắt đầu/kết thúc; giao diện hiển thị tối đa 14 ngày và bốn khung bữa: `Breakfast`, `Lunch`, `Dinner`, `Snack`.

**Các giai đoạn thực thi**

1. **Khởi tạo khoảng ngày:** giao diện tạo giá trị `from` và `to` ở trình duyệt, sau đó dựng các ngày trong khoảng.
2. **Kiểm tra phiên:** trang tìm access token `culinary_access_token` trong local storage. Không có token thì hiện yêu cầu đăng nhập và không gọi các API dữ liệu cá nhân.
3. **Tải danh sách món:** khi đã đăng nhập, gọi Recipe API để lấy tối đa 100 Recipe công khai làm lựa chọn cho các slot. Vì đây là danh sách công khai, Recipe riêng tư/nháp không được đảm bảo có mặt.
4. **Tải lịch của user:** gọi `GET /api/v1/me/meal-plan?from={from}&to={to}`. Backend lọc theo User và ngày, rồi trả ID, ngày, loại bữa, Recipe ID, tên, slug và ảnh chính.
5. **Tạo shopping list:** gọi `POST /api/v1/me/shopping-list/generate` với cùng khoảng ngày; backend tổng hợp nguyên liệu rồi trả danh sách.
6. **Hiển thị:** giao diện đặt món vào các slot, hiển thị số bữa đã lên lịch, nhóm shopping list theo từ khóa tên nguyên liệu và tính tiến độ đã mua.
7. **Đổi khoảng ngày:** thay `from` hoặc `to` chạy lại toàn bộ tải dữ liệu và generate list.

Các request tải danh sách Recipe, lịch và shopping list nằm trong cùng một luồng tải. Lỗi 401 được hiển thị thành thông báo phiên hết hạn; các lỗi tải khác hiện dùng thông báo chung. Hiện UI chưa tách trạng thái lỗi riêng cho từng nguồn dữ liệu.

### 3.2 Thêm món hoặc thay món

**Các giai đoạn thực thi**

1. Người dùng mở slot trống bằng **Thêm món**, hoặc mở slot đã có món bằng **Đổi món**.
2. Giao diện hiện danh sách Recipe lấy ở bước tải trang. Khi người dùng chọn món, gửi ngày, loại bữa và Recipe ID qua `PUT /api/v1/me/meal-plan`.
3. Backend lấy User ID từ JWT, chỉ chấp nhận loại bữa `Breakfast`, `Lunch`, `Dinner` hoặc `Snack`, rồi kiểm tra Recipe tồn tại và chưa bị xóa mềm.
4. Backend tìm slot theo User + ngày + loại bữa:
   - Nếu chưa có entry, tạo `MealPlanEntry`.
   - Nếu đã có entry, thay `RecipeId` trên entry đó và cập nhật thời gian sửa.
5. Database unique index bảo vệ slot không bị tạo trùng nếu có hai yêu cầu đồng thời.
6. Sau khi lưu, UI tải lại lịch và tạo lại shopping list để cập nhật nguyên liệu theo món mới.

**Lưu ý quyền Recipe:** endpoint legacy kiểm tra Recipe chưa bị xóa, nhưng kiểm tra trong endpoint này không ràng buộc trạng thái Published hoặc chủ sở hữu. Nếu nghiệp vụ yêu cầu chỉ được xếp món công khai hoặc món riêng do chính user sở hữu, cần bổ sung điều kiện trạng thái/quyền ở backend; danh sách chọn Recipe công khai ở UI không thay thế được kiểm tra quyền phía server.

### 3.3 Xóa món khỏi lịch

**Các giai đoạn thực thi**

1. Người dùng nhấn nút xóa trên một thẻ món.
2. UI gọi `DELETE /api/v1/me/meal-plan/{id}`.
3. Backend tìm entry bằng cả ID và User ID hiện tại. Entry không tồn tại hoặc thuộc user khác trả 404.
4. Backend xóa entry và trả 204 No Content.
5. UI tải lại lịch và yêu cầu generate lại shopping list. Nguyên liệu chỉ phát sinh từ món đã bỏ sẽ không còn trong kết quả tổng hợp mới.

### 3.4 Tạo lại danh sách nguyên liệu legacy

Endpoint `POST /api/v1/me/shopping-list/generate` nhận `From` và `To`. Nếu ngày kết thúc trước ngày bắt đầu, backend trả lỗi validation. Khi khoảng ngày hợp lệ, quá trình diễn ra như sau:

1. Tìm các `MealPlanEntry` thuộc user hiện tại trong khoảng ngày và nạp Recipe cùng Ingredients.
2. Loại Recipe đã xóa mềm, Ingredient đã xóa hoặc Ingredient không có tên hợp lệ.
3. Chuẩn hóa một số đơn vị. Ví dụ kg được đổi về g, l về ml; gram/gam được chuẩn hóa; một số quy tắc đặc biệt được áp dụng cho nguyên liệu đếm như gạo hoặc tôm.
4. Gom nguyên liệu theo tên chuẩn hóa và đơn vị tương thích, sau đó cộng các số lượng có dữ liệu.
5. Chuẩn hóa lượng kết quả để hiển thị thuận tiện; ví dụ lượng g/ml đủ lớn có thể được đổi lại sang kg/l, đơn vị đếm được làm tròn theo quy tắc hiện tại.
6. Đối chiếu với item tự sinh đã được tick trước đó. Nếu tên, đơn vị và lượng sau chuẩn hóa khớp, backend cố gắng giữ `IsChecked`.
7. Xóa item tự sinh legacy cũ rồi lưu các item kết quả mới. Item nhập tay (`RecipeId` null) và item thuộc danh sách v2 (`ShoppingListId` khác null) không thuộc tập bị thay.
8. UI nhận kết quả và nhóm theo các từ khóa tiếng Việt như thịt/cá, rau củ, gia vị. Phân nhóm này là heuristic phía client, không phải phân loại thông minh/AI.

**Giới hạn quan trọng về khoảng ngày:** dữ liệu item tự sinh legacy không lưu khoảng ngày. Khi backend tìm các item cũ cần thay, truy vấn lọc theo user, có Recipe ID và không thuộc ShoppingList v2 nhưng không lọc theo `From`/`To`. Vì thế, generate cho một tuần sẽ thay danh sách tự sinh legacy trước đó của user, kể cả danh sách được tạo khi xem tuần khác. Nếu cần lưu nhiều danh sách theo tuần, mô hình v2 `ShoppingList` có ID/tên riêng phù hợp hơn.

**Giới hạn khẩu phần:** `MealPlanEntry` legacy không có trường `Servings`, nên số lượng nguyên liệu được cộng theo định lượng gốc của Recipe. Chức năng scale theo số suất chỉ có ở Recipe serving preview và một số luồng v2.

### 3.5 Tích/bỏ tích và xóa nguyên liệu

**Tích hoặc bỏ tích**

1. Khi checkbox thay đổi, UI gửi `PUT /api/v1/me/shopping-list/{id}/check` với `isChecked`.
2. Backend tìm ShoppingListItem theo ID và User ID hiện tại; không tìm thấy thì trả 404.
3. Backend cập nhật `IsChecked`, ghi thời gian sửa và trả item mới.
4. UI cập nhật trạng thái item và tính lại số đã mua trên tổng số item.

**Xóa một item**

1. UI gửi `DELETE /api/v1/me/shopping-list/{id}`.
2. Backend chỉ xóa item thuộc user đang đăng nhập.
3. Sau phản hồi thành công, UI bỏ item khỏi danh sách hiện trên màn hình.

**Xóa các item đã mua:** nút **Xóa tất cả mục đã mua** gọi thao tác xóa cho từng item đã tick. API legacy có chức năng thêm item thủ công nhưng trang `/meal-planner` hiện chưa có form để tạo item đó. Khi có lỗi ở một thao tác xóa, UI báo lỗi từ handler của thao tác tương ứng.

## 4. Các tính năng nâng cao ở API v2

### 4.1 Tạo kế hoạch có tên và thêm món theo servings

API v2 gồm `GET /api/v1/me/meal-plans`, `GET /api/v1/me/meal-plans/current`, `POST /api/v1/me/meal-plans` và các route thêm/sửa/xóa item dưới `/api/v1/me/meal-plans/{id}/items`. Tất cả đều yêu cầu đăng nhập.

**Tạo một plan**

1. Client gửi tên, ngày bắt đầu và ngày kết thúc qua `POST /me/meal-plans`.
2. Backend xác thực tên không rỗng, không dài quá giới hạn và ngày kết thúc không trước ngày bắt đầu.
3. Backend tạo `MealPlan` gắn với User ID hiện tại và lưu vào database.
4. Trả 201 Created cùng ID và nội dung plan.

**Thêm hoặc thay món trong plan**

1. Client gửi Recipe ID, ngày, MealType, Servings và Note tùy chọn tới `POST /me/meal-plans/{id}/items`.
2. Backend xác nhận plan thuộc user hiện tại.
3. Backend kiểm tra ngày nằm trong khoảng plan, loại bữa thuộc bốn giá trị hợp lệ và servings ít nhất bằng 1.
4. Backend tìm slot theo plan/ngày/loại bữa. Nếu có slot thì cập nhật Recipe, servings, note; nếu chưa có thì tạo `MealPlanItem`.
5. Database unique index bảo vệ chống trùng slot. Client có thể dùng `PUT` để cập nhật và `DELETE` để xóa item.

`GET /me/meal-plans/current` chọn plan bao phủ ngày hiện tại theo ngày UTC; nếu không có plan phù hợp thì trả 404. Một điểm cần hoàn thiện là endpoint update item cần được kiểm tra để áp cùng quy tắc tồn tại/trạng thái Recipe như endpoint thêm item.

### 4.2 Shopping list có tên và nhiều danh sách

V2 cung cấp tạo, liệt kê phân trang, xem chi tiết, thêm/xóa item, toggle checked, xóa toàn bộ item đã tick và xóa danh sách:

- `POST /api/v1/me/shopping-lists`
- `GET /api/v1/me/shopping-lists?page=...&pageSize=...`
- `GET /api/v1/me/shopping-lists/{id}`
- `POST /api/v1/me/shopping-lists/{id}/items`
- `PATCH /api/v1/me/shopping-lists/{id}/items/{itemId}/toggle`
- `DELETE /api/v1/me/shopping-lists/{id}/items/{itemId}`
- `DELETE /api/v1/me/shopping-lists/{id}/items/checked`
- `DELETE /api/v1/me/shopping-lists/{id}`

**Tạo ShoppingList từ Recipe**

1. Client gửi tên danh sách, danh sách Recipe ID và `TargetServings`.
2. Backend xác thực tên và giới hạn `TargetServings` từ 1 đến 100.
3. Backend chỉ lấy Recipe đã Published và chưa xóa. Nếu không tìm thấy đủ mọi Recipe ID duy nhất được gửi, trả Not Found thay vì tạo danh sách không đầy đủ.
4. Với từng Recipe, tính hệ số `TargetServings / Recipe.Servings`.
5. Nhân số lượng Ingredients theo hệ số, chuẩn hóa các đơn vị có thể đổi và gộp theo tên/đơn vị.
6. Lưu ShoppingList và ShoppingListItems, rồi trả danh sách mới.

**Tạo ShoppingList từ MealPlan v2**

1. Client gọi `POST /me/meal-plans/{id}/generate-shopping-list`.
2. Backend tìm plan theo ID và User ID; không thuộc user hiện tại thì không trả dữ liệu plan.
3. Mỗi item dùng tỷ lệ servings của item trên Recipe chuẩn để scale nguyên liệu.
4. Backend gộp các nguyên liệu tương đồng, suy ra category, đặt thứ tự và tạo ShoppingList mới tên `Đi chợ: {Title}`.
5. Mỗi lần gọi hiện tạo danh sách mới; API chưa đồng bộ kết quả vào một danh sách đã sinh trước đó.

Chuyển đổi đơn vị v2 hiện hỗ trợ các đơn vị phổ biến g/kg và ml/l. Đơn vị không nằm trong bảng chuyển đổi được giữ nguyên; các nguyên liệu có đơn vị không tương thích không thể cộng chính xác chỉ dựa vào tên.

### 4.3 Đánh dấu bữa đã dùng

Mỗi `MealPlanItem` v2 có `Consumed` và `ConsumedAt`. API `PATCH /api/v1/me/meal-plans/{planId}/items/{itemId}/consumed` nhận giá trị boolean `Consumed`.

1. Client gửi plan ID, item ID và trạng thái muốn đặt.
2. Backend tìm item đồng thời xác nhận item nằm trong plan thuộc User ID hiện tại.
3. Nếu không tìm thấy tổ hợp phù hợp, trả 404.
4. Nếu đặt `Consumed=true`, backend ghi thời điểm UTC vào `ConsumedAt`; nếu bỏ đánh dấu, xóa thời điểm đó.
5. Lần tổng hợp dinh dưỡng sau đó chỉ tính các item đã được đánh dấu consumed.

Endpoint này tác động lên MealPlanItem v2. Nó không thể dùng trực tiếp để ghi nhận bữa từ MealPlanEntry legacy.

### 4.4 Mục tiêu dinh dưỡng cá nhân

API `GET/PUT /api/v1/me/nutrition-goals` tải hoặc cập nhật mục tiêu gắn với user hiện tại. Khi cập nhật, backend xác thực:

- Tuổi từ 13 đến 120.
- Chiều cao từ 100 đến 250 cm.
- Cân nặng từ 30 đến 350 kg.
- Giới tính, mức hoạt động và mục tiêu phải là enum hợp lệ.
- Giá trị chiều cao/cân nặng phải là số hữu hạn.

Sau khi xác thực dữ liệu, `NutritionGoalCalculator` tính theo quy tắc ứng dụng:

1. BMR dùng công thức Mifflin–St Jeor: `10 × cân nặng + 6.25 × chiều cao − 5 × tuổi + hệ số giới tính` (nam `+5`, nữ `−161`).
2. TDEE = BMR × hệ số hoạt động: ít vận động `1.2`, nhẹ `1.375`, vừa `1.55`, cao `1.725`.
3. Điều chỉnh theo mục tiêu: giảm cân `−500 kcal`, duy trì `0`, tăng cơ `+250 kcal`.
4. Calories mục tiêu không thấp hơn BMR.
5. Protein mục tiêu = `1.6 g × cân nặng (kg)`.
6. Fat mục tiêu = 25% calories chia cho 9 kcal/g.
7. Carbohydrate là phần calories còn lại sau protein và fat chia cho 4 kcal/g.
8. Backend lưu hồ sơ và các mục tiêu calories/macros; response còn có BMR và TDEE.

Đây là quy tắc ước tính của phần mềm, không phải chẩn đoán hay hướng dẫn điều trị và không thay thế tư vấn chuyên gia dinh dưỡng/y tế.

### 4.5 Tổng hợp dinh dưỡng đã tiêu thụ

API `GET /api/v1/me/nutrition-summary?date=...&period=day|week` làm các bước:

1. Xác định ngày mục tiêu; mặc định là hôm nay theo UTC. Ngày trong tương lai bị từ chối.
2. Chuẩn hóa `period`; chỉ hỗ trợ `day` hoặc `week`.
3. Với tuần, tìm tuần từ thứ Hai đến hết Chủ nhật. Mốc lọc được chuyển thành khoảng thời gian UTC nửa mở `[start, end)`.
4. Truy vấn MealPlanItem thuộc user hiện tại có `Consumed=true` và `ConsumedAt` nằm trong khoảng.
5. Tính từng chất theo tỷ lệ `MealPlanItem.Servings / Recipe.Servings`, rồi cộng Calories, Protein, Carbohydrates và Fat.
6. Trả ngày/khoảng ngày, period, số bữa đã dùng và tổng dinh dưỡng.

Response summary hiện chưa có Fiber hoặc Sodium, dù Recipe có thể lưu hai giá trị này. Do việc lọc dựa trên UTC, client cần thống nhất múi giờ hiển thị để tránh hiểu nhầm bữa ăn sát nửa đêm nằm ở ngày nào.

## 5. Tiện ích mở rộng trên Recipe

### 5.1 Xem trước Recipe theo số khẩu phần

Recipe API có `GET /api/v1/recipes/{slug}/servings?servings=...` và `POST /api/v1/recipes/{slug}/scale`:

1. Client gửi slug và số servings mục tiêu.
2. Backend từ chối servings nhỏ hơn 1 hoặc lớn hơn 1000.
3. Chỉ Recipe Published và chưa xóa mới được tìm thấy; nếu không, trả 404.
4. Backend tính hệ số `servings mục tiêu / servings gốc`.
5. Nhân lượng từng Ingredient và các giá trị Calories, Protein, Carbohydrates, Fat, Fiber, Sodium theo hệ số.
6. Trả preview; không ghi đè Recipe gốc.

Đây là phép scale tuyến tính theo khẩu phần. Các quy tắc làm tròn/đơn vị phục vụ nấu ăn nên được xử lý thêm ở UI nếu cần kết quả thân thiện hơn.

### 5.2 Xuất Recipe PDF

`GET /api/v1/recipes/{slug}/export/pdf` chỉ tìm Recipe Published chưa xóa. Khi hợp lệ, backend:

1. Nạp Recipe, Ingredients và Steps.
2. Sắp xếp Ingredients theo `SortOrder`, Steps theo `StepNumber`.
3. Tạo PDF gồm tiêu đề, khẩu phần, danh sách nguyên liệu và các bước.
4. Trả file MIME `application/pdf` với tên theo slug.

**Giới hạn hiện tại cần biết:** bộ tạo PDF là dạng tối giản, dùng font Helvetica cơ bản và nội dung ASCII, chỉ dựng một trang, chưa đưa ảnh công thức vào tài liệu. Vì vậy không nên mô tả tính năng hiện tại là PDF trình bày hoàn chỉnh hoặc đảm bảo hiển thị tiếng Việt có dấu; cần nâng cấp font Unicode, phân trang và xử lý ảnh nếu yêu cầu chất lượng in cao.

## 6. Xác thực, quyền sở hữu và phản hồi lỗi

- Các route cá nhân dưới `/api/v1/me/...` yêu cầu xác thực. Backend lấy User ID từ claim `NameIdentifier` hoặc `sub`.
- Các thao tác đọc/cập nhật/xóa dữ liệu cá nhân đều phải giới hạn theo User ID. ID không tồn tại hoặc không thuộc user thường trả 404 để không tiết lộ đối tượng của user khác.
- Dữ liệu đầu vào không hợp lệ trả validation response (thường là 422); request không có phiên trả 401.
- UI hiện kiểm tra token ở browser, xử lý riêng lỗi 401 khi tải trang và hiển thị thông báo chung với một số thao tác khác. Các hàm thao tác chưa đồng nhất trong việc đưa chi tiết lỗi API ra người dùng; cần cải thiện để phân biệt 401, validation và lỗi máy chủ.
- Kiểm tra quyền ở client chỉ hỗ trợ trải nghiệm. Quy tắc sở hữu, trạng thái Recipe và giới hạn dữ liệu cần luôn được xác thực lại ở API.

## 7. Khác biệt trạng thái và hạng mục cần hoàn thiện

| Hạng mục | Hiện trạng mã nguồn | Việc cần lưu ý |
|---|---|---|
| Trang `/meal-planner` | Dùng MealPlanEntry và ShoppingListItem legacy | Đang có lịch theo ngày/bữa, đổi/xóa món, generate và checklist. |
| Kế hoạch v2 | Có API và migration cho MealPlan/MealPlanItem | UI hiện hành chưa gọi; có tên, khoảng ngày, servings, note và consumed. |
| Shopping list v2 | Có API và migration cho nhiều danh sách | UI hiện hành chưa quản lý danh sách có tên; v2 hỗ trợ phân trang, item và checked. |
| Serving preview | Có Recipe API | Chỉ Recipe công khai đã Published; kết quả không lưu vào Recipe. |
| Nutrition goal/summary | Có API backend | Summary dựa trên MealPlanItem v2 đã consumed; chưa gắn UI hiện tại. |
| Recipe PDF | Có endpoint trả file | Đầu ra tối giản, ASCII, một trang, chưa bao gồm ảnh. |

Đề xuất thứ tự hoàn thiện:

1. Chọn rõ sẽ duy trì legacy hay chuyển giao diện Meal Planner sang v2; không trộn dữ liệu giữa hai mô hình.
2. Nếu chuyển v2, xác định cách chuyển dữ liệu MealPlanEntry và ShoppingListItems legacy, gồm cả item thủ công.
3. Hoàn thiện UI cho nhiều plan/list, servings, note, toggle consumed, quản lý item và trạng thái lỗi cụ thể.
4. Quy định thao tác generate list là tạo list mới hay cập nhật list có sẵn; hiện v2 generate tạo list mới mỗi lần.
5. Bổ sung kiểm tra quyền/trạng thái Recipe nhất quán cho mọi endpoint thêm và cập nhật MealPlanItem.
6. Thống nhất xử lý ngày/múi giờ, chuẩn hóa đơn vị và chính sách gộp nguyên liệu không tương thích.
7. Nâng cấp PDF để hỗ trợ Unicode, nhiều trang và ảnh nếu đây là yêu cầu phát hành.

## 8. Kịch bản nghiệm thu theo luồng

1. **Phiên đăng nhập:** mở trang khi chưa có token, token hợp lệ và token hết hạn; xác nhận không lộ dữ liệu cá nhân và thông báo đúng.
2. **Khoảng ngày:** tải mặc định một tuần, đổi hai mốc, kiểm tra khoảng không hợp lệ và giới hạn hiển thị 14 ngày.
3. **Tạo/thay slot:** thêm món, đổi món cùng slot, kiểm tra chỉ có một entry và từ chối MealType ngoài danh sách.
4. **Cô lập user:** thử đọc/xóa entry hoặc shopping item bằng ID thuộc tài khoản khác.
5. **Generate nguyên liệu:** nhiều Recipe có nguyên liệu trùng, đơn vị kg/g và l/ml, lượng null, Recipe/Ingredient bị xóa.
6. **Giữ checked:** generate lại khi item không đổi và khi tên/lượng/đơn vị thay đổi; xác nhận trạng thái tick đúng chính sách.
7. **Phạm vi ngày legacy:** tạo danh sách tự sinh từ hai khoảng ngày khác nhau để xác nhận hành vi thay thế toàn bộ item tự sinh của user.
8. **Checklist:** tích/bỏ tích, xóa item, xóa tất cả item đã mua và mô phỏng lỗi một item.
9. **Plan v2:** tạo plan với khoảng ngày, thêm/sửa/xóa item; từ chối ngày ngoài khoảng, MealType không hợp lệ, servings nhỏ hơn 1 và slot duplicate.
10. **Shopping list v2:** tạo từ nhiều Recipe, kiểm tra Recipe không Published, scale, gộp đơn vị, phân trang, toggle và xóa checked.
11. **Nutrition:** cập nhật hồ sơ hợp lệ/không hợp lệ; tổng hợp ngày/tuần chỉ từ meal đã consumed; thử ngày tương lai, period không hỗ trợ và ranh giới UTC.
12. **Recipe tiện ích:** servings 1/1000 và ngoài giới hạn; Recipe không Published; PDF với tiêu đề tiếng Việt, nội dung nhiều trang và công thức có ảnh.

## 9. Kết luận

Luồng Meal Planner hiện hành đã cung cấp các bước cơ bản: xác thực user, tải Recipe và lịch, thêm/đổi/xóa món, tổng hợp nguyên liệu và theo dõi đã mua. Nền tảng backend mở rộng thêm kế hoạch có tên, khẩu phần từng món, nhiều shopping list và theo dõi dinh dưỡng.

Điểm quan trọng nhất trong báo cáo triển khai là sự khác nhau giữa legacy và v2: UI chưa dùng các API nâng cao v2, legacy list tự sinh không lưu phạm vi ngày và cũng không scale theo servings. Vì vậy, để cung cấp trọn trải nghiệm Meal Planner nâng cao, bước tiếp theo là thống nhất mô hình dữ liệu và chuyển UI có kế hoạch, sau đó kiểm thử quyền sở hữu, số lượng, đơn vị, ngày giờ và trường hợp lỗi đầu-cuối.
