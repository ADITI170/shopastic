import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Cart } from '../models/models';
import { NavigationService } from '../services/navigation.service';
import { UtilityService } from '../services/utility.service';

@Component({
  selector: 'app-account',
  templateUrl: './account.component.html',
  styleUrls: ['./account.component.css'],
})


export class AccountComponent implements OnInit {

  constructor( 
    private navigationService: NavigationService,
    public utilityService: UtilityService,
    private router: Router
    ) {}
    message = '';
    classname = '';

usersPreviousCarts: Cart[] = [];
  ngOnInit(): void {
      // Get Previous Carts
      this.navigationService
      .getAllPreviousCarts(this.utilityService.getUser().id)
      .subscribe((res: any) => {
        this.usersPreviousCarts = res;
      });
  }

  cancelOrder(cart: Cart) {
    this.navigationService.deletePreviousCart(
      this.utilityService.getUser().id,
      cart.id
    ).subscribe(
      () => {
        this.router.navigate(['/account']);
      }
    )
    this.classname = "text-success";
    this.message = "Order Cancelled, Refund Initiated."
  }
  
  

  returnOrder(cart: any) {
    // Add the logic to return the order
    console.log('Return order:', cart);
  }
}