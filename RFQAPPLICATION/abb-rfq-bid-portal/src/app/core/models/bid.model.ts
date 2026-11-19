export interface SubmitBidItemRequest {
  rfqItemId: number;
  unitPrice: number;
}

export interface SubmitBidRequest {
  items: SubmitBidItemRequest[];
}

export interface BidItemResponse {
  bidItemId: number;
  rfqItemId: number;
  itemName: string;
  rfqQuantity: number;
  unit: string;
  unitPrice: number;
}

export interface BidResponse {
  bidId: number;
  rfqId: number;
  roundId: number;
  supplierId: number;
  status: string;
  submittedAt: string;
  withdrawnAt?: string | null;
  currencyCode?: string | null;
  items: BidItemResponse[];
}

export interface BidSummaryResponse {
  bidId: number;
  rfqId: number;
  roundId: number;
  rfqNumber: string;
  rfqTitle: string;
  currencyCode: string;
  roundNumber: number;
  status: string;
  submittedAt: string;
  withdrawnAt?: string | null;
}

export interface PagedBidsResponse {
  items: BidSummaryResponse[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface BidDetailResponse {
  bidId: number;
  rfqId: number;
  rfqNumber: string;
  rfqTitle: string;
  currencyCode: string;
  roundId: number;
  roundNumber: number;
  roundStatus: string;
  roundDeadline: string;
  status: string;
  submittedAt: string;
  withdrawnAt?: string | null;
  items: BidItemResponse[];
}

export interface BidQuery {
  page?: number;
  pageSize?: number;
  rfqId?: number;
  status?: string;
}