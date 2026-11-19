import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  PagedSupplierAvailableRfqs,
  SupplierAvailableRfqDetail,
  SupplierRfqQuery
} from '../../../core/models/supplier-rfq.model';

@Injectable({ providedIn: 'root' })
export class SupplierRfqService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/supplier/rfqs`;

  getAvailable(query: SupplierRfqQuery = {}): Observable<PagedSupplierAvailableRfqs> {
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
    if (query.title?.trim()) {
      params = params.set('title', query.title.trim());
    }
    if (query.search?.trim()) {
      params = params.set('search', query.search.trim());
    }
    return this.http.get<PagedSupplierAvailableRfqs>(this.baseUrl, { params });
  }

  getById(rfqId: number): Observable<SupplierAvailableRfqDetail> {
    return this.http.get<SupplierAvailableRfqDetail>(`${this.baseUrl}/${rfqId}`);
  }
}