import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

// ===== INTERFACES =====

export interface QuotationItem {
  quotationItemId: number;
  quotationId: number;
  productId: number;
  variantId: number;
  quantity: number;
  unitOfMeasure: string;
  unitPrice: number;
  lineTotal: number;
  notes: string;
  productName: string;
  size: string;
  classType: string;
  availableStock: number;
  supplierName: string;
  category: string;
}

export interface Quotation {
  quotationId: number;
  quotationNumber: string;
  customerId: number;
  staffId: number;
  quotationDate: string;
  validUntil: string;
  subtotal: number;
  discountPercentage: number;
  discountAmount: number;
  taxPercentage: number;
  taxAmount: number;
  totalAmount: number;
  statusId: number;
  convertedBillId: number;
  notes: string;
  termsConditions: string;
  createdAt: string;
  updatedAt: string;
  customerName: string;
  customerContact: string;
  staffName: string;
  status: string;
  items: QuotationItem[];
}

export interface CreateQuotationRequest {
  customerId: number;
  quotationDate: string;
  totalAmount: number;
  discountAmount: number;
  validUntil: string;
  notes: string;
  termsConditions: string;
  items: {
    productName: string;
    size: string;
    quantity: number;
    unitPrice: number;
    notes: string;
  }[];
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[];
  timestamp: string;
}

// ===== SERVICE =====

@Injectable({
  providedIn: 'root'
})
export class QuotationService {

  private baseUrl = 'http://localhost:5050/api/Quotations';

  constructor(private http: HttpClient) {}

  // Saari quotations
  getAllQuotations(): Observable<ApiResponse<Quotation[]>> {
    return this.http.get<ApiResponse<Quotation[]>>(`${this.baseUrl}`);
  }

  // Single quotation by ID
  getQuotationById(id: number): Observable<ApiResponse<Quotation>> {
    return this.http.get<ApiResponse<Quotation>>(`${this.baseUrl}/${id}`);
  }

  // Quotation by number (e.g. "QT-001")
  getQuotationByNumber(number: string): Observable<ApiResponse<Quotation>> {
    return this.http.get<ApiResponse<Quotation>>(`${this.baseUrl}/number/${number}`);
  }

  // Search by value
  searchQuotation(searchValue: string): Observable<ApiResponse<Quotation>> {
    return this.http.get<ApiResponse<Quotation>>(`${this.baseUrl}/search/${searchValue}`);
  }
  getQuotationPdfById(quotationId: number): Observable<Blob> {
  return this.http.get(
    `${this.baseUrl}/${quotationId}/pdf`,
    { responseType: 'blob' }
  );
}
// Quotation number se PDF (NEW API)
getQuotationPdfByNumber(quotationNumber: string): Observable<Blob> {
  return this.http.get(
    `${this.baseUrl}/number/${quotationNumber}/pdf`,
    { responseType: 'blob' }
  );
}

  // Customer ki quotations
  getCustomerQuotations(customerId: number): Observable<ApiResponse<Quotation[]>> {
    return this.http.get<ApiResponse<Quotation[]>>(`${this.baseUrl}/customer/${customerId}`);
  }

  // Pending quotations
  getPendingQuotations(): Observable<ApiResponse<Quotation[]>> {
    return this.http.get<ApiResponse<Quotation[]>>(`${this.baseUrl}/pending`);
  }

  // Nai quotation banao
  createQuotation(data: CreateQuotationRequest): Observable<ApiResponse<Quotation>> {
    return this.http.post<ApiResponse<Quotation>>(`${this.baseUrl}`, data);
  }
}