
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



    payRent() {
        if (!this.profile || !this.profile.rentAmount) return;

        this.api.createOrder(this.profile.rentAmount).subscribe({
            next: (res: any) => {
                this.openRazorpay(res.orderId);
            },
            error: (err) => alert('Failed to create payment order. ' + err.message)
        });
    }


    openRazorpay(orderId: string) {
        const options = {
            "key": "rzp_test_S0VAf9TqG9Ylvv", // Enter the Key ID generated from the Dashboard

            "amount": this.profile.rentAmount * 100, // Amount is in currency subunits. Default currency is INR.
            "currency": "INR",
            "name": "PG Management",
            "description": "Rent Payment",
            "image": "https://example.com/your_logo",
            "order_id": orderId,
            "handler": (response: any) => {
                this.verifyPayment(response);
            },
            "prefill": {
                "name": this.profile.name,
                "contact": this.user.mobile
            },
            "theme": {
                "color": "#3399cc"
            }
        };

        const rzp1 = new (window as any).Razorpay(options);
        rzp1.on('payment.failed', function (response: any) {
            alert(response.error.description);
        });
        rzp1.open();
    }


    verifyPayment(response: any) {
        this.api.verifyPayment({
            guestId: this.profile.id,
            orderId: response.razorpay_order_id,
            paymentId: response.razorpay_payment_id,
            signature: response.razorpay_signature
        }).subscribe({
            next: (res: any) => {
                if (res.status === 'success') {
                    // Instant UI Update
                    if (this.profile) {
                        this.profile.paymentStatus = 'Paid';
                        this.profile.lastPaidDate = new Date();
                        if (res.nextDueDate) {
                            this.profile.rentDueDate = res.nextDueDate;
                        }
                    }
                    this.loadProfile(); // Sync with backend
                } else {
                    console.error('Payment verification failed');
                }
            },
            error: (err) => console.error('Payment verification error', err)
        });
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
