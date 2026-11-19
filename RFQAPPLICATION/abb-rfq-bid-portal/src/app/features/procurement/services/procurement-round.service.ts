import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { CreateRfqRoundRequest, RfqRoundResponse } from '../../../core/models/procurement-rfq.model';

@Injectable({ providedIn: 'root' })
export class ProcurementRoundService {
  private readonly http = inject(HttpClient);

  private roundsUrl(rfqId: number): string {
    return `${environment.apiBaseUrl}/rfqs/${rfqId}/rounds`;
  }

  list(rfqId: number): Observable<RfqRoundResponse[]> {
    return this.http.get<RfqRoundResponse[]>(this.roundsUrl(rfqId));
  }

  getById(rfqId: number, roundId: number): Observable<RfqRoundResponse> {
    return this.http.get<RfqRoundResponse>(`${this.roundsUrl(rfqId)}/${roundId}`);
  }

  create(rfqId: number, request: CreateRfqRoundRequest): Observable<RfqRoundResponse> {
    return this.http.post<RfqRoundResponse>(this.roundsUrl(rfqId), request);
  }

  publish(rfqId: number, roundId: number): Observable<RfqRoundResponse> {
    return this.http.patch<RfqRoundResponse>(`${this.roundsUrl(rfqId)}/${roundId}/publish`, {});
  }

  close(rfqId: number, roundId: number): Observable<RfqRoundResponse> {
    return this.http.patch<RfqRoundResponse>(`${this.roundsUrl(rfqId)}/${roundId}/close`, {});
  }
}