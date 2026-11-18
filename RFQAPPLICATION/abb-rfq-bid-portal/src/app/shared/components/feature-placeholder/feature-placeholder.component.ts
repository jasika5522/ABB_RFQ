import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { EmptyStateComponent } from '../empty-state/empty-state.component';
import { PageHeaderComponent } from '../page-header/page-header.component';

@Component({
  selector: 'app-feature-placeholder',
  standalone: true,
  imports: [PageHeaderComponent, EmptyStateComponent],
  templateUrl: './feature-placeholder.component.html',
  styleUrl: './feature-placeholder.component.css'
})
export class FeaturePlaceholderComponent {
  private readonly route = inject(ActivatedRoute);

  title = this.route.snapshot.data['title'] ?? 'Page';
  emptyTitle = this.route.snapshot.data['emptyTitle'] ?? 'No records';
  emptyMessage =
    this.route.snapshot.data['emptyMessage'] ??
    'This page is ready for backend integration.';
  actionLabel = this.route.snapshot.data['actionLabel'] ?? '';
  breadcrumbs = this.route.snapshot.data['breadcrumbs'] ?? [];
}