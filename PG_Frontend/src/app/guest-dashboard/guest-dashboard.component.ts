
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { ApiService } from '../services/api.service';
import { AuthService } from '../services/auth.service';

@Component({
    selector: 'app-guest-dashboard',
    standalone: true,
    imports: [CommonModule, FormsModule, ReactiveFormsModule],
    templateUrl: './guest-dashboard.component.html',
    styleUrls: ['./guest-dashboard.component.css']
})
export class GuestDashboardComponent implements OnInit {
    profile: any = null;
    complaints: any[] = [];
    transactions: any[] = [];
    user: any;
    activeTab: string = 'complaints';
    showComplaintModal: boolean = false;
    isMobile: boolean = false;
    complaintForm: FormGroup;
    errorMessage: string = '';

    complaintTypes: string[] = [
        'AC', 'FAN', 'plumbing', 'heater', 'bed related', 'cuboard related',
        'door or wood related', 'chairs', 'TV', 'water dispenser', 'Frdige',
        'power socktes', 'Keys', 'Working desks', 'Washing Machine'
    ];


    constructor(
        private api: ApiService,
        private auth: AuthService,
        private fb: FormBuilder,
        private route: ActivatedRoute,
        private router: Router
    ) {
        this.user = this.auth.currentUserValue;
        this.complaintForm = this.fb.group({
            type: ['', Validators.required],
            description: ['', [Validators.required, Validators.maxLength(600)]]
        });
    }

    ngOnInit() {
        this.isMobile = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
        if (this.user) {
            this.loadProfile();
            this.loadComplaints();
            this.loadTransactions();
        }

        // Restore tab from URL
        this.route.queryParams.subscribe(params => {
            if (params['tab']) {
                this.activeTab = params['tab'];
            }
        });
    }

    switchTab(tab: string) {
        this.activeTab = tab;
        // Update URL without reloading
        this.router.navigate([], {
            relativeTo: this.route,
            queryParams: { tab: tab },
            queryParamsHandling: 'merge', // merge with existing params
        });
    }




    loadProfile() {
        this.api.getProfile(this.user.id).subscribe({
            next: (data) => {
                this.profile = data;
                this.errorMessage = '';
                this.loadTransactions(); // Load transactions after knowing guest ID
            },
            error: (err) => {
                console.error('Error loading profile', err);
                this.errorMessage = 'Guest record not found. Please contact the administrator to ensure your profile is correctly set up.';
            }
        });
    }

    loadComplaints() {
        this.api.getMyComplaints(this.user.id).subscribe({
            next: (data: any) => this.complaints = data,
            error: (err) => console.error('Error loading complaints', err)
        });
    }

    loadTransactions() {
        if (this.profile && this.profile.id) {
            this.api.getPaymentHistory(this.profile.id).subscribe({
                next: (data: any) => this.transactions = data,
                error: (err) => console.error('Error loading transactions', err)
            });
        }
    }

    openComplaintModal() {
        this.showComplaintModal = true;
        this.complaintForm.reset({ type: '', description: '' });
    }

    closeComplaintModal() {
        this.showComplaintModal = false;
    }

    registerComplaint() {
        if (this.complaintForm.valid && this.profile) {
            const data = {
                guestId: this.profile.id,
                type: this.complaintForm.value.type,
                description: this.complaintForm.value.description
            };
            this.api.raiseComplaint(data).subscribe(() => {
                this.closeComplaintModal();
                this.loadComplaints();
            });
        }
    }

    cancelComplaint(id: number) {
        if (confirm('Cancel this complaint?')) {
            this.api.cancelComplaint(id).subscribe(() => this.loadComplaints());
        }
    }


    showNoticeConfirmationModal: boolean = false;
    showWarningModal: boolean = false;
    warningMessage: string = '';

    initiateNotice() {
        this.showNoticeConfirmationModal = true;
    }

    closeNoticeConfirmation() {
        this.showNoticeConfirmationModal = false;
    }

    closeWarningModal() {
        this.showWarningModal = false;
        this.warningMessage = '';
    }

    confirmNoticeInitiation() {
        this.api.initiateNotice(this.user.id).subscribe({
            next: (res: any) => {
                this.profile.noticeStatus = 'Pending';
                this.loadProfile();
                this.closeNoticeConfirmation();
            },
            error: (err) => {
                const msg = err.error || "Failed to initiate notice";
                this.warningMessage = msg;
                this.closeNoticeConfirmation(); // Close the confirm modal
                this.showWarningModal = true;   // Open the warning modal
            }
        });
    }



    showPaymentModal: boolean = false;
    paymentUtr: string = '';
    upiUrl: string = '';

    payRent() {
        if (!this.profile || !this.profile.rentAmount) return;

        this.api.createOrder(this.profile.id, this.profile.rentAmount).subscribe({
            next: (res: any) => {
                this.upiUrl = res.upiUrl;
                this.showPaymentModal = true;
            },
            error: (err) => alert('Failed to initiate payment. ' + err.message)
        });
    }

    submitUtr() {
        if (!this.paymentUtr || this.paymentUtr.length < 12) {
            alert('Please enter a valid 12-digit UTR number.');
            return;
        }

        this.api.verifyPayment(this.profile.id, this.paymentUtr, this.profile.rentAmount).subscribe({
            next: (res: any) => {
                this.showPaymentModal = false;
                this.paymentUtr = '';
                this.loadProfile();
                this.loadTransactions();
                alert('Payment proof submitted for verification. Admin will approve it shortly.');
            },
            error: (err) => alert('Failed to submit payment proof.')
        });
    }

    closePaymentModal() {
        this.showPaymentModal = false;
        this.paymentUtr = '';
    }

    encodeUrl(url: string): string {
        return encodeURIComponent(url);
    }

    getDaysLeftForExit(): number {
        if (!this.profile || !this.profile.rentDueDate) return 0;
        const due = new Date(this.profile.rentDueDate);
        const today = new Date();
        const diffTime = due.getTime() - today.getTime();
        const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
        return diffDays > 0 ? diffDays : 0;
    }
}
