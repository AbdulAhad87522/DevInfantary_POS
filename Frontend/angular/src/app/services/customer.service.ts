import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Customer {
  customerId: number;
  fullName: string;
  phone: string;
  address: string;
  currentBalance: number;
  customerType: string;
  isActive: boolean;
  createdAt: Date;
  updatedAt: Date;
  notes: string;
}

export interface CustomerDto {
  fullName: string;
  phone: string;
  address: string;
  customerType: string;
  notes?: string;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[] | null;
  timestamp: string;
}

@Injectable({
  providedIn: 'root'
})
export class CustomerService {
  private apiUrl = `${environment.apiUrl}/Customers`;

  constructor(private http: HttpClient) { }

  // Get all customers
  getAllCustomers(includeInactive: boolean = false): Observable<ApiResponse<Customer[]>> {
    return this.http.get<ApiResponse<Customer[]>>(`${this.apiUrl}?includeInactive=${includeInactive}`);
  }

  // Get customer by ID
  getCustomerById(id: number): Observable<ApiResponse<Customer>> {
    return this.http.get<ApiResponse<Customer>>(`${this.apiUrl}/${id}`);
  }

  // Create new customer
  createCustomer(customerData: any): Observable<ApiResponse<Customer>> {
    const customerDto: CustomerDto = {
      fullName: customerData.fullName,
      phone: customerData.phone || '',
      address: customerData.address || '',
      customerType: customerData.customerType || 'retail',
      notes: customerData.notes || ''
    };
    
    return this.http.post<ApiResponse<Customer>>(this.apiUrl, customerDto);
  }

  // Update customer
  updateCustomer(id: number, customerData: any): Observable<ApiResponse<Customer>> {
    return this.http.put<ApiResponse<Customer>>(`${this.apiUrl}/${id}`, customerData);
  }

  // Delete customer (soft delete)
  deleteCustomer(id: number): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.apiUrl}/${id}`);
  }

  // Restore customer
  restoreCustomer(id: number): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/${id}/restore`, {});
  }

  // Search customers
  searchCustomers(term: string): Observable<ApiResponse<Customer[]>> {
    return this.http.get<ApiResponse<Customer[]>>(`${this.apiUrl}/search?term=${term}`);
  }

  // Get customer balance
  getCustomerBalance(id: number): Observable<ApiResponse<number>> {
    return this.http.get<ApiResponse<number>>(`${this.apiUrl}/${id}/balance`);
  }

  // Update customer balance
  updateCustomerBalance(id: number, amount: number): Observable<ApiResponse<any>> {
    return this.http.patch<ApiResponse<any>>(`${this.apiUrl}/${id}/balance`, { amount });
  }
}