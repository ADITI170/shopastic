import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { User } from '../models/models';
import { Product } from '../models/models';

@Component({
  selector: 'app-admin-page',
  templateUrl: './admin-page.component.html',
  styleUrls: ['./admin-page.component.css']
})
export class AdminPageComponent {
  users: User[] = [];
  products: Product[] = [];
  editProductId: number | null = null; // Updated type of editProductId to be nullable
  showUsersTable: boolean = false;
  showProductsTable: boolean = false;
  constructor(private http: HttpClient) {}

  getUsersData(): void {
    this.http.get<User[]>('https://localhost:7149/api/Shopping/GetAllUsers').subscribe(data => {
      this.users = data;
      this.showUsersTable = true;
      this.showProductsTable = false;
      console.log(this.users);
    });
  }

  deleteUser(id: number): void {
    this.http.delete(`https://localhost:7149/api/Shopping/DeleteUser/${id}`).subscribe(response => {
      console.log(response);
      this.users = this.users.filter(user => user.id !== id);
    }, error => {
      console.log(error);
    });
  }

  getProductsData(): void {
    this.http.get<Product[]>('https://localhost:7149/api/Shopping/GetAllProducts').subscribe(data => {
      this.products = data;
      this.showUsersTable = false;
      this.showProductsTable = true;
      console.log(this.products);
    });
  }

  DelProd(id: number): void {
    this.http.delete(`https://localhost:7149/api/Shopping/DeleteProduct/${id}`).subscribe(response => {
      console.log(response);
      this.products = this.products.filter(product => product.id !== id);
    }, error => {
      console.log(error);
    });
  }

  editProduct(id: number): void {

    const updatedProduct: Partial<Product> = {};
    const titleCell = document.querySelector(`#title-${id}`);
    const descriptionCell = document.querySelector(`#description-${id}`);
    const quantityCell = document.querySelector(`#quantity-${id}`);
  
    if (titleCell && descriptionCell && quantityCell) {
      updatedProduct.title = (titleCell as HTMLTableCellElement).innerText.trim();
      updatedProduct.description = (descriptionCell as HTMLTableCellElement).innerText.trim();
      updatedProduct.quantity = parseInt((quantityCell as HTMLTableCellElement).innerText.trim(), 10);
    }
  
    this.http.put(`https://localhost:7149/api/Shopping/UpdateProduct/${id}`, updatedProduct).subscribe(response => {
      console.log(response);
      const index = this.products.findIndex(product => product.id === id);
      if (index !== -1) {
        this.products[index] = { ...this.products[index], ...updatedProduct };
      }
      this.editProductId = null;
    }, error => {
      console.log(error);
    });
  }
  saveChanges(productId: number): void {
    this.editProductId = null; // Exit the edit mode
  
    // Find the edited product by ID
    const editedProduct = this.products.find(product => product.id === productId);
    if (editedProduct) {
      // Send the updated product data to the API
      this.http.put(`https://localhost:7149/api/Shopping/UpdateProduct/${productId}`, editedProduct)
        .subscribe(response => {
          console.log(response);
        }, error => {
          console.log(error);
        });
    }
  }
  startEditing(productId: number): void {
    this.editProductId = productId;
  }

  cancelEditing(): void {
    this.editProductId = null;
  }
}