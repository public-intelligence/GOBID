export interface ErrorResponse {
  error: {
    title: string;
    detail: Record<string, string[]>;
  };
}
export interface ErrorAdapter {
  title: string;
  detail: Record<string, string[]>;
}

export interface Pagination {
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  totalRecords: number;
  currentPage?: number;
  firstPage?: string | null;
  lastPage?: string | null;
  nextPage?: string | null;
  previousPage?: string | null;
}

export type QueryParams = Record<string, number | string | null | boolean>;

export interface PaginationResponse<T> {
  pageNumber: number;
  pageSize: number;
  totalRecords: number;
  totalPages: number;
  results: T[];
}

export interface ServerResponse<T> {
  data: T;
  message: string;
  errors: string;
  succeeded: boolean;
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
