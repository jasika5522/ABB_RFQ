import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Award, AwardListQuery, CreateAwardRequest, PagedAwards } from '../models/award.model';

@Injectable({ providedIn: 'root' })
export class AwardService {
  private readonly http = inject(HttpClient);

  list(query: AwardListQuery = {}): Observable<PagedAwards> {
    let params = new HttpParams();
    if (query.page) {
      params = params.set('page', String(query.page));
    }
    if (query.pageSize) {
      params = params.set('pageSize', String(query.pageSize));
    }
    if (query.rfqNumber?.trim()) {
      params = params.set('rfqNumber', query.rfqNumber.trim());
    }
    if (query.companyName?.trim()) {
      params = params.set('companyName', query.companyName.trim());
    }
    return this.http.get<PagedAwards>(`${environment.apiBaseUrl}/awards`, { params });
  }

  getByRfq(rfqId: number): Observable<Award> {
    return this.http.get<Award>(`${environment.apiBaseUrl}/rfqs/${rfqId}/award`);
  }

  create(rfqId: number, roundId: number, request: CreateAwardRequest): Observable<Award> {
    return this.http.post<Award>(
      `${environment.apiBaseUrl}/rfqs/${rfqId}/rounds/${roundId}/award`,
      request
    );
  }
}