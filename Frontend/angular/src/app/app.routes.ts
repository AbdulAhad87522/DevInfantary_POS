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

export const routes: Routes = [
    {path:'',component:DashboardComponent},
    {path:'inventory', component:InventoryComponent},
    {path:'customers',component:CustomerComponent,children:[
        {path:'',component:CustomerDetailsComponent},
        {path:'add',component:AddCustomerComponent},
    ]},
    {path:'batches', component:BatchComponent},
    {path:'suppliers',component:SupplierComponent,children:[
        {path:'',component:SupplierDetailsComponent},
        {path:'add',component:AddSupplierComponent},
    ]},
    {path:'supplier-bills',component:SupplierDashboardComponent},
    {path:'return-items',component:ReturnItemsComponent},
    {path:'quotations',component:QuotationComponent},
    {path:'customer-bills',component:CustomerBillsComponent}
];
