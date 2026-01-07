
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class ApiService {
    private baseUrl = 'http://localhost:5122/api';

    constructor(private http: HttpClient) { }

    // Admin
    addSupervisor(data: any) { return this.http.post(`${this.baseUrl}/admin/add-supervisor`, data, { responseType: 'text' }); }
    getSupervisors() { return this.http.get(`${this.baseUrl}/admin/supervisors`); }
    toggleSupervisor(id: number) { return this.http.post(`${this.baseUrl}/admin/toggle-supervisor/${id}`, {}); }
    addGuest(data: any) { return this.http.post(`${this.baseUrl}/admin/add-guest`, data, { responseType: 'text' }); }
    toggleGuest(id: number) { return this.http.post(`${this.baseUrl}/admin/toggle-guest/${id}`, {}); }

    // Room Management
    addRoom(data: any) { return this.http.post(`${this.baseUrl}/admin/add-room`, data, { responseType: 'text' }); }
    getRooms() { return this.http.get(`${this.baseUrl}/admin/rooms`); }
    getAvailableRooms(share: number, floor?: number, roomType?: string) {
        let url = `${this.baseUrl}/admin/available-rooms?share=${share}`;
        if (floor) url += `&floor=${floor}`;
        if (roomType) url += `&roomType=${roomType}`;
        return this.http.get(url);
    }

    getAllGuests() { return this.http.get(`${this.baseUrl}/admin/guests`); }

    // Complaints (Unified)
    getComplaints() { return this.http.get(`${this.baseUrl}/complaints`); }
    getMyComplaints(userId: number) { return this.http.get(`${this.baseUrl}/complaints/my/${userId}`); }
    raiseComplaint(data: any) { return this.http.post(`${this.baseUrl}/complaints`, data); }
    updateComplaintStatus(id: number, data: any) { return this.http.put(`${this.baseUrl}/complaints/${id}/status`, data); }
    cancelComplaint(id: number) { return this.http.delete(`${this.baseUrl}/complaints/${id}`); }

    // Supervisor
    getSupervisorComplaints() { return this.getComplaints(); } // Reusing unified endpoint

    // Guest

    getProfile(userId: number) { return this.http.get(`${this.baseUrl}/guest/profile/${userId}`); }
    initiateNotice(userId: number) { return this.http.post(`${this.baseUrl}/guest/initiate-notice/${userId}`, {}, { responseType: 'text' }); }

    // Payment
    createOrder(amount: number) { return this.http.post(`${this.baseUrl}/payment/create-order`, { amount }); }
    verifyPayment(data: any) { return this.http.post(`${this.baseUrl}/payment/verify`, data); }

    getPaymentHistory(guestId: number) { return this.http.get(`${this.baseUrl}/payment/history/${guestId}`); }
    getAllTransactions() { return this.http.get(`${this.baseUrl}/payment/history/all`); }


    approveNotice(guestId: number) {
        return this.http.post(`${this.baseUrl}/admin/approve-notice/${guestId}`, {}, { responseType: 'text' });
    }

    revertNotice(guestId: number) {
        return this.http.post(`${this.baseUrl}/admin/revert-notice/${guestId}`, {}, { responseType: 'text' });
    }

    resetPassword(userId: number) {
        return this.http.post(`${this.baseUrl}/admin/reset-password/${userId}`, {}, { responseType: 'text' });
    }
}

