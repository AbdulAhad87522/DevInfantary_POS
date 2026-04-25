import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

// ============================================
// INTERFACES - API se jo data aayega uski shape
// ============================================

export interface SupplierSummary {
  supplierId: number;
  supplierName: string;
  totalPrice: number;
  paid: number;
  remaining: number;
  status: string;
  batchCount: number;
}

export interface BatchItem {
  purchaseBatchItemId: number;
  purchaseBatchId: number;
  variantId: number;
  productName: string;
  size: string;
  classType: string;
  quantityReceived: number;
  costPrice: number;
  salePrice: number;
  lineTotal: number;
  createdAt: string;
}

export interface Batch {
  batchId: number;
  supplierId: number;
  batchName: string;
  totalPrice: number;
  paid: number;
  remaining: number;
  status: string;
  createdAt: string;
  items: BatchItem[];
}

export interface Payment {
  paymentId: number;
  supplierId: number;
  batchId: number;
  paymentAmount: number;
  paymentDate: string;
  remarks: string;
  createdAt: string;
  supplierName: string;
  batchName: string;
}

export interface PaymentRequest {
  supplierId: number;
  paymentAmount: number;
  paymentDate: string;
  remarks: string;
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
  providedIn: 'root',
})
export class SupplierBillsService {
  private baseUrl = `${environment.apiUrl}/SupplierBills`;

  constructor(private http: HttpClient) {}

  getSummaries(search?: string): Observable<ApiResponse<SupplierSummary[]>> {
    let params = new HttpParams();
    if (search && search.trim() !== '') {
      params = params.set('search', search);
    }
    return this.http.get<ApiResponse<SupplierSummary[]>>(
      `${this.baseUrl}/summaries`,
      { params },
    );
  }

  getSupplierSummary(
    supplierId: number,
  ): Observable<ApiResponse<SupplierSummary>> {
    return this.http.get<ApiResponse<SupplierSummary>>(
      `${this.baseUrl}/supplier/${supplierId}/summary`,
    );
  }

  getSupplierBatches(supplierId: number): Observable<ApiResponse<Batch[]>> {
    return this.http.get<ApiResponse<Batch[]>>(
      `${this.baseUrl}/supplier/${supplierId}/batches`,
    );
  }

  getBatchById(batchId: number): Observable<ApiResponse<Batch>> {
    return this.http.get<ApiResponse<Batch>>(
      `${this.baseUrl}/batch/${batchId}`,
    );
  }

  getSupplierPayments(supplierId: number): Observable<ApiResponse<Payment[]>> {
    return this.http.get<ApiResponse<Payment[]>>(
      `${this.baseUrl}/supplier/${supplierId}/payments`,
    );
  }

  getBatchPayments(batchId: number): Observable<ApiResponse<Payment[]>> {
    return this.http.get<ApiResponse<Payment[]>>(
      `${this.baseUrl}/batch/${batchId}/payments`,
    );
  }

  addPayment(payment: PaymentRequest): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.baseUrl}/payment`, payment);
  }
}