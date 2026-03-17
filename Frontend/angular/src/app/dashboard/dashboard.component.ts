import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';  // Add this import
import { DashboardService } from '../services/dashboard.service';
import { DecimalPipe } from '@angular/common';
import {
  DashboardStats,
  SalesChartData,
  CategorySales,
  PaymentMethodStats
} from '../models/dashboard.model';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule,DecimalPipe],
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

  constructor(
    private dashboardService: DashboardService,
    private router: Router , // Add this
    private authService: AuthService
  ) { }

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

  // Add this logout method
  logout() {
    console.log('Logout clicked');
    // Navigate to login page (adjust the path as needed)
    this.authService.logout();
  }
  // ============================================================
// CHARTS ADDITIONS — Merge into DashboardComponent class
// ============================================================
//
// 1. Add these imports at the top of dashboard.component.ts:
//
//    import { DecimalPipe } from '@angular/common';
//
// 2. Add DecimalPipe to the imports array in @Component:
//    imports: [CommonModule, DecimalPipe]
//
// 3. Add these properties and methods to the DashboardComponent class:
// ============================================================

// ---- Color palettes ----
categoryColors: string[] = [
  '#007bff', '#28a745', '#ffc107', '#dc3545',
  '#17a2b8', '#6f42c1', '#fd7e14', '#20c997'
];

donutColors: string[] = [
  '#007bff', '#28a745', '#ffc107', '#dc3545', '#17a2b8', '#6f42c1'
];

// ---- SVG grid lines (5 horizontal lines across 200px height) ----
gridYLines: number[] = [0, 40, 80, 120, 160, 200];

// ---- Computed chart data ----

/** SVG (x,y) coordinates for each sales trend data point */
get chartPoints(): { x: number; y: number }[] {
  if (!this.salesTrend || this.salesTrend.length === 0) return [];
  const maxVal = Math.max(...this.salesTrend.map(d => d.sales), 1);
  const n = this.salesTrend.length;
  return this.salesTrend.map((d, i) => ({
    x: n === 1 ? 300 : (i / (n - 1)) * 580 + 10,
    y: 190 - (d.sales / maxVal) * 170
  }));
}

/** SVG polyline path string for the trend line */
getLinePath(): string {
  const pts = this.chartPoints;
  if (pts.length === 0) return '';
  return 'M ' + pts.map(p => `${p.x},${p.y}`).join(' L ');
}

/** SVG area path (line + closed bottom) */
getAreaPath(): string {
  const pts = this.chartPoints;
  if (pts.length === 0) return '';
  const line = 'M ' + pts.map(p => `${p.x},${p.y}`).join(' L ');
  const last = pts[pts.length - 1];
  const first = pts[0];
  return `${line} L ${last.x},200 L ${first.x},200 Z`;
}

/** Sampled x-axis labels (up to 6) */
get xAxisLabels(): string[] {
  if (!this.salesTrend || this.salesTrend.length === 0) return [];
  const n = this.salesTrend.length;
  const step = Math.max(1, Math.floor(n / 5));
  const labels: string[] = [];
  for (let i = 0; i < n; i += step) {
    labels.push(this.salesTrend[i].period);
  }
  return labels;
}

/** Donut chart segments from paymentStats */
get donutSegments(): { path: string; color: string; label: string; pct: number }[] {
  if (!this.paymentStats || this.paymentStats.length === 0) return [];
  const total = this.paymentStats.reduce((s, p) => s + (p.amount || 0), 0);
  if (total === 0) return [];
  const R = 75;
  let angle = 0;
  return this.paymentStats.map((p, i) => {
    const value = p.amount || 0;
    const pct = (value / total) * 100;
    const sweep = (value / total) * 2 * Math.PI;
    const x1 = R * Math.cos(angle);
    const y1 = R * Math.sin(angle);
    const x2 = R * Math.cos(angle + sweep);
    const y2 = R * Math.sin(angle + sweep);
    const large = sweep > Math.PI ? 1 : 0;
    const path = `M 0 0 L ${x1} ${y1} A ${R} ${R} 0 ${large} 1 ${x2} ${y2} Z`;
    angle += sweep;
    return {
      path,
      color: this.donutColors[i % this.donutColors.length],
      label: p.paymentMethod || `Method ${i + 1}`,
      pct
    };
  });
}

/** Revenue cards for the summary row */
get revenueSummary(): { icon: string; label: string; value: number; bills: number; color: string }[] {
  if (!this.stats) return [];
  return [
    { icon: '💰', label: "Today's Revenue",  value: this.stats.todayRevenue,  bills: this.stats.todayBills,  color: '#28a745' },
    { icon: '📅', label: 'Week Revenue',      value: this.stats.weekRevenue,   bills: this.stats.weekBills,   color: '#007bff' },
    { icon: '🗓', label: 'Month Revenue',     value: this.stats.monthRevenue,  bills: this.stats.monthBills,  color: '#17a2b8' },
    { icon: '∞',  label: 'Total Revenue',     value: this.stats.totalRevenue,  bills: 0,                      color: '#6f42c1' },
  ];
}

/** Width % relative to max revenue for sparkline bars */
getRevenueBarWidth(value: number): number {
  if (!this.stats) return 0;
  const max = this.stats.totalRevenue || 1;
  return Math.min((value / max) * 100, 100);
}

/** In-stock product count */
get stockOkCount(): number {
  if (!this.stats) return 0;
  return Math.max(0, this.stats.totalProducts - this.stats.lowStockProducts - this.stats.outOfStockProducts);
}

/** % of products currently available (not out of stock) */
get fulfillmentRate(): number {
  if (!this.stats || this.stats.totalProducts === 0) return 0;
  return ((this.stats.totalProducts - this.stats.outOfStockProducts) / this.stats.totalProducts) * 100;
}

/** SVG stroke-dasharray for radial progress (circumference = 2π×50 ≈ 314) */
get fulfillmentDash(): string {
  const full = 314;
  const filled = (this.fulfillmentRate / 100) * full;
  return `${filled} ${full - filled}`;
}

/** Color of the radial ring based on fulfillment */
get fulfillmentColor(): string {
  const r = this.fulfillmentRate;
  if (r >= 80) return '#28a745';
  if (r >= 50) return '#ffc107';
  return '#dc3545';
}
}