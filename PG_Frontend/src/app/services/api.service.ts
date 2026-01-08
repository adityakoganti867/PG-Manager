
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class ApiService {
    private baseUrl = 'https://pgapi-acg2g7a6c0gmcjey.centralindia-01.azurewebsites.net/api';

    constructor(private http: HttpClient) { }

    private getHeaders(): { [header: string]: string } {
        const userStr = localStorage.getItem('user');
        if (userStr) {
            const user = JSON.parse(userStr);
            if (user && user.id) {
                return { 'X-Admin-Id': user.id.toString() };
            }
        }
        return {};
    }

    // Admin
    addSupervisor(data: any) { return this.http.post(`${this.baseUrl}/admin/add-supervisor`, data, { headers: this.getHeaders(), responseType: 'text' }); }
    getSupervisors() { return this.http.get(`${this.baseUrl}/admin/supervisors`, { headers: this.getHeaders() }); }
    toggleSupervisor(id: number) { return this.http.post(`${this.baseUrl}/admin/toggle-supervisor/${id}`, {}, { headers: this.getHeaders() }); }
    addGuest(data: any) { return this.http.post(`${this.baseUrl}/admin/add-guest`, data, { headers: this.getHeaders(), responseType: 'text' }); }
    toggleGuest(id: number) { return this.http.post(`${this.baseUrl}/admin/toggle-guest/${id}`, {}, { headers: this.getHeaders() }); }

    // Room Management
    addRoom(data: any) { return this.http.post(`${this.baseUrl}/admin/add-room`, data, { headers: this.getHeaders(), responseType: 'text' }); }
    getRooms() { return this.http.get(`${this.baseUrl}/admin/rooms`, { headers: this.getHeaders() }); }
    getAvailableRooms(share: number, floor?: number, roomType?: string) {
        let url = `${this.baseUrl}/admin/available-rooms?share=${share}`;
        if (floor) url += `&floor=${floor}`;
        if (roomType) url += `&roomType=${roomType}`;
        return this.http.get(url, { headers: this.getHeaders() });
    }

    getAllGuests() { return this.http.get(`${this.baseUrl}/admin/guests`, { headers: this.getHeaders() }); }

    // Complaints (Unified)
    getComplaints() { return this.http.get(`${this.baseUrl}/complaints`, { headers: this.getHeaders() }); }
    getMyComplaints(userId: number) { return this.http.get(`${this.baseUrl}/complaints/my/${userId}`); }
    raiseComplaint(data: any) { return this.http.post(`${this.baseUrl}/complaints`, data); }
    updateComplaintStatus(id: number, data: any) { return this.http.put(`${this.baseUrl}/complaints/${id}/status`, data, { headers: this.getHeaders() }); }
    cancelComplaint(id: number) { return this.http.delete(`${this.baseUrl}/complaints/${id}`); }

    // Supervisor
    getSupervisorComplaints() { return this.getComplaints(); } // Reusing unified endpoint

    // Guest
    getProfile(userId: number) { return this.http.get(`${this.baseUrl}/guest/profile/${userId}`); }
    initiateNotice(userId: number) { return this.http.post(`${this.baseUrl}/guest/initiate-notice/${userId}`, {}, { responseType: 'text' }); }

    // Payment (Free UPI)
    createOrder(guestId: number, amount: number) {
        return this.http.post(`${this.baseUrl}/payment/create-order-for-guest`, { guestId, amount });
    }
    verifyPayment(guestId: number, utr: string, amount: number) {
        return this.http.post(`${this.baseUrl}/payment/verify`, { guestId, utr, amount });
    }

    getPendingPayments() { return this.http.get(`${this.baseUrl}/payment/pending`, { headers: this.getHeaders() }); }
    approvePayment(txId: number) { return this.http.post(`${this.baseUrl}/payment/approve/${txId}`, {}, { headers: this.getHeaders() }); }
    rejectPayment(txId: number) { return this.http.post(`${this.baseUrl}/payment/reject/${txId}`, {}, { headers: this.getHeaders() }); }

    getPaymentHistory(guestId: number) { return this.http.get(`${this.baseUrl}/payment/history/${guestId}`); }
    getAllTransactions() { return this.http.get(`${this.baseUrl}/payment/history/all`, { headers: this.getHeaders() }); }

    // Settings
    getSettings() { return this.http.get(`${this.baseUrl}/settings`, { headers: this.getHeaders() }); }
    updateUpiSettings(data: { upiId: string, upiName: string }) {
        return this.http.post(`${this.baseUrl}/settings/update-upi`, data, { headers: this.getHeaders() });
    }

    approveNotice(guestId: number) {
        return this.http.post(`${this.baseUrl}/admin/approve-notice/${guestId}`, {}, { headers: this.getHeaders(), responseType: 'text' });
    }

    revertNotice(guestId: number) {
        return this.http.post(`${this.baseUrl}/admin/revert-notice/${guestId}`, {}, { headers: this.getHeaders(), responseType: 'text' });
    }

    resetPassword(userId: number) {
        return this.http.post(`${this.baseUrl}/admin/reset-password/${userId}`, {}, { headers: this.getHeaders(), responseType: 'text' });
    }

    // SuperAdmin
    getProperties() { return this.http.get(`${this.baseUrl}/superadmin/properties`, { headers: this.getHeaders() }); }
    addProperty(data: any) { return this.http.post(`${this.baseUrl}/superadmin/add-property`, data, { headers: this.getHeaders() }); }
    toggleProperty(id: number) { return this.http.post(`${this.baseUrl}/superadmin/toggle-property/${id}`, {}, { headers: this.getHeaders() }); }
}

