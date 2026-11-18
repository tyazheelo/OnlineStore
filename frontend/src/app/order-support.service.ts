import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiConfigurationService } from './api-configuration.service';

export interface DeliveryAddress {
  country: string;
  state: string | null;
  city: string;
  street: string;
  postalCode: string;
}

export interface OrderItem {
  productId: string;
  productName: string;
  quantity: number;
  price: number;
  totalCost: number;
}

export interface ShopOrder {
  id: string;
  customerId: string;
  customerName: string;
  customerEmail: string;
  orderNumber: string;
  items: OrderItem[];
  deliveryAddress: string;
  deliveryAddressDetails: DeliveryAddress;
  status: string;
  subTotal: number;
  deliveryCost: number;
  totalCost: number;
  paymentType: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface SupportMessage {
  id: string;
  authorId: string;
  authorName: string;
  isAdmin: boolean;
  message: string;
  createdAt: string;
}

export interface SupportTicket {
  id: string;
  orderId: string;
  orderNumber: string;
  customerId: string;
  customerName: string;
  assignedAdminId: string | null;
  status: 'Open' | 'InProgress' | 'Resolved';
  createdAt: string;
  updatedAt: string;
  messages: SupportMessage[];
}

@Injectable({ providedIn: 'root' })
export class OrderSupportService {
  private readonly http = inject(HttpClient);
  private readonly apiConfiguration = inject(ApiConfigurationService);

  getMyOrders(token: string): Observable<ShopOrder[]> {
    return this.http.get<ShopOrder[]>(`${this.apiConfiguration.orderApiUrl}/my-orders`, {
      headers: this.auth(token),
    });
  }

  getAllOrders(token: string): Observable<ShopOrder[]> {
    return this.http.get<ShopOrder[]>(this.apiConfiguration.orderApiUrl, {
      headers: this.auth(token),
    });
  }

  getMyTickets(token: string): Observable<SupportTicket[]> {
    return this.http.get<SupportTicket[]>(`${this.apiConfiguration.supportApiUrl}/my`, {
      headers: this.auth(token),
    });
  }

  getAllTickets(token: string): Observable<SupportTicket[]> {
    return this.http.get<SupportTicket[]>(`${this.apiConfiguration.supportApiUrl}/admin`, {
      headers: this.auth(token),
    });
  }

  createTicket(token: string, orderId: string, message: string): Observable<SupportTicket> {
    return this.http.post<SupportTicket>(
      this.apiConfiguration.supportApiUrl,
      { orderId, message },
      { headers: this.auth(token) },
    );
  }

  addMessage(token: string, ticketId: string, message: string): Observable<SupportTicket> {
    return this.http.post<SupportTicket>(
      `${this.apiConfiguration.supportApiUrl}/${ticketId}/messages`,
      { message },
      { headers: this.auth(token) },
    );
  }

  claimTicket(token: string, ticketId: string): Observable<SupportTicket> {
    return this.http.post<SupportTicket>(
      `${this.apiConfiguration.supportApiUrl}/${ticketId}/claim`,
      {},
      { headers: this.auth(token) },
    );
  }

  resolveTicket(token: string, ticketId: string): Observable<SupportTicket> {
    return this.http.post<SupportTicket>(
      `${this.apiConfiguration.supportApiUrl}/${ticketId}/resolve`,
      {},
      { headers: this.auth(token) },
    );
  }

  updateOrderStatus(token: string, orderId: string, status: string): Observable<ShopOrder> {
    return this.http.put<ShopOrder>(
      `${this.apiConfiguration.orderApiUrl}/${orderId}/status`,
      { status },
      { headers: this.auth(token) },
    );
  }

  updateOrderAddress(
    token: string,
    orderId: string,
    address: DeliveryAddress,
  ): Observable<ShopOrder> {
    return this.http.put<ShopOrder>(
      `${this.apiConfiguration.orderApiUrl}/${orderId}/address`,
      address,
      { headers: this.auth(token) },
    );
  }

  cancelOrder(token: string, orderId: string): Observable<ShopOrder> {
    return this.http.put<ShopOrder>(
      `${this.apiConfiguration.orderApiUrl}/${orderId}/cancel`,
      {},
      { headers: this.auth(token) },
    );
  }

  private auth(token: string): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }
}
