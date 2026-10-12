export interface AdminUserDto {
  id: string;
  userName: string;
  email: string;
  displayName: string;
  avatarUrl?: string | null;
  bio?: string | null;
  roles: string[];
  isActive: boolean;
  createdAt: string;
}

export interface AdminUserQuery {
  page: number;
  pageSize: number;
  keyword?: string;
  isActive?: boolean;
}

export interface PagedUsersResponse {
  items: AdminUserDto[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface UpdateUserStatusRequest {
  isActive: boolean;
}
