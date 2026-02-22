import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

// ==================== INTERFACES ====================

export interface Product {
  productId: number;
  name: string;
  description: string | null;
  categoryId: number | null;
  categoryName: string | null;
  supplierId: number | null;
  supplierName: string | null;
  isActive: boolean;
  createdAt: Date;
  updatedAt: Date;
  notes: string | null;
  variants: ProductVariant[];
}

export interface ProductVariant {
  variantId: number;
  productId: number;
  size: string | null;
  color: string | null;
  classType: string | null;
  unitOfMeasure: string;
  quantityInStock: number;
  pricePerUnit: number;
  pricePerLength: number | null;
  reorderLevel: number;
  location: string | null;
  isActive: boolean;
  createdAt: Date;
  updatedAt: Date;
  notes: string | null;
}

export interface ProductWithStock {
  productId: number;
  name: string;
  description: string | null;
  categoryName: string;
  supplierName: string;
  variantCount: number;
  totalStock: number;
  totalValue: number;
  isActive: boolean;
}

export interface Category {
  lookupId: number;
  value: string;
  description?: string;
}

// ==================== DTOs ====================

export interface CreateProductDto {
  name: string;
  description?: string | null;
  categoryId: number;
  supplierId?: number | null;
  notes?: string | null;
  variants: CreateProductVariantDto[];
}

export interface CreateProductVariantDto {
  size?: string | null;
  color?: string | null;
  classType?: string | null;
  unitOfMeasure: string;
  quantityInStock: number;
  pricePerUnit: number;
  pricePerLength?: number | null;
  reorderLevel: number;
  location?: string | null;
  notes?: string | null;
}

export interface UpdateProductDto {
  name: string;
  description?: string | null;
  categoryId: number;
  supplierId?: number | null;
  notes?: string | null;
}

export interface UpdateProductVariantDto {
  variantId: number;
  size?: string | null;
  color?: string | null;
  classType?: string | null;
  unitOfMeasure: string;
  quantityInStock: number;
  pricePerUnit: number;
  pricePerLength?: number | null;
  reorderLevel: number;
  location?: string | null;
  isActive: boolean;
  notes?: string | null;
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

// ==================== RESPONSE INTERFACES ====================

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
  success: boolean;
  message?: string;
}

