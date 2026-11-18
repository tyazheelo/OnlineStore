import { DatePipe, DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { LanguageService, SiteLanguage } from './language.service';
import {
  DeliveryAddress,
  OrderSupportService,
  ShopOrder,
  SupportMessage,
  SupportTicket,
} from './order-support.service';
import { ProductReviewReport } from './review-moderation.component';
import { ProductApiService } from './product-api.service';

const ADMIN_ORDERS_PAGE_SIZE = 5;
type OrderGroup = 'active' | 'cancelled' | 'completed';

interface AuthSession {
  userId: string;
  token: string;
  roles: string[];
  expiresAtUtc: string;
}

@Component({
  selector: 'app-admin-support-page',
  standalone: true,
  imports: [DatePipe, DecimalPipe],
  templateUrl: './admin-support-page.html',
  styleUrl: './admin-support-page.scss',
})
export class AdminSupportPage implements OnInit {
  private readonly productApi = inject(ProductApiService);
  private readonly orderSupport = inject(OrderSupportService);
  private readonly router = inject(Router);
  protected readonly language = inject(LanguageService);
  protected readonly orders = signal<ShopOrder[]>([]);
  protected readonly selectedOrderGroup = signal<OrderGroup>('active');
  protected readonly refreshingOrders = signal(false);
  protected readonly filteredOrders = computed(() => this.orders().filter(order => {
    switch (this.selectedOrderGroup()) {
      case 'cancelled':
        return order.status === 'Cancelled';
      case 'completed':
        return order.status === 'Delivered';
      default:
        return order.status !== 'Cancelled' && order.status !== 'Delivered';
    }
  }));
  protected readonly orderGroupCounts = computed(() => {
    const orders = this.orders();
    return {
      active: orders.filter(order => order.status !== 'Cancelled' && order.status !== 'Delivered').length,
      cancelled: orders.filter(order => order.status === 'Cancelled').length,
      completed: orders.filter(order => order.status === 'Delivered').length,
    };
  });
  protected readonly orderPage = signal(1);
  protected readonly orderPageCount = computed(() =>
    Math.max(1, Math.ceil(this.filteredOrders().length / ADMIN_ORDERS_PAGE_SIZE)),
  );
  protected readonly visibleOrders = computed(() => {
    const page = Math.min(this.orderPage(), this.orderPageCount());
    const start = (page - 1) * ADMIN_ORDERS_PAGE_SIZE;
    return this.filteredOrders().slice(start, start + ADMIN_ORDERS_PAGE_SIZE);
  });
  protected readonly tickets = signal<SupportTicket[]>([]);
  protected readonly reviewReports = signal<ProductReviewReport[]>([]);
  protected readonly reviewReportsLoading = signal(false);
  protected readonly reviewReportsError = signal('');
  protected readonly reviewReportBusy = signal('');
  protected readonly selectedTicketId = signal('');
  protected readonly currentAdminId = signal('');
  protected readonly replyDraft = signal('');
  protected readonly editingOrderId = signal('');
  protected readonly editingAddress = signal<DeliveryAddress | null>(null);
  protected readonly loading = signal(true);
  protected readonly busy = signal('');
  protected readonly orderStatusErrors = signal<Partial<Record<string, string>>>({});
  protected readonly errorMessage = signal('');
  protected readonly statusMessage = signal('');
  protected readonly orderStatuses = ['Pending', 'Paid', 'Processing', 'Shipped', 'Delivered'];

  protected t(text: string): string {
    if (this.language.current() === 'ru') {
      const statuses: Record<string, string> = {
        Pending: 'Ожидает оплаты',
        PaymentConfirmationPending: 'Подтверждается оплата',
        Paid: 'Оплачен',
        Processing: 'В обработке',
        Shipped: 'Отправлен',
        Delivered: 'Доставлен',
        Cancelled: 'Отменён',
        Open: 'Открыто',
        InProgress: 'В работе',
        Resolved: 'Закрыто',
      };
      if (statuses[text]) return statuses[text];
    }
    return this.language.translate(text);
  }

  protected setLanguage(language: SiteLanguage): void {
    this.language.set(language);
  }

  protected changeOrderPage(delta: number): void {
    this.orderPage.update(page => Math.max(1, Math.min(this.orderPageCount(), page + delta)));
  }

  protected selectOrderGroup(group: OrderGroup): void {
    this.selectedOrderGroup.set(group);
    this.orderPage.set(1);
  }

  protected refreshOrders(): void {
    const session = this.readSession();
    if (!session || this.refreshingOrders()) return;

    this.refreshingOrders.set(true);
    this.errorMessage.set('');
    this.orderSupport.getAllOrders(session.token).subscribe({
      next: orders => {
        this.orders.set([...orders].sort(
          (left, right) => new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime(),
        ));
        this.orderPage.set(1);
        this.refreshingOrders.set(false);
      },
      error: error => {
        this.errorMessage.set(this.getError(error, 'Не удалось обновить список заказов.'));
        this.refreshingOrders.set(false);
      },
    });
  }

  protected openOrder(orderId: string): void {
    const order = this.orders().find(item => item.id === orderId);
    if (!order) return;
    const group: OrderGroup = order.status === 'Cancelled'
      ? 'cancelled'
      : order.status === 'Delivered' ? 'completed' : 'active';
    this.selectOrderGroup(group);
    const index = this.filteredOrders().findIndex(item => item.id === orderId);
    if (index < 0) return;
    this.orderPage.set(Math.floor(index / ADMIN_ORDERS_PAGE_SIZE) + 1);
    window.setTimeout(() => {
      document.getElementById(`order-${orderId}`)?.scrollIntoView({
        behavior: 'smooth',
        block: 'center',
      });
    });
  }

  protected reportReasonLabel(reason: string): string {
    const labels: Record<string, string> = {
      Profanity: 'Нецензурная брань',
      NotAboutProduct: 'Комментарий не относится к товару',
      SexualContent: 'Эротический контент',
      Spam: 'Спам или реклама',
      Harassment: 'Оскорбления или травля',
      Other: 'Другая причина',
    };
    return this.t(labels[reason] ?? reason);
  }

  protected supportMessageAuthor(message: SupportMessage, ticket: SupportTicket): string {
    const authorName = this.validSupportName(message.authorName);
    if (authorName) return authorName;
    return message.isAdmin
      ? this.t('Поддержка')
      : this.supportCustomerName(ticket);
  }

  protected supportCustomerName(ticket: SupportTicket): string {
    return this.validSupportName(ticket.customerName) || ticket.customerId || this.t('Пользователь');
  }

  private validSupportName(name: string | null | undefined): string {
    const value = name?.trim();
    return value && !['undefined', 'null'].includes(value.toLowerCase()) ? value : '';
  }

  protected resolveReviewReport(report: ProductReviewReport, hideReview: boolean): void {
    const session = this.readSession();
    if (!session || this.reviewReportBusy()) return;

    this.reviewReportBusy.set(report.id);
    this.reviewReportsError.set('');
    this.productApi.resolveReviewReport(report.id, hideReview, session.token).subscribe({
      next: () => {
        this.reviewReports.update(reports => reports.filter(item => item.id !== report.id));
        this.statusMessage.set(hideReview
          ? 'Отзыв скрыт и исключён из рейтинга.'
          : 'Жалоба отклонена.');
        this.reviewReportBusy.set('');
      },
      error: error => {
        this.reviewReportsError.set(this.getError(error, 'Не удалось обработать жалобу.'));
        this.reviewReportBusy.set('');
      },
    });
  }

  ngOnInit(): void {
    const session = this.readSession();
    if (!session) {
      this.errorMessage.set('Войдите в аккаунт администратора.');
      this.loading.set(false);
      return;
    }
    if (!session.roles.some(role => role.toLowerCase() === 'admin')) {
      this.errorMessage.set('Доступ запрещён. Для этого действия нужна роль администратора.');
      this.loading.set(false);
      return;
    }
    this.currentAdminId.set(session.userId);
    this.load(session.token);
    this.loadReviewReports(session.token);
  }

  protected selectedTicket(): SupportTicket | null {
    return this.tickets().find(ticket => ticket.id === this.selectedTicketId()) ?? null;
  }

  protected selectTicket(ticket: SupportTicket): void {
    this.selectedTicketId.set(ticket.id);
    this.replyDraft.set('');
    this.errorMessage.set('');
  }

  protected onReplyInput(event: Event): void {
    if (event.target instanceof HTMLTextAreaElement) this.replyDraft.set(event.target.value);
  }

  protected sendReply(event: Event): void {
    event.preventDefault();
    const ticket = this.selectedTicket();
    const session = this.readSession();
    const message = this.replyDraft().trim();
    if (!ticket || !session || message.length < 2 || this.busy()) return;

    this.busy.set(`ticket-${ticket.id}`);
    this.errorMessage.set('');
    this.orderSupport.addMessage(session.token, ticket.id, message).subscribe({
      next: updated => {
        this.replaceTicket(updated);
        this.replyDraft.set('');
        this.statusMessage.set('Ответ отправлен.');
        this.busy.set('');
      },
      error: error => this.fail(error, 'Не удалось отправить ответ.'),
    });
  }

  protected claimTicket(ticket: SupportTicket): void {
    const session = this.readSession();
    if (!session || this.busy()) return;
    this.busy.set(`ticket-${ticket.id}`);
    this.errorMessage.set('');
    this.orderSupport.claimTicket(session.token, ticket.id).subscribe({
      next: updated => {
        this.replaceTicket(updated);
        this.statusMessage.set('Обращение взято в работу.');
        this.busy.set('');
      },
      error: error => this.fail(error, 'Не удалось взять обращение в работу.'),
    });
  }

  protected resolveTicket(ticket: SupportTicket): void {
    const session = this.readSession();
    if (!session || this.busy()) return;
    this.busy.set(`ticket-${ticket.id}`);
    this.errorMessage.set('');
    this.orderSupport.resolveTicket(session.token, ticket.id).subscribe({
      next: updated => {
        this.replaceTicket(updated);
        this.statusMessage.set('Обращение закрыто.');
        this.busy.set('');
      },
      error: error => this.fail(error, 'Не удалось закрыть обращение.'),
    });
  }

  protected onOrderStatusChange(order: ShopOrder, event: Event): void {
    if (!(event.target instanceof HTMLSelectElement)) return;
    const session = this.readSession();
    if (!session || this.busy()) return;
    const status = event.target.value;
    const select = event.target;
    this.busy.set(`order-${order.id}`);
    this.orderStatusErrors.update(errors => ({ ...errors, [order.id]: undefined }));
    this.errorMessage.set('');
    this.orderSupport.updateOrderStatus(session.token, order.id, status).subscribe({
      next: updated => {
        this.replaceOrder(updated, order);
        this.statusMessage.set('Изменения заказа сохранены.');
        this.busy.set('');
      },
      error: error => {
        select.value = order.status;
        this.orderStatusErrors.update(errors => ({
          ...errors,
          [order.id]: this.getOrderStatusError(error),
        }));
        this.busy.set('');
      },
    });
  }

  private getOrderStatusError(error: unknown): string {
    if (!(error instanceof HttpErrorResponse)) return 'Не удалось изменить статус заказа.';
    if (error.error?.error === 'Orders can only be cancelled through the cancel order action.')
      return 'Отменить заказ можно только кнопкой отмены.';
    if (error.status === 409) return 'Нельзя изменить статус заказа из текущего состояния.';
    if (error.status === 503) return 'Сервис резервирования товара временно недоступен.';
    return this.getError(error, 'Не удалось изменить статус заказа.');
  }

  protected beginAddressEdit(order: ShopOrder): void {
    this.editingOrderId.set(order.id);
    this.editingAddress.set({ ...order.deliveryAddressDetails });
    this.errorMessage.set('');
  }

  protected cancelAddressEdit(): void {
    this.editingOrderId.set('');
    this.editingAddress.set(null);
  }

  protected onAddressInput(field: keyof DeliveryAddress, event: Event): void {
    if (!(event.target instanceof HTMLInputElement)) return;
    const current = this.editingAddress();
    if (!current) return;
    const value = event.target.value;
    this.editingAddress.set({ ...current, [field]: field === 'state' ? value || null : value });
  }

  protected saveAddress(order: ShopOrder, event: Event): void {
    event.preventDefault();
    const session = this.readSession();
    const address = this.editingAddress();
    if (!session || !address || this.busy()) return;

    this.busy.set(`order-${order.id}`);
    this.errorMessage.set('');
    this.orderSupport.updateOrderAddress(session.token, order.id, address).subscribe({
      next: updated => {
        this.replaceOrder(updated, order);
        this.cancelAddressEdit();
        this.statusMessage.set('Адрес доставки сохранён.');
        this.busy.set('');
      },
      error: error => this.fail(error, 'Не удалось изменить адрес доставки.'),
    });
  }

  protected cancelOrder(order: ShopOrder): void {
    const session = this.readSession();
    if (!session || this.busy() || !window.confirm(`${this.t('Отменить заказ')} ${order.orderNumber}?`)) return;
    this.busy.set(`order-${order.id}`);
    this.errorMessage.set('');
    this.orderSupport.cancelOrder(session.token, order.id).subscribe({
      next: updated => {
        this.replaceOrder(updated, order);
        this.statusMessage.set('Заказ отменён, резерв товара освобождён.');
        this.busy.set('');
      },
      error: error => this.fail(error, 'Не удалось отменить заказ.'),
    });
  }

  protected goToStore(): void {
    void this.router.navigateByUrl('/');
  }

  private load(token: string): void {
    this.loading.set(true);
    forkJoin({
      orders: this.orderSupport.getAllOrders(token),
      tickets: this.orderSupport.getAllTickets(token),
    }).subscribe({
      next: data => {
        this.orders.set([...data.orders].sort(
          (left, right) => new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime(),
        ));
        this.tickets.set(data.tickets);
        this.selectedTicketId.set(data.tickets[0]?.id ?? '');
        this.loading.set(false);
      },
      error: error => {
        this.errorMessage.set(this.getError(error, 'Не удалось загрузить заказы и обращения.'));
        this.loading.set(false);
      },
    });
  }

  private loadReviewReports(token: string): void {
    this.reviewReportsLoading.set(true);
    this.reviewReportsError.set('');
    this.productApi.getReviewReports(token).subscribe({
      next: reports => this.reviewReports.set(reports),
      error: error => {
        this.reviewReportsError.set(this.getError(error, 'Не удалось загрузить список жалоб.'));
        this.reviewReportsLoading.set(false);
      },
      complete: () => this.reviewReportsLoading.set(false),
    });
  }

  private replaceTicket(ticket: SupportTicket): void {
    this.tickets.update(tickets => tickets.map(item => item.id === ticket.id ? ticket : item));
  }

  private replaceOrder(updated: ShopOrder, current: ShopOrder): void {
    this.orders.update(orders => orders.map(order => order.id === updated.id
      ? { ...updated, customerName: current.customerName, customerEmail: current.customerEmail }
      : order).sort(
        (left, right) => new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime(),
      ));
  }

  private fail(error: unknown, fallback: string): void {
    this.errorMessage.set(this.getError(error, fallback));
    this.busy.set('');
  }

  private getError(error: unknown, fallback: string): string {
    if (!(error instanceof HttpErrorResponse)) return fallback;
    if (error.status === 401) return 'Сессия истекла. Войдите в аккаунт снова.';
    if (error.status === 403) return 'Доступ запрещён. Для этого действия нужна роль администратора.';
    if (error.status === 0) return 'Нет соединения с Order API или Identity API.';
    return error.error?.error ?? fallback;
  }

  private readSession(): AuthSession | null {
    const value = window.sessionStorage.getItem('forma.auth');
    if (!value) return null;

    try {
      const session = JSON.parse(value) as AuthSession;
      if (!session.token || new Date(session.expiresAtUtc) <= new Date()) {
        window.sessionStorage.removeItem('forma.auth');
        return null;
      }
      return session;
    } catch (error) {
      console.warn('Could not read the saved FORMA session on admin page.', error);
      window.sessionStorage.removeItem('forma.auth');
      return null;
    }
  }
}
