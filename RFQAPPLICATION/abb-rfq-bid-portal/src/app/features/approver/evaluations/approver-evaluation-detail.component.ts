import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { catchError, finalize, forkJoin, of, throwError } from 'rxjs';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { EvaluationService } from '../../../core/services/evaluation.service';
import { AwardService } from '../../../core/services/award.service';
import { ProcurementRfqService } from '../../procurement/services/procurement-rfq.service';
import { AuthService } from '../../../core/services/auth.service';
import { EvaluationDecision, EvaluationResults, EvaluatedBid } from '../../../core/models/evaluation.model';
import { Award } from '../../../core/models/award.model';

@Component({
  selector: 'app-approver-evaluation-detail',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule, DatePipe, CurrencyPipe, PageHeaderComponent],
  templateUrl: './approver-evaluation-detail.component.html',
  styleUrl: './approver-evaluation-detail.component.css'
})
export class ApproverEvaluationDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  private readonly evaluation = inject(EvaluationService);
  private readonly awardService = inject(AwardService);
  private readonly rfqService = inject(ProcurementRfqService);
  private readonly auth = inject(AuthService);

  rfqId = 0;
  roundId = 0;
  rfqTitle = '';

  loading = false;
  loadError = '';
  results: EvaluationResults | null = null;
  decision: EvaluationDecision | null = null;
  existingAward: Award | null = null;

  submitting = false;
  decisionError = '';
  confirmDecision: 'Approved' | 'Rejected' | null = null;

  awarding = false;
  awardError = '';
  confirmAward = false;

  readonly rejectForm = this.fb.nonNullable.group({
    reason: ['', [Validators.required, Validators.pattern(/\S+/)]]
  });

  readonly awardForm = this.fb.nonNullable.group({
    awardComment: ['', [Validators.required, Validators.pattern(/\S+/)]]
  });

  ngOnInit(): void {
    this.rfqId = Number(this.route.snapshot.paramMap.get('rfqId'));
    this.roundId = Number(this.route.snapshot.paramMap.get('roundId'));
    if (!this.rfqId || !this.roundId) {
      this.loadError = 'Invalid evaluation reference.';
      return;
    }
    this.load();
  }

  get decisionRecorded(): boolean {
    return (
      this.decision?.status === 'Approved' ||
      this.decision?.status === 'Rejected' ||
      !!this.decision?.decision
    );
  }

  get canApprove(): boolean {
    return (
      !this.decisionRecorded &&
      this.results !== null &&
      !this.results.summary.isLowestRankTied
    );
  }

  get canReject(): boolean {
    return !this.decisionRecorded && this.results !== null && this.results.summary.isLowestRankTied;
  }

  get winningBid(): EvaluatedBid | null {
    if (!this.results || this.results.summary.isLowestRankTied) {
      return null;
    }
    const lowest = this.results.summary.lowestRank;
    const bids = this.results.bids.filter((b) => b.rankPosition === lowest);
    return bids.length === 1 ? bids[0] : null;
  }

  get evaluationPending(): boolean {
    return !this.loading && !this.loadError && this.results === null;
  }

  get canAward(): boolean {
    return (
      this.decision?.status === 'Approved' &&
      !!this.winningBid &&
      !this.existingAward
    );
  }

  load(): void {
    this.loading = true;
    this.loadError = '';

    this.rfqService.getById(this.rfqId).subscribe({
      next: (rfq) => (this.rfqTitle = rfq.title)
    });

    forkJoin({
      results: this.evaluation.getResults(this.rfqId, this.roundId).pipe(
        catchError((error: HttpErrorResponse) =>
          error.status === 404 ? of(null) : throwError(() => error)
        )
      ),
      decision: this.evaluation.getDecision(this.rfqId, this.roundId).pipe(
        catchError((error: HttpErrorResponse) =>
          error.status === 404 || error.status === 400 ? of(null) : throwError(() => error)
        )
      ),
      award: this.awardService.getByRfq(this.rfqId).pipe(
        catchError((error: HttpErrorResponse) => (error.status === 404 ? of(null) : throwError(() => error)))
      )
    })
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: ({ results, decision, award }) => {
          this.results = results;
          this.decision = decision;
          this.existingAward = award;
        },
        error: (error: HttpErrorResponse) => this.handleError(error)
      });
  }

  submitDecision(decision: 'Approved' | 'Rejected'): void {
    if (decision === 'Rejected') {
      if (this.rejectForm.invalid) {
        this.rejectForm.markAllAsTouched();
        return;
      }
    }

    this.submitting = true;
    this.decisionError = '';
    this.confirmDecision = null;

    const body =
      decision === 'Approved'
        ? { decision: 'Approved' }
        : { decision: 'Rejected', reason: this.rejectForm.controls.reason.value.trim() };

    this.evaluation
      .submitDecision(this.rfqId, this.roundId, body)
      .pipe(finalize(() => (this.submitting = false)))
      .subscribe({
        next: (response) => {
          this.decision = response;
          this.load();
        },
        error: (error: HttpErrorResponse) => {
          this.decisionError = error.error?.message ?? 'Unable to record decision.';
          if (error.status === 401) {
            this.auth.logout();
          }
        }
      });
  }

  createAward(): void {
    const winning = this.winningBid;
    if (!winning || this.awardForm.invalid) {
      this.awardForm.markAllAsTouched();
      return;
    }

    this.awarding = true;
    this.awardError = '';
    this.confirmAward = false;

    this.awardService
      .create(this.rfqId, this.roundId, {
        bidId: winning.bidId,
        awardComment: this.awardForm.controls.awardComment.value.trim()
      })
      .pipe(finalize(() => (this.awarding = false)))
      .subscribe({
        next: (award) => {
          this.existingAward = award;
          this.load();
        },
        error: (error: HttpErrorResponse) => {
          this.awardError = error.error?.message ?? 'Award failed.';
          if (error.status === 401) {
            this.auth.logout();
          }
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