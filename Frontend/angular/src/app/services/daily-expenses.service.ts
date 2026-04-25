import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

// DTOs and Models
export interface DailyExpense {
  expenseId: number;
  date: Date;
  description: string;
  amount: number;
  category?: string;
  createdAt?: Date;
  updatedAt?: Date;
}

export interface DailyExpenseDto {
  date: Date;
  description: string;
  amount: number;
  category?: string;
}

export interface DailyExpenseUpdateDto {
  date?: Date;
  description?: string;
  amount?: number;
  category?: string;
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

@Injectable({
  providedIn: 'root'
})
export class DailyExpensesService {
  private apiUrl = `${environment.apiUrl}/DailyExpenses`;

  constructor(private http: HttpClient) { }

  // Get all expenses
  getAllExpenses(): Observable<ApiResponse<DailyExpense[]>> {
    return this.http.get<ApiResponse<DailyExpense[]>>(this.apiUrl);
  }

  // Get paginated expenses
  getExpensesPaginated(pageNumber: number = 1, pageSize: number = 10): Observable<PaginatedResponse<DailyExpense>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());
    
    return this.http.get<PaginatedResponse<DailyExpense>>(`${this.apiUrl}/paginated`, { params });
  }

  // Get expense by ID
  getExpenseById(id: number): Observable<ApiResponse<DailyExpense>> {
    return this.http.get<ApiResponse<DailyExpense>>(`${this.apiUrl}/${id}`);
  }

  // Get expenses by date range
  getExpensesByDateRange(startDate: Date, endDate: Date): Observable<ApiResponse<DailyExpense[]>> {
    const params = new HttpParams()
      .set('startDate', startDate.toISOString())
      .set('endDate', endDate.toISOString());
    
    return this.http.get<ApiResponse<DailyExpense[]>>(`${this.apiUrl}/date-range`, { params });
  }

  // Search expenses
  searchExpenses(term: string): Observable<ApiResponse<DailyExpense[]>> {
    const params = new HttpParams().set('term', term);
    return this.http.get<ApiResponse<DailyExpense[]>>(`${this.apiUrl}/search`, { params });
  }

  // Get total amount
  getTotalAmount(startDate?: Date, endDate?: Date): Observable<ApiResponse<number>> {
    let params = new HttpParams();
    if (startDate) {
      params = params.set('startDate', startDate.toISOString());
    }
    if (endDate) {
      params = params.set('endDate', endDate.toISOString());
    }
    return this.http.get<ApiResponse<number>>(`${this.apiUrl}/total`, { params });
  }

  // Create expense
  createExpense(dto: DailyExpenseDto): Observable<ApiResponse<DailyExpense>> {
    return this.http.post<ApiResponse<DailyExpense>>(this.apiUrl, dto);
  }

  // Update expense
  updateExpense(id: number, dto: DailyExpenseUpdateDto): Observable<ApiResponse<boolean>> {
    return this.http.put<ApiResponse<boolean>>(`${this.apiUrl}/${id}`, dto);
  }

  // Delete expense
  deleteExpense(id: number): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.apiUrl}/${id}`);
  }
}