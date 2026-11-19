import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { SupplierResponse, UpdateSupplierProfileRequest } from '../../../core/models/supplier.model';

@Injectable({ providedIn: 'root' })
export class SupplierService {
  private readonly http = inject(HttpClient);

  getMyProfile(): Observable<SupplierResponse> {
    return this.http.get<SupplierResponse>(`${environment.apiBaseUrl}/suppliers/me`);
  }

  updateMyProfile(request: UpdateSupplierProfileRequest): Observable<SupplierResponse> {
    return this.http.put<SupplierResponse>(`${environment.apiBaseUrl}/suppliers/me`, request);
  }
}