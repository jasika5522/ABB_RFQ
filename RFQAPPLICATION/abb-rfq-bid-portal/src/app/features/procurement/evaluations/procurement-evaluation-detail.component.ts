import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { catchError, finalize, forkJoin, of, switchMap, throwError } from 'rxjs';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { EvaluationService } from '../../../core/services/evaluation.service';
import { ProcurementRfqService } from '../services/procurement-rfq.service';
import { ProcurementRoundService } from '../services/procurement-round.service';
import { AuthService } from '../../../core/services/auth.service';
import { EvaluationResults } from '../../../core/models/evaluation.model';
import { RfqRoundResponse } from '../../../core/models/procurement-rfq.model';

@Component({
  selector: 'app-procurement-evaluation-detail',
  standalone: true,
  imports: [RouterLink, DatePipe, CurrencyPipe, PageHeaderComponent],
  templateUrl: './procurement-evaluation-detail.component.html',
  styleUrl: './procurement-evaluation-detail.component.css'
})
export class ProcurementEvaluationDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly evaluation = inject(EvaluationService);
  private readonly rfqService = inject(ProcurementRfqService);
  private readonly roundService = inject(ProcurementRoundService);
  private readonly auth = inject(AuthService);

  rfqId = 0;
  roundId = 0;
  rfqTitle = '';
  rfqNumber = '';
  round: RfqRoundResponse | null = null;

  loading = false;
  loadError = '';
  results: EvaluationResults | null = null;

  evaluating = false;
  evaluateError = '';
  confirmEvaluate = false;

  ngOnInit(): void {
    this.rfqId = Number(this.route.snapshot.paramMap.get('rfqId'));
    this.roundId = Number(this.route.snapshot.paramMap.get('roundId'));
    if (!this.rfqId || !this.roundId) {
      this.loadError = 'Invalid evaluation reference.';
      return;
    }
    this.load();
  }

  get canEvaluate(): boolean {
    return (
      !this.results && this.round?.status === 'Closed' && this.rfqStatus === 'Closed'
    );
  }

  get missingPersistedResults(): boolean {
    return (
      !this.results &&
      !this.canEvaluate &&
      !this.loading &&
      (this.round?.status === 'Evaluated' || this.round?.status === 'Awarded')
    );
  }

  rfqStatus = '';

  get tieMessage(): string {
    if (!this.results) {
      return '';
    }
    return this.results.summary.isLowestRankTied
      ? 'Multiple suppliers have the same lowest bid. Approver review is required before an award can be made.'
      : 'Evaluation has a unique lowest bid. Awaiting Approver decision.';
  }

  load(): void {
    this.loading = true;
    this.loadError = '';
    this.results = null;

    forkJoin({
      rfq: this.rfqService.getById(this.rfqId),
      rounds: this.roundService.list(this.rfqId),
      results: this.evaluation.getResults(this.rfqId, this.roundId).pipe(
        catchError((error: HttpErrorResponse) => {
          if (error.status === 404) {
            return of(null);
          }
          return throwError(() => error);
        })
      )
    })
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: ({ rfq, rounds, results }) => {
          this.rfqTitle = rfq.title;
          this.rfqNumber = rfq.rfqNumber;
          this.rfqStatus = rfq.status;
          this.round = rounds.find((r) => r.roundId === this.roundId) ?? null;
          this.results = results;
        },
        error: (error: HttpErrorResponse) => this.handleError(error)
      });
  }

  runEvaluate(): void {
    if (!this.canEvaluate) {
      return;
    }
    this.evaluating = true;
    this.evaluateError = '';
    this.confirmEvaluate = false;

    this.evaluation
      .evaluate(this.rfqId, this.roundId)
      .pipe(
        switchMap(() => this.evaluation.getResults(this.rfqId, this.roundId)),
        finalize(() => (this.evaluating = false))
      )
      .subscribe({
        next: (results) => {
          this.results = results;
          this.evaluateError = '';
          this.rfqStatus = 'Evaluated';
          if (this.round) {
            this.round = { ...this.round, status: 'Evaluated' };
          }
        },
        error: (error: HttpErrorResponse) => {
          if (error.status === 401) {
            this.auth.logout();
            return;
          }
          if (error.status === 409) {
            this.evaluation.getResults(this.rfqId, this.roundId).subscribe({
              next: (results) => {
                this.results = results;
                this.evaluateError = '';
              },
              error: () => {
                this.evaluateError =
                  error.error?.message ?? 'Evaluation exists but results could not be loaded.';
              }
            });
            return;
          }
          this.evaluateError = error.error?.message ?? 'Evaluation failed.';
        }
      });
  }

  lowestRankClass(rank: number): string {
    if (!this.results) {
      return '';
    }
    return rank === this.results.summary.lowestRank ? 'rank-lowest' : '';
  }

  private handleError(error: HttpErrorResponse): void {
    if (error.status === 401) {
      this.auth.logout();
      return;
    }
    this.loadError = error.error?.message ?? 'Unable to load evaluation.';
  }
}