// API Response wrapper
export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors?: string[];
  timestamp: Date;
}

// Dashboard Stats
export interface DashboardStats {
  // Revenue & Sales
  todayRevenue: number;
  weekRevenue: number;
  monthRevenue: number;
  yearRevenue: number;
  totalRevenue: number;

  // Bills
  todayBills: number;
  weekBills: number;
  monthBills: number;
  totalBills: number;
  pendingBills: number;

  // Customers
  totalCustomers: number;
  activeCustomers: number;
  totalOutstanding: number;
  overdueAmount: number;

  // Inventory
  totalProducts: number;
  lowStockProducts: number;
  outOfStockProducts: number;
  totalStockValue: number;

  // Suppliers
  totalSuppliers: number;
  suppliersPending: number;
  suppliersPaid: number;

  // Quotations
  totalQuotations: number;
  pendingQuotations: number;
  quotationsValue: number;

  // Profit
  estimatedProfit: number;
  profitMargin: number;
}

// Sales Chart Data
export interface SalesChartData {
  period: string;
  sales: number;
  billCount: number;
}

// Category Sales
export interface CategorySales {
  categoryName: string;
  totalSales: number;
  itemCount: number;
  percentage: number;
}

// Payment Method Stats
export interface PaymentMethodStats {
  paymentMethod: string;
  amount: number;
  count: number;
  percentage: number;
}
