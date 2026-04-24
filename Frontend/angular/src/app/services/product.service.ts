import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ProductVariant {
  variantId: number;
  productId: number;
  size: string;
  color: string;
  classType: string;
  unitOfMeasure: string;
  quantityInStock: number;
  pricePerUnit: number;
  pricePerLength: number;
  lengthInFeet: number;       // ✅ Fix: was missing, backend uses lengthInFeet
  reorderLevel: number;
  location: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  notes: string;
}

export interface Product {
  productId: number;
  name: string;
  description: string;
  categoryId: number;
  categoryName: string;
  supplierId: number;
  supplierName: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  notes: string;
  variants: ProductVariant[];
}

export interface Category {
  lookupId: number;
  value: string;
  description: string;
}

export interface Supplier {
  supplierId: number;
  name: string;
  contact: string;
  address: string;
  accountBalance: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  notes: string;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[];
  timestamp: string;
}

// ✅ Correct payload interfaces matching Swagger exactly
export interface CreateProductPayload {
  name: string;
  description: string;
  categoryId: number;
  supplierId: number;
}

export interface CreateVariantPayload {
  size: string;
  classType: string;
  unitOfMeasure: string;
  quantityInStock: number;
  pricePerUnit: number;
  pricePerLength: number;
  lengthInFeet: number;       // ✅ Fix: was lengthValue in component
  reorderLevel: number;
  color?: string;
  location?: string;
  notes?: string;
}

export interface UpdateVariantPayload {
  variantId: number;
  size: string;
  classType: string;
  unitOfMeasure: string;
  quantityInStock: number;
  pricePerUnit: number;
  pricePerLength: number;
  lengthInFeet: number;       // ✅ Fix: was lengthValue
  reorderLevel: number;
  isActive: boolean;
  color?: string;
  location?: string;
  notes?: string;
}

// POS Search interfaces
export interface PosSearchRequest {
  searchTerm: string;
  maxResults?: number;
  categoryId?: number;
}

export interface PosVariantResult {
  variantId: number;
  size: string;
  classType: string;
  unitOfMeasure: string;
  quantityInStock: number;
  pricePerUnit: number;
  pricePerLength: number;
  lengthInFeet: number;
  inStock: boolean;
  displayText: string;
}

export interface PosProductResult {
  productId: number;
  productName: string;
  description: string;
  categoryName: string;
  supplierName: string;
  variants: PosVariantResult[];
}

