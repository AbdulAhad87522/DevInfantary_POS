import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

// ===== INTERFACES =====
// Interfaces — file ke top pr existing interfaces ke saath add karo

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

export interface BillReturn {
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
export interface CustomerBillSummary {
  customerId: number;
  customerName: string;
  totalAmount: number;
  paid: number;
  remaining: number;
  billCount: number;
}

export interface CustomerBillItem {
  billItemId: number;
  productName: string;
  size: string;
  unitOfMeasure: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface CustomerBillDetail {
  billId: number;
  billNumber: string;
  customerId: number;
  customerName: string;
  billDate: string;
  totalAmount: number;
  amountPaid: number;
  amountDue: number;
  paymentStatus: string;
  items: CustomerBillItem[];
}

export interface PaymentAllocation {
  id: number;
  referenceNumber: string;
  dueBefore: number;
  paymentApplied: number;
  dueAfter: number;
}

export interface PaymentResult {
  totalPayment: number;
  applied: number;
  remaining: number;
  allocations: PaymentAllocation[];
}

export interface RecordPaymentDto {
  customerId: number;
  paymentAmount: number;
  remarks: string;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[] | null;
  timestamp: string;
}

// ===== SERVICE =====

@Injectable({
  providedIn: 'root'
})
export class CustomerBillsService {
  private apiUrl = `${environment.apiUrl}/CustomerBills`;

  constructor(private http: HttpClient) { }

  // GET /api/CustomerBills/summaries?search=...
  getAllSummaries(search: string = ''): Observable<ApiResponse<CustomerBillSummary[]>> {
    const url = search 
      ? `${this.apiUrl}/summaries?search=${search}` 
      : `${this.apiUrl}/summaries`;
    return this.http.get<ApiResponse<CustomerBillSummary[]>>(url);
  }

  // GET /api/CustomerBills/customer/{customerId}/summary
  getCustomerSummary(customerId: number): Observable<ApiResponse<CustomerBillSummary>> {
    return this.http.get<ApiResponse<CustomerBillSummary>>(`${this.apiUrl}/customer/${customerId}/summary`);
  }

  // GET /api/CustomerBills/customer/{customerId}
  getCustomerBills(customerId: number): Observable<ApiResponse<CustomerBillDetail[]>> {
    return this.http.get<ApiResponse<CustomerBillDetail[]>>(`${this.apiUrl}/customer/${customerId}`);
  }

  // GET /api/CustomerBills/bill/{billId}
  getBillDetail(billId: number): Observable<ApiResponse<CustomerBillDetail>> {
    return this.http.get<ApiResponse<CustomerBillDetail>>(`${this.apiUrl}/bill/${billId}`);
  }

  // GET /api/CustomerBills/customer/{customerId}/payments
  getCustomerPayments(customerId: number): Observable<ApiResponse<any[]>> {
    return this.http.get<ApiResponse<any[]>>(`${this.apiUrl}/customer/${customerId}/payments`);
  }

  // POST /api/CustomerBills/payment
  recordPayment(paymentData: RecordPaymentDto): Observable<ApiResponse<PaymentResult>> {
    return this.http.post<ApiResponse<PaymentResult>>(`${this.apiUrl}/payment`, paymentData);
  }
  getReturnsByBillId(billId: number): Observable<ApiResponse<BillReturn[]>> {
  return this.http.get<ApiResponse<BillReturn[]>>(
    `${environment.apiUrl}/Returns/bill-id/${billId}`
  );
}
}