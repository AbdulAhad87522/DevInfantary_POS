import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';


interface InventoryItem {
  description: string;
  supplier: string;
  active: boolean;
  product: string;
  size: string;
  unit: string;
  class: string;
  pricePerUnit: number;
  pricePerLength: number;
  lengthFt: number;
  stock: number;
  reorder: number;
  minQty: number;
}

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './inventory.component.html',
  styleUrl: './inventory.component.css'
})
export class InventoryComponent {

// Dummy data (1 item as requested; in real app, fetch from DB/service)
  items: InventoryItem[] = [
    {
      description: 'High-quality PVC Pipe',
      supplier: 'ABC Suppliers',
      active: true,
      product: 'PVC Pipe',
      size: '2 inch',
      unit: 'Piece',
      class: 'A',
      pricePerUnit: 50,
      pricePerLength: 10,
      lengthFt: 20,
      stock: 150,
      reorder: 50,
      minQty: 30
    }
  ];

  // KPIs (derived or from DB; hardcoded for demo)
  totalItems = 1;
  lowStock = 0;
  outOfStock = 0;

  // Search term
  searchTerm = '';

  // Filter mode (for buttons)
  filter = 'all'; // 'all', 'low', 'out'

  // Filtered items (computed in real app)
  get filteredItems() {
    // Placeholder: In real app, filter based on searchTerm and this.filter
    return this.items.filter(item => 
      item.description.toLowerCase().includes(this.searchTerm.toLowerCase())
    );
  }

  // Button actions (placeholders)
  addProduct() { console.log('Add Product'); }
  addVariant() { console.log('Add Variant'); }
  showLowStock() { this.filter = 'low'; /* filter logic */ }
  showOutStock() { this.filter = 'out'; /* filter logic */ }
  showAll() { this.filter = 'all'; /* reset */ }

  // Edit actions
  editProduct(item: InventoryItem) { console.log('Edit Product:', item); }
  editVariant(item: InventoryItem) { console.log('Edit Variant:', item); }

}
