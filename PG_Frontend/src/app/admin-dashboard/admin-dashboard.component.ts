
import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule, Validators } from '@angular/forms';
import { ApiService } from '../services/api.service';
import { AuthService } from '../services/auth.service';

@Component({
    selector: 'app-admin-dashboard',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, FormsModule],
    templateUrl: './admin-dashboard.component.html',
    styleUrls: ['./admin-dashboard.component.css']
})
export class AdminDashboardComponent implements OnInit {
    activeTab: string = 'rooms';
    supervisorForm: FormGroup;
    guestForm: FormGroup;
    supervisors: any[] = [];
    guests: any[] = [];
    complaints: any[] = [];
    transactions: any[] = [];
    user: any;

    // Complaint Filters
    complaintStatusFilter: string = '';

    get pendingNoticeCount(): number {
        return this.guests ? this.guests.filter(g => g.noticeStatus === 'Pending').length : 0;
    }

    get pendingNoticeGuests(): any[] {
        return this.guests ? this.guests.filter(g => g.noticeStatus === 'Pending') : [];
    }

    get registeredComplaintsCount(): number {
        return this.complaints ? this.complaints.filter(c => c.status === 'Registered').length : 0;
    }

    get pendingVerificationCount(): number {
        return this.transactions ? this.transactions.filter(t => t.status === 'Pending').length : 0;
    }

    selectedGuest: any = null;

    showAddSupervisorModal: boolean = false;
    showResetPasswordModal: boolean = false;
    userToResetId: number | null = null;
    showAddGuestModal: boolean = false;
    showStatusUpdateModal: boolean = false;
    selectedComplaint: any = null;
    statusUpdateForm: FormGroup;

    settings: any[] = [];
    upiSettings = { upiId: '', upiName: '' };

    // Filters
    guestStatusFilter: string = '';
    rentStatusFilter: string = '';
    guestTypeFilter: string = '';
    transactionStatusFilter: string = '';

    // Toast Notification
    showSuccessToast: boolean = false;
    toastMessage: string = '';

    // Room Filters
    roomFloorFilter: any = '';
    roomShareFilter: any = '';
    roomTypeFilter: string = '';

    // Custom Dropdown State
    openDropdown: string | null = null;

    // Rooms
    rooms: any[] = [];
    roomForm: FormGroup;
    availableRooms: any[] = [];
    showAddRoomModal: boolean = false;

    constructor(private fb: FormBuilder, private api: ApiService, private auth: AuthService) {
        this.user = this.auth.currentUserValue;
        this.supervisorForm = this.fb.group({
            name: ['', Validators.required],
            mobile: ['', Validators.required],
            joiningDate: ['', Validators.required]
        });

        this.roomForm = this.fb.group({
            roomNumber: ['', Validators.required],
            floorNumber: [1, Validators.required],
            sharingType: [1, Validators.required],
            roomType: ['Non-AC', Validators.required]
        });

        this.statusUpdateForm = this.fb.group({
            status: ['Registered', Validators.required],
            estimatedResolutionDays: [1],
            notes: ['']
        });

        this.guestForm = this.fb.group({
            name: ['', [Validators.required, Validators.maxLength(10)]],
            mobile: ['', Validators.required],

            // Room selection flow
            shareType: ['', Validators.required],
            floorNumber: [''], // Optional
            roomType: [''], // Optional
            roomNumber: ['', Validators.required],

            rentType: ['Regular', Validators.required],

            // Regular Fields
            occupation: [''],
            advanceAmount: [''],
            rentAmount: [''], // Monthly Rent
            joiningDate: [''],

            // Daily Fields
            perDayRent: [''],
            startPeriod: [''],
            endPeriod: ['']
        });

        // Initialize validators based on default type
        this.onRentTypeChange();
    }

    toggleDropdown(name: string, event: Event) {
        event.stopPropagation();
        this.openDropdown = this.openDropdown === name ? null : name;
    }

    setFilter(filterName: string, value: any) {
        (this as any)[filterName] = value;
        this.openDropdown = null;
    }

    @HostListener('document:click')
    closeDropdowns() {
        this.openDropdown = null;
    }

