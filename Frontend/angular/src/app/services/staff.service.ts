/**
 * ============================================================
 * staff.service.ts
 * ============================================================
 *
 * TWO FIXES applied vs the previous version:
 *
 * 1. API WRAPPER — Your API wraps every response in:
 *      { success: boolean, message: string, data: T, errors: any }
 *    So we define ApiResponse<T> and extract `.data` with map().
 *
 * 2. CAMELCASE — Your API returns camelCase JSON
 *      (staffId, name, isActive, roleName …)
 *    All interface fields are now camelCase to match exactly.
 * ============================================================
 */

import { Injectable } from '@angular/core';
import {
  HttpClient, HttpParams, HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { environment } from '../../environments/environment';

// ─────────────────────────────────────────────────────────────
// API WRAPPER  — every endpoint returns this envelope
// ─────────────────────────────────────────────────────────────

/**
 * Generic API response wrapper your .NET backend returns.
 * Example:
 *   { success: true, message: "...", data: [...], errors: null }
 */
export interface ApiResponse<T> {
  success:   boolean;
  message:   string;
  data:      T;
  errors:    any;
  timestamp: string;
}

// ─────────────────────────────────────────────────────────────
// MODELS  — camelCase to match .NET JSON output
// ─────────────────────────────────────────────────────────────

/** Staff record returned by GET endpoints */
export interface Staff {
  staffId:   number;
  name:      string;
  email:     string | null;
  contact:   string | null;
  cnic:      string | null;
  address:   string | null;
  roleId:    number;
  roleName:  string;
  username:  string;
  isActive:  boolean;
  hireDate:  string | null;   // ISO date string
  createdAt: string | null;
  updatedAt: string | null;
  lastLogin: string | null;
}

/** Paginated response wrapper */
export interface PaginatedResponse<T> {
  data:         T[];
  pageNumber:   number;
  pageSize:     number;
  totalRecords: number;
}

// ─────────────────────────────────────────────────────────────
// REQUEST DTOs  — sent as JSON body to the API
// ─────────────────────────────────────────────────────────────

/** Body for POST /api/staff */
export interface StaffDto {
  name:      string;
  email?:    string;
  contact?:  string;
  cnic?:     string;
  address?:  string;
  roleName:  string;
  username:  string;
  password:  string;
  hireDate?: string;
}

/** Body for PUT /api/staff/:id */
export interface StaffUpdateDto {
  name:      string;
  email?:    string;
  contact?:  string;
  cnic?:     string;
  address?:  string;
  roleName:  string;
  username?: string;
  hireDate?: string;
}

/** Body for PATCH /api/staff/:id/change-password */
export interface StaffChangePasswordDto {
  currentPassword: string;
  newPassword:     string;
}

// ─────────────────────────────────────────────────────────────
// SERVICE
// ─────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class StaffService {

  /** Base URL → https://devinfantarypos-production.up.railway.app/api/staff */
  private readonly base = `${environment.apiUrl}/staff`;

  constructor(private http: HttpClient) {}

  // ── GET /api/staff ──────────────────────────────────────────
  /**
   * Returns ALL staff. Response is wrapped in ApiResponse<Staff[]>.
   * We use map(r => r.data) to unwrap and return only the array.
   */
  getAll(includeInactive = false): Observable<Staff[]> {
    const params = new HttpParams()
      .set('includeInactive', String(includeInactive));

    return this.http
      .get<ApiResponse<Staff[]>>(this.base, { params })
      .pipe(
        // Unwrap the envelope → always returns a plain Staff[]
        map(response => {
          console.log('[StaffService] Raw API response:', response);
          // Guard: if data is null/undefined return empty array
          return Array.isArray(response?.data) ? response.data : [];
        }),
        catchError(this.handleError)
      );
  }

  // ── GET /api/staff/paginated ────────────────────────────────
  getPaginated(
    pageNumber: number,
    pageSize: number,
    includeInactive = false
  ): Observable<PaginatedResponse<Staff>> {
    const params = new HttpParams()
      .set('pageNumber',      String(pageNumber))
      .set('pageSize',        String(pageSize))
      .set('includeInactive', String(includeInactive));

    return this.http
      .get<ApiResponse<PaginatedResponse<Staff>>>(`${this.base}/paginated`, { params })
      .pipe(
        map(r => r.data),
        catchError(this.handleError)
      );
  }

  // ── GET /api/staff/:id ──────────────────────────────────────
  getById(id: number): Observable<Staff> {
    return this.http
      .get<ApiResponse<Staff>>(`${this.base}/${id}`)
      .pipe(
        map(r => r.data),
        catchError(this.handleError)
      );
  }

  // ── POST /api/staff ─────────────────────────────────────────
  /** Create a new staff member; returns the created Staff object */
  create(dto: StaffDto): Observable<Staff> {
    return this.http
      .post<ApiResponse<Staff>>(this.base, dto)
      .pipe(
        map(r => r.data),
        catchError(this.handleError)
      );
  }

  // ── PUT /api/staff/:id ──────────────────────────────────────
  /** Update staff; returns true on success */
  update(id: number, dto: StaffUpdateDto): Observable<boolean> {
    return this.http
      .put<ApiResponse<boolean>>(`${this.base}/${id}`, dto)
      .pipe(
        map(r => r.success),
        catchError(this.handleError)
      );
  }

  // ── DELETE /api/staff/:id ───────────────────────────────────
  /** Soft-delete (is_active = 0); returns true on success */
  delete(id: number): Observable<boolean> {
    return this.http
      .delete<ApiResponse<boolean>>(`${this.base}/${id}`)
      .pipe(
        map(r => r.success),
        catchError(this.handleError)
      );
  }

  // ── PATCH /api/staff/:id/restore ────────────────────────────
  /** Re-activate a soft-deleted staff member */
  restore(id: number): Observable<boolean> {
    return this.http
      .patch<ApiResponse<boolean>>(`${this.base}/${id}/restore`, {})
      .pipe(
        map(r => r.success),
        catchError(this.handleError)
      );
  }

  // ── GET /api/staff/search ───────────────────────────────────
  /**
   * Server-side search across name, email, contact, username, cnic.
   * Debounce calls in the component (350ms) to avoid excess requests.
   */
  search(term: string, includeInactive = false): Observable<Staff[]> {
    const params = new HttpParams()
      .set('term',      term)
      .set('includeInactive', String(includeInactive));

    return this.http
      .get<ApiResponse<Staff[]>>(`${this.base}/search`, { params })
      .pipe(
        map(r => Array.isArray(r?.data) ? r.data : []),
        catchError(this.handleError)
      );
  }

  // ── PATCH /api/staff/:id/change-password ────────────────────
  /**
   * Change password — server verifies current password first.
   * Returns false (not an HTTP error) when current password is wrong.
   */
  changePassword(id: number, dto: StaffChangePasswordDto): Observable<boolean> {
    return this.http
      .patch<ApiResponse<boolean>>(`${this.base}/${id}/change-password`, dto)
      .pipe(
        map(r => r.success),
        catchError(this.handleError)
      );
  }

  // ── GET /api/staff/role/:roleName ───────────────────────────
  /** Get all active staff in a specific role */
  getByRole(roleName: string): Observable<Staff[]> {
    return this.http
      .get<ApiResponse<Staff[]>>(`${this.base}/role/${encodeURIComponent(roleName)}`)
      .pipe(
        map(r => Array.isArray(r?.data) ? r.data : []),
        catchError(this.handleError)
      );
  }

  // ─────────────────────────────────────────────────────────────
  // PRIVATE — centralised HTTP error handler
  // ─────────────────────────────────────────────────────────────
  private handleError(err: HttpErrorResponse): Observable<never> {
    let msg = 'An unexpected error occurred.';

    if (err.status === 0) {
      msg = 'Cannot reach the server. Please check your connection.';
    } else if (err.status === 404) {
      msg = 'Record not found (404).';
    } else if (err.status === 400) {
      msg = err.error?.message ?? 'Bad request (400).';
    } else if (err.status === 500) {
      msg = 'Internal server error (500). Check API logs.';
    }

    console.error('[StaffService] HTTP', err.status, err.url, err.error);
    return throwError(() => new Error(msg));
  }
}