// Advanced Search
export interface ProductSearchRequest {
  searchTerm?: string;
  categoryId?: number;
  supplierId?: number;
  inStock?: boolean;
  lowStock?: boolean;
  minPrice?: number;
  maxPrice?: number;
  includeInactive?: boolean;
  pageNumber?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class ProductService {
  private baseUrl = 'http://localhost:5050/api/Products';
  private supplierUrl = 'http://localhost:5050/api/Suppliers';

  constructor(private http: HttpClient) {}

  // ─── Products ───────────────────────────────────────────────────────────────

  getAllProducts(includeInactive = false): Observable<ApiResponse<Product[]>> {
    return this.http.get<ApiResponse<Product[]>>(
      `${this.baseUrl}?includeInactive=${includeInactive}`
    );
  }

  getProductById(id: number): Observable<ApiResponse<Product>> {
    return this.http.get<ApiResponse<Product>>(`${this.baseUrl}/${id}`);
  }

  // ✅ Fix: payload is ONLY {name, description, categoryId, supplierId} — no variants array
  createProduct(data: CreateProductPayload): Observable<ApiResponse<Product>> {
    return this.http.post<ApiResponse<Product>>(`${this.baseUrl}`, data);
  }

  // ✅ Fix: same minimal payload for update
  updateProduct(id: number, data: CreateProductPayload): Observable<ApiResponse<boolean>> {
    return this.http.put<ApiResponse<boolean>>(`${this.baseUrl}/${id}`, data);
  }

  deleteProduct(id: number): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.baseUrl}/${id}`);
  }

  restoreProduct(id: number): Observable<ApiResponse<boolean>> {
    return this.http.post<ApiResponse<boolean>>(`${this.baseUrl}/${id}/restore`, {});
  }

  // ─── Filters / Views ────────────────────────────────────────────────────────

  getAllCategories(): Observable<ApiResponse<Category[]>> {
    return this.http.get<ApiResponse<Category[]>>(`${this.baseUrl}/categories`);
  }

  getLowStockItems(threshold = 10): Observable<ApiResponse<ProductVariant[]>> {
    return this.http.get<ApiResponse<ProductVariant[]>>(
      `${this.baseUrl}/low-stock?threshold=${threshold}`
    );
  }

  getOutOfStockItems(): Observable<ApiResponse<ProductVariant[]>> {
    return this.http.get<ApiResponse<ProductVariant[]>>(`${this.baseUrl}/out-of-stock`);
  }

  getInventoryValue(): Observable<ApiResponse<number>> {
    return this.http.get<ApiResponse<number>>(`${this.baseUrl}/inventory-value`);
  }

  getByCategory(categoryId: number, includeInactive = false): Observable<ApiResponse<Product[]>> {
    return this.http.get<ApiResponse<Product[]>>(
      `${this.baseUrl}/by-category/${categoryId}?includeInactive=${includeInactive}`
    );
  }

  getBySupplier(supplierId: number, includeInactive = false): Observable<ApiResponse<Product[]>> {
    return this.http.get<ApiResponse<Product[]>>(
      `${this.baseUrl}/by-supplier/${supplierId}?includeInactive=${includeInactive}`
    );
  }

  // ─── Search ─────────────────────────────────────────────────────────────────

  // Advanced inventory search (POST /api/Products/search)
  searchProducts(payload: ProductSearchRequest): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/search`, payload);
  }

  // POS / quick search (POST /api/Products/pos-search)
  posSearch(payload: PosSearchRequest): Observable<ApiResponse<PosProductResult[]>> {
    return this.http.post<ApiResponse<PosProductResult[]>>(
      `${this.baseUrl}/pos-search`,
      payload
    );
  }

  // ─── Variants ───────────────────────────────────────────────────────────────

  getVariantsByProduct(productId: number): Observable<ApiResponse<ProductVariant[]>> {
    return this.http.get<ApiResponse<ProductVariant[]>>(
      `${this.baseUrl}/${productId}/variants`
    );
  }

  getVariantById(variantId: number): Observable<ApiResponse<ProductVariant>> {
    return this.http.get<ApiResponse<ProductVariant>>(
      `${this.baseUrl}/variants/${variantId}`
    );
  }

  // ✅ Fix: payload uses lengthInFeet (not lengthValue), matches Swagger exactly
  createVariant(productId: number, data: CreateVariantPayload): Observable<ApiResponse<ProductVariant>> {
    return this.http.post<ApiResponse<ProductVariant>>(
      `${this.baseUrl}/${productId}/variants`,
      data
    );
  }

  // ✅ Fix: payload uses lengthInFeet and includes isActive
  updateVariant(variantId: number, data: UpdateVariantPayload): Observable<ApiResponse<boolean>> {
    return this.http.put<ApiResponse<boolean>>(
      `${this.baseUrl}/variants/${variantId}`,
      data
    );
  }

  deleteVariant(variantId: number): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(
      `${this.baseUrl}/variants/${variantId}`
    );
  }

  updateVariantStock(
    variantId: number,
    newQuantity: number,
    reason = 'Manual adjustment'
  ): Observable<ApiResponse<boolean>> {
    return this.http.patch<ApiResponse<boolean>>(
      `${this.baseUrl}/variants/${variantId}/stock?reason=${encodeURIComponent(reason)}`,
      newQuantity
    );
  }

  // ─── Suppliers ──────────────────────────────────────────────────────────────

  getAllSuppliers(includeInactive = false): Observable<ApiResponse<Supplier[]>> {
    return this.http.get<ApiResponse<Supplier[]>>(
      `${this.supplierUrl}?includeInactive=${includeInactive}`
    );
  }
  getProductsWithDetails(includeInactive = false): Observable<ApiResponse<Product[]>> {
  return this.http.get<ApiResponse<Product[]>>(
    `${this.baseUrl}/with-details?includeInactive=${includeInactive}`
  );
}
}