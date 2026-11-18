import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CartCheckoutRequest } from './cart-drawer.component';
import { ApiConfigurationService } from './api-configuration.service';

export interface CartApiItemDto {
  productId: string;
  productNameSnapshot: string;
  quantity: number;
  unitPriceSnapshot: number;
  isAvailable: boolean;
}

export interface CartApiDto {
  items: CartApiItemDto[];
}

export interface OrderApiDto {
  orderNumber: string;
  status: string;
  subTotal: number;
  deliveryCost: number;
  totalCost: number;
}

@Injectable({ providedIn: 'root' })
export class CartApiService {
  private readonly http = inject(HttpClient);
  private readonly api = inject(ApiConfigurationService);

  getCart(token: string): Observable<CartApiDto> {
    return this.http.get<CartApiDto>(this.api.cartApiUrl, { headers: this.auth(token) });
  }

  addItem(productId: string, quantity: number, token: string): Observable<void> {
    return this.http.post<void>(
      `${this.api.cartApiUrl}/items`,
      { productId, quantity },
      { headers: this.auth(token) },
    );
  }

  updateItem(productId: string, quantity: number, token: string): Observable<void> {
    return this.http.put<void>(
      `${this.api.cartApiUrl}/items/${productId}`,
      { quantity },
      { headers: this.auth(token) },
    );
  }

  removeItem(productId: string, token: string): Observable<void> {
    return this.http.delete<void>(`${this.api.cartApiUrl}/items/${productId}`, {
      headers: this.auth(token),
    });
  }

  checkout(details: CartCheckoutRequest, token: string): Observable<OrderApiDto> {
    return this.http.post<OrderApiDto>(`${this.api.orderApiUrl}/checkout`, details, {
      headers: this.auth(token),
    });
  }

  private auth(token: string): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }
}
