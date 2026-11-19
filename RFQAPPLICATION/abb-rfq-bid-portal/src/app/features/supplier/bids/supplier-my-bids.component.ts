import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { BidService } from '../services/bid.service';
import { AuthService } from '../../../core/services/auth.service';
import { BidSummaryResponse } from '../../../core/models/bid.model';

@Component({
  selector: 'app-supplier-my-bids',
  standalone: true,
  imports: [RouterLink, DatePipe, PageHeaderComponent, EmptyStateComponent],
  templateUrl: './supplier-my-bids.component.html',
  styleUrl: './supplier-my-bids.component.css'
})
export class SupplierMyBidsComponent implements OnInit {
  private readonly bidService = inject(BidService);
  private readonly auth = inject(AuthService);

  loading = false;
  loadError = '';
  items: BidSummaryResponse[] = [];
  page = 1;
  pageSize = 10;
  totalCount = 0;

  ngOnInit(): void {
    this.loadPage(1);
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  loadPage(page: number): void {
    this.page = page;
    this.loading = true;
    this.loadError = '';

    this.bidService
      .getMyBids({ page, pageSize: this.pageSize })
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (result) => {
          this.items = result.items;
          this.totalCount = result.totalCount;
          this.page = result.page;
        },
        error: (error: HttpErrorResponse) => this.handleError(error)
      });
  }

  statusClass(status: string): string {
    switch (status) {
      case 'Submitted':
        return 'pill pill-submitted';
      case 'Withdrawn':
        return 'pill pill-withdrawn';
      case 'Accepted':
        return 'pill pill-accepted';
      case 'Rejected':
        return 'pill pill-rejected';
      default:
        return 'pill';
    }
  }

  prevPage(): void {
    if (this.page > 1) {
      this.loadPage(this.page - 1);
    }
  }

  nextPage(): void {
    if (this.page < this.totalPages) {
      this.loadPage(this.page + 1);
    }
  }

  private handleError(error: HttpErrorResponse): void {
    if (error.status === 401) {
      this.auth.logout();
      return;
    }
    this.loadError = error.error?.message ?? 'Unable to load your bids.';
  }
}