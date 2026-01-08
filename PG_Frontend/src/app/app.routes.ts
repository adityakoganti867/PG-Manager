import { Routes, CanActivateFn } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { AdminDashboardComponent } from './admin-dashboard/admin-dashboard.component';
import { SupervisorDashboardComponent } from './supervisor-dashboard/supervisor-dashboard.component';
import { GuestDashboardComponent } from './guest-dashboard/guest-dashboard.component';
import { SuperAdminDashboardComponent } from './superadmin-dashboard/superadmin-dashboard.component';
import { inject } from '@angular/core';
import { AuthService } from './services/auth.service';

const adminGuard: CanActivateFn = () => inject(AuthService).canActivate(0);
const supervisorGuard: CanActivateFn = () => inject(AuthService).canActivate(1);
const guestGuard: CanActivateFn = () => inject(AuthService).canActivate(2);
const superAdminGuard: CanActivateFn = () => inject(AuthService).canActivate(3);

export const routes: Routes = [
    { path: 'login', component: LoginComponent },
    { path: 'admin', component: AdminDashboardComponent, canActivate: [adminGuard] },
    { path: 'supervisor', component: SupervisorDashboardComponent, canActivate: [supervisorGuard] },
    { path: 'guest', component: GuestDashboardComponent, canActivate: [guestGuard] },
    { path: 'superadmin', component: SuperAdminDashboardComponent, canActivate: [superAdminGuard] },
    { path: '', redirectTo: 'login', pathMatch: 'full' }
];
