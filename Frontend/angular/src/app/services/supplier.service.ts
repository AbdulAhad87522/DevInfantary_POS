import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Supplier {
  supplierId: number;
  name: string;
  contact: string;
  address: string;
  accountBalance: number;
  isActive: boolean;
  createdAt: Date;
  updatedAt: Date;
  notes: string;
}

export interface SupplierDto {
  name: string;
  contact: string;
  address: string;
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
export class SupplierService {
  private apiUrl = `${environment.apiUrl}/Suppliers`;

  constructor(private http: HttpClient) { }

  // Get all suppliers
  getAllSuppliers(includeInactive: boolean = false): Observable<ApiResponse<Supplier[]>> {
    return this.http.get<ApiResponse<Supplier[]>>(`${this.apiUrl}?includeInactive=${includeInactive}`);
  }

  // Get supplier by ID
  getSupplierById(id: number): Observable<ApiResponse<Supplier>> {
    return this.http.get<ApiResponse<Supplier>>(`${this.apiUrl}/${id}`);
  }

  // Create new supplier
  createSupplier(supplierData: any): Observable<ApiResponse<Supplier>> {
    const supplierDto: SupplierDto = {
      name: supplierData.name,
      contact: supplierData.contact || '',
      address: supplierData.address || '',
      notes: supplierData.notes || ''
    };
    
    return this.http.post<ApiResponse<Supplier>>(this.apiUrl, supplierDto);
  }

  // Update supplier
  updateSupplier(id: number, supplierData: any): Observable<ApiResponse<Supplier>> {
    return this.http.put<ApiResponse<Supplier>>(`${this.apiUrl}/${id}`, supplierData);
  }

  // Delete supplier (soft delete)
  deleteSupplier(id: number): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.apiUrl}/${id}`);
  }

  // Restore supplier
  restoreSupplier(id: number): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/${id}/restore`, {});
  }

  // Search suppliers
  searchSuppliers(term: string): Observable<ApiResponse<Supplier[]>> {
    return this.http.get<ApiResponse<Supplier[]>>(`${this.apiUrl}/search?term=${term}`);
  }

  // Get supplier balance
  getSupplierBalance(id: number): Observable<ApiResponse<number>> {
    return this.http.get<ApiResponse<number>>(`${this.apiUrl}/${id}/balance`);
  }

  // Update supplier balance
  updateSupplierBalance(id: number, amount: number): Observable<ApiResponse<any>> {
    return this.http.patch<ApiResponse<any>>(`${this.apiUrl}/${id}/balance`, { amount });
  }
}