export interface EvaluationSummary {
  rfqId: number;
  rfqNumber: string;
  roundId: number;
  roundNumber: number;
  currencyCode: string;
  evaluatedAt: string | null;
  lowestTotalPrice: number;
  lowestRank: number;
  isLowestRankTied: boolean;
  lowestRankSupplierCount: number;
}

export interface EvaluatedBid {
  bidId: number;
  supplierId: number;
  companyName: string;
  registrationNumber: string;
  totalPrice: number;
  currencyCode: string;
  rankPosition: number;
  evaluatedAt: string;
}

export interface EvaluationResults {
  summary: EvaluationSummary;
  bids: EvaluatedBid[];
}

export interface EvaluateRoundResponse {
  rfqId: number;
  rfqNumber: string;
  roundId: number;
  roundNumber: number;
  currencyCode: string;
  evaluatedAt: string;
  evaluatedBidCount: number;
  lowestTotalPrice: number;
  lowestRank: number;
  isLowestRankTied: boolean;
  lowestRankSupplierCount: number;
}

export interface EvaluationDecision {
  status: string;
  rfqId: number;
  roundId: number;
  decision?: string | null;
  reason?: string | null;
  approverUserId?: number | null;
  decidedAt?: string | null;
  lowestTotalPrice?: number | null;
  lowestRank?: number | null;
  lowestRankSupplierCount?: number | null;
  isLowestRankTied?: boolean | null;
}

export interface EvaluationDecisionRequest {
  decision: string;
  reason?: string | null;
}

export interface EvaluationListRow {
  rfqId: number;
  rfqNumber: string;
  rfqTitle: string;
  rfqStatus: string;
  roundId: number;
  roundNumber: number;
  roundStatus: string;
  deadline: string | null;
  hasEvaluation: boolean;
  decisionStatus?: string | null;
  isLowestRankTied?: boolean | null;
}