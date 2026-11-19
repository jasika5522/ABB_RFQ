import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuditLog, AuditLogQuery, PagedAuditLogs } from '../models/audit-log.model';

@Injectable({ providedIn: 'root' })
export class AuditLogService {
  private readonly http = inject(HttpClient);

  list(query: AuditLogQuery = {}): Observable<PagedAuditLogs> {
    let params = new HttpParams();
    if (query.page) {
      params = params.set('page', String(query.page));
    }
    if (query.pageSize) {
      params = params.set('pageSize', String(query.pageSize));
    }
    if (query.action?.trim()) {
      params = params.set('action', query.action.trim());
    }
    if (query.entityType?.trim()) {
      params = params.set('entityType', query.entityType.trim());
    }
    if (query.entityId) {
      params = params.set('entityId', String(query.entityId));
    }
    if (query.userId) {
      params = params.set('userId', String(query.userId));
    }
    if (query.fromDate) {
      params = params.set('fromDate', query.fromDate);
    }
    if (query.toDate) {
      params = params.set('toDate', query.toDate);
    }
    return this.http.get<PagedAuditLogs>(`${environment.apiBaseUrl}/audit-logs`, { params });
  }

  getById(auditId: number): Observable<AuditLog> {
    return this.http.get<AuditLog>(`${environment.apiBaseUrl}/audit-logs/${auditId}`);
  }

  getByEntity(
    entityType: string,
    entityId: number,
    page = 1,
    pageSize = 10
  ): Observable<PagedAuditLogs> {
    const params = new HttpParams()
      .set('page', String(page))
      .set('pageSize', String(pageSize));
    return this.http.get<PagedAuditLogs>(
      `${environment.apiBaseUrl}/audit-logs/entity/${encodeURIComponent(entityType)}/${entityId}`,
      { params }
    );
  }
}