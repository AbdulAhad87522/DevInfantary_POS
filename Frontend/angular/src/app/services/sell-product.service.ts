import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

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

export interface Customer {
  customerId: number;
  name: string;
  contact: string;
  address: string;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[];
  timestamp: string;
}

@Injectable({ providedIn: 'root' })
export class SellProductService {
  private billsUrl = 'http://localhost:5050/api/Bills';
  private customersUrl = 'http://localhost:5050/api/Customers';
  private productsUrl = 'http://localhost:5050/api/Products';
  private quotationsUrl = 'http://localhost:5050/api/Quotations';

  constructor(private http: HttpClient) {}

  createBill(data: any): Observable<ApiResponse<any>> {
  return this.http.post<ApiResponse<any>>(`${this.billsUrl}`, data);
}

  searchBills(data: any): Observable<ApiResponse<Bill[]>> {
    return this.http.post<ApiResponse<Bill[]>>(`${this.billsUrl}/search`, data);
  }

  getQuotationByNumber(number: string): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(
      `${this.quotationsUrl}/number/${number}`,
    );
  }
  getBillPdfById(billId: number): Observable<Blob> {
  return this.http.get(
    `${this.billsUrl}/${billId}/pdf`,
    { responseType: 'blob' }
  );
}

  getAllCustomers(): Observable<ApiResponse<any[]>> {
    return this.http.get<ApiResponse<any[]>>(`${this.customersUrl}`);
  }

  getProductVariants(productId: number): Observable<ApiResponse<any[]>> {
    return this.http.get<ApiResponse<any[]>>(
      `${this.productsUrl}/${productId}/variants`,
    );
  }

  getBillPdf(billNumber: string): Observable<ApiResponse<string>> {
    return this.http.get<ApiResponse<string>>(
      `${this.billsUrl}/number/${billNumber}/pdf`,
    );
  }
}