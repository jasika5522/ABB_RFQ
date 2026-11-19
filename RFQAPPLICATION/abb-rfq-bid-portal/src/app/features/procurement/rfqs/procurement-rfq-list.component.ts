import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { ProcurementRfqService } from '../services/procurement-rfq.service';
import { AuthService } from '../../../core/services/auth.service';
import { RfqSummaryResponse } from '../../../core/models/procurement-rfq.model';

const RFQ_STATUSES = ['Draft', 'Published', 'Closed', 'Evaluated', 'Awarded'];

@Component({
  selector: 'app-procurement-rfq-list',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, DatePipe, PageHeaderComponent, EmptyStateComponent],
  templateUrl: './procurement-rfq-list.component.html',
  styleUrl: './procurement-rfq-list.component.css'
})
export class ProcurementRfqListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly rfqService = inject(ProcurementRfqService);
  private readonly auth = inject(AuthService);

  loading = false;
  loadError = '';
  items: RfqSummaryResponse[] = [];
  page = 1;
  pageSize = 10;
  totalCount = 0;
  readonly statuses = RFQ_STATUSES;

  readonly filters = this.fb.nonNullable.group({
    rfqNumber: [''],
    title: [''],
    status: ['']
  });

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
    const raw = this.filters.getRawValue();

    this.rfqService
      .getAll({
        page: this.page,
        pageSize: this.pageSize,
        rfqNumber: raw.rfqNumber || undefined,
        title: raw.title || undefined,
        status: raw.status || undefined
      })
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

  applyFilters(): void {
    this.loadPage(1);
  }

  clearFilters(): void {
    this.filters.reset({ rfqNumber: '', title: '', status: '' });
    this.loadPage(1);
  }

  statusClass(status: string): string {
    return `status-pill status-${status.toLowerCase()}`;
  }

  private handleError(error: HttpErrorResponse): void {
    if (error.status === 401) {
      this.auth.logout();
      return;
    }
    this.loadError = error.error?.message ?? 'Unable to load RFQs.';
  }
}