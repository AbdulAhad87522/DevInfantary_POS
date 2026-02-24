import { Routes } from '@angular/router';
import { DashboardComponent } from './dashboard/dashboard.component';
import { InventoryComponent } from './inventory/inventory.component';
import { CustomerComponent } from './customer/customer.component';
import { BatchComponent } from './batch/batch.component';
import { SupplierComponent } from './supplier/supplier.component';
import { CustomerDetailsComponent } from './customer/customer-details/customer-details.component';
import { AddCustomerComponent } from './customer/add-customer/add-customer.component';
import { SupplierDetailsComponent } from './supplier/supplier-details/supplier-details.component';
import { AddSupplierComponent } from './supplier/add-supplier/add-supplier.component';
import { SupplierDashboardComponent } from './supplier-bills/supplier-dashboard.component';
import { ReturnItemsComponent } from './return-items/return-items.component';
import { QuotationComponent } from './quotation/quotation.component';
import { CustomerBillsComponent } from './customer-bills/customer-bills.component';
import { SellProductComponent } from './sell-product/sell-product.component';
import { LoginComponent } from './components/login/login.component';
import { authGuard } from './guards/auth.guard';
import { roleGuard } from './guards/role.guard';

export const routes: Routes = [
  // Public route (no authentication required)
  { path: 'login', component: LoginComponent },

  // Protected routes (authentication required)
  {
    path: '',
    component: DashboardComponent,
    canActivate: [authGuard]
  },
  {
    path: 'inventory',
    component: InventoryComponent,
    canActivate: [authGuard]
  },
  {
    path: 'customers',
    component: CustomerComponent,
    canActivate: [authGuard],
    children: [
      { path: '', component: CustomerDetailsComponent },
      { path: 'add', component: AddCustomerComponent },
    ]
  },
  {
    path: 'batches',
    component: BatchComponent,
    canActivate: [authGuard]
  },
  {
    path: 'suppliers',
    component: SupplierComponent,
    canActivate: [authGuard],
    children: [
      { path: '', component: SupplierDetailsComponent },
      { path: 'add', component: AddSupplierComponent },
    ]
  },
  {
    path: 'supplier-bills',
    component: SupplierDashboardComponent,
    canActivate: [authGuard, roleGuard(['Admin', 'Manager'])]
  },
  {
    path: 'return-items',
    component: ReturnItemsComponent,
    canActivate: [authGuard]
  },
  {
    path: 'quotations',
    component: QuotationComponent,
    canActivate: [authGuard]
  },
  {
    path: 'customer-bills',
    component: CustomerBillsComponent,
    canActivate: [authGuard]
  },
  {
    path: 'sell-product',
    component: SellProductComponent,
    canActivate: [authGuard]
  },

  // Wildcard route - redirect to login if not authenticated
  { path: '**', redirectTo: '/login' }
];
