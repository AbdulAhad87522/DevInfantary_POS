import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

// ===== INTERFACES (Swagger se match) =====

export interface BillItem {
  billItemId: number;
  billId: number;
  productId: number;
  variantId: number;
  quantity: number;
  unitOfMeasure: string;
  unitPrice: number;
  lineTotal: number;
  notes: string;
  productName: string;
  size: string;
}

export interface Bill {
  billId: number;
  billNumber: string;
  customerId: number;
  staffId: number;
  billDate: string;
  subtotal: number;
  discountPercentage: number;
  discountAmount: number;
  taxPercentage: number;
  taxAmount: number;
  totalAmount: number;
  amountPaid: number;
  amountDue: number;
  paymentStatusId: number;
  createdAt: string;
  updatedAt: string;
  customerName: string;
  paymentStatus: string;
  items: BillItem[];
}

export interface CreateBillItemDto {
  productName: string;
  size: string;
  quantity: number;
  unitPrice: number;
  discount: number;
}

export interface CreateBillDto {
  customerId: number;
  billDate: string;
  totalAmount: number;
  paidAmount: number;
  discountAmount: number;
  items: CreateBillItemDto[];
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[] | null;
  timestamp: string;
}

export interface PaginatedBillResponse {
  success: boolean;
  message: string;
  data: Bill[];
  totalRecords: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface BillSearchDto {
  startDate?: string;
  endDate?: string;
  customerId?: number;
  billNumber?: string;
  paymentStatus?: string;
}

// ===== SERVICE =====

@Injectable({
  providedIn: 'root'
})
export class BillService {
  private apiUrl = `${environment.apiUrl}/Bills`;

  constructor(private http: HttpClient) { }

  // GET /api/Bills - Saare bills
  getAllBills(): Observable<ApiResponse<Bill[]>> {
    return this.http.get<ApiResponse<Bill[]>>(this.apiUrl);
  }

  // GET /api/Bills/paginated
  getBillsPaginated(
    pageNumber: number = 1,
    pageSize: number = 10,
    filters?: { startDate?: string; endDate?: string; customerId?: number; billNumber?: string; paymentStatus?: string }
  ): Observable<PaginatedBillResponse> {
    let url = `${this.apiUrl}/paginated?pageNumber=${pageNumber}&pageSize=${pageSize}`;
    if (filters?.startDate) url += `&startDate=${filters.startDate}`;
    if (filters?.endDate) url += `&endDate=${filters.endDate}`;
    if (filters?.customerId) url += `&customerId=${filters.customerId}`;
    if (filters?.billNumber) url += `&billNumber=${filters.billNumber}`;
    if (filters?.paymentStatus) url += `&paymentStatus=${filters.paymentStatus}`;
    return this.http.get<PaginatedBillResponse>(url);
  }

  // GET /api/Bills/{id}
  getBillById(id: number): Observable<ApiResponse<Bill>> {
    return this.http.get<ApiResponse<Bill>>(`${this.apiUrl}/${id}`);
  }

  // GET /api/Bills/number/{billNumber}
  getBillByNumber(billNumber: string): Observable<ApiResponse<Bill>> {
    return this.http.get<ApiResponse<Bill>>(`${this.apiUrl}/number/${billNumber}`);
  }

  // GET /api/Bills/customer/{customerId}
  getBillsByCustomer(customerId: number): Observable<ApiResponse<Bill[]>> {
    return this.http.get<ApiResponse<Bill[]>>(`${this.apiUrl}/customer/${customerId}`);
  }

  // GET /api/Bills/pending
  getPendingBills(): Observable<ApiResponse<Bill[]>> {
    return this.http.get<ApiResponse<Bill[]>>(`${this.apiUrl}/pending`);
  }

  // GET /api/Bills/customer/{customerId}/outstanding
  getCustomerOutstanding(customerId: number): Observable<ApiResponse<number>> {
    return this.http.get<ApiResponse<number>>(`${this.apiUrl}/customer/${customerId}/outstanding`);
  }

  // POST /api/Bills
  createBill(billData: CreateBillDto): Observable<ApiResponse<Bill>> {
    return this.http.post<ApiResponse<Bill>>(this.apiUrl, billData);
  }

  // POST /api/Bills/search
  searchBills(searchDto: BillSearchDto): Observable<ApiResponse<Bill[]>> {
    return this.http.post<ApiResponse<Bill[]>>(`${this.apiUrl}/search`, searchDto);
  }
}