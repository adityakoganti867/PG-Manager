
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { Router } from '@angular/router';

@Injectable({
    providedIn: 'root'
})
export class AuthService {
    private apiUrl = 'http://localhost:5122/api/auth'; // Ensure this matches backend port
    private userSubject = new BehaviorSubject<any>(this.getUserFromStorage());
    public user$ = this.userSubject.asObservable();

    constructor(private http: HttpClient, private router: Router) {
        // Listen for storage changes in other tabs
        window.addEventListener('storage', (event) => {
            if (event.key === 'user' && !event.newValue) {
                // Logout detected in another tab
                this.userSubject.next(null);
                this.router.navigate(['/login']);
            }
        });
    }

    private getUserFromStorage() {
        if (typeof window === 'undefined') return null;
        const userStr = localStorage.getItem('user');
        return userStr ? JSON.parse(userStr) : null;
    }

    login(mobile: string, password: string): Observable<any> {
        return this.http.post(`${this.apiUrl}/login`, { mobile, password }).pipe(
            tap((user: any) => {
                localStorage.setItem('user', JSON.stringify(user));
                this.userSubject.next(user);
                this.redirectUser(user.role);
            })
        );
    }

    checkStatus(mobile: string) {
        return this.http.get(`${this.apiUrl}/check-status?mobile=${mobile}`);
    }

    setPassword(mobile: string, password: string) {
        return this.http.post(`${this.apiUrl}/set-password`, { mobile, password }, { responseType: 'text' });
    }

    logout() {
        localStorage.removeItem('user');
        this.userSubject.next(null);
        this.router.navigate(['/login']);
    }

    redirectUser(role: number) {
        if (role === 0) this.router.navigate(['/admin']);
        else if (role === 1) this.router.navigate(['/supervisor']);
        else if (role === 2) this.router.navigate(['/guest']);
    }

    get currentUserValue() {
        return this.userSubject.value;
    }

    canActivate(expectedRole: number | number[]): boolean {
        const user = this.currentUserValue;
        if (!user) {
            this.router.navigate(['/login']);
            return false;
        }

        const roles = Array.isArray(expectedRole) ? expectedRole : [expectedRole];
        if (!roles.includes(user.role)) {
            this.redirectUser(user.role);
            return false;
        }

        return true;
    }
}
