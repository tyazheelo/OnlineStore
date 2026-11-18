import { Component, computed, inject, input, output, signal } from '@angular/core';
import { LanguageService } from './language.service';
import { CartItem, Product } from './product.models';

export interface CartCheckoutRequest {
  country: string;
  state: string;
  city: string;
  street: string;
  postalCode: string;
  paymentType: 'ByCard';
}

export interface PlacedOrder {
  orderNumber: string;
  status: string;
  subTotal: number;
  deliveryCost: number;
  totalCost: number;
}

@Component({
  selector: 'app-cart-drawer',
  templateUrl: './cart-drawer.component.html',
  styleUrl: './cart-drawer.component.scss',
})
export class CartDrawerComponent {
  private readonly language = inject(LanguageService);
  readonly items = input<CartItem[]>([]);
  readonly opened = input(false);
  readonly authenticated = input(false);
  readonly cartBusy = input(false);
  readonly checkoutBusy = input(false);
  readonly cartError = input('');
  readonly hasUnsyncedItems = input(false);
  readonly checkoutError = input('');
  readonly placedOrder = input<PlacedOrder | null>(null);
  readonly closed = output<void>();
  readonly quantityChanged = output<{ productId: string | number; delta: number }>();
  readonly itemRemoved = output<string | number>();
  readonly productOpened = output<Product>();
  readonly authenticationRequested = output<void>();
  readonly cartSyncRequested = output<void>();
  readonly checkoutRequested = output<CartCheckoutRequest>();
  readonly orderNoticeDismissed = output<void>();

  protected readonly checkoutOpen = signal(false);
  protected readonly count = computed(() =>
    this.items().reduce((total, item) => total + item.quantity, 0),
  );
  protected readonly total = computed(() =>
    this.items().reduce((sum, item) => sum + item.product.price * item.quantity, 0),
  );
  protected readonly hasUnavailableItems = computed(() =>
    this.items().some((item) => item.available === false),
  );

  protected t(text: string): string {
    return this.language.translate(text);
  }

  protected productName(product: Product): string {
    return this.language.current() === 'en' && product.nameEn?.trim()
      ? product.nameEn
      : product.name;
  }

  protected productCategory(product: Product): string {
    return this.language.current() === 'en' && product.categoryEn?.trim()
      ? product.categoryEn
      : product.category;
  }

  protected formatPrice(price: number): string {
    return new Intl.NumberFormat(this.language.current() === 'ru' ? 'ru-RU' : 'en-US', {
      style: 'currency',
      currency: 'RUB',
      minimumFractionDigits: 0,
      maximumFractionDigits: 2,
    }).format(price);
  }

  protected openProduct(product: Product): void {
    this.productOpened.emit(product);
  }

  protected startCheckout(): void {
    if (!this.authenticated()) {
      this.authenticationRequested.emit();
      return;
    }

    this.checkoutOpen.set(true);
  }

  protected submitCheckout(event: Event): void {
    event.preventDefault();
    if (!(event.currentTarget instanceof HTMLFormElement)) return;

    const formData = new FormData(event.currentTarget);
    this.checkoutRequested.emit({
      country: this.getFormValue(formData, 'country'),
      state: this.getFormValue(formData, 'state'),
      city: this.getFormValue(formData, 'city'),
      street: this.getFormValue(formData, 'street'),
      postalCode: this.getFormValue(formData, 'postalCode'),
      paymentType: 'ByCard',
    });
  }

  protected cancelCheckout(): void {
    if (!this.checkoutBusy()) this.checkoutOpen.set(false);
  }

  protected dismissOrder(): void {
    this.checkoutOpen.set(false);
    this.orderNoticeDismissed.emit();
    this.closed.emit();
  }

  private getFormValue(formData: FormData, key: string): string {
    const value = formData.get(key);
    return typeof value === 'string' ? value.trim() : '';
  }

  protected close(): void {
    if (this.checkoutBusy()) return;
    this.checkoutOpen.set(false);
    this.closed.emit();
  }
}
