//import { Component } from '@angular/core';

//@Component({
//  selector: 'app-dashboard',
//  standalone: true,
//  imports: [],
//  templateUrl: './dashboard.component.html',
//  styleUrl: './dashboard.component.css'
//})
//export class DashboardComponent {

//}


import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DashboardService } from '../Services/dashboard.service';
import {
  DashboardStats,
  SalesChartData,
  CategorySales,
  PaymentMethodStats
} from '../models/dashboard.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],  // ✅ Import CommonModule for *ngIf, *ngFor, pipes
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  // Data properties
  stats: DashboardStats | null = null;
  salesTrend: SalesChartData[] = [];
  categorySales: CategorySales[] = [];
  paymentStats: PaymentMethodStats[] = [];

  // UI state
  loading: boolean = false;
  errorMessage: string = '';

  constructor(private dashboardService: DashboardService) { }

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.loading = true;
    this.errorMessage = '';

    // Load Dashboard Stats
    this.dashboardService.getStats().subscribe({
      next: (response) => {
        if (response.success) {
          this.stats = response.data;
          console.log('✅ Dashboard stats loaded:', this.stats);
        } else {
          this.errorMessage = response.message;
        }
        this.loading = false;
      },
      error: (error) => {
        console.error('❌ Error loading dashboard stats:', error);
        this.errorMessage = 'Failed to load dashboard. Make sure backend is running on https://localhost:7073';
        this.loading = false;
      }
    });

    // Load Sales Trend
    this.dashboardService.getSalesTrend(30).subscribe({
      next: (response) => {
        if (response.success) {
          this.salesTrend = response.data;
          console.log('✅ Sales trend loaded:', this.salesTrend);
        }
      },
      error: (error: any) => console.error('❌ Error loading sales trend:', error)
    });

    // Load Category Sales
    this.dashboardService.getCategorySales().subscribe({
      next: (response: { success: any; data: CategorySales[]; }) => {
        if (response.success) {
          this.categorySales = response.data;
          console.log('✅ Category sales loaded:', this.categorySales);
        }
      },
      error: (error: any) => console.error('❌ Error loading category sales:', error)
    });

    // Load Payment Stats
    this.dashboardService.getPaymentStats().subscribe({
      next: (response: { success: any; data: PaymentMethodStats[]; }) => {
        if (response.success) {
          this.paymentStats = response.data;
          console.log('✅ Payment stats loaded:', this.paymentStats);
        }
      },
      error: (error: any) => console.error('❌ Error loading payment stats:', error)
    });
  }

  // Helper method to format currency
  formatCurrency(value: number): string {
    return value.toLocaleString('en-PK', { minimumFractionDigits: 0, maximumFractionDigits: 0 });
  }

  // Helper method to format percentage
  formatPercentage(value: number): string {
    return value.toFixed(1) + '%';
  }
}
