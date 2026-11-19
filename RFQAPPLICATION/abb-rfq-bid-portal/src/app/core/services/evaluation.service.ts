import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  EvaluateRoundResponse,
  EvaluationDecision,
  EvaluationDecisionRequest,
  EvaluationResults
} from '../models/evaluation.model';

@Injectable({ providedIn: 'root' })
export class EvaluationService {
  private readonly http = inject(HttpClient);

  evaluate(rfqId: number, roundId: number): Observable<EvaluateRoundResponse> {
    return this.http.post<EvaluateRoundResponse>(
      `${environment.apiBaseUrl}/rfqs/${rfqId}/rounds/${roundId}/evaluate`,
      {}
    );
  }

  getResults(rfqId: number, roundId: number): Observable<EvaluationResults> {
    return this.http.get<EvaluationResults>(
      `${environment.apiBaseUrl}/rfqs/${rfqId}/rounds/${roundId}/evaluation`
    );
  }

  getDecision(rfqId: number, roundId: number): Observable<EvaluationDecision> {
    return this.http.get<EvaluationDecision>(
      `${environment.apiBaseUrl}/rfqs/${rfqId}/rounds/${roundId}/evaluation/decision`
    );
  }

  submitDecision(
    rfqId: number,
    roundId: number,
    request: EvaluationDecisionRequest
  ): Observable<EvaluationDecision> {
    return this.http.post<EvaluationDecision>(
      `${environment.apiBaseUrl}/rfqs/${rfqId}/rounds/${roundId}/evaluation/decision`,
      request
    );
  }
}