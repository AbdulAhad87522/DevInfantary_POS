import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

// ============================================
// INTERFACES
// ============================================

export interface ReturnItem {
  returnItemId: number;
  returnId: number;
  variantId: number;
  quantity: number;
  conditionNote: string;
  productName: string;
  size: string;
  unitOfMeasure: string;
  unitPrice: number;
  lineTotal: number;
}

export interface Return {
  returnId: number;
  billId: number;
  customerId: number;
  returnDate: string;
  refundAmount: number;
  statusId: number;
  reason: string;
  notes: string;
  createdAt: string;
  updatedAt: string;
  billNumber: string;
  customerName: string;
  status: string;
  items: ReturnItem[];
}

// Bill search se jo aayega
export interface BillItem {
  billItemId: number;
  billId: number;
  productId: number;
  variantId: number;
  productName: string;
  size: string;
  unitOfMeasure: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  notes: string;
}

export interface BillDetail {
  billId: number;
  billNumber: string;
  customerName: string;
  billDate: string;
  totalAmount: number;
  items: BillItem[];
}

// POST ke liye request body
export interface CreateReturnRequest {
  billId: number;
  refundAmount: number;
  reason: string;
  notes: string;
  restoreStock: boolean;
  items: {
    variantId: number;
    productName: string;
    size: string;
    unit: string;
    quantity: number;
    unitPrice: number;
    lineTotal: number;
    maxQuantity: number;
  }[];
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[];
  timestamp: string;
}

// ============================================
// SERVICE
// ============================================

@Injectable({
  providedIn: 'root'
})
export class ReturnsService {

  private baseUrl = 'http://localhost:5050/api/Returns';

  constructor(private http: HttpClient) {}

  // Bill number se bill ka data lao (items table fill hoga isi se)
  getBillByNumber(billNumber: string): Observable<ApiResponse<BillDetail>> {
    return this.http.get<ApiResponse<BillDetail>>(`${this.baseUrl}/bill/${billNumber}`);
  }

  // Return submit karo
  createReturn(data: CreateReturnRequest): Observable<ApiResponse<Return>> {
    return this.http.post<ApiResponse<Return>>(`${this.baseUrl}`, data);
  }

  // Saare returns dekho (agar list page chahiye future mein)
  getAllReturns(): Observable<ApiResponse<Return[]>> {
    return this.http.get<ApiResponse<Return[]>>(`${this.baseUrl}`);
  }

  // Single return dekho
  getReturnById(id: number): Observable<ApiResponse<Return>> {
    return this.http.get<ApiResponse<Return>>(`${this.baseUrl}/${id}`);
  }
}