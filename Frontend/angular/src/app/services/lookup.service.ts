/**
 * lookup.service.ts
 * ─────────────────────────────────────────────────────────────
 * Wraps every ICategoryService endpoint.
 *
 * CASING STRATEGY:
 *   The Category interface declares BOTH PascalCase and camelCase
 *   for every field using TypeScript intersection trick so the
 *   component works regardless of what your .NET serialiser sends.
 *   The extractArray() in the component safely unwraps paginated
 *   responses too.
 * ─────────────────────────────────────────────────────────────
 */

import { Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';

// ── Models ────────────────────────────────────────────────────
/**
 * Supports BOTH camelCase (default .NET with JsonNamingPolicy.CamelCase)
 * AND PascalCase (default System.Text.Json without policy).
 * Only one set will be populated at runtime — the component handles both.
 */
export interface Category {
  // camelCase (most common with modern .NET setup)
  lookupId?:     number;
  type?:         string;
  value?:        string;
  description?:  string | null;
  displayOrder?: number | null;
  isActive?:     boolean;
  createdAt?:    string | null;

  // PascalCase (default System.Text.Json without naming policy)
  LookupId?:     number;
  Type?:         string;
  Value?:        string;
  Description?:  string | null;
  DisplayOrder?: number | null;
  IsActive?:     boolean;
  CreatedAt?:    string | null;
}

export interface PaginatedResponse<T> {
  // PascalCase
  Data?:         T[];
  PageNumber?:   number;
  PageSize?:     number;
  TotalRecords?: number;
  // camelCase
  data?:         T[];
  pageNumber?:   number;
  pageSize?:     number;
  totalRecords?: number;
}

/** Body for POST /api/categories */
export interface CategoryDto {
  Value:         string;
  Description?:  string;
  DisplayOrder?: number;
}

/** Body for PUT /api/categories/:id */
export interface CategoryUpdateDto {
  Value:         string;
  Description?:  string;
  DisplayOrder?: number;
}

// ── Service ───────────────────────────────────────────────────
@Injectable({ providedIn: 'root' })
export class LookupService {

/** e.g. https://devinfantarypos-production.up.railway.app/api/categories */
  private readonly base = `${environment.apiUrl}/categories`;

  constructor(private http: HttpClient) {}

  // GET /api/categories
  getAll(includeInactive = false): Observable<any> {
    const params = new HttpParams().set('includeInactive', String(includeInactive));
    return this.http.get<any>(this.base, { params }).pipe(catchError(this.handleError));
  }

  // GET /api/categories/paginated
  getPaginated(pageNumber: number, pageSize: number, includeInactive = false): Observable<any> {
    const params = new HttpParams()
      .set('pageNumber',      String(pageNumber))
      .set('pageSize',        String(pageSize))
      .set('includeInactive', String(includeInactive));
    return this.http.get<any>(`${this.base}/paginated`, { params }).pipe(catchError(this.handleError));
  }

  // GET /api/categories/:id
  getById(id: number): Observable<any> {
    return this.http.get<any>(`${this.base}/${id}`).pipe(catchError(this.handleError));
  }

  // POST /api/categories
  create(dto: CategoryDto): Observable<any> {
    return this.http.post<any>(this.base, dto).pipe(catchError(this.handleError));
  }

  // PUT /api/categories/:id
  update(id: number, dto: CategoryUpdateDto): Observable<any> {
    return this.http.put<any>(`${this.base}/${id}`, dto).pipe(catchError(this.handleError));
  }

  // DELETE /api/categories/:id  (soft delete)
  delete(id: number): Observable<any> {
    return this.http.delete<any>(`${this.base}/${id}`).pipe(catchError(this.handleError));
  }

  // PATCH /api/categories/:id/restore
  restore(id: number): Observable<any> {
    return this.http.patch<any>(`${this.base}/${id}/restore`, {}).pipe(catchError(this.handleError));
  }

  // GET /api/categories/search?searchTerm=xyz
  search(term: string, includeInactive = false): Observable<any> {
    const params = new HttpParams()
      .set('searchTerm',      term)
      .set('includeInactive', String(includeInactive));
    return this.http.get<any>(`${this.base}/search`, { params }).pipe(catchError(this.handleError));
  }

  // ── Error handler ─────────────────────────────────────────
  private handleError(err: HttpErrorResponse): Observable<never> {
    let msg = 'An unexpected error occurred.';
    if      (err.status === 0)   msg = 'Cannot reach server. Check API is running and CORS is configured.';
    else if (err.status === 401) msg = 'Unauthorized (401). Please log in again.';
    else if (err.status === 403) msg = 'Forbidden (403). You do not have permission.';
    else if (err.status === 404) msg = 'Not found (404).';
    else if (err.status === 400) msg = err.error?.message ?? 'Bad request (400).';
    else if (err.status === 500) msg = 'Server error (500). Check API logs.';
    console.error('[LookupService]', err.status, err.url, err.error);
    return throwError(() => new Error(msg));
  }
}