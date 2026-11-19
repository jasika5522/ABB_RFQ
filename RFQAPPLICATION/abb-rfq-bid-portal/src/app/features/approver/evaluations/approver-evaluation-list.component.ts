import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { EvaluationCatalogService } from '../../../core/services/evaluation-catalog.service';
import { EvaluationListRow } from '../../../core/models/evaluation.model';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-approver-evaluation-list',
  standalone: true,
  imports: [RouterLink, PageHeaderComponent, EmptyStateComponent],
  templateUrl: './approver-evaluation-list.component.html',
  styleUrl: './approver-evaluation-list.component.css'
})
export class ApproverEvaluationListComponent implements OnInit {
  private readonly catalog = inject(EvaluationCatalogService);
  private readonly auth = inject(AuthService);

  loading = false;
  loadError = '';
  items: EvaluationListRow[] = [];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.loadError = '';
    this.catalog
      .loadForApprover()
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (items) => (this.items = items),
        error: (error: HttpErrorResponse) => this.handleError(error)
      });
  }

  decisionLabel(row: EvaluationListRow): string {
    return row.decisionStatus ?? 'Pending';
  }

  private handleError(error: HttpErrorResponse): void {
    if (error.status === 401) {
      this.auth.logout();
      return;
    }
    this.loadError = error.error?.message ?? 'Unable to load evaluations.';
  }
}