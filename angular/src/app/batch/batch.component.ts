import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

interface Batch {
  batchName: string;
  total_price: number;
  paid: number;
  status: string;
  supplier_name: string;
  remaining: number;
}

@Component({
  selector: 'app-batch',
  standalone: true,
  imports: [CommonModule,FormsModule],
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
    console.log('Add New clicked');
    // Add navigation or modal logic here
  }

  onEdit(batch: Batch) {
    console.log('Edit clicked for:', batch);
    // Add edit logic here
  }

  onAddDetails(batch: Batch) {
    console.log('Add Details clicked for:', batch);
    // Add details logic here
  }
}