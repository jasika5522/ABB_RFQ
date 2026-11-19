export interface RfqItemRequest {
  itemName: string;
  description?: string | null;
  quantity: number;
  unit: string;
}

export interface CreateRfqRequest {
  rfqNumber: string;
  title: string;
  description?: string | null;
  currencyCode: string;
  items: RfqItemRequest[];
}

export interface UpdateRfqRequest {
  title: string;
  description?: string | null;
  currencyCode: string;
  items: RfqItemRequest[];
}

export interface RfqItemResponse {
  rfqItemId: number;
  rfqId: number;
  itemName: string;
  description?: string | null;
  quantity: number;
  unit: string;
  createdAt: string;
}

export interface RfqSummaryResponse {
  rfqId: number;
  rfqNumber: string;
  title: string;
  currencyCode: string;
  status: string;
  createdBy: number;
  createdAt: string;
  updatedAt: string;
}

export interface RfqDetailResponse {
  rfqId: number;
  rfqNumber: string;
  title: string;
  description?: string | null;
  currencyCode: string;
  status: string;
  createdBy: number;
  createdAt: string;
  updatedAt: string;
  items: RfqItemResponse[];
}

export interface PagedRfqsResponse {
  items: RfqSummaryResponse[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface RfqListQuery {
  page?: number;
  pageSize?: number;
  status?: string;
  rfqNumber?: string;
  title?: string;
}

export interface CreateRfqRoundRequest {
  deadline: string;
}

export interface RfqRoundResponse {
  roundId: number;
  rfqId: number;
  roundNumber: number;
  status: string;
  publishedAt?: string | null;
  deadline: string;
  closedAt?: string | null;
  createdAt: string;
}