    ngOnInit() {
        this.loadSupervisors();
        this.loadGuests();
        this.loadComplaints();
        this.loadTransactions();
        this.loadRooms();
        this.loadSettings();

        // Listen to changes to recalculate
        this.guestForm.get('rentType')?.valueChanges.subscribe(() => this.onRentTypeChange());
        this.guestForm.get('startPeriod')?.valueChanges.subscribe(() => this.calculateTotalRent());
        this.guestForm.get('endPeriod')?.valueChanges.subscribe(() => this.calculateTotalRent());
        this.guestForm.get('perDayRent')?.valueChanges.subscribe(() => this.calculateTotalRent());

        // Listen for share/floor/type changes to load available rooms
        this.guestForm.get('shareType')?.valueChanges.subscribe(() => this.loadAvailableRooms());
        this.guestForm.get('floorNumber')?.valueChanges.subscribe(() => this.loadAvailableRooms());
        this.guestForm.get('roomType')?.valueChanges.subscribe(() => this.loadAvailableRooms());
    }

    get filteredTransactions(): any[] {
        if (!this.transactions) return [];
        return this.transactions.filter(t => {
            const matchesStatus = !this.transactionStatusFilter || t.status === this.transactionStatusFilter;
            return matchesStatus;
        });
    }

    loadSettings() {
        this.api.getSettings().subscribe((data: any) => {
            this.settings = data;
            const upiId = data.find((s: any) => s.key === 'UpiId')?.value;
            const upiName = data.find((s: any) => s.key === 'UpiName')?.value;
            this.upiSettings = {
                upiId: upiId || '',
                upiName: upiName || ''
            };
        });
    }

    updateUpiSettings() {
        this.api.updateUpiSettings(this.upiSettings).subscribe({
            next: () => {
                this.showToast('UPI Settings Updated');
                this.loadSettings();
            },
            error: (err) => alert('Failed to update settings')
        });
    }

    loadAvailableRooms() {
        const share = this.guestForm.get('shareType')?.value;
        const floor = this.guestForm.get('floorNumber')?.value;
        const type = this.guestForm.get('roomType')?.value;

        console.log('Filter values:', { share, floor, type });

        if (share) {
            // Convert empty strings to undefined for optional parameters
            const floorParam = floor ? Number(floor) : undefined;
            const typeParam = type && type !== '' ? type : undefined;

            console.log('API params:', { share, floorParam, typeParam });

            this.api.getAvailableRooms(share, floorParam, typeParam).subscribe((data: any) => {
                console.log('Available rooms:', data);
                this.availableRooms = data;
            });
        }
    }

    loadRooms() {
        this.api.getRooms().subscribe((data: any) => this.rooms = data);
    }

    addRoom() {
        if (this.roomForm.valid) {
            this.api.addRoom(this.roomForm.value).subscribe({
                next: () => {
                    this.showToast('Room Added Successfully');
                    this.loadRooms();
                    this.roomForm.reset({ floorNumber: 1, sharingType: 1, roomType: 'Non-AC' });
                    this.closeAddRoom();
                },
                error: (err) => alert(err.error || 'Failed to add room')
            });
        }
    }

    openAddRoom() { this.showAddRoomModal = true; }
    closeAddRoom() { this.showAddRoomModal = false; }

    onRentTypeChange() {
        const type = this.guestForm.get('rentType')?.value;
        const regularControls = ['occupation', 'advanceAmount', 'rentAmount', 'joiningDate'];
        const dailyControls = ['perDayRent', 'startPeriod', 'endPeriod'];

        if (type === 'Regular') {
            regularControls.forEach(c => this.guestForm.get(c)?.setValidators(Validators.required));
            dailyControls.forEach(c => this.guestForm.get(c)?.clearValidators());
        } else {
            regularControls.forEach(c => this.guestForm.get(c)?.clearValidators());
            dailyControls.forEach(c => this.guestForm.get(c)?.setValidators(Validators.required));
        }

        // Update validity
        [...regularControls, ...dailyControls].forEach(c => this.guestForm.get(c)?.updateValueAndValidity());
    }

