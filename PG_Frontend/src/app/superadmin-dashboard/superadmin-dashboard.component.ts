
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule, Validators } from '@angular/forms';
import { ApiService } from '../services/api.service';
import { AuthService } from '../services/auth.service';

@Component({
    selector: 'app-superadmin-dashboard',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, FormsModule],
    templateUrl: './superadmin-dashboard.component.html',
    styleUrls: ['./superadmin-dashboard.component.css']
})
export class SuperAdminDashboardComponent implements OnInit {
    properties: any[] = [];
    propertyForm: FormGroup;
    showAddModal: boolean = false;
    showConfirmModal: boolean = false;
    modalStep: 'confirm' | 'success' = 'confirm';
    propertyToToggle: any = null;
    user: any;

    constructor(private fb: FormBuilder, private api: ApiService, private auth: AuthService) {
        this.user = this.auth.currentUserValue;
        this.propertyForm = this.fb.group({
            propertyName: ['', [Validators.required, Validators.maxLength(30)]],
            address: ['', [Validators.required, Validators.maxLength(30)]],
            ownerName: ['', [Validators.required, Validators.maxLength(30)]],
            adminUsername: ['', [Validators.required, Validators.maxLength(10)]],
            adminMobile: ['', [Validators.required, Validators.pattern('^[0-9]{10}$')]]
        });
    }

    ngOnInit() {
        this.loadProperties();
    }

    loadProperties() {
        this.api.getProperties().subscribe((data: any) => {
            console.log('Properties data:', data);
            this.properties = data;
        });
    }

    openAddModal() {
        this.showAddModal = true;
    }

    closeModal() {
        this.showAddModal = false;
        this.propertyForm.reset();
    }

    addProperty() {
        if (this.propertyForm.valid) {
            this.api.addProperty(this.propertyForm.value).subscribe(() => {
                this.closeModal();
                this.loadProperties();
            });
        }
    }

    toggleProperty(id: number) {
        const prop = this.properties.find((p: any) => p.id === id);
        if (prop) {
            this.propertyToToggle = prop;
            this.modalStep = 'confirm';
            this.showConfirmModal = true;
        }
    }

    confirmToggle() {
        if (this.propertyToToggle) {
            this.api.toggleProperty(this.propertyToToggle.id).subscribe(() => {
                this.modalStep = 'success';
                this.loadProperties();
            });
        }
    }

    closeConfirmModal() {
        this.showConfirmModal = false;
        this.propertyToToggle = null;
        this.modalStep = 'confirm';
    }
}
