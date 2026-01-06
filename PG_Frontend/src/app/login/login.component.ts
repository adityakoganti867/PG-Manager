
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, FormsModule } from '@angular/forms';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

@Component({
    selector: 'app-login',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, FormsModule],
    templateUrl: './login.component.html',
    styleUrls: ['./login.component.css']
})
export class LoginComponent {
    loginForm: FormGroup;
    errorMsg: string = '';

    showSetPasswordModal: boolean = false;
    userMobileForSet: string = '';
    newPassword: string = '';
    confirmPassword: string = '';
    passwordError: string = '';

    constructor(private fb: FormBuilder, private auth: AuthService, private router: Router) {
        // If already logged in, redirect to appropriate dashboard
        const user = this.auth.currentUserValue;
        if (user) {
            this.auth.redirectUser(user.role);
        }

        this.loginForm = this.fb.group({
            mobile: ['', Validators.required],
            password: ['', Validators.required]
        });
    }

    onSubmit() {
        if (this.loginForm.valid) {
            this.auth.login(this.loginForm.value.mobile, this.loginForm.value.password).subscribe({
                next: (user: any) => {
                    // Auto-route based on user's actual role
                    // No role validation needed - just route to appropriate dashboard
                },
                error: (err) => {
                    this.errorMsg = 'Login Failed. Check credentials.';
                    console.error(err);
                }
            });
        }
    }

    onMobileBlur() {
        const mobile = this.loginForm.get('mobile')?.value;
        if (mobile && mobile.length >= 10) {
            this.auth.checkStatus(mobile).subscribe({
                next: (res: any) => {
                    if (!res.isPasswordSet) {
                        this.userMobileForSet = mobile;
                        this.showSetPasswordModal = true;
                    }
                },
                error: (err) => console.error("Error checking status", err)
            });
        }
    }

    onSetPasswordSubmit() {
        if (!this.newPassword || !this.confirmPassword) {
            this.passwordError = "Both fields are required";
            return;
        }
        if (this.newPassword !== this.confirmPassword) {
            this.passwordError = "Passwords do not match";
            return;
        }

        this.auth.setPassword(this.userMobileForSet, this.newPassword).subscribe({
            next: (res) => {
                // Auto Login
                this.auth.login(this.userMobileForSet, this.newPassword).subscribe({
                    next: (user: any) => {
                        this.showSetPasswordModal = false;
                        // Login success, redirection happens in service
                    },
                    error: (err) => {
                        this.passwordError = "Login failed after setting password. Please try logging in manually.";
                    }
                });
            },
            error: (err) => {
                this.passwordError = "Failed to set password";
            }
        });
    }

    closeSetPasswordModal() {
        this.showSetPasswordModal = false;
        this.passwordError = '';
        this.newPassword = '';
        this.confirmPassword = '';
    }
}
