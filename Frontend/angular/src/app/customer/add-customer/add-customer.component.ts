import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-add-customer',
  standalone: true,
  imports: [FormsModule,CommonModule],
  templateUrl: './add-customer.component.html',
  styleUrl: './add-customer.component.css'
})
export class AddCustomerComponent {

  // Add these properties
showAddForm = false;
newCustomer: any = {
  fullName: '',
  phone: '',
  address: ''
};

// Methods
openAddCustomerForm() {
  this.showAddForm = true;
  this.newCustomer = { fullName: '', phone: '', address: '' };
}

closeAddForm() {
  this.showAddForm = false;
}

onSubmitCustomer(form: any) {
  this.closeAddForm();
}
}
