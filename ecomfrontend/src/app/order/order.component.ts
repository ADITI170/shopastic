


import { Component, OnInit } from '@angular/core';
import { FormControl } from '@angular/forms';
import { Router } from '@angular/router';
import { timer } from 'rxjs';
import { Cart, Order, Payment, PaymentMethod } from '../models/models';
import { NavigationService } from '../services/navigation.service';
import { UtilityService } from '../services/utility.service';

declare var Razorpay: any;

@Component({
  selector: 'app-order',
  templateUrl: './order.component.html',
  styleUrls: ['./order.component.css'],
})
export class OrderComponent implements OnInit {
  selectedPaymentMethodName = '';
  selectedPaymentMethod = new FormControl('0');

  address = '';
  mobileNumber = '';
  displaySpinner = false;
  message = '';
  classname = '';

  paymentMethods: PaymentMethod[] = [];

  usersCart: Cart = {
    id: 0,
    user: this.utilityService.getUser(),
    cartItems: [],
    ordered: false,
    orderedOn: '',
  };

  usersPaymentInfo: Payment = {
    id: 0,
    user: this.utilityService.getUser(),
    paymentMethod: {
      id: 0,
      type: '',
      provider: '',
      available: false,
      reason: '',
    },
    totalAmount: 0,
    shipingCharges: 0,
    amountReduced: 0,
    amountPaid: 0,
    createdAt: '',
  };

  constructor(
    private navigationService: NavigationService,
    public utilityService: UtilityService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Get Payment Methods
    this.navigationService.getPaymentMethods().subscribe((res) => {
      this.paymentMethods = res;
    });

    this.selectedPaymentMethod.valueChanges.subscribe((res: any) => {
      if (res === '0') this.selectedPaymentMethodName = '';
      else this.selectedPaymentMethodName = res.toString();
    });

    // Get Cart
    this.navigationService
      .getActiveCartOfUser(this.utilityService.getUser().id)
      .subscribe((res: any) => {
        this.usersCart = res;
        this.utilityService.calculatePayment(res, this.usersPaymentInfo);
      });

    // Set address and phone number
    this.address = this.utilityService.getUser().address;
    this.mobileNumber = this.utilityService.getUser().mobile;
  }

  getPaymentMethod(id: string) {
    let x = this.paymentMethods.find((v) => v.id === parseInt(id));
    return x?.type + ' - ' + x?.provider;
  }

  async placeOrder() {
    this.displaySpinner = true;
  
    if (this.usersPaymentInfo.totalAmount === 0) {
      this.displaySpinner = false;
      this.message = 'Cart is empty. Please add products to cart.';
      this.classname = 'text-danger';
      return;
    }
  
    try {
      let isPaymentSuccessful = await this.payMoney();
  
      if (!isPaymentSuccessful) {
        this.displaySpinner = false;
        this.message = 'Something went wrong! Payment failed!';
        this.classname = 'text-danger';
        return;
      }
  
      let step = 0;
      let count = timer(0, 3000).subscribe((res) => {
        ++step;
        if (step === 1) {
          this.message = 'Processing Payment';
          this.classname = 'text-success';
        }
        if (step === 2) {
          this.message = 'Payment Successful, Order is being placed.';
          this.storeOrder();
        }
        if (step === 3) {
          this.message = 'Your Order has been placed';
          this.displaySpinner = false;
        }
        if (step === 4) {
          this.router.navigateByUrl('/home');
          count.unsubscribe();
        }
      });
    } catch (error) {
      this.displaySpinner = false;
      this.message = 'Something went wrong! Payment did not happen!';
      this.classname = 'text-danger';
    }
  }

  async payMoney(): Promise<boolean> {
    return new Promise<boolean>((resolve, reject) => {
      const options = {
        key: 'rzp_test_fTAXDBq1XJfbfY',
        amount: this.usersPaymentInfo.amountPaid * 100,
        currency: 'INR',
        name: 'Shopify',
        description: 'Purchase Description',
        image: 'path_to_your_logo',
        order_id: '', // Set the order ID as per your implementation
        handler: (response: any) => {
          // Process the payment response
          if (response.razorpay_payment_id) {
            resolve(true);
          } else {
            reject(false);
          }
        },
        prefill: {
          name: 'Customer Name',
          email: 'customer@example.com',
          contact: 'customer_contact',
        },
        // Add any other options as required
      };

      const razorpayInstance = new Razorpay(options);
      razorpayInstance.open();
    });
  }

  storeOrder() {
    let payment: Payment;
    let pmid = 0;
    if (this.selectedPaymentMethod.value)
      pmid = parseInt(this.selectedPaymentMethod.value);

    payment = {
      id: 0,
      paymentMethod: {
        id: pmid,
        type: '',
        provider: '',
        available: false,
        reason: '',
      },
      user: this.utilityService.getUser(),
      totalAmount: this.usersPaymentInfo.totalAmount,
      shipingCharges: this.usersPaymentInfo.shipingCharges,
      amountReduced: this.usersPaymentInfo.amountReduced,
      amountPaid: this.usersPaymentInfo.amountPaid,
      createdAt: '',
    };

    this.navigationService
      .insertPayment(payment)
      .subscribe((paymentResponse: any) => {
        payment.id = parseInt(paymentResponse);
        let order: Order = {
          id: 0,
          user: this.utilityService.getUser(),
          cart: this.usersCart,
          payment: payment,
          createdAt: '',
        };
        this.navigationService.insertOrder(order).subscribe((orderResponse) => {
          this.utilityService.changeCart.next(0);
        });
      });
  }
}
