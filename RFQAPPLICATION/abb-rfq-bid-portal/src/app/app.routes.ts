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
          {
            path: 'rfqs',
            component: FeaturePlaceholderComponent,
            data: {
              title: 'Available RFQs',
              emptyTitle: 'No RFQs available',
              emptyMessage: 'Published RFQs open for bidding will appear here.',
              actionLabel: 'Refresh list'
            }
          },
          {
            path: 'bids',
            component: FeaturePlaceholderComponent,
            data: {
              title: 'My Bids',
              emptyTitle: 'No bids submitted',
              emptyMessage: 'Your bids across RFQ rounds will be listed here.',
              actionLabel: 'View RFQs'
            }
          },
          { path: 'profile', component: SupplierProfileComponent },
          { path: '', pathMatch: 'full', redirectTo: 'dashboard' }
        ]
      },
      {
        path: 'procurement',
        canActivate: [roleGuard([ROLES.ProcurementOfficer])],
        children: [
          { path: 'dashboard', component: ProcurementDashboardComponent },
          {
            path: 'suppliers',
            component: FeaturePlaceholderComponent,
            data: {
              title: 'Supplier Management',
              emptyTitle: 'No suppliers',
              actionLabel: 'Review pending approvals'
            }
          },
          {
            path: 'rfqs',
            component: FeaturePlaceholderComponent,
            data: {
              title: 'RFQ Management',
              emptyTitle: 'No RFQs',
              actionLabel: 'Create RFQ'
            }
          },
          {
            path: 'rounds',
            component: FeaturePlaceholderComponent,
            data: {
              title: 'RFQ Rounds',
              emptyTitle: 'No rounds',
              actionLabel: 'Manage rounds'
            }
          },
          {
            path: 'evaluations',
            component: FeaturePlaceholderComponent,
            data: {
              title: 'Bid Evaluation',
              emptyTitle: 'No evaluations',
              actionLabel: 'Start evaluation'
            }
          },
          {
            path: 'awards',
            component: FeaturePlaceholderComponent,
            data: {
              title: 'Awards',
              emptyTitle: 'No awards',
              actionLabel: 'Create award'
            }
          },
          {
            path: 'audit-logs',
            component: FeaturePlaceholderComponent,
            data: {
              title: 'Audit Logs',
              emptyTitle: 'No audit records',
              actionLabel: 'Export logs'
            }
          },
          { path: '', pathMatch: 'full', redirectTo: 'dashboard' }
        ]
      },
      {
        path: 'approver',
        canActivate: [roleGuard([ROLES.Approver])],
        children: [
          { path: 'dashboard', component: ApproverDashboardComponent },
          {
            path: 'suppliers',
            component: FeaturePlaceholderComponent,
            data: { title: 'Supplier Management', emptyTitle: 'No suppliers' }
          },
          {
            path: 'rfqs',
            component: FeaturePlaceholderComponent,
            data: { title: 'RFQs', emptyTitle: 'No RFQs' }
          },
          {
            path: 'evaluations',
            component: FeaturePlaceholderComponent,
            data: { title: 'Bid Evaluations', emptyTitle: 'No evaluations' }
          },
          {
            path: 'decisions',
            component: FeaturePlaceholderComponent,
            data: {
              title: 'Evaluation Decisions',
              emptyTitle: 'No decisions pending',
              actionLabel: 'Review evaluation'
            }
          },
          {
            path: 'awards',
            component: FeaturePlaceholderComponent,
            data: { title: 'Awards', emptyTitle: 'No awards' }
          },
          {
            path: 'audit-logs',
            component: FeaturePlaceholderComponent,
            data: { title: 'Audit Logs', emptyTitle: 'No audit records' }
          },
          { path: '', pathMatch: 'full', redirectTo: 'dashboard' }
        ]
      }
    ]
  },
  { path: '**', component: NotFoundComponent }
];