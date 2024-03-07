import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { NavigationService } from '../services/navigation.service';
import { Product } from '../models/models';
import { FormsModule } from '@angular/forms';
import { UtilityService } from '../services/utility.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-search-results',
  templateUrl: './search-result.component.html',
  styleUrls: ['./search-result.component.css']
})
export class SearchResultsComponent implements OnInit {
  searchQuery: string = '';
  searchResults: Product[] = [];
  loading: boolean = false;
  error: boolean = false;
  //searchResults: any[] = [];
  view: string = 'grid'; // Define the `view` property
  constructor(
    public route: ActivatedRoute,
    public navigationService: NavigationService,
    private router: Router,
    public utilityService: UtilityService
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe((params: { [x: string]: string }) => {
      this.searchQuery = params['q'];

      this.searchProducts();
    });
  }
  viewProductDetails(product: any) {
    // Assuming you have a route defined for product details
    this.router.navigate(['/product-details', product.id]);
   }
   isAdmin(): boolean {
    const role = localStorage.getItem('role');
    return role === 'Admin';
  }
  searchProducts(): void {
    this.loading = true;
    this.navigationService.searchProducts(this.searchQuery).subscribe(
      (results: Product[]) => {
        this.searchResults = results;
        this.loading = false;
        this.error = false;
      },
      (error) => {
        this.loading = false;
        this.error = true;
      }
    );
  }
}