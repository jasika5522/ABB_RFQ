export interface RfqItemResponse {
  rfqItemId: number;
  rfqId: number;
  itemName: string;
  description?: string | null;
  quantity: number;
  unit: string;
  createdAt: string;
}

export interface SupplierAvailableRfqSummary {
  rfqId: number;
  rfqNumber: string;
  title: string;
  description?: string | null;
  currencyCode: string;
  publishedRoundId: number;
  roundNumber: number;
  publishedAt?: string | null;
  deadline: string;
  mandatoryItemCount: number;
}

export interface PagedSupplierAvailableRfqs {
  items: SupplierAvailableRfqSummary[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface SupplierRfqEligibility {
  approvalStatus: string;
  canSubmitBid: boolean;
  message?: string | null;
}

export interface SupplierAvailableRfqDetail {
  rfqId: number;
  rfqNumber: string;
  title: string;
  description?: string | null;
  currencyCode: string;
  publishedRoundId: number;
  roundNumber: number;
  publishedAt?: string | null;
  deadline: string;
  items: RfqItemResponse[];
  eligibility: SupplierRfqEligibility;
}

export interface SupplierRfqQuery {
  page?: number;
  pageSize?: number;
  rfqNumber?: string;
  title?: string;
  search?: string;
}