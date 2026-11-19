import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  BidDetailResponse,
  BidQuery,
  BidResponse,
  BidSummaryResponse,
  PagedBidsResponse,
  SubmitBidRequest
} from '../../../core/models/bid.model';

@Injectable({ providedIn: 'root' })
export class BidService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/bids`;

  getMyBids(query: BidQuery = {}): Observable<PagedBidsResponse> {
    let params = new HttpParams();
    if (query.page) {
      params = params.set('page', String(query.page));
    }
    if (query.pageSize) {
      params = params.set('pageSize', String(query.pageSize));
    }
    if (query.rfqId) {
      params = params.set('rfqId', String(query.rfqId));
    }
    if (query.status) {
      params = params.set('status', query.status);
    }
    return this.http.get<PagedBidsResponse>(`${this.baseUrl}/me`, { params });
  }

  getById(bidId: number): Observable<BidDetailResponse> {
    return this.http.get<BidDetailResponse>(`${this.baseUrl}/${bidId}`);
  }

  submit(rfqId: number, roundId: number, request: SubmitBidRequest): Observable<BidResponse> {
    return this.http.post<BidResponse>(
      `${environment.apiBaseUrl}/rfqs/${rfqId}/rounds/${roundId}/bids`,
      request
    );
  }

  withdraw(bidId: number): Observable<BidResponse> {
    return this.http.patch<BidResponse>(`${this.baseUrl}/${bidId}/withdraw`, {});
  }

  getActiveSubmittedBid(rfqId: number, roundId: number): Observable<BidSummaryResponse | null> {
    return this.getMyBids({ rfqId, status: 'Submitted', page: 1, pageSize: 50 }).pipe(
      map((page) => page.items.find((b) => b.roundId === roundId) ?? null)
    );
  }
}