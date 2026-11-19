import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  CreateRfqRequest,
  PagedRfqsResponse,
  RfqDetailResponse,
  RfqListQuery,
  UpdateRfqRequest
} from '../../../core/models/procurement-rfq.model';

@Injectable({ providedIn: 'root' })
export class ProcurementRfqService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/rfqs`;

  getAll(query: RfqListQuery = {}): Observable<PagedRfqsResponse> {
    let params = new HttpParams();
    if (query.page) {
      params = params.set('page', String(query.page));
    }
    if (query.pageSize) {
      params = params.set('pageSize', String(query.pageSize));
    }
    if (query.status?.trim()) {
      params = params.set('status', query.status.trim());
    }
    if (query.rfqNumber?.trim()) {
      params = params.set('rfqNumber', query.rfqNumber.trim());
    }
    if (query.title?.trim()) {
      params = params.set('title', query.title.trim());
    }
    return this.http.get<PagedRfqsResponse>(this.baseUrl, { params });
  }

  getById(rfqId: number): Observable<RfqDetailResponse> {
    return this.http.get<RfqDetailResponse>(`${this.baseUrl}/${rfqId}`);
  }

  create(request: CreateRfqRequest): Observable<RfqDetailResponse> {
    return this.http.post<RfqDetailResponse>(this.baseUrl, request);
  }

  update(rfqId: number, request: UpdateRfqRequest): Observable<RfqDetailResponse> {
    return this.http.put<RfqDetailResponse>(`${this.baseUrl}/${rfqId}`, request);
  }
}