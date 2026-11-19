import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { AwardService } from '../../../core/services/award.service';
import { AuthService } from '../../../core/services/auth.service';
import { Award } from '../../../core/models/award.model';

@Component({
  selector: 'app-approver-award-detail',
  standalone: true,
  imports: [RouterLink, DatePipe, CurrencyPipe, PageHeaderComponent],
  templateUrl: './approver-award-detail.component.html',
  styleUrl: './approver-award-detail.component.css'
})
export class ApproverAwardDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly awardService = inject(AwardService);
  private readonly auth = inject(AuthService);

  loading = false;
  loadError = '';
  notAwarded = false;
  award: Award | null = null;

  ngOnInit(): void {
    const rfqId = Number(this.route.snapshot.paramMap.get('rfqId'));
    if (!rfqId) {
      this.loadError = 'Invalid RFQ ID.';
      return;
    }
    this.loading = true;
    this.awardService
      .getByRfq(rfqId)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (award) => {
          this.award = award;
          this.notAwarded = false;
        },
        error: (error: HttpErrorResponse) => {
          if (error.status === 401) {
            this.auth.logout();
            return;
          }
          if (error.status === 404) {
            this.award = null;
            this.notAwarded = true;
            return;
          }
          this.loadError = error.error?.message ?? 'Unable to load award.';
        }
      });
  }
}