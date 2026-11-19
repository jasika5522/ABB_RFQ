import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  PagedSuppliers,
  SupplierDetail,
  SupplierListQuery,
  UpdateSupplierStatusRequest
} from '../models/supplier-management.model';

@Injectable({ providedIn: 'root' })
export class SupplierManagementService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/suppliers`;

  list(query: SupplierListQuery = {}): Observable<PagedSuppliers> {
    let params = new HttpParams();
    if (query.page) {
      params = params.set('page', String(query.page));
    }
    if (query.pageSize) {
      params = params.set('pageSize', String(query.pageSize));
    }
    if (query.approvalStatus?.trim()) {
      params = params.set('approvalStatus', query.approvalStatus.trim());
    }
    if (query.companyName?.trim()) {
      params = params.set('companyName', query.companyName.trim());
    }
    return this.http.get<PagedSuppliers>(this.baseUrl, { params });
  }

  getById(supplierId: number): Observable<SupplierDetail> {
    return this.http.get<SupplierDetail>(`${this.baseUrl}/${supplierId}`);
  }

  updateStatus(supplierId: number, request: UpdateSupplierStatusRequest): Observable<SupplierDetail> {
    return this.http.patch<SupplierDetail>(`${this.baseUrl}/${supplierId}/status`, request);
  }
}