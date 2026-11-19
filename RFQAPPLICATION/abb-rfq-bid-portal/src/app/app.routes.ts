import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';
import { roleGuard } from './core/guards/role.guard';
import { ROLES } from './core/constants/roles';
import { AuthLayoutComponent } from './shared/layout/auth-layout/auth-layout.component';
import { MainLayoutComponent } from './shared/layout/main-layout/main-layout.component';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { SupplierDashboardComponent } from './features/dashboard/supplier-dashboard/supplier-dashboard.component';
import { ProcurementDashboardComponent } from './features/dashboard/procurement-dashboard/procurement-dashboard.component';
import { ApproverDashboardComponent } from './features/dashboard/approver-dashboard/approver-dashboard.component';
import { FeaturePlaceholderComponent } from './shared/components/feature-placeholder/feature-placeholder.component';
import { SupplierProfileComponent } from './features/supplier/profile/supplier-profile.component';
import { SupplierRfqListComponent } from './features/supplier/rfqs/supplier-rfq-list.component';
import { SupplierRfqDetailComponent } from './features/supplier/rfqs/supplier-rfq-detail.component';
import { SupplierMyBidsComponent } from './features/supplier/bids/supplier-my-bids.component';
import { SupplierBidDetailComponent } from './features/supplier/bids/supplier-bid-detail.component';
import { ProcurementRfqListComponent } from './features/procurement/rfqs/procurement-rfq-list.component';
import { ProcurementRfqCreateComponent } from './features/procurement/rfqs/procurement-rfq-create.component';
import { ProcurementRfqDetailComponent } from './features/procurement/rfqs/procurement-rfq-detail.component';
import { ProcurementEvaluationListComponent } from './features/procurement/evaluations/procurement-evaluation-list.component';
import { ProcurementEvaluationDetailComponent } from './features/procurement/evaluations/procurement-evaluation-detail.component';
import { ApproverEvaluationListComponent } from './features/approver/evaluations/approver-evaluation-list.component';
import { ApproverEvaluationDetailComponent } from './features/approver/evaluations/approver-evaluation-detail.component';
import { ApproverAwardListComponent } from './features/approver/awards/approver-award-list.component';
import { ApproverAwardDetailComponent } from './features/approver/awards/approver-award-detail.component';
import { AuditLogListComponent } from './features/audit-logs/audit-log-list.component';
import { AuditLogDetailComponent } from './features/audit-logs/audit-log-detail.component';
import { ProcurementSupplierListComponent } from './features/procurement/suppliers/procurement-supplier-list.component';
import { ProcurementSupplierDetailComponent } from './features/procurement/suppliers/procurement-supplier-detail.component';
import { ApproverSupplierListComponent } from './features/approver/suppliers/approver-supplier-list.component';
import { ApproverSupplierDetailComponent } from './features/approver/suppliers/approver-supplier-detail.component';
import { NotFoundComponent } from './features/not-found/not-found.component';
import { HomeRedirectComponent } from './features/home/home-redirect.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', component: HomeRedirectComponent },
  {
    path: '',
    component: AuthLayoutComponent,
    canActivate: [guestGuard],
    children: [
      { path: 'login', component: LoginComponent },
      { path: 'register', component: RegisterComponent }
    ]
  },
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      {
        path: 'supplier',
        canActivate: [roleGuard([ROLES.Supplier])],
        children: [
          { path: 'dashboard', component: SupplierDashboardComponent },
          { path: 'rfqs', component: SupplierRfqListComponent },
          { path: 'rfqs/:rfqId', component: SupplierRfqDetailComponent },
          { path: 'my-bids', component: SupplierMyBidsComponent },
          { path: 'my-bids/:bidId', component: SupplierBidDetailComponent },
          { path: 'bids', redirectTo: 'my-bids', pathMatch: 'full' },
          { path: 'profile', component: SupplierProfileComponent },
          { path: '', pathMatch: 'full', redirectTo: 'dashboard' }
        ]
      },
      {
        path: 'procurement',
        canActivate: [roleGuard([ROLES.ProcurementOfficer])],
        children: [
          { path: 'dashboard', component: ProcurementDashboardComponent },
          { path: 'suppliers', component: ProcurementSupplierListComponent },
          { path: 'suppliers/:supplierId', component: ProcurementSupplierDetailComponent },
          { path: 'rfqs', component: ProcurementRfqListComponent },
          { path: 'rfqs/create', component: ProcurementRfqCreateComponent },
          { path: 'rfqs/:rfqId', component: ProcurementRfqDetailComponent },
          { path: 'rounds', redirectTo: 'rfqs', pathMatch: 'full' },
          { path: 'evaluations', component: ProcurementEvaluationListComponent },
          {
            path: 'evaluations/:rfqId/:roundId',
            component: ProcurementEvaluationDetailComponent
          },
          { path: 'audit-logs', redirectTo: '/audit-logs', pathMatch: 'full' },
          { path: '', pathMatch: 'full', redirectTo: 'dashboard' }
        ]
      },
      {
        path: 'approver',
        canActivate: [roleGuard([ROLES.Approver])],
        children: [
          { path: 'dashboard', component: ApproverDashboardComponent },
          { path: 'evaluations', component: ApproverEvaluationListComponent },
          {
            path: 'evaluations/:rfqId/:roundId',
            component: ApproverEvaluationDetailComponent
          },
          { path: 'awards', component: ApproverAwardListComponent },
          { path: 'awards/:rfqId', component: ApproverAwardDetailComponent },
          { path: 'decisions', redirectTo: 'evaluations', pathMatch: 'full' },
          { path: 'audit-logs', redirectTo: '/audit-logs', pathMatch: 'full' },
          { path: 'suppliers', component: ApproverSupplierListComponent },
          { path: 'suppliers/:supplierId', component: ApproverSupplierDetailComponent },
          {
            path: 'rfqs',
            component: FeaturePlaceholderComponent,
            data: { title: 'RFQs', emptyTitle: 'No RFQs' }
          },
          { path: '', pathMatch: 'full', redirectTo: 'dashboard' }
        ]
      },
      {
        path: 'audit-logs',
        canActivate: [roleGuard([ROLES.ProcurementOfficer, ROLES.Approver])],
        children: [
          { path: '', component: AuditLogListComponent },
          { path: ':auditId', component: AuditLogDetailComponent }
        ]
      }
    ]
  },
  { path: '**', component: NotFoundComponent }
];