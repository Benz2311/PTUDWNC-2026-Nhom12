export interface CommentAuthor {
  id: string;
  displayName: string;
  avatarUrl?: string | null;
}

export interface CommentItem {
  id: string;
  recipeId: string;
  content: string;
  createdAt: string;
  updatedAt?: string | null;
  parentCommentId?: string | null;
  isHidden: boolean;
  author: CommentAuthor;
  replies: CommentItem[];
}

export interface CommentPagedResult {
  items: CommentItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}
