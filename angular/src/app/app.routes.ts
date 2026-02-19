import { Routes } from '@angular/router';
import { DashboardComponent } from './dashboard/dashboard.component';
import { InventoryComponent } from './inventory/inventory.component';
import { CustomerComponent } from './customer/customer.component';
import { BatchComponent } from './batch/batch.component';
import { SupplierComponent } from './supplier/supplier.component';

export const routes: Routes = [
    {path:'',component:DashboardComponent},
    {path:'inventory', component:InventoryComponent},
    {path:'customers',component:CustomerComponent},
    {path:'batches', component:BatchComponent},
    {path:'suppliers',component:SupplierComponent}
];
