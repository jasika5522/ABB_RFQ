import { Injectable, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin, from, Observable, of } from 'rxjs';
import { catchError, concatMap, map, mergeMap, toArray } from 'rxjs/operators';
import { EvaluationListRow } from '../models/evaluation.model';
import { ProcurementRfqService } from '../../features/procurement/services/procurement-rfq.service';
import { ProcurementRoundService } from '../../features/procurement/services/procurement-round.service';
import { EvaluationService } from './evaluation.service';

@Injectable({ providedIn: 'root' })
export class EvaluationCatalogService {
  private readonly rfqService = inject(ProcurementRfqService);
  private readonly roundService = inject(ProcurementRoundService);
  private readonly evaluationService = inject(EvaluationService);

  /**
   * Build listing rows from RFQ + round status only.
   * Does not call GET evaluation (avoids 404 noise for unevaluated closed rounds).
   */
  loadForProcurement(): Observable<EvaluationListRow[]> {
    return forkJoin([
      this.rfqService.getAll({ page: 1, pageSize: 100, status: 'Closed' }),
      this.rfqService.getAll({ page: 1, pageSize: 100, status: 'Evaluated' }),
      this.rfqService.getAll({ page: 1, pageSize: 100, status: 'Awarded' })
    ]).pipe(
      map(([closed, evaluated, awarded]) => {
        const mapById = new Map<number, (typeof closed.items)[0]>();
        for (const r of [...closed.items, ...evaluated.items, ...awarded.items]) {
          mapById.set(r.rfqId, r);
        }
        return [...mapById.values()];
      }),
      mergeMap((rfqs) =>
        rfqs.length === 0
          ? of([] as EvaluationListRow[])
          : from(rfqs).pipe(
              concatMap((rfq) =>
                this.roundService.list(rfq.rfqId).pipe(
                  map((rounds) =>
                    rounds
                      .filter((round) => round.status === 'Closed' || round.status === 'Evaluated')
                      .map((round) => this.toProcurementRow(rfq, round))
                  )
                )
              ),
              toArray(),
              map((groups) => groups.flat())
            )
      ),
      map((rows) => this.sortRows(rows))
    );
  }

  /** Approver: only rounds with completed evaluation (round status Evaluated/Awarded). */
  loadForApprover(): Observable<EvaluationListRow[]> {
    return forkJoin([
      this.rfqService.getAll({ page: 1, pageSize: 100, status: 'Evaluated' }),
      this.rfqService.getAll({ page: 1, pageSize: 100, status: 'Awarded' })
    ]).pipe(
      map(([evaluated, awarded]) => {
        const mapById = new Map<number, (typeof evaluated.items)[0]>();
        for (const r of [...evaluated.items, ...awarded.items]) {
          mapById.set(r.rfqId, r);
        }
        return [...mapById.values()];
      }),
      mergeMap((rfqs) =>
        rfqs.length === 0
          ? of([] as EvaluationListRow[])
          : from(rfqs).pipe(
              concatMap((rfq) =>
                this.roundService.list(rfq.rfqId).pipe(
                  map((rounds) =>
                    rounds
                      .filter((round) => round.status === 'Evaluated' || round.status === 'Awarded')
                      .map((round) => this.toApproverRow(rfq, round))
                  ),
                  mergeMap((rows) => from(rows)),
                  concatMap((row) =>
                    this.evaluationService.getDecision(row.rfqId, row.roundId).pipe(
                      map((decision) => ({
                        ...row,
                        decisionStatus: decision.status ?? 'Pending',
                        isLowestRankTied: decision.isLowestRankTied ?? null
                      })),
                      catchError((error: HttpErrorResponse) => {
                        if (error.status === 400 || error.status === 404) {
                          return of(row);
                        }
                        throw error;
                      })
                    )
                  )
                )
              ),
              toArray()
            )
      ),
      map((rows) => this.sortRows(rows))
    );
  }

  private toProcurementRow(
    rfq: { rfqId: number; rfqNumber: string; title: string; status: string },
    round: {
      roundId: number;
      roundNumber: number;
      status: string;
      deadline: string | null;
    }
  ): EvaluationListRow {
    const hasEvaluation = round.status === 'Evaluated' || round.status === 'Awarded';
    return {
      rfqId: rfq.rfqId,
      rfqNumber: rfq.rfqNumber,
      rfqTitle: rfq.title,
      rfqStatus: rfq.status,
      roundId: round.roundId,
      roundNumber: round.roundNumber,
      roundStatus: round.status,
      deadline: round.deadline,
      hasEvaluation
    };
  }

  private toApproverRow(
    rfq: { rfqId: number; rfqNumber: string; title: string; status: string },
    round: {
      roundId: number;
      roundNumber: number;
      status: string;
      deadline: string | null;
    }
  ): EvaluationListRow {
    return {
      rfqId: rfq.rfqId,
      rfqNumber: rfq.rfqNumber,
      rfqTitle: rfq.title,
      rfqStatus: rfq.status,
      roundId: round.roundId,
      roundNumber: round.roundNumber,
      roundStatus: round.status,
      deadline: round.deadline,
      hasEvaluation: true,
      decisionStatus: 'Pending'
    };
  }

  private sortRows(rows: EvaluationListRow[]): EvaluationListRow[] {
    return [...rows].sort((a, b) => {
      const n = a.rfqNumber.localeCompare(b.rfqNumber);
      return n !== 0 ? n : a.roundNumber - b.roundNumber;
    });
  }
}