    calculatedTotal: number = 0;

    calculateTotalRent() {
        const type = this.guestForm.get('rentType')?.value;
        if (type === 'Daily') {
            const start = this.guestForm.get('startPeriod')?.value;
            const end = this.guestForm.get('endPeriod')?.value;
            const rate = this.guestForm.get('perDayRent')?.value;

            if (start && end && rate) {
                const startDate = new Date(start);
                const endDate = new Date(end);
                const days = Math.floor((endDate.getTime() - startDate.getTime()) / (1000 * 60 * 60 * 24)) + 1;

                if (days > 0) {
                    this.calculatedTotal = days * rate;
                } else {
                    this.calculatedTotal = 0;
                }
            } else {
                // Reset to 0 if any field is missing
                this.calculatedTotal = 0;
            }
        } else {
            // Reset to 0 for non-Daily rent types
            this.calculatedTotal = 0;
        }
    }

    // Override addGuest to map fields correctly
    addGuest() {
        if (this.guestForm.valid) {
            const val = this.guestForm.value;
            const payload: any = {
                name: val.name,
                mobile: val.mobile,
                roomNumber: val.roomNumber.toString(), // Ensure string
                rentType: val.rentType
            };

            if (val.rentType === 'Daily') {
                payload.joiningDate = val.startPeriod;
                payload.endDate = val.endPeriod;
                payload.perDayRent = val.perDayRent;
                payload.rentAmount = this.calculatedTotal;
                payload.occupation = 'Daily Guest';
                payload.advanceAmount = 0;
            } else {
                payload.occupation = val.occupation;
                payload.advanceAmount = val.advanceAmount;
                payload.rentAmount = val.rentAmount;
                payload.joiningDate = val.joiningDate;
                payload.endDate = null;
            }

            this.api.addGuest(payload).subscribe({
                next: () => {
                    this.showToast('Guest Added Successfully');
                    this.loadGuests();
                    this.loadRooms();
                    this.guestForm.reset({ rentType: 'Regular', shareType: '', floorNumber: '', roomType: '' });
                    this.closeAddGuest();
                },
                error: (err) => alert(err.error || 'Failed to add guest')
            });
        }
    }

    get filteredGuests() {
        return this.guests.filter(g => {
            // Status Filter
            let statusMatch = true;
            if (this.guestStatusFilter) {
                const isActive = g.user.isActive;
                const isNotice = g.isInNoticePeriod;
                if (this.guestStatusFilter === 'Active') statusMatch = isActive && !isNotice;
                else if (this.guestStatusFilter === 'Notice') statusMatch = isActive && isNotice;
                else if (this.guestStatusFilter === 'Disabled') statusMatch = !isActive;
            }


            // Rent Filter
            let rentMatch = true;
            if (this.rentStatusFilter) {
                const rentStatus = this.getRentStatus(g).text;
                if (this.rentStatusFilter === 'Overdue' && rentStatus !== 'Overdue') rentMatch = false;

                if (this.rentStatusFilter === 'DueToday' && rentStatus !== 'Due Today') rentMatch = false;
                if (this.rentStatusFilter === 'NearDue' && rentStatus !== 'Near Due') rentMatch = false;
                if (this.rentStatusFilter === 'Safe' && rentStatus !== 'Safe') rentMatch = false;
            }

            // Type Filter
            let typeMatch = true;
            if (this.guestTypeFilter) {
                const gType = g.rentType || 'Regular';
                if (this.guestTypeFilter === 'Regular' && gType !== 'Regular') typeMatch = false;
                if (this.guestTypeFilter === 'Daily' && gType !== 'Daily') typeMatch = false;
            }

            return statusMatch && rentMatch && typeMatch;
        }).sort((a, b) => {
            // Natural sort for room numbers
            return a.roomNumber.localeCompare(b.roomNumber, undefined, { numeric: true });
        });
    }

    switchTab(tab: string) { this.activeTab = tab; }

    loadSupervisors() {
        this.api.getSupervisors().subscribe((data: any) => this.supervisors = data);
    }

