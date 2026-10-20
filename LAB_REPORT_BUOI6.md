# BAO CAO LAB - MON PHAT TRIEN UNG DUNG WEB NANG CAO

Lab: 06 | Tu ngay: 09/09/2026 | den ngay: 15/09/2026
MSSV:  | Ho va ten: Nguyen Van Quoc
Nhom: 12

Cong viec:

STT | Cong viec duoc giao | Lien ket den github branch | Tien do %
1 | Ra soat va hoan thien cac lop tien ich phia Front-end phuc vu Authentication, quan ly phien dang nhap va xu ly loi tra ve tu Backend. | https://github.com/Benz2311/PTUDWNC-2026-Nhom12/tree/feat/NguyenVanQuoc-update-buoi6 | 100%

Da hoan thanh:
- Tao lib/api/errors.ts: chuan hoa loi API theo RFC 7807 ProblemDetails, ApiError, parseProblemDetails, toApiError, isNetworkError. Ho tro 400, 401, 403, 404, 409, 422 va loi ket noi mang.
- Tao lib/auth-store.ts: zustand store quan ly user, accessToken, refreshToken, roles, isAuthenticated, isLoading, setUser, setTokens, clear, setLoading, hydrate.
- Tao components/auth/protected-route.tsx: ProtectedRoute voi roles check va redirect.
- Tich hop auth-store vao contexts/AuthContext.tsx (login, register, logout, updateProfile, hydrate, session-expired).
- Kiem tra TypeScript Types Auth duoc import va su dung thong nhat.
- Kiem thu cac tinh huong dang nhap thanh cong/sai, token het han, khong du quyen, loi Backend.
- Chay TypeScript compile va production build xac nhan khong loi.

Chua hoan thanh: Khong.

Luu y:
- Cong viec duoc giao: Liet ke moi tinh nang/chuc nang/cong viec duoc giao tren mot dong.
- Tu danh gia muc do hoan thanh va ghi vao cot Tien do %.
EOF