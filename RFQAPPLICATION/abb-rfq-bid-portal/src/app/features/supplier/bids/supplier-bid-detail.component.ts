import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { BidService } from '../services/bid.service';
import { AuthService } from '../../../core/services/auth.service';
import { BidDetailResponse } from '../../../core/models/bid.model';

@Component({
  selector: 'app-supplier-bid-detail',
  standalone: true,
  imports: [RouterLink, DatePipe, DecimalPipe, PageHeaderComponent],
  templateUrl: './supplier-bid-detail.component.html',
  styleUrl: './supplier-bid-detail.component.css'
})
export class SupplierBidDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly bidService = inject(BidService);
  private readonly auth = inject(AuthService);

  loading = false;
  withdrawing = false;
  loadError = '';
  actionError = '';
  successMessage = '';
  bid: BidDetailResponse | null = null;
  confirmWithdraw = false;

  ngOnInit(): void {
    const bidId = Number(this.route.snapshot.paramMap.get('bidId'));
    if (!bidId) {
      this.loadError = 'Invalid bid identifier.';
      return;
    }
    this.loadBid(bidId);
  }

  get bidTotal(): number {
    if (!this.bid) {
      return 0;
    }
    return this.bid.items.reduce((sum, line) => sum + line.rfqQuantity * line.unitPrice, 0);
  }

  get canWithdraw(): boolean {
    if (!this.bid || this.bid.status !== 'Submitted') {
      return false;
    }
    return new Date(this.bid.roundDeadline) > new Date();
  }

  loadBid(bidId: number): void {
    this.loading = true;
    this.loadError = '';
    this.bidService
      .getById(bidId)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (bid) => (this.bid = bid),
        error: (error: HttpErrorResponse) => this.handleError(error)
      });
  }

  requestWithdraw(): void {
    this.confirmWithdraw = true;
    this.actionError = '';
  }

  cancelWithdraw(): void {
    this.confirmWithdraw = false;
  }

  withdraw(): void {
    if (!this.bid) {
      return;
    }
    this.withdrawing = true;
    this.actionError = '';
    this.successMessage = '';

    this.bidService
      .withdraw(this.bid.bidId)
      .pipe(finalize(() => (this.withdrawing = false)))
      .subscribe({
        next: () => {
          this.successMessage = 'Bid withdrawn successfully. You may submit a new bid while the round is open.';
          this.confirmWithdraw = false;
          this.loadBid(this.bid!.bidId);
        },
        error: (error: HttpErrorResponse) => {
          this.actionError = this.resolveError(error);
          if (error.status === 401) {
            this.auth.logout();
          }
        }
      });
  }

  private handleError(error: HttpErrorResponse): void {
    if (error.status === 401) {
      this.auth.logout();
      return;
    }
    this.loadError =
      error.error?.message ??
      (error.status === 404 ? 'Bid not found.' : error.status === 403 ? 'Access denied.' : 'Unable to load bid.');
  }

  private resolveError(error: HttpErrorResponse): string {
    return (
      error.error?.message ??
      (error.status === 409
        ? 'This action conflicts with the current bid state.'
        : error.status === 400
          ? 'Withdrawal is not allowed.'
          : 'Unable to withdraw bid.')
    );
  }
}