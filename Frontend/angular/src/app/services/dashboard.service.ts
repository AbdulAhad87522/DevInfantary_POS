import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  ApiResponse,
  DashboardStats,
  SalesChartData,
  CategorySales,
  PaymentMethodStats
} from '../models/dashboard.model';

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private apiUrl = `${environment.apiUrl}/dashboard`;

  constructor(private http: HttpClient) { }

  getStats(): Observable<ApiResponse<DashboardStats>> {
    return this.http.get<ApiResponse<DashboardStats>>(`${this.apiUrl}/stats`);
  }

  getSalesTrend(days: number = 30): Observable<ApiResponse<SalesChartData[]>> {
    return this.http.get<ApiResponse<SalesChartData[]>>(`${this.apiUrl}/sales-trend?days=${days}`);
  }

  getCategorySales(): Observable<ApiResponse<CategorySales[]>> {
    return this.http.get<ApiResponse<CategorySales[]>>(`${this.apiUrl}/category-sales`);
  }

  getPaymentStats(): Observable<ApiResponse<PaymentMethodStats[]>> {
    return this.http.get<ApiResponse<PaymentMethodStats[]>>(`${this.apiUrl}/payment-stats`);
  }
}
