import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { SupplierRfqService } from '../services/supplier-rfq.service';
import { BidService } from '../services/bid.service';
import { AuthService } from '../../../core/services/auth.service';
import { SupplierAvailableRfqDetail } from '../../../core/models/supplier-rfq.model';
import { BidSummaryResponse } from '../../../core/models/bid.model';

@Component({
  selector: 'app-supplier-rfq-detail',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule, DatePipe, DecimalPipe, PageHeaderComponent],
  templateUrl: './supplier-rfq-detail.component.html',
  styleUrl: './supplier-rfq-detail.component.css'
})
export class SupplierRfqDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly supplierRfqService = inject(SupplierRfqService);
  private readonly bidService = inject(BidService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  loading = false;
  loadError = '';
  rfq: SupplierAvailableRfqDetail | null = null;
  activeBid: BidSummaryResponse | null = null;

  showBidForm = false;
  submitting = false;
  submitError = '';
  submitSuccess = '';

  readonly bidForm = this.fb.group({});

  ngOnInit(): void {
    const rfqId = Number(this.route.snapshot.paramMap.get('rfqId'));
    if (!rfqId) {
      this.loadError = 'Invalid RFQ identifier.';
      return;
    }
    this.loadRfq(rfqId);
  }

  get canOpenBidForm(): boolean {
    return !!(
      this.rfq?.eligibility.canSubmitBid &&
      !this.activeBid &&
      !this.showBidForm
    );
  }

  get bidTotal(): number {
    if (!this.rfq) {
      return 0;
    }
    return this.rfq.items.reduce((sum, item) => {
      const control = this.bidForm.get(String(item.rfqItemId));
      const price = Number(control?.value ?? 0);
      return sum + item.quantity * price;
    }, 0);
  }

  lineTotal(rfqItemId: number, quantity: number): number {
    const price = Number(this.bidForm.get(String(rfqItemId))?.value ?? 0);
    return quantity * price;
  }

  loadRfq(rfqId: number): void {
    this.loading = true;
    this.loadError = '';
    this.submitSuccess = '';
    this.showBidForm = false;

    this.supplierRfqService
      .getById(rfqId)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (rfq) => {
          this.rfq = rfq;
          this.buildBidForm(rfq);
          this.bidService.getActiveSubmittedBid(rfq.rfqId, rfq.publishedRoundId).subscribe({
            next: (bid) => (this.activeBid = bid)
          });
        },
        error: (error: HttpErrorResponse) => this.handleError(error)
      });
  }

  openBidForm(): void {
    if (!this.rfq?.eligibility.canSubmitBid || this.activeBid) {
      return;
    }
    this.submitError = '';
    this.submitSuccess = '';
    this.showBidForm = true;
    this.buildBidForm(this.rfq);
  }

  cancelBidForm(): void {
    if (this.rfq) {
      this.buildBidForm(this.rfq);
    }
    this.showBidForm = false;
    this.submitError = '';
  }

  submitBid(): void {
    if (!this.rfq || this.bidForm.invalid) {
      this.bidForm.markAllAsTouched();
      return;
    }

    const items = this.rfq.items.map((item) => ({
      rfqItemId: item.rfqItemId,
      unitPrice: Number(this.bidForm.get(String(item.rfqItemId))?.value)
    }));

    this.submitting = true;
    this.submitError = '';
    this.submitSuccess = '';

    this.bidService
      .submit(this.rfq.rfqId, this.rfq.publishedRoundId, { items })
      .pipe(finalize(() => (this.submitting = false)))
      .subscribe({
        next: (response) => {
          this.submitSuccess = `Bid #${response.bidId} submitted successfully.`;
          this.showBidForm = false;
          this.bidService
            .getActiveSubmittedBid(this.rfq!.rfqId, this.rfq!.publishedRoundId)
            .subscribe((bid) => (this.activeBid = bid));
        },
        error: (error: HttpErrorResponse) => {
          this.submitError = this.resolveSubmitError(error);
          if (error.status === 401) {
            this.auth.logout();
          }
        }
      });
  }

  eligibilityClass(): string {
    const status = this.rfq?.eligibility.approvalStatus;
    if (status === 'Approved' && this.rfq?.eligibility.canSubmitBid) {
      return 'notice notice-ok';
    }
    if (status === 'Suspended') {
      return 'notice notice-error';
    }
    return 'notice notice-warn';
  }

  private buildBidForm(rfq: SupplierAvailableRfqDetail): void {
    const group: Record<string, ReturnType<FormBuilder['control']>> = {};
    for (const item of rfq.items) {
      group[String(item.rfqItemId)] = this.fb.control('', [
        Validators.required,
        Validators.min(0.01)
      ]);
    }
    Object.keys(this.bidForm.controls).forEach((key) => this.bidForm.removeControl(key));
    for (const [key, control] of Object.entries(group)) {
      this.bidForm.addControl(key, control);
    }
  }

  private handleError(error: HttpErrorResponse): void {
    if (error.status === 401) {
      this.auth.logout();
      return;
    }
    this.loadError =
      error.error?.message ??
      (error.status === 403
        ? 'Access denied.'
        : error.status === 404
          ? 'RFQ not found or not available for bidding.'
          : 'Unable to load RFQ details.');
  }

  private resolveSubmitError(error: HttpErrorResponse): string {
    return (
      error.error?.message ??
      (error.status === 403
        ? 'You are not allowed to submit a bid.'
        : error.status === 409
          ? 'An active bid already exists for this round.'
          : error.status === 400
            ? 'Validation failed. Check unit prices for every item.'
            : 'Unable to submit bid.')
    );
  }
}