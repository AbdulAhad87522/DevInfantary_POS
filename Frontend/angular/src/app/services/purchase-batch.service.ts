import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

// ===== INTERFACES =====

export interface PurchaseBatchItem {
  purchaseBatchItemId: number;
  purchaseBatchId: number;
  variantId: number;
  quantityReceived: number;
  costPrice: number;
  lineTotal: number;
  createdAt: string;
  productName: string;
  size: string;
  classType: string;
  salePrice: number;
}

export interface PurchaseBatch {
  batchId: number;
  supplierId: number;
  batchName: string;
  totalPrice: number;
  paid: number;
  remaining: number;
  status: string;
  createdAt: string;
  supplierName: string;
  items: PurchaseBatchItem[];
}

export interface VariantOption {
  variantId: number;
  productName: string;
  size: string;
  classType: string;
  salePrice: number;
  quantityInStock: number;
}

export interface CreateBatchItemDto {
  variantId: number;
  quantityReceived: number;
  costPrice: number;
  salePrice: number;
}

export interface CreateBatchDto {
  supplierId: number;
  batchName: string;
  totalPrice: number;
  paid: number;
  status: string;
  items: CreateBatchItemDto[];
}

export interface UpdateBatchDto {
  supplierId: number;
  batchName: string;
  totalPrice: number;
  paid: number;
  status: string;
}

export interface BatchSearchDto {
  startDate?: string;
  endDate?: string;
  supplierId?: number;
  batchName?: string;
  status?: string;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[] | null;
  timestamp: string;
}

export interface PaginatedBatchResponse {
  success: boolean;
  message: string;
  data: PurchaseBatch[];
  totalRecords: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

// ===== SERVICE =====

@Injectable({
  providedIn: 'root'
})
export class PurchaseBatchService {
  private apiUrl = `${environment.apiUrl}/PurchaseBatches`;

  constructor(private http: HttpClient) { }

  // GET /api/PurchaseBatches
  getAllBatches(): Observable<ApiResponse<PurchaseBatch[]>> {
    return this.http.get<ApiResponse<PurchaseBatch[]>>(this.apiUrl);
  }

  // GET /api/PurchaseBatches/paginated
  getBatchesPaginated(
    pageNumber: number = 1,
    pageSize: number = 10,
    filters?: { startDate?: string; endDate?: string; supplierId?: number; batchName?: string; status?: string }
  ): Observable<PaginatedBatchResponse> {
    let url = `${this.apiUrl}/paginated?pageNumber=${pageNumber}&pageSize=${pageSize}`;
    if (filters?.supplierId) url += `&supplierId=${filters.supplierId}`;
    if (filters?.batchName)  url += `&batchName=${filters.batchName}`;
    if (filters?.status)     url += `&status=${filters.status}`;
    if (filters?.startDate)  url += `&startDate=${filters.startDate}`;
    if (filters?.endDate)    url += `&endDate=${filters.endDate}`;
    return this.http.get<PaginatedBatchResponse>(url);
  }

  // GET /api/PurchaseBatches/{id}
  // Returns full batch with items[] included — used for Detail View
  getBatchById(id: number): Observable<ApiResponse<PurchaseBatch>> {
    return this.http.get<ApiResponse<PurchaseBatch>>(`${this.apiUrl}/${id}`);
  }

  // POST /api/PurchaseBatches
  createBatch(batchData: CreateBatchDto): Observable<ApiResponse<PurchaseBatch>> {
    return this.http.post<ApiResponse<PurchaseBatch>>(this.apiUrl, batchData);
  }

  // PUT /api/PurchaseBatches/{id}
  updateBatch(id: number, batchData: UpdateBatchDto): Observable<ApiResponse<boolean>> {
    return this.http.put<ApiResponse<boolean>>(`${this.apiUrl}/${id}`, batchData);
  }

  // DELETE /api/PurchaseBatches/{id}
  deleteBatch(id: number): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.apiUrl}/${id}`);
  }

  // GET /api/PurchaseBatches/variants?search=...
  getVariants(search: string = ''): Observable<ApiResponse<VariantOption[]>> {
    const url = search
      ? `${this.apiUrl}/variants?search=${search}`
      : `${this.apiUrl}/variants`;
    return this.http.get<ApiResponse<VariantOption[]>>(url);
  }

  // GET /api/PurchaseBatches/next-id
  getNextId(): Observable<ApiResponse<number>> {
    return this.http.get<ApiResponse<number>>(`${this.apiUrl}/next-id`);
  }

  // POST /api/PurchaseBatches/search
  searchBatches(searchDto: BatchSearchDto): Observable<ApiResponse<PurchaseBatch[]>> {
    return this.http.post<ApiResponse<PurchaseBatch[]>>(`${this.apiUrl}/search`, searchDto);
  }

  // GET /api/PurchaseBatches/supplier/{supplierId}
  getBatchesBySupplier(supplierId: number): Observable<ApiResponse<PurchaseBatch[]>> {
    return this.http.get<ApiResponse<PurchaseBatch[]>>(`${this.apiUrl}/supplier/${supplierId}`);
  }

  // GET /api/PurchaseBatches/pending
  getPendingBatches(): Observable<ApiResponse<PurchaseBatch[]>> {
    return this.http.get<ApiResponse<PurchaseBatch[]>>(`${this.apiUrl}/pending`);
  }
}