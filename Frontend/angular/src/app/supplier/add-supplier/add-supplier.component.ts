import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

@Component({
  selector: 'app-add-supplier',
  standalone: true,
  imports: [CommonModule,FormsModule],
  templateUrl: './add-supplier.component.html',
  styleUrl: './add-supplier.component.css'
})
export class AddSupplierComponent {


  router:Router=inject(Router)
  // Add these properties
showAddForm = true;
fullname:string='';
phone:string='';
address:string='';

// Methods
openAddCustomerForm() {
  this.showAddForm = true;
}

closeAddForm() {
  this.showAddForm = false;
  this.router.navigate(['/suppliers/']);
}

onSubmitCustomer(form: any) {
  this.closeAddForm();
}
}
