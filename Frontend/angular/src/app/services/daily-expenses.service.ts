import { Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface DailyExpense {
  expenseId: number;
  description: string;
  date: string;
  amount: number;
  createdAt?: string;
  updatedAt?: string;
}

export interface DailyExpenseDto {
  description: string;
  date: string;
  amount: number;
}

export interface DailyExpenseUpdateDto {
  description?: string;
  date?: string;
  amount?: number;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors?: string[];
}

export interface PaginatedResponse<T> {
  success: boolean;
  message: string;
  data: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

@Injectable({ providedIn: 'root' })
export class DailyExpensesService {
  private apiUrl = `${environment.apiUrl}/DailyExpenses`;

  constructor(private http: HttpClient) {}

  private authHeaders(): { headers: HttpHeaders } {
    const token = localStorage.getItem('token') ?? '';
    return {
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      }),
    };
  }

  getAllExpenses(): Observable<ApiResponse<DailyExpense[]>> {
    return this.http.get<ApiResponse<DailyExpense[]>>(this.apiUrl);
  }

  getExpensesPaginated(
    pageNumber = 1,
    pageSize = 10
  ): Observable<PaginatedResponse<DailyExpense>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PaginatedResponse<DailyExpense>>(
      `${this.apiUrl}/paginated`,
      { params }
    );
  }

  getExpenseById(id: number): Observable<ApiResponse<DailyExpense>> {
    return this.http.get<ApiResponse<DailyExpense>>(`${this.apiUrl}/${id}`);
  }

  getExpensesByDateRange(
    startDate: string,
    endDate: string
  ): Observable<ApiResponse<DailyExpense[]>> {
    const params = new HttpParams()
      .set('startDate', startDate)
      .set('endDate', endDate);
    return this.http.get<ApiResponse<DailyExpense[]>>(
      `${this.apiUrl}/date-range`,
      { params }
    );
  }

  searchExpenses(term: string): Observable<ApiResponse<DailyExpense[]>> {
    const params = new HttpParams().set('term', term);
    return this.http.get<ApiResponse<DailyExpense[]>>(
      `${this.apiUrl}/search`,
      { params }
    );
  }

  getTotalAmount(
    startDate?: string,
    endDate?: string
  ): Observable<ApiResponse<number>> {
    let params = new HttpParams();
    if (startDate) params = params.set('startDate', startDate);
    if (endDate) params = params.set('endDate', endDate);
    return this.http.get<ApiResponse<number>>(`${this.apiUrl}/total`, {
      params,
    });
  }

  createExpense(dto: DailyExpenseDto): Observable<ApiResponse<DailyExpense>> {
    return this.http.post<ApiResponse<DailyExpense>>(
      this.apiUrl,
      dto,
      this.authHeaders()
    );
  }

  updateExpense(
    id: number,
    dto: DailyExpenseUpdateDto
  ): Observable<ApiResponse<boolean>> {
    return this.http.put<ApiResponse<boolean>>(
      `${this.apiUrl}/${id}`,
      dto,
      this.authHeaders()
    );
  }

  deleteExpense(id: number): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(
      `${this.apiUrl}/${id}`,
      this.authHeaders()
    );
  }
}