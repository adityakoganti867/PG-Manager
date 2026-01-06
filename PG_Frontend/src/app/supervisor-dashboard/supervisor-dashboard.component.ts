import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ApiService } from '../services/api.service';

@Component({
    selector: 'app-supervisor-dashboard',
    standalone: true,
    imports: [CommonModule, FormsModule, ReactiveFormsModule],
    templateUrl: './supervisor-dashboard.component.html',
    styleUrls: ['./supervisor-dashboard.component.css']
})
export class SupervisorDashboardComponent implements OnInit {
    complaints: any[] = [];
    showStatusUpdateModal: boolean = false;
    selectedComplaint: any = null;
    statusUpdateForm: FormGroup;

    // Filters
    complaintStatusFilter: string = '';
    openDropdown: string | null = null;

    constructor(private api: ApiService, private fb: FormBuilder) {
        this.statusUpdateForm = this.fb.group({
            status: ['Registered', Validators.required],
            estimatedResolutionDays: [1],
            notes: ['']
        });
    }

    toggleDropdown(name: string, event: Event) {
        event.stopPropagation();
        this.openDropdown = this.openDropdown === name ? null : name;
    }

    setFilter(filterName: string, value: any) {
        (this as any)[filterName] = value;
        this.openDropdown = null;
    }

    ngOnInit() {
        this.loadComplaints();
    }

    loadComplaints() {
        this.api.getComplaints().subscribe((data: any) => this.complaints = data);
    }

    get filteredComplaints() {
        return this.complaints.filter(c => {
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
}
