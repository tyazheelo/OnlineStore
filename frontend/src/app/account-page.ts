import { HttpErrorResponse } from '@angular/common/http';
import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { LanguageService } from './language.service';
import { OrderSupportService, ShopOrder, SupportTicket } from './order-support.service';
import { IdentityApiService, UserProfile } from './identity-api.service';

const ACCOUNT_ORDERS_PAGE_SIZE = 5;
type OrderGroup = 'active' | 'cancelled' | 'completed';

interface AuthSession {
  token: string;
  roles: string[];
  firstName: string;
  lastName: string;
  expiresAtUtc: string;
}

@Component({
  selector: 'app-account-page',
  standalone: true,
  imports: [DatePipe, DecimalPipe],
  templateUrl: './account-page.html',
  styleUrl: './account-page.scss',
})
export class AccountPage implements OnInit {
  private readonly identityApi = inject(IdentityApiService);
  private readonly router = inject(Router);
  private readonly orderSupport = inject(OrderSupportService);
  protected readonly language = inject(LanguageService);

  protected readonly profile = signal<UserProfile | null>(null);
  protected readonly roles = signal<string[]>([]);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly editing = signal(false);
  protected readonly errorMessage = signal('');
  protected readonly statusMessage = signal('');
  protected readonly firstName = signal('');
  protected readonly lastName = signal('');
  protected readonly userName = signal('');
  protected readonly phoneNumber = signal('');
  protected readonly orders = signal<ShopOrder[]>([]);
  protected readonly selectedOrderGroup = signal<OrderGroup>('active');
  protected readonly expandedOrders = signal<Record<string, boolean>>({});
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
    Math.max(1, Math.ceil(this.filteredOrders().length / ACCOUNT_ORDERS_PAGE_SIZE)),
  );
  protected readonly visibleOrders = computed(() => {
    const page = Math.min(this.orderPage(), this.orderPageCount());
    const start = (page - 1) * ACCOUNT_ORDERS_PAGE_SIZE;
    return this.filteredOrders().slice(start, start + ACCOUNT_ORDERS_PAGE_SIZE);
  });
  protected readonly tickets = signal<SupportTicket[]>([]);
  protected readonly expandedTickets = signal<Record<string, boolean>>({});
  protected readonly ordersLoading = signal(false);
  protected readonly supportLoading = signal(false);
  protected readonly supportBusy = signal(false);
  protected readonly supportError = signal('');
  protected readonly supportMessage = signal('');
  protected readonly supportOrderId = signal('');
  protected readonly supportDraft = signal('');
  protected readonly replyDrafts = signal<Partial<Record<string, string>>>({});
  protected readonly replyingTo = signal('');

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
        'By card': 'Банковская карта',
        Open: 'Открыто',
        InProgress: 'В работе',
        Resolved: 'Закрыто',
      };
      if (statuses[text]) return statuses[text];
    }
    return this.language.translate(text);
  }

  ngOnInit(): void {
    const session = this.readSession();
    if (!session) {
      this.errorMessage.set('Войдите в аккаунт, чтобы посмотреть данные профиля.');
      this.loading.set(false);
      return;
    }

    this.roles.set(session.roles ?? []);
    this.loadCustomerActivity(session.token);
    this.identityApi.getUserProfile(session.token).subscribe({
      next: (profile) => {
        this.profile.set(profile);
        this.firstName.set(profile.firstName);
        this.lastName.set(profile.lastName);
        this.userName.set(profile.userName);
        this.phoneNumber.set(profile.phoneNumber ?? '');
        this.loading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage.set(error.status === 401
          ? 'Сессия истекла. Войдите в аккаунт снова.'
          : 'Не удалось загрузить данные профиля. Попробуйте позже.');
        this.loading.set(false);
      },
    });
  }

  protected onTextInput(event: Event, field: 'firstName' | 'lastName' | 'userName' | 'phoneNumber'): void {
    if (!(event.target instanceof HTMLInputElement)) return;
    const value = event.target.value;
    if (field === 'firstName') this.firstName.set(value);
    else if (field === 'lastName') this.lastName.set(value);
    else if (field === 'userName') this.userName.set(value);
    else this.phoneNumber.set(value);
    this.errorMessage.set('');
    this.statusMessage.set('');
  }

  protected saveProfile(event: Event): void {
    event.preventDefault();
    const session = this.readSession();
    if (!session || this.saving()) return;

    this.saving.set(true);
    this.errorMessage.set('');
    this.statusMessage.set('');
    this.identityApi.updateUserProfile({
      firstName: this.firstName().trim(),
      lastName: this.lastName().trim(),
      userName: this.userName().trim(),
      phoneNumber: this.phoneNumber().trim() || null,
    }, session.token).subscribe({
      next: (profile) => {
        this.profile.set(profile);
        this.firstName.set(profile.firstName);
        this.lastName.set(profile.lastName);
        this.userName.set(profile.userName);
        this.phoneNumber.set(profile.phoneNumber ?? '');
        session.firstName = profile.firstName;
        session.lastName = profile.lastName;
        window.sessionStorage.setItem('forma.auth', JSON.stringify(session));
        this.editing.set(false);
        this.statusMessage.set('Данные профиля сохранены.');
        this.saving.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage.set(error.status === 401
          ? 'Сессия истекла. Войдите в аккаунт снова.'
          : this.getSaveError(error));
        this.saving.set(false);
      },
    });
  }

  protected cancelEditing(): void {
    const profile = this.profile();
    if (profile) {
      this.firstName.set(profile.firstName);
      this.lastName.set(profile.lastName);
      this.userName.set(profile.userName);
      this.phoneNumber.set(profile.phoneNumber ?? '');
    }
    this.editing.set(false);
    this.errorMessage.set('');
  }

  protected goToStore(): void {
    void this.router.navigateByUrl('/');
  }

  protected isAdmin(): boolean {
    return this.roles().some((role) => role.toLowerCase() === 'admin');
  }

  protected changeOrderPage(delta: number): void {
    this.orderPage.update(page => Math.max(1, Math.min(this.orderPageCount(), page + delta)));
  }

  protected selectOrderGroup(group: OrderGroup): void {
    this.selectedOrderGroup.set(group);
    this.orderPage.set(1);
  }

  protected isOrderExpanded(orderId: string): boolean {
    return this.expandedOrders()[orderId] ?? false;
  }

  protected toggleOrder(orderId: string): void {
    this.expandedOrders.update(expanded => ({
      ...expanded,
      [orderId]: !expanded[orderId],
    }));
  }

  protected onSupportOrderChange(event: Event): void {
    if (event.target instanceof HTMLSelectElement) this.supportOrderId.set(event.target.value);
  }

  protected onSupportDraftInput(event: Event): void {
    if (event.target instanceof HTMLTextAreaElement) this.supportDraft.set(event.target.value);
  }

  protected onReplyDraftInput(ticketId: string, event: Event): void {
    if (!(event.target instanceof HTMLTextAreaElement)) return;
    const value = event.target.value;
    this.replyDrafts.update(drafts => ({ ...drafts, [ticketId]: value }));
  }

  protected isTicketExpanded(ticketId: string): boolean {
    return this.expandedTickets()[ticketId] ?? false;
  }

  protected toggleTicket(ticketId: string): void {
    this.expandedTickets.update(expanded => ({
      ...expanded,
      [ticketId]: !expanded[ticketId],
    }));
  }

  protected createSupportRequest(event: Event): void {
    event.preventDefault();
    const session = this.readSession();
    const orderId = this.supportOrderId();
    const message = this.supportDraft().trim();
    if (!session || !orderId || message.length < 2 || this.supportBusy()) return;

    this.supportBusy.set(true);
    this.supportError.set('');
    this.supportMessage.set('');
    this.orderSupport.createTicket(session.token, orderId, message).subscribe({
      next: ticket => {
        this.tickets.update(tickets => [ticket, ...tickets]);
        this.supportDraft.set('');
        this.supportOrderId.set('');
        this.supportMessage.set('Обращение отправлено. Ответ появится здесь.');
        this.supportBusy.set(false);
      },
      error: error => {
        this.supportError.set(this.getSupportError(error, 'Не удалось отправить обращение.'));
        this.supportBusy.set(false);
      },
    });
  }

  protected replyToSupport(ticket: SupportTicket, event: Event): void {
    event.preventDefault();
    const session = this.readSession();
    const message = (this.replyDrafts()[ticket.id] ?? '').trim();
    if (!session || message.length < 2 || this.replyingTo()) return;

    this.replyingTo.set(ticket.id);
    this.supportError.set('');
    this.supportMessage.set('');
    this.orderSupport.addMessage(session.token, ticket.id, message).subscribe({
      next: updated => {
        this.tickets.update(tickets => tickets.map(item => item.id === updated.id ? updated : item));
        this.replyDrafts.update(drafts => ({ ...drafts, [ticket.id]: '' }));
        this.supportMessage.set('Ответ отправлен.');
        this.replyingTo.set('');
      },
      error: error => {
        this.supportError.set(this.getSupportError(error, 'Не удалось отправить сообщение.'));
        this.replyingTo.set('');
      },
    });
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
      console.warn('Could not read the saved FORMA session on account page.', error);
      window.sessionStorage.removeItem('forma.auth');
      return null;
    }
  }

  private loadCustomerActivity(token: string): void {
    this.ordersLoading.set(true);
    this.orderSupport.getMyOrders(token).subscribe({
      next: orders => {
        this.orders.set([...orders].sort(
          (left, right) => new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime(),
        ));
        this.ordersLoading.set(false);
      },
      error: error => {
        this.supportError.set(this.getSupportError(error, 'Не удалось загрузить заказы.'));
        this.ordersLoading.set(false);
      },
    });
    this.supportLoading.set(true);
    this.orderSupport.getMyTickets(token).subscribe({
      next: tickets => {
        this.tickets.set(tickets);
        this.supportLoading.set(false);
      },
      error: error => {
        this.supportError.set(this.getSupportError(error, 'Не удалось загрузить обращения.'));
        this.supportLoading.set(false);
      },
    });
  }

  private getSupportError(error: HttpErrorResponse, fallback: string): string {
    if (error.status === 401) return 'Сессия истекла. Войдите в аккаунт снова.';
    if (error.status === 0) return 'Нет соединения с Order API.';
    return error.error?.error ?? fallback;
  }

  private getSaveError(error: HttpErrorResponse): string {
    const errors: unknown = error.error?.errors;
    if (Array.isArray(errors) && errors.length > 0) {
      return errors.map((message) => this.translateProfileError(String(message))).join(' ');
    }
    if (errors && typeof errors === 'object') {
      const messages = Object.values(errors)
        .flatMap((value) => Array.isArray(value) ? value : [value])
        .filter((value): value is string => typeof value === 'string');
      if (messages.length > 0) {
        return messages.map((message) => this.translateProfileError(message)).join(' ');
      }
    }
    if (error.status === 0) return 'Нет соединения с Identity API.';
    return 'Не удалось сохранить данные. Проверьте правильность имени и номера телефона.';
  }

  private translateProfileError(message: string): string {
    if (message === 'This username is already taken.' || /User name .* is already taken/i.test(message)) {
      return 'Имя пользователя уже занято.';
    }
    if (/regular expression|username.*invalid/i.test(message)) {
      return 'Имя пользователя может содержать латинские буквы, цифры и символы . _ + -';
    }
    if (/The FirstName field is required/i.test(message)) return 'Укажите имя.';
    if (/The LastName field is required/i.test(message)) return 'Укажите фамилию.';
    if (/The UserName field is required/i.test(message)) return 'Укажите имя пользователя.';
    if (/The PhoneNumber field is not a valid phone number/i.test(message)) return 'Укажите корректный номер телефона.';
    if (/The field (FirstName|LastName) must be a string with a maximum length of 100/i.test(message)) {
      return 'Имя и фамилия не должны быть длиннее 100 символов.';
    }
    if (/The field UserName must be a string with a minimum length of 3 and a maximum length of 256/i.test(message)) {
      return 'Имя пользователя должно содержать от 3 до 256 символов.';
    }
    if (/The field PhoneNumber must be a string with a maximum length of 32/i.test(message)) {
      return 'Номер телефона не должен быть длиннее 32 символов.';
    }
    return 'Проверьте введённые данные и попробуйте снова.';
  }
}