    loadGuests() {
        this.api.getAllGuests().subscribe((data: any) => this.guests = data);
    }

    loadComplaints() {
        this.api.getComplaints().subscribe((data: any) => this.complaints = data);
    }

    approvePayment(txId: number) {
        if (confirm('Verify that you received the payment in your bank account?')) {
            this.api.approvePayment(txId).subscribe(() => {
                this.showToast('Payment Approved');
                this.loadTransactions();
                this.loadGuests();
            });
        }
    }

    rejectPayment(txId: number) {
        if (confirm('Reject this payment?')) {
            this.api.rejectPayment(txId).subscribe(() => {
                this.showToast('Payment Rejected');
                this.loadTransactions();
            });
        }
    }

    loadTransactions() {
        this.api.getAllTransactions().subscribe({
            next: (data: any) => this.transactions = data,
            error: (err: any) => console.error("Error loading transactions", err)
        });
    }

    get filteredComplaints() {
        return this.complaints.filter(c => {
            // Status Filter
            if (this.complaintStatusFilter && c.status !== this.complaintStatusFilter) return false;
            return true;
        });
    }

    openStatusUpdate(complaint: any) {
        this.selectedComplaint = complaint;
        this.showStatusUpdateModal = true;
        this.statusUpdateForm.reset({
            status: complaint.status,
            estimatedResolutionDays: complaint.estimatedResolutionDays || 1,
            notes: complaint.notes || ''
        });
    }

    closeStatusUpdate() {
        this.showStatusUpdateModal = false;
        this.selectedComplaint = null;
    }

    updateComplaintStatus() {
        if (this.statusUpdateForm.valid && this.selectedComplaint) {
            this.api.updateComplaintStatus(this.selectedComplaint.id, this.statusUpdateForm.value)
                .subscribe(() => {
                    this.closeStatusUpdate();
                    this.loadComplaints();
                });
        }
    }

    addSupervisor() {
        if (this.supervisorForm.valid) {
            this.api.addSupervisor(this.supervisorForm.value).subscribe({
                next: () => {
                    this.showToast('Supervisor Added Successfully');
                    this.loadSupervisors();
                    this.supervisorForm.reset();
                    this.closeAddSupervisor();
                },
                error: (err) => {
                    console.error('Add Supervisor Error:', err);
                    if (err.error && (err.error.includes('mobile') || err.error.includes('exists'))) {
                        // Set manual error on the form control
                        this.supervisorForm.get('mobile')?.setErrors({ duplicate: true });
                    } else {
                        alert(err.error || 'Failed to add supervisor. Please try again.');
                    }
                }
            });
        }
    }

    toggleSupervisor(id: number) {
        this.api.toggleSupervisor(id).subscribe(() => this.loadSupervisors());
    }

    resetPassword(id: number) {
        this.userToResetId = id;
        this.showResetPasswordModal = true;
    }

    confirmResetPassword() {
        if (this.userToResetId) {
            this.api.resetPassword(this.userToResetId).subscribe({
                next: () => {
                    this.showToast('Password Reset Successfully');
                    this.closeResetPasswordModal();
                },
                error: (err) => alert('Failed to reset password')
            });
        }
    }

    closeResetPasswordModal() {
        this.showResetPasswordModal = false;
        this.userToResetId = null;
    }

    toggleGuest(id: number) {
        this.api.toggleGuest(id).subscribe({
            next: () => this.loadGuests(),
            error: (err) => {
                console.error(err);
                alert('Failed to toggle guest status. Please check if the backend is running the latest version.');
            }
        });
    }

    viewGuest(guest: any) { this.selectedGuest = guest; }
    closeModal() { this.selectedGuest = null; }

    openAddSupervisor() { this.showAddSupervisorModal = true; }
    closeAddSupervisor() { this.showAddSupervisorModal = false; }

    openAddGuest() {
        this.showAddGuestModal = true;
        this.guestForm.reset({ rentType: 'Regular', shareType: '', floorNumber: '', roomType: '' });
        this.availableRooms = [];
    }
    closeAddGuest() { this.showAddGuestModal = false; }


