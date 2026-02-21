import { Component, EventEmitter, Input, Output, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { 
  FormGroup, 
  FormControl, 
  Validators, 
  ReactiveFormsModule 
} from '@angular/forms';
import { FormsModule } from '@angular/forms';

interface Batch {
  batchName: string;
  total_price: number;
  paid: number;
  status: string;
  supplier_name: string;
  remaining: number;
}
interface BatchLine {
  quantity: number;
  costPrice: number;
  lineTotal: number;
  productName: string;
  size: string;
  class: string;
  salePrice: number;
}

interface Supplier {
  id: number;
  name: string;
}

@Component({
  selector: 'app-batch',
  standalone: true,
  imports: [CommonModule,FormsModule, ReactiveFormsModule],
  templateUrl: './batch.component.html',
  styleUrls: ['./batch.component.css']
})
export class BatchComponent {
  // Dummy data for batches
  batches: Batch[] = [
    { batchName: 'Batch A1', total_price: 1500, paid: 1200, status: 'Active', supplier_name: 'Supplier X', remaining: 300 },
    { batchName: 'Batch B2', total_price: 2000, paid: 2000, status: 'Completed', supplier_name: 'Supplier Y', remaining: 0 },
    { batchName: 'Batch C3', total_price: 800, paid: 500, status: 'Pending', supplier_name: 'Supplier Z', remaining: 300 },
    { batchName: 'Batch D4', total_price: 1200, paid: 1000, status: 'Active', supplier_name: 'Supplier X', remaining: 200 },
    { batchName: 'Batch E5', total_price: 1800, paid: 1800, status: 'Completed', supplier_name: 'Supplier Y', remaining: 0 }
  ];

  // Search functionality (basic filter on batchName)
  searchTerm: string = '';
  filteredBatches = this.batches;

  onSearch() {
    this.filteredBatches = this.batches.filter(batch =>
      batch.batchName.toLowerCase().includes(this.searchTerm.toLowerCase())
    );
  }

  // Button actions (placeholders)
  onRefresh() {
    console.log('Refresh clicked');
    // Add refresh logic here (e.g., reload data)
  }

  onAddNew() {
    this.showAddBatchForm=true;
    console.log('Add New clicked');
    // Add navigation or modal logic here
  }

  onEdit(batch: Batch) {
    console.log('Edit clicked for:', batch);
    // Add edit logic here
    this.showEditForm=true;
  }

  onAddDetails(batch: Batch) {
    console.log('Add Details clicked for:', batch);
    // Add details logic here
    this.showAddBatchForm=true;
  }


// Code fo ADD BATCH component is here


showAddBatchForm = false; // Toggle for showing add batch form
batchId = 1; // In real app → from route param or service

  // Batch header data
  batch = {
    name: '',
    supplierId: '',
    date: new Date().toISOString().split('T')[0],
    status: 'Partial'
  };

  // Dummy suppliers (in real app → from service)
  suppliers: Supplier[] = [
    { id: 1, name: 'General Supplies' },
    { id: 2, name: 'ABC Traders' },
    { id: 3, name: 'PVC Masters' },
    { id: 4, name: 'Hardware Hub' }
  ];

  // Current line being added
  newLine = {
    quantity: 0,
    costPrice: 0
  };

  // Selected variant (dummy for demo)
  selectedVariant: string | null = null;
  variantSize = '';
  variantClass = '';
  variantSalePrice = 0;

  // Lines added to batch
  batchItems: BatchLine[] = [
    // Dummy initial row (like your screenshot)
    {
      quantity: 80.00,
      costPrice: 12.00,
      lineTotal: 960.00,
      productName: 'UPVC Pipe AS PER...',
      size: '1 1/2"',
      class: 'CLASS "O"',
      salePrice: 145.30
    }
  ];

  // Totals
  totalAmount = 0;
  paidAmount = 0;
  get remaining() {
    return this.totalAmount - this.paidAmount;
  }

  get paidPercentage() {
    return this.totalAmount > 0 ? Math.min(100, (this.paidAmount / this.totalAmount) * 100) : 0;
  }

  ngOnInit() {
    this.calculateTotals();
     this.initForm();
  }

  // Simulate selecting a variant (in real app → open modal or dropdown)
  selectVariant() {
    // Dummy selection for demo
    this.selectedVariant = 'UPVC Pipe AS PER 1 1/2" CLASS "O"';
    this.variantSize = '1 1/2"';
    this.variantClass = 'CLASS "O"';
    this.variantSalePrice = 145.30;
  }

  get canAddLine(): boolean {
    return this.newLine.quantity > 0 && this.newLine.costPrice > 0 && !!this.selectedVariant;
  }

  addToBatch() {
    if (!this.canAddLine) return;

    const lineTotal = this.newLine.quantity * this.newLine.costPrice;

    this.batchItems.push({
      quantity: this.newLine.quantity,
      costPrice: this.newLine.costPrice,
      lineTotal,
      productName: this.selectedVariant!,
      size: this.variantSize,
      class: this.variantClass,
      salePrice: this.variantSalePrice
    });

    this.newLine = { quantity: 0, costPrice: 0 };
    this.selectedVariant = null; // reset variant after adding
    this.variantSize = '';
    this.variantClass = '';
    this.variantSalePrice = 0;

    this.calculateTotals();
  }

  removeItem(index: number) {
    if (confirm('Remove this item from batch?')) {
      this.batchItems.splice(index, 1);
      this.calculateTotals();
    }
  }

  private calculateTotals() {
    this.totalAmount = this.batchItems.reduce((sum, item) => sum + item.lineTotal, 0);
  }

  // Placeholder actions
  saveBatch() {
    console.log('Saving batch:', {
      batch: this.batch,
      items: this.batchItems,
      paid: this.paidAmount,
      remaining: this.remaining
    });
    // In real app: call service.saveBatch(...)
  }

  cancel() {
    this.showAddBatchForm=false;
    // In real app: navigate back or confirm discard
    console.log('Batch cancelled');
  }

  //code for edit Batch:
  showEditForm=false;
  @Input() batchdata: BatchData = {
    supplier: '',
    batchName: '',
    totalPrice: 0,
    paid: 0
  };
  
  @Output() save = new EventEmitter<BatchData>();
  @Output() cancelit = new EventEmitter<void>();

  batchForm!: FormGroup;

  private initForm(): void {
    this.batchForm = new FormGroup({
      supplier: new FormControl(this.batchdata.supplier, [
        Validators.required,
        Validators.minLength(2)
      ]),
      batchName: new FormControl(this.batchdata.batchName, [
        Validators.required,
        Validators.minLength(2)
      ]),
      totalPrice: new FormControl(this.batchdata.totalPrice, [
        Validators.required,
        Validators.min(0)
      ]),
      paid: new FormControl(this.batchdata.paid, [
        Validators.required,
        Validators.min(0)
      ])
    });
  }

  isFieldInvalid(field: string): boolean {
    const control = this.batchForm.get(field);
    return !!(control && control.invalid && (control.dirty || control.touched));
  }

  getErrorMessage(field: string): string {
    const control = this.batchForm.get(field);
    
    if (!control || !control.errors) return '';

    if (control.errors['required']) {
      return `${this.capitalize(field)} is required`;
    }
    if (control.errors['minlength']) {
      return `Minimum ${control.errors['minlength'].requiredLength} characters required`;
    }
    if (control.errors['min']) {
      return 'Value must be greater than or equal to 0';
    }

    return 'Invalid input';
  }

  private capitalize(text: string): string {
    return text.charAt(0).toUpperCase() + text.slice(1);
  }

  onSubmit(): void {
    if (this.batchForm.valid) {
      const formData: BatchData = {
        supplier: this.batchForm.value.supplier,
        batchName: this.batchForm.value.batchName,
        totalPrice: this.batchForm.value.totalPrice,
        paid: this.batchForm.value.paid
      };
      this.save.emit(formData);
    } else {
      this.batchForm.markAllAsTouched();
    }
  }

  onCancel(event?: Event): void {
     this.showEditForm=false;
    if (event) {
      event.preventDefault();
      event.stopPropagation();
    }
    this.cancelit.emit();
  }
 
}

interface BatchData {
  supplier: string;
  batchName: string;
  totalPrice: number;
  paid: number;

}