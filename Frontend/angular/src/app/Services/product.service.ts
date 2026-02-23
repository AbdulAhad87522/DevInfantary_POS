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

@Injectable({ providedIn: 'root' })
export class ProductService {
  private baseUrl = 'http://localhost:5050/api/Products';
  private supplierUrl = 'http://localhost:5050/api/Suppliers';

  constructor(private http: HttpClient) {}

  getAllProducts(includeInactive = false): Observable<ApiResponse<Product[]>> {
    return this.http.get<ApiResponse<Product[]>>(
      `${this.baseUrl}?includeInactive=${includeInactive}`
    );
  }

  getProductById(id: number): Observable<ApiResponse<Product>> {
    return this.http.get<ApiResponse<Product>>(`${this.baseUrl}/${id}`);
  }

  createProduct(data: any): Observable<ApiResponse<Product>> {
    return this.http.post<ApiResponse<Product>>(`${this.baseUrl}`, data);
  }

  updateProduct(id: number, data: any): Observable<ApiResponse<boolean>> {
    return this.http.put<ApiResponse<boolean>>(`${this.baseUrl}/${id}`, data);
  }

  deleteProduct(id: number): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.baseUrl}/${id}`);
  }

  getAllCategories(): Observable<ApiResponse<Category[]>> {
    return this.http.get<ApiResponse<Category[]>>(`${this.baseUrl}/categories`);
  }

  getLowStockItems(): Observable<ApiResponse<ProductVariant[]>> {
    return this.http.get<ApiResponse<ProductVariant[]>>(`${this.baseUrl}/low-stock`);
  }

  getOutOfStockItems(): Observable<ApiResponse<ProductVariant[]>> {
    return this.http.get<ApiResponse<ProductVariant[]>>(`${this.baseUrl}/out-of-stock`);
  }

  getInventoryValue(): Observable<ApiResponse<number>> {
    return this.http.get<ApiResponse<number>>(`${this.baseUrl}/inventory-value`);
  }

  // Variant APIs
  getVariantsByProduct(productId: number): Observable<ApiResponse<ProductVariant[]>> {
    return this.http.get<ApiResponse<ProductVariant[]>>(`${this.baseUrl}/${productId}/variants`);
  }

  createVariant(productId: number, data: any): Observable<ApiResponse<ProductVariant>> {
    return this.http.post<ApiResponse<ProductVariant>>(`${this.baseUrl}/${productId}/variants`, data);
  }

  updateVariant(variantId: number, data: any): Observable<ApiResponse<boolean>> {
    return this.http.put<ApiResponse<boolean>>(`${this.baseUrl}/variants/${variantId}`, data);
  }

  deleteVariant(variantId: number): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.baseUrl}/variants/${variantId}`);
  }

  // ✅ Suppliers API
  getAllSuppliers(includeInactive = false): Observable<ApiResponse<Supplier[]>> {
    return this.http.get<ApiResponse<Supplier[]>>(
      `${this.supplierUrl}?includeInactive=${includeInactive}`
    );
  }
}