// ==================== SERVICE ====================

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private apiUrl = `${environment.apiUrl}/Products`;

  constructor(private http: HttpClient) { }

  // ==================== PRODUCT ENDPOINTS ====================

  /**
   * Get all products
   * GET /api/Products?includeInactive=false
   */
  getAllProducts(includeInactive: boolean = false): Observable<ApiResponse<Product[]>> {
    return this.http.get<ApiResponse<Product[]>>(`${this.apiUrl}?includeInactive=${includeInactive}`);
  }

  /**
   * Get paginated products
   * GET /api/Products/paginated?pageNumber=1&pageSize=10&includeInactive=false
   */
  getProductsPaginated(pageNumber: number = 1, pageSize: number = 10, includeInactive: boolean = false): Observable<PaginatedResponse<Product>> {
    return this.http.get<PaginatedResponse<Product>>(`${this.apiUrl}/paginated?pageNumber=${pageNumber}&pageSize=${pageSize}&includeInactive=${includeInactive}`);
  }

  /**
   * Get product by ID
   * GET /api/Products/{id}
   */
  getProductById(id: number): Observable<ApiResponse<Product>> {
    return this.http.get<ApiResponse<Product>>(`${this.apiUrl}/${id}`);
  }

  /**
   * Create new product
   * POST /api/Products
   */
  createProduct(productData: CreateProductDto): Observable<ApiResponse<Product>> {
    return this.http.post<ApiResponse<Product>>(this.apiUrl, productData);
  }

  /**
   * Update product
   * PUT /api/Products/{id}
   */
  updateProduct(id: number, productData: UpdateProductDto): Observable<ApiResponse<boolean>> {
    return this.http.put<ApiResponse<boolean>>(`${this.apiUrl}/${id}`, productData);
  }

  /**
   * Delete product (soft delete)
   * DELETE /api/Products/{id}
   */
  deleteProduct(id: number): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.apiUrl}/${id}`);
  }

  /**
   * Restore deleted product
   * POST /api/Products/{id}/restore
   */
  restoreProduct(id: number): Observable<ApiResponse<boolean>> {
    return this.http.post<ApiResponse<boolean>>(`${this.apiUrl}/${id}/restore`, {});
  }

  // ==================== VARIANT ENDPOINTS ====================

  /**
   * Get all variants for a specific product
   * GET /api/Products/{productId}/variants
   */
  getProductVariants(productId: number): Observable<ApiResponse<ProductVariant[]>> {
    return this.http.get<ApiResponse<ProductVariant[]>>(`${this.apiUrl}/${productId}/variants`);
  }

  /**
   * Get variant by ID
   * GET /api/Products/variants/{variantId}
   */
  getVariantById(variantId: number): Observable<ApiResponse<ProductVariant>> {
    return this.http.get<ApiResponse<ProductVariant>>(`${this.apiUrl}/variants/${variantId}`);
  }

  /**
   * Add variant to a product
   * POST /api/Products/{productId}/variants
   */
  addVariant(productId: number, variantData: CreateProductVariantDto): Observable<ApiResponse<ProductVariant>> {
    return this.http.post<ApiResponse<ProductVariant>>(`${this.apiUrl}/${productId}/variants`, variantData);
  }

  /**
   * Update variant
   * PUT /api/Products/variants/{variantId}
   */
  updateVariant(variantId: number, variantData: UpdateProductVariantDto): Observable<ApiResponse<boolean>> {
    return this.http.put<ApiResponse<boolean>>(`${this.apiUrl}/variants/${variantId}`, variantData);
  }

  /**
   * Delete variant
   * DELETE /api/Products/variants/{variantId}
   */
  deleteVariant(variantId: number): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.apiUrl}/variants/${variantId}`);
  }

  /**
   * Update variant stock
   * PATCH /api/Products/variants/{variantId}/stock?reason=Manual adjustment
   */
  updateVariantStock(variantId: number, quantityChange: number, reason: string = 'Manual adjustment'): Observable<ApiResponse<boolean>> {
    return this.http.patch<ApiResponse<boolean>>(`${this.apiUrl}/variants/${variantId}/stock?reason=${encodeURIComponent(reason)}`, quantityChange);
  }

  // ==================== SEARCH & REPORTS ====================

  /**
   * Advanced product search
   * POST /api/Products/search
   */
  searchProducts(searchDto: ProductSearchDto): Observable<PaginatedResponse<Product>> {
    return this.http.post<PaginatedResponse<Product>>(`${this.apiUrl}/search`, searchDto);
  }

  /**
   * Get products by category
   * GET /api/Products/by-category/{categoryId}?includeInactive=false
   */
  getProductsByCategory(categoryId: number, includeInactive: boolean = false): Observable<ApiResponse<Product[]>> {
    return this.http.get<ApiResponse<Product[]>>(`${this.apiUrl}/by-category/${categoryId}?includeInactive=${includeInactive}`);
  }

  /**
   * Get products by supplier
   * GET /api/Products/by-supplier/{supplierId}?includeInactive=false
   */
  getProductsBySupplier(supplierId: number, includeInactive: boolean = false): Observable<ApiResponse<Product[]>> {
    return this.http.get<ApiResponse<Product[]>>(`${this.apiUrl}/by-supplier/${supplierId}?includeInactive=${includeInactive}`);
  }

  /**
   * Get products with stock summary
   * GET /api/Products/stock-summary
   */
  getProductsWithStockSummary(): Observable<ApiResponse<ProductWithStock[]>> {
    return this.http.get<ApiResponse<ProductWithStock[]>>(`${this.apiUrl}/stock-summary`);
  }

  /**
   * Get low stock items
   * GET /api/Products/low-stock?threshold=10
   */
  getLowStockItems(threshold: number = 10): Observable<ApiResponse<ProductVariant[]>> {
    return this.http.get<ApiResponse<ProductVariant[]>>(`${this.apiUrl}/low-stock?threshold=${threshold}`);
  }

  /**
   * Get out of stock items
   * GET /api/Products/out-of-stock
   */
  getOutOfStockItems(): Observable<ApiResponse<ProductVariant[]>> {
    return this.http.get<ApiResponse<ProductVariant[]>>(`${this.apiUrl}/out-of-stock`);
  }

  /**
   * Get total inventory value
   * GET /api/Products/inventory-value
   */
  getTotalInventoryValue(): Observable<ApiResponse<number>> {
    return this.http.get<ApiResponse<number>>(`${this.apiUrl}/inventory-value`);
  }

  // ==================== CATEGORIES ====================

  /**
   * Get all product categories
   * GET /api/Products/categories
   */
  getAllCategories(): Observable<ApiResponse<Category[]>> {
    return this.http.get<ApiResponse<Category[]>>(`${this.apiUrl}/categories`);
  }

  // ==================== HELPER METHODS ====================

  /**
   * Format price for display
   */
  formatPrice(price: number): string {
    return new Intl.NumberFormat('en-PK', {
      style: 'currency',
      currency: 'PKR',
      minimumFractionDigits: 0,
      maximumFractionDigits: 2
    }).format(price);
  }

  /**
   * Get stock status
   */
  getStockStatus(variant: ProductVariant): 'in-stock' | 'low-stock' | 'out-of-stock' {
    if (variant.quantityInStock <= 0) {
      return 'out-of-stock';
    }
    if (variant.quantityInStock <= variant.reorderLevel) {
      return 'low-stock';
    }
    return 'in-stock';
  }

  /**
   * Get stock status color
   */
  getStockStatusColor(variant: ProductVariant): string {
    const status = this.getStockStatus(variant);
    switch (status) {
      case 'in-stock': return 'green';
      case 'low-stock': return 'orange';
      case 'out-of-stock': return 'red';
    }
  }
} 
