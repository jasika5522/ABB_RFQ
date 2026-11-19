import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { AwardService } from '../../../core/services/award.service';
import { Award } from '../../../core/models/award.model';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-approver-award-list',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    DatePipe,
    CurrencyPipe,
    PageHeaderComponent,
    EmptyStateComponent
  ],
  templateUrl: './approver-award-list.component.html',
  styleUrl: './approver-award-list.component.css'
})
export class ApproverAwardListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly awardService = inject(AwardService);
  private readonly auth = inject(AuthService);

  loading = false;
  loadError = '';
  items: Award[] = [];
  page = 1;
  pageSize = 10;
  totalCount = 0;

  readonly filters = this.fb.nonNullable.group({
    rfqNumber: [''],
    companyName: ['']
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

    this.awardService
      .list({
        page: this.page,
        pageSize: this.pageSize,
        rfqNumber: raw.rfqNumber || undefined,
        companyName: raw.companyName || undefined
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
    this.filters.reset({ rfqNumber: '', companyName: '' });
    this.loadPage(1);
  }

  private handleError(error: HttpErrorResponse): void {
    if (error.status === 401) {
      this.auth.logout();
      return;
    }
    this.loadError = error.error?.message ?? 'Unable to load awards.';
  }
}