    showApproveNoticeModal: boolean = false;
    guestToApproveId: number | null = null;
    showRevertNoticeModal: boolean = false;
    guestToRevertId: number | null = null;

    approveNotice(guestId: number) {
        this.guestToApproveId = guestId;
        this.showApproveNoticeModal = true;
    }

    closeApproveNoticeModal() {
        this.showApproveNoticeModal = false;
        this.guestToApproveId = null;
    }

    confirmApproveNotice() {
        if (!this.guestToApproveId) return;

        this.api.approveNotice(this.guestToApproveId).subscribe({
            next: (res) => {
                // alert('Notice Approved'); // Suppress if desired, or keep generic toast
                this.loadGuests();
                // If modal is open, refresh or close it
                if (this.selectedGuest && this.selectedGuest.id === this.guestToApproveId) {
                    this.selectedGuest.noticeStatus = 'Approved';
                    this.selectedGuest.isInNoticePeriod = true;
                    this.selectedGuest.noticeStartDate = new Date();
                }
                this.closeApproveNoticeModal();
            },
            error: (err) => {
                alert("Failed to approve notice");
                this.closeApproveNoticeModal();
            }
        });
    }

    revertNotice(guestId: number) {
        this.guestToRevertId = guestId;
        this.showRevertNoticeModal = true;
    }

    closeRevertNoticeModal() {
        this.showRevertNoticeModal = false;
        this.guestToRevertId = null;
    }

    confirmRevertNotice() {
        if (!this.guestToRevertId) return;

        this.api.revertNotice(this.guestToRevertId).subscribe({
            next: (res) => {
                this.loadGuests();
                // Update locally if selected
                if (this.selectedGuest && this.selectedGuest.id === this.guestToRevertId) {
                    this.selectedGuest.noticeStatus = 'None';
                    this.selectedGuest.isInNoticePeriod = false;
                    this.selectedGuest.noticeStartDate = null;
                    this.selectedGuest = null; // Close guest details modal
                }
                this.closeRevertNoticeModal();
            },
            error: (err) => {
                alert("Failed to revert notice");
                this.closeRevertNoticeModal();
            }
        });
    }


    getRentStatus(guest: any): { text: string, class: string } {
        // User requested to keep rent status as Safe/Due even during Notice Period

        // Pending Status check (displayed in Status column usually, but can override rent status if desired, 
        // user said "instead of next due rent show under notice status". User meant confirmed status.
        // But let's handle Pending too if needed. For now sticking to confirmed as requested.)
        if (!guest.rentDueDate) return { text: 'N/A', class: '' };

        const today = new Date();
        today.setHours(0, 0, 0, 0);
        const due = new Date(guest.rentDueDate);
        due.setHours(0, 0, 0, 0);

        const diffTime = due.getTime() - today.getTime();
        const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

        if (diffDays < 0) return { text: 'Overdue', class: 'rent-overdue' }; // Red Blinking
        if (diffDays === 0) return { text: 'Due Today', class: 'rent-due-today' }; // Orange
        if (diffDays <= 2) return { text: 'Near Due', class: 'rent-near' }; // Yellow
        return { text: 'Safe', class: 'rent-safe' }; // Green
    }


    getDaysLeftForExit(guest: any): number {
        if (!guest || !guest.rentDueDate) return 0;
        const due = new Date(guest.rentDueDate);
        const today = new Date();
        const diffTime = due.getTime() - today.getTime();
        const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
        return diffDays > 0 ? diffDays : 0;
    }

    showToast(message: string) {
        this.toastMessage = message;
        this.showSuccessToast = true;
        setTimeout(() => {
            this.showSuccessToast = false;
        }, 2000);
    }

    // Filtered Rooms
    get filteredRooms() {
        return this.rooms.filter(r => {
            // Floor filter
            if (this.roomFloorFilter && r.floorNumber != this.roomFloorFilter) return false;

            // Share type filter
            if (this.roomShareFilter && r.sharingType != this.roomShareFilter) return false;

            // Room type filter (AC/Non-AC)
            if (this.roomTypeFilter && r.roomType !== this.roomTypeFilter) return false;

            return true;
        });
    }
}
