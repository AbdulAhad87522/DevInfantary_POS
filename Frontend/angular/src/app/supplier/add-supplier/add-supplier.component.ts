import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-add-supplier',
  standalone: true,
  imports: [CommonModule,FormsModule],
  templateUrl: './add-supplier.component.html',
  styleUrl: './add-supplier.component.css'
})
export class AddSupplierComponent {
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
}

onSubmitCustomer(form: any) {
  this.closeAddForm();
}
}
