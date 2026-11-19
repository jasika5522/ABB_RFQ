export interface Award {
  awardId: number;
  rfqId: number;
  rfqNumber: string;
  rfqTitle: string;
  currencyCode: string;
  roundId: number;
  roundNumber: number;
  bidId: number;
  supplierId: number;
  companyName: string;
  registrationNumber: string;
  awardedBidTotalPrice: number;
  awardedBy: number;
  awardComment: string;
  awardedAt: string;
}

export interface PagedAwards {
  items: Award[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface CreateAwardRequest {
  bidId: number;
  awardComment: string;
}

export interface AwardListQuery {
  page?: number;
  pageSize?: number;
  rfqNumber?: string;
  companyName?: string;
}