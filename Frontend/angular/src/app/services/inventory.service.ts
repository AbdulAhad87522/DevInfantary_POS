import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Category {
  lookupId: number;
  value: string;
  description?: string;
}

export interface ProductVariant {
  variantId: number;
  productId: number;
  size?: string;
  color?: string;
  classType?: string;
  unitOfMeasure: string;
  quantityInStock: number;
  pricePerUnit: number;
  pricePerLength?: number;
  reorderLevel: number;
  location?: string;
  isActive: boolean;
  createdAt: Date;
  updatedAt: Date;
  notes?: string;
}

export interface Product {
  productId: number;
  name: string;
  description?: string;
  categoryId?: number;
  categoryName?: string;
  supplierId?: number;
  supplierName?: string;
  isActive: boolean;
  createdAt: Date;
  updatedAt: Date;
  notes?: string;
  variants: ProductVariant[];
}

export interface ProductWithStock {
  productId: number;
  name: string;
  description?: string;
  categoryName: string;
  supplierName: string;
  variantCount: number;
  totalStock: number;
  totalValue: number;
  isActive: boolean;
}

export interface CreateProductDto {
  name: string;
  description?: string;
  categoryId: number;
  supplierId?: number;
  notes?: string;
  variants: CreateProductVariantDto[];
}

export interface CreateProductVariantDto {
  size?: string;
  color?: string;
  classType?: string;
  unitOfMeasure: string;
  quantityInStock: number;
  pricePerUnit: number;
  pricePerLength?: number;
  reorderLevel: number;
  location?: string;
  notes?: string;
}

export interface UpdateProductDto {
  name: string;
  description?: string;
  categoryId: number;
  supplierId?: number;
  notes?: string;
}

export interface UpdateProductVariantDto {
  variantId: number;
  size?: string;
  color?: string;
  classType?: string;
  unitOfMeasure: string;
  quantityInStock: number;
  pricePerUnit: number;
  pricePerLength?: number;
  reorderLevel: number;
  location?: string;
  isActive: boolean;
  notes?: string;
}

export interface ProductSearchDto {
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

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[] | null;
  timestamp: string;
}

export interface PaginatedResponse<T> {
  data: T[];
  totalRecords: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  success?: boolean;
  message?: string;
}

@Injectable({
  providedIn: 'root'
})
export class InventoryService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  // ==================== PRODUCTS ====================

  // Get all products
  getAllProducts(includeInactive: boolean = false): Observable<ApiResponse<Product[]>> {
    return this.http.get<ApiResponse<Product[]>>(`${this.apiUrl}/Products?includeInactive=${includeInactive}`);
  }

  // Get paginated products
  getProductsPaginated(pageNumber: number = 1, pageSize: number = 10, includeInactive: boolean = false): Observable<PaginatedResponse<Product>> {
    return this.http.get<PaginatedResponse<Product>>(`${this.apiUrl}/Products/paginated?pageNumber=${pageNumber}&pageSize=${pageSize}&includeInactive=${includeInactive}`);
  }

  // Get product by ID
  getProductById(id: number): Observable<ApiResponse<Product>> {
    return this.http.get<ApiResponse<Product>>(`${this.apiUrl}/Products/${id}`);
  }

  // Create new product
  createProduct(productData: CreateProductDto): Observable<ApiResponse<Product>> {
    return this.http.post<ApiResponse<Product>>(`${this.apiUrl}/Products`, productData);
  }

  // Update product
  updateProduct(id: number, productData: UpdateProductDto): Observable<ApiResponse<boolean>> {
    return this.http.put<ApiResponse<boolean>>(`${this.apiUrl}/Products/${id}`, productData);
  }

  // Delete product (soft delete)
  deleteProduct(id: number): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.apiUrl}/Products/${id}`);
  }

  // Restore product
  restoreProduct(id: number): Observable<ApiResponse<boolean>> {
    return this.http.post<ApiResponse<boolean>>(`${this.apiUrl}/Products/${id}/restore`, {});
  }

  // Search products
  searchProducts(searchDto: ProductSearchDto): Observable<PaginatedResponse<Product>> {
    return this.http.post<PaginatedResponse<Product>>(`${this.apiUrl}/Products/search`, searchDto);
  }

  // Get products by category
  getProductsByCategory(categoryId: number, includeInactive: boolean = false): Observable<ApiResponse<Product[]>> {
    return this.http.get<ApiResponse<Product[]>>(`${this.apiUrl}/Products/by-category/${categoryId}?includeInactive=${includeInactive}`);
  }

  // Get products by supplier
  getProductsBySupplier(supplierId: number, includeInactive: boolean = false): Observable<ApiResponse<Product[]>> {
    return this.http.get<ApiResponse<Product[]>>(`${this.apiUrl}/Products/by-supplier/${supplierId}?includeInactive=${includeInactive}`);
  }

  // Get products with stock summary
  getStockSummary(): Observable<ApiResponse<ProductWithStock[]>> {
    return this.http.get<ApiResponse<ProductWithStock[]>>(`${this.apiUrl}/Products/stock-summary`);
  }

  // Get low stock items
  getLowStockItems(threshold: number = 10): Observable<ApiResponse<ProductVariant[]>> {
    return this.http.get<ApiResponse<ProductVariant[]>>(`${this.apiUrl}/Products/low-stock?threshold=${threshold}`);
  }

  // Get out of stock items
  getOutOfStockItems(): Observable<ApiResponse<ProductVariant[]>> {
    return this.http.get<ApiResponse<ProductVariant[]>>(`${this.apiUrl}/Products/out-of-stock`);
  }

  // Get total inventory value
  getInventoryValue(): Observable<ApiResponse<number>> {
    return this.http.get<ApiResponse<number>>(`${this.apiUrl}/Products/inventory-value`);
  }

  // Get all categories
  getCategories(): Observable<ApiResponse<Category[]>> {
    return this.http.get<ApiResponse<Category[]>>(`${this.apiUrl}/Products/categories`);
  }

  // ==================== VARIANTS ====================

  // Get variants for a product
  getProductVariants(productId: number): Observable<ApiResponse<ProductVariant[]>> {
    return this.http.get<ApiResponse<ProductVariant[]>>(`${this.apiUrl}/Products/${productId}/variants`);
  }

  // Get variant by ID
  getVariantById(variantId: number): Observable<ApiResponse<ProductVariant>> {
    return this.http.get<ApiResponse<ProductVariant>>(`${this.apiUrl}/Products/variants/${variantId}`);
  }

  // Add variant to product
  addVariant(productId: number, variantData: CreateProductVariantDto): Observable<ApiResponse<ProductVariant>> {
    return this.http.post<ApiResponse<ProductVariant>>(`${this.apiUrl}/Products/${productId}/variants`, variantData);
  }

  // Update variant
  updateVariant(variantId: number, variantData: UpdateProductVariantDto): Observable<ApiResponse<boolean>> {
    return this.http.put<ApiResponse<boolean>>(`${this.apiUrl}/Products/variants/${variantId}`, variantData);
  }

  // Delete variant
  deleteVariant(variantId: number): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.apiUrl}/Products/variants/${variantId}`);
  }

  // Update variant stock
  updateVariantStock(variantId: number, quantityChange: number, reason: string = 'Manual adjustment'): Observable<ApiResponse<boolean>> {
    return this.http.patch<ApiResponse<boolean>>(`${this.apiUrl}/Products/variants/${variantId}/stock?reason=${reason}`, quantityChange);
  }
}