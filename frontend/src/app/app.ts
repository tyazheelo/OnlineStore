import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, computed, effect, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { catchError, concatMap, filter, from, map, Observable, of, toArray } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { LanguageService, SiteLanguage } from './language.service';
import { CartCheckoutRequest, CartDrawerComponent, PlacedOrder } from './cart-drawer.component';
import { ProductDetailComponent, ProductReviewSubmission } from './product-detail.component';
import {
  ProductReview,
  ProductReviewReport,
  ReviewModerationComponent,
  ReviewReportSubmission,
} from './review-moderation.component';
import { CartItem, Product } from './product.models';
import { CartApiDto, CartApiService, OrderApiDto } from './cart-api.service';
import { AuthResponse, IdentityApiService } from './identity-api.service';
import {
  CreateProductRequest,
  ProductApiDto,
  ProductCategoryDto,
  ProductReviewApiResponse,
  ProductApiService,
  UpdateProductRequest,
} from './product-api.service';

const CATALOG_PAGE_SIZE = 12;

interface ProductSpecification {
  key: string;
  value: string;
  keyEn: string;
  valueEn: string;
}

@Component({
  selector: 'app-root',
  imports: [DecimalPipe, RouterOutlet, ReviewModerationComponent, CartDrawerComponent, ProductDetailComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly cartApi = inject(CartApiService);
  private readonly identityApi = inject(IdentityApiService);
  private readonly productApi = inject(ProductApiService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly language = inject(LanguageService);
  protected readonly stars = [1, 2, 3, 4, 5];
  protected readonly accountPage = signal(false);
  protected readonly adminPage = signal(false);
  protected readonly categoryOptions = signal<ProductCategoryDto[]>([]);
  protected readonly categories = computed(() => [
    'Все товары',
    ...this.categoryOptions().map((category) => category.name),
  ]);
  protected readonly products = signal<Product[]>([]);
  protected readonly productsLoaded = signal(false);
  protected readonly productsError = signal('');

  protected readonly selectedCategory = signal('Все товары');
  protected readonly selectedTag = signal('Все метки');
  protected readonly tags = computed(() => [
    'Все метки',
    ...new Set(this.products().flatMap((product) => product.tags)),
  ]);
  protected readonly productTagSuggestions = computed(() =>
    [...new Set(this.products().flatMap((product) => product.tags))].sort((a, b) => a.localeCompare(b)),
  );
  protected readonly searchTerm = signal('');
  protected readonly minPrice = signal<number | null>(null);
  protected readonly maxPrice = signal<number | null>(null);
  protected readonly cartItems = signal<CartItem[]>([]);
  protected readonly cartOpen = signal(false);
  protected readonly cartBusy = signal(false);
  protected readonly cartError = signal('');
  protected readonly checkoutBusy = signal(false);
  protected readonly checkoutError = signal('');
  protected readonly placedOrder = signal<PlacedOrder | null>(null);
  protected readonly hasUnsyncedCartItems = computed(() =>
    this.cartItems().some((item) => item.synced === false),
  );
  protected readonly accountOpen = signal(false);
  protected readonly selectedProduct = signal<Product | null>(null);
  protected readonly email = signal('');
  protected readonly userName = signal('');
  protected readonly password = signal('');
  protected readonly firstName = signal('');
  protected readonly lastName = signal('');
  protected readonly authMode = signal<'login' | 'register'>('login');
  protected readonly currentUser = signal<AuthResponse | null>(this.readStoredUser());
  protected readonly isAdmin = computed(() =>
    this.currentUser()?.roles?.some((role) => role.toLowerCase() === 'admin') ?? false,
  );
  protected readonly adminProductOpen = signal(false);
  protected readonly adminProductBusy = signal(false);
  protected readonly adminProductMessage = signal('');
  protected readonly categoryCreationOpen = signal(false);
  protected readonly categoryCreationBusy = signal(false);
  protected readonly categoryCreationMessage = signal('');
  protected readonly newCategoryName = signal('');
  protected readonly newCategoryNameEn = signal('');
  protected readonly productBeingEdited = signal<Product | null>(null);
  protected readonly newProductName = signal('');
  protected readonly newProductNameEn = signal('');
  protected readonly newProductDescription = signal('');
  protected readonly newProductDescriptionEn = signal('');
  protected readonly newProductCategoryId = signal('');
  protected readonly newProductPrice = signal<number | null>(null);
  protected readonly newProductStock = signal<number | null>(null);
  protected readonly newProductImageUrl = signal('');
  protected readonly newProductTags = signal('');
  protected readonly productSpecifications = signal<ProductSpecification[]>([]);
  protected readonly authBusy = signal(false);
  protected readonly authMessage = signal('');
  protected readonly reviews = signal<ProductReview[]>([]);
  protected readonly reportOpen = signal(false);
  protected readonly reportReview = signal<ProductReview | null>(null);
  protected readonly reportBusy = signal(false);
  protected readonly reportMessage = signal('');
  protected readonly adminReportsOpen = signal(false);
  protected readonly adminReports = signal<ProductReviewReport[]>([]);
  protected readonly adminReportsBusy = signal(false);
  protected readonly adminReportsMessage = signal('');
  protected readonly moderationBusy = signal(false);
  protected readonly reviewsLoading = signal(false);
  protected readonly reviewRating = signal(5);
  protected readonly reviewComment = signal('');
  protected readonly reviewBusy = signal(false);
  protected readonly reviewMessage = signal('');
  protected readonly toastMessage = signal('');
  protected readonly invalidPriceRange = computed(() => {
    const minPrice = this.minPrice();
    const maxPrice = this.maxPrice();
    return minPrice !== null && maxPrice !== null && minPrice > maxPrice;
  });

  protected t(text: string): string {
    const passwordPrefix = 'Пароль должен содержать: ';
    if (text.startsWith(passwordPrefix) && this.language.current() === 'en') {
      const requirements = text.slice(passwordPrefix.length, -1)
        .split(', ')
        .map((requirement) => this.language.translate(requirement));
      return `Password must include: ${requirements.join(', ')}.`;
    }
    return this.language.translate(text);
  }

  protected setLanguage(language: SiteLanguage): void {
    this.language.set(language);
  }

  protected changeCatalogPage(delta: number): void {
    this.catalogPage.update(page => Math.max(1, Math.min(this.catalogPageCount(), page + delta)));
  }

  protected categoryLabel(category: string): string {
    if (category === 'Все товары') return this.t(category);
    const categoryOption = this.categoryOptions().find((item) => item.name === category);
    return this.language.current() === 'en' && categoryOption?.nameEn?.trim()
      ? categoryOption.nameEn
      : category;
  }

  protected categoryOptionLabel(category: ProductCategoryDto): string {
    return this.language.current() === 'en' && category.nameEn?.trim()
      ? category.nameEn
      : category.name;
  }

  protected productName(product: Product): string {
    return this.language.current() === 'en' && product.nameEn?.trim()
      ? product.nameEn
      : product.name;
  }

  protected productDescription(product: Product): string {
    return this.language.current() === 'en' && product.descriptionEn?.trim()
      ? product.descriptionEn
      : product.description;
  }

  protected productCategory(product: Product): string {
    return this.language.current() === 'en' && product.categoryEn?.trim()
      ? product.categoryEn
      : product.category;
  }

  protected tagLabel(tag: string): string {
    return this.t(tag);
  }

  protected reviewCountLabel(count: number): string {
    if (this.language.current() === 'en') return `${count} ${count === 1 ? 'review' : 'reviews'}`;
    const lastTwoDigits = count % 100;
    const lastDigit = count % 10;
    const word = lastTwoDigits >= 11 && lastTwoDigits <= 14
      ? 'отзывов'
      : lastDigit === 1
        ? 'отзыв'
        : lastDigit >= 2 && lastDigit <= 4
          ? 'отзыва'
          : 'отзывов';
    return `${count} ${word}`;
  }
  protected readonly cartCount = computed(() =>
    this.cartItems().reduce((count, item) => count + item.quantity, 0),
  );
  protected readonly filteredProducts = computed(() => {
    const category = this.selectedCategory();
    const selectedTag = this.selectedTag();
    const query = this.searchTerm().trim().toLocaleLowerCase(this.language.current() === 'ru' ? 'ru' : 'en');
    const minPrice = this.minPrice();
    const maxPrice = this.maxPrice();

    if (this.invalidPriceRange()) {
      return [];
    }

    return this.products().filter((product) => {
      const matchesCategory = category === 'Все товары' || product.category === category;
      const matchesTag = selectedTag === 'Все метки' || product.tags.includes(selectedTag);
      const matchesMinPrice = minPrice === null || product.price >= minPrice;
      const matchesMaxPrice = maxPrice === null || product.price <= maxPrice;
      const matchesQuery =
        !query ||
        `${product.name} ${product.nameEn ?? ''} ${product.category} ${product.categoryEn ?? ''} ${product.description} ${product.descriptionEn ?? ''}`
          .toLocaleLowerCase(this.language.current() === 'ru' ? 'ru' : 'en')
          .includes(query);

      return matchesCategory && matchesTag && matchesMinPrice && matchesMaxPrice && matchesQuery;
    });
  });
  protected readonly catalogPage = signal(1);
  protected readonly catalogPageCount = computed(() =>
    Math.max(1, Math.ceil(this.filteredProducts().length / CATALOG_PAGE_SIZE)),
  );
  protected readonly visibleProducts = computed(() => {
    const lastPage = this.catalogPageCount();
    const page = Math.min(this.catalogPage(), lastPage);
    const start = (page - 1) * CATALOG_PAGE_SIZE;
    return this.filteredProducts().slice(start, start + CATALOG_PAGE_SIZE);
  });

  constructor() {
    effect(() => {
      this.filteredProducts();
      this.catalogPage.set(1);
    });
    this.accountPage.set(this.isStandalonePage(this.router.url));
    this.adminPage.set(this.router.url.startsWith('/admin'));
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((event) => {
        this.accountPage.set(this.isStandalonePage(event.urlAfterRedirects));
        this.adminPage.set(event.urlAfterRedirects.startsWith('/admin'));
      });
    this.loadProducts();
    this.loadCategories();
  }

  protected formatPrice(price: number): string {
    return new Intl.NumberFormat(this.language.current() === 'ru' ? 'ru-RU' : 'en-US', {
      style: 'currency',
      currency: 'RUB',
      minimumFractionDigits: 0,
      maximumFractionDigits: 2,
    }).format(price);
  }

  protected addToCart(product: Product): void {
    if (this.cartBusy()) {
      this.cartError.set('Корзина обновляется. Повторите действие через мгновение.');
      return;
    }
    const user = this.currentUser();
    if (!user || this.hasUnsyncedCartItems()) {
      this.updateLocalCart(product, 1, false);
      this.showToast(`${this.productName(product)} ${this.t('Добавлен в корзину')}`);
      return;
    }

    if (typeof product.id !== 'string') {
      this.cartError.set('Не удалось определить товар для корзины.');
      return;
    }

    this.cartBusy.set(true);
    this.cartError.set('');
    this.cartApi.addItem(product.id, 1, user.token).subscribe({
      next: () => {
        if (this.currentUser()?.userId !== user.userId) return;
        this.updateLocalCart(product, 1, true);
        this.showToast(`${this.productName(product)} ${this.t('Добавлен в корзину')}`);
      },
      error: (error: HttpErrorResponse) => {
        if (this.currentUser()?.userId !== user.userId) return;
        this.cartError.set(this.getRequestError(error, 'Не удалось добавить товар в корзину.'));
        this.cartBusy.set(false);
      },
      complete: () => {
        if (this.currentUser()?.userId === user.userId) this.cartBusy.set(false);
      },
    });
  }

  protected openProduct(product: Product): void {
    this.cartOpen.set(false);
    this.selectedProduct.set(product);
    this.reviews.set([]);
    this.reviewMessage.set('');
    this.reviewRating.set(5);
    this.reviewComment.set('');
    if (typeof product.id !== 'string') {
      this.reviewMessage.set('Отзывы будут доступны после загрузки товара из каталога.');
      return;
    }

    this.reviewsLoading.set(true);
    const user = this.currentUser();
    this.loadProductReviews(product.id, user);
  }

  protected closeProduct(): void {
    this.selectedProduct.set(null);
    this.reviewsLoading.set(false);
  }

  protected openReportForm(review: ProductReview): void {
    const user = this.currentUser();
    if (!user) {
      this.authMessage.set('Войдите в аккаунт, чтобы пожаловаться на отзыв.');
      this.accountOpen.set(true);
      return;
    }
    if (this.isOwnReview(review, user)) {
      this.reviewMessage.set('Нельзя пожаловаться на собственный отзыв.');
      return;
    }
    this.reportReview.set(review);
    this.reportMessage.set('');
    this.reportOpen.set(true);
  }

  protected closeReportForm(): void {
    this.reportOpen.set(false);
    this.reportReview.set(null);
  }

  protected clearReportMessage(): void {
    this.reportMessage.set('');
  }

  protected submitReport(submission: ReviewReportSubmission): void {
    const product = this.selectedProduct();
    const review = this.reportReview();
    const user = this.currentUser();
    if (!product || typeof product.id !== 'string' || !review || !user) {
      this.reportMessage.set('Войдите в аккаунт, чтобы пожаловаться на отзыв.');
      return;
    }
    const productId = product.id;
    if (this.isOwnReview(review, user)) {
      this.reportMessage.set('Нельзя пожаловаться на собственный отзыв.');
      return;
    }
    if (this.reportBusy()) return;

    this.reportBusy.set(true);
    this.reportMessage.set('');
    this.productApi.submitReviewReport(productId, review.id, submission, user.token).subscribe({
      next: () => {
        this.closeReportForm();
        this.reviewMessage.set('Жалоба отправлена администратору на рассмотрение.');
      },
      error: (error: HttpErrorResponse) => {
        this.reportMessage.set(this.getRequestError(error, 'Не удалось отправить жалобу.'));
        this.reportBusy.set(false);
      },
      complete: () => this.reportBusy.set(false),
    });
  }

  protected openAdminSupport(): void {
    if (!this.isAdmin()) return;
    void this.router.navigateByUrl('/admin');
  }

  private loadAdminReports(): void {
    const user = this.currentUser();
    if (!user || !this.isAdmin()) return;
    this.adminReportsBusy.set(true);
    this.adminReportsMessage.set('');
    this.productApi.getReviewReports(user.token).subscribe({
      next: (reports) => this.adminReports.set(reports),
      error: (error: HttpErrorResponse) => {
        this.adminReportsMessage.set(this.getRequestError(error, 'Не удалось загрузить список жалоб.'));
        this.adminReportsBusy.set(false);
      },
      complete: () => this.adminReportsBusy.set(false),
    });
  }

  protected resolveAdminReport(report: ProductReviewReport, hideReview: boolean): void {
    const user = this.currentUser();
    if (!user || !this.isAdmin() || this.moderationBusy()) return;
    this.moderationBusy.set(true);
    this.productApi.resolveReviewReport(report.id, hideReview, user.token).subscribe({
      next: () => {
        this.adminReports.update((items) => items.filter((item) => item.id !== report.id));
        if (hideReview) {
          this.reviews.update((items) => items.filter((item) => item.id !== report.reviewId));
          this.loadProducts();
          this.loadAdminReports();
          this.reviewMessage.set('Отзыв скрыт и исключён из рейтинга.');
        }
      },
      error: (error: HttpErrorResponse) => {
        this.adminReportsMessage.set(this.getRequestError(error, 'Не удалось обработать жалобу.'));
        this.moderationBusy.set(false);
      },
      complete: () => this.moderationBusy.set(false),
    });
  }

  protected hideReview(review: ProductReview): void {
    const user = this.currentUser();
    if (!user || !this.isAdmin() || this.moderationBusy()) return;
    this.moderationBusy.set(true);
    this.productApi.hideReview(review.id, user.token).subscribe({
      next: () => {
        this.reviews.update((items) => items.filter((item) => item.id !== review.id));
        this.loadProducts();
        this.reviewMessage.set('Отзыв скрыт и исключён из рейтинга.');
      },
      error: (error: HttpErrorResponse) => {
        this.reviewMessage.set(this.getRequestError(error, 'Не удалось скрыть отзыв.'));
        this.moderationBusy.set(false);
      },
      complete: () => this.moderationBusy.set(false),
    });
  }

  protected changeQuantity(productId: string | number, delta: number): void {
    if (this.cartBusy()) return;
    const item = this.cartItems().find((cartItem) => cartItem.product.id === productId);
    if (!item) return;
    const quantity = item.quantity + delta;
    if (quantity <= 0) {
      this.removeFromCart(productId);
      return;
    }

    const user = this.currentUser();
    if (!user || item.synced === false) {
      this.cartItems.update((items) => items.map((cartItem) =>
        cartItem.product.id === productId ? { ...cartItem, quantity } : cartItem,
      ));
      return;
    }
    if (typeof productId !== 'string') {
      this.cartError.set('Не удалось определить товар в корзине.');
      return;
    }

    this.cartBusy.set(true);
    this.cartError.set('');
    this.cartApi.updateItem(String(productId), quantity, user.token).subscribe({
      next: () => {
        if (this.currentUser()?.userId !== user.userId) return;
        this.cartItems.update((items) => items.map((cartItem) =>
          cartItem.product.id === productId
            ? { ...cartItem, quantity, available: true, synced: true }
            : cartItem,
        ));
      },
      error: (error: HttpErrorResponse) => {
        if (this.currentUser()?.userId !== user.userId) return;
        this.cartError.set(this.getRequestError(error, 'Не удалось изменить количество товара.'));
        this.cartBusy.set(false);
      },
      complete: () => {
        if (this.currentUser()?.userId === user.userId) this.cartBusy.set(false);
      },
    });
  }

  protected removeFromCart(productId: string | number): void {
    if (this.cartBusy()) return;
    const item = this.cartItems().find((cartItem) => cartItem.product.id === productId);
    if (!item) return;
    const user = this.currentUser();
    if (!user || item.synced === false) {
      this.cartItems.update((items) => items.filter((cartItem) => cartItem.product.id !== productId));
      return;
    }
    if (typeof productId !== 'string') {
      this.cartError.set('Не удалось определить товар в корзине.');
      return;
    }

    this.cartBusy.set(true);
    this.cartError.set('');
    this.cartApi.removeItem(String(productId), user.token).subscribe({
      next: () => {
        if (this.currentUser()?.userId !== user.userId) return;
        this.cartItems.update((items) =>
          items.filter((cartItem) => cartItem.product.id !== productId),
        );
      },
      error: (error: HttpErrorResponse) => {
        if (this.currentUser()?.userId !== user.userId) return;
        this.cartError.set(this.getRequestError(error, 'Не удалось удалить товар из корзины.'));
        this.cartBusy.set(false);
      },
      complete: () => {
        if (this.currentUser()?.userId === user.userId) this.cartBusy.set(false);
      },
    });
  }

  protected requestCheckout(details: CartCheckoutRequest): void {
    const user = this.currentUser();
    if (!user) {
      this.requestCheckoutAuthentication();
      return;
    }
    if (this.checkoutBusy() || this.cartBusy()) return;
    if (this.hasUnsyncedCartItems()) {
      this.checkoutError.set('Сначала синхронизируйте корзину с сервером.');
      return;
    }
    if (this.cartItems().length === 0 || this.cartItems().some((item) => item.available === false)) {
      this.checkoutError.set('Проверьте наличие товаров в корзине перед оформлением.');
      return;
    }

    this.checkoutBusy.set(true);
    this.checkoutError.set('');
    this.placedOrder.set(null);
    this.cartApi.checkout(details, user.token).subscribe({
      next: (order) => {
        if (this.currentUser()?.userId !== user.userId) return;
        this.placedOrder.set({
          orderNumber: order.orderNumber,
          status: order.status,
          subTotal: order.subTotal,
          deliveryCost: order.deliveryCost,
          totalCost: order.totalCost,
        });
        this.cartItems.set([]);
        this.cartError.set('');
        this.loadUserCart();
      },
      error: (error: HttpErrorResponse) => {
        if (this.currentUser()?.userId !== user.userId) return;
        this.checkoutError.set(this.getRequestError(error, 'Не удалось создать заказ.'));
        this.checkoutBusy.set(false);
      },
      complete: () => {
        if (this.currentUser()?.userId === user.userId) this.checkoutBusy.set(false);
      },
    });
  }

  protected requestCheckoutAuthentication(): void {
    this.authMessage.set('');
    this.accountOpen.set(true);
  }

  protected retryCartSynchronization(): void {
    const pendingItems = this.cartItems().filter((item) => item.synced === false);
    const user = this.currentUser();
    if (!user || pendingItems.length === 0 || this.cartBusy()) return;

    this.cartBusy.set(true);
    this.cartError.set('');
    this.cartApi.getCart(user.token).subscribe({
      next: (cart) => {
        if (this.currentUser()?.userId !== user.userId) {
          this.cartBusy.set(false);
          return;
        }
        const remainingItems: CartItem[] = [];
        const requests = pendingItems.map((item) => {
          if (typeof item.product.id !== 'string') {
            remainingItems.push(item);
            return of(false);
          }

          const remoteItem = cart.items.find((candidate) => candidate.productId === item.product.id);
          if (remoteItem?.quantity === item.quantity) return of(true);

          const request = remoteItem
            ? this.cartApi.updateItem(item.product.id, item.quantity, user.token)
            : this.cartApi.addItem(item.product.id, item.quantity, user.token);
          return request.pipe(
            map(() => true),
            catchError((error: HttpErrorResponse) => {
              remainingItems.push(item);
              this.cartError.set(this.getRequestError(error, 'Не удалось синхронизировать корзину.'));
              return of(false);
            }),
          );
        });

        from(requests).pipe(
          concatMap((request) => request),
          toArray(),
        ).subscribe({
          next: () => {
            if (this.currentUser()?.userId !== user.userId) {
              this.cartBusy.set(false);
              return;
            }
            this.cartItems.set(remainingItems);
            this.loadUserCart(remainingItems);
          },
          error: (error: HttpErrorResponse) => {
            if (this.currentUser()?.userId !== user.userId) {
              this.cartBusy.set(false);
              return;
            }
            this.cartError.set(this.getRequestError(error, 'Не удалось синхронизировать корзину.'));
            this.cartBusy.set(false);
          },
        });
      },
      error: (error: HttpErrorResponse) => {
        if (this.currentUser()?.userId !== user.userId) {
          this.cartBusy.set(false);
          return;
        }
        this.cartError.set(this.getRequestError(error, 'Не удалось загрузить корзину с сервера.'));
        this.cartBusy.set(false);
      },
    });
  }

  protected closeCart(): void {
    this.cartOpen.set(false);
    this.checkoutError.set('');
    this.placedOrder.set(null);
  }

  protected dismissOrderNotice(): void {
    this.placedOrder.set(null);
    this.closeCart();
  }

  private updateLocalCart(product: Product, quantityDelta: number, synced: boolean): void {
    this.cartItems.update((items) => {
      const existingItem = items.find((item) => item.product.id === product.id);
      if (existingItem) {
        return items.map((item) => item.product.id === product.id
          ? {
              ...item,
              product,
              quantity: item.quantity + quantityDelta,
              available: synced ? true : undefined,
              synced,
            }
          : item);
      }

      return [...items, {
        product,
        quantity: quantityDelta,
        available: synced ? true : undefined,
        synced,
      }];
    });
  }

  protected onSearch(event: Event): void {
    if (event.target instanceof HTMLInputElement) {
      this.searchTerm.set(event.target.value);
    }
  }

  protected onPriceInput(event: Event, bound: 'min' | 'max'): void {
    if (event.target instanceof HTMLInputElement) {
      const value = event.target.value.trim();
      const price = value === '' ? null : Number(value);

      if (price === null || (Number.isFinite(price) && price >= 0)) {
        if (bound === 'min') {
          this.minPrice.set(price);
        } else {
          this.maxPrice.set(price);
        }
      }
    }
  }

  protected resetFilters(): void {
    this.searchTerm.set('');
    this.selectedCategory.set('Все товары');
    this.selectedTag.set('Все метки');
    this.minPrice.set(null);
    this.maxPrice.set(null);
  }

  protected onEmailInput(event: Event): void {
    if (event.target instanceof HTMLInputElement) {
      this.email.set(event.target.value);
      this.authMessage.set('');
    }
  }

  protected onUserNameInput(event: Event): void {
    if (event.target instanceof HTMLInputElement) {
      this.userName.set(event.target.value);
      this.authMessage.set('');
    }
  }

  protected onPasswordInput(event: Event): void {
    if (event.target instanceof HTMLInputElement) {
      this.password.set(event.target.value);
      this.authMessage.set('');
    }
  }

  protected onNameInput(event: Event, field: 'firstName' | 'lastName'): void {
    if (event.target instanceof HTMLInputElement) {
      (field === 'firstName' ? this.firstName : this.lastName).set(event.target.value);
      this.authMessage.set('');
    }
  }

  protected setReviewComment(comment: string): void {
    this.reviewComment.set(comment);
    this.reviewMessage.set('');
  }

  protected onNewProductTextInput(
    event: Event,
    field: 'name' | 'nameEn' | 'description' | 'descriptionEn' | 'imageUrl' | 'tags',
  ): void {
    if (!(event.target instanceof HTMLInputElement || event.target instanceof HTMLTextAreaElement)) return;
    const value = event.target.value;
    switch (field) {
      case 'name':
        this.newProductName.set(value);
        break;
      case 'nameEn':
        this.newProductNameEn.set(value);
        break;
      case 'description':
        this.newProductDescription.set(value);
        break;
      case 'descriptionEn':
        this.newProductDescriptionEn.set(value);
        break;
      case 'imageUrl':
        this.newProductImageUrl.set(value);
        break;
      case 'tags':
        this.newProductTags.set(value);
        break;
    }
    this.adminProductMessage.set('');
  }

  protected onNewProductNumberInput(event: Event, field: 'price' | 'stock'): void {
    if (!(event.target instanceof HTMLInputElement)) return;
    const rawValue = event.target.value.trim();
    const value = rawValue === '' ? null : Number(rawValue);
    if (value !== null && (!Number.isFinite(value) || value < 0)) return;

    if (field === 'price') this.newProductPrice.set(value);
    else this.newProductStock.set(value);
    this.adminProductMessage.set('');
  }

  protected onNewProductCategoryChange(event: Event): void {
    if (event.target instanceof HTMLSelectElement) {
      this.newProductCategoryId.set(event.target.value);
      this.adminProductMessage.set('');
    }
  }

  protected openCategoryCreation(): void {
    if (!this.isAdmin()) return;
    this.newCategoryName.set('');
    this.newCategoryNameEn.set('');
    this.categoryCreationMessage.set('');
    this.categoryCreationOpen.set(true);
  }

  protected closeCategoryCreation(): void {
    if (!this.categoryCreationBusy()) this.categoryCreationOpen.set(false);
  }

  protected onCategoryNameInput(event: Event): void {
    if (!(event.target instanceof HTMLInputElement)) return;
    this.newCategoryName.set(event.target.value);
    this.categoryCreationMessage.set('');
  }

  protected onCategoryNameEnInput(event: Event): void {
    if (!(event.target instanceof HTMLInputElement)) return;
    this.newCategoryNameEn.set(event.target.value);
    this.categoryCreationMessage.set('');
  }

  protected saveCategory(event: Event): void {
    event.preventDefault();
    const user = this.currentUser();
    const name = this.newCategoryName().trim();
    if (!user || !this.isAdmin()) {
      this.categoryCreationMessage.set('Добавлять категории может только администратор.');
      return;
    }
    if (!name) {
      this.categoryCreationMessage.set('Укажите название категории.');
      return;
    }
    if (this.categoryCreationBusy()) return;

    this.categoryCreationBusy.set(true);
    this.categoryCreationMessage.set('');
    this.productApi.createCategory(name, this.newCategoryNameEn().trim(), user.token).subscribe({
      next: (category) => {
        this.categoryOptions.update((categories) =>
          [...categories, category].sort((left, right) => left.name.localeCompare(right.name)),
        );
        this.newProductCategoryId.set(category.id);
        this.categoryCreationOpen.set(false);
        this.showToast(this.t('Категория добавлена'));
      },
      error: (error: HttpErrorResponse) => {
        this.categoryCreationMessage.set(this.getRequestError(
          error,
          'Не удалось добавить категорию.',
        ));
        this.categoryCreationBusy.set(false);
      },
      complete: () => this.categoryCreationBusy.set(false),
    });
  }

  protected openProductCreation(): void {
    if (!this.isAdmin()) return;
    this.productBeingEdited.set(null);
    this.adminProductMessage.set('');
    if (!this.newProductCategoryId() && this.categoryOptions().length > 0) {
      this.newProductCategoryId.set(this.categoryOptions()[0].id);
    }
    this.adminProductOpen.set(true);
  }

  protected openProductEdit(product: Product): void {
    if (!this.isAdmin()) return;
    if (typeof product.id !== 'string') {
      this.adminProductMessage.set('Не удалось определить товар для редактирования.');
      return;
    }

    this.productBeingEdited.set(product);
    this.newProductName.set(product.name);
    this.newProductNameEn.set(product.nameEn ?? '');
    this.newProductDescription.set(product.description);
    this.newProductDescriptionEn.set(product.descriptionEn ?? '');
    this.newProductCategoryId.set(
      this.categoryOptions().find((category) => category.name === product.category)?.id ?? '',
    );
    this.newProductPrice.set(product.price);
    this.newProductImageUrl.set(product.imageUrl);
    this.newProductTags.set(product.tags.join(', '));
    this.productSpecifications.set(
      Object.entries(product.specifications).map(([key, value]) => ({
        key,
        value,
        keyEn: product.specificationsEn?.[key]?.name ?? '',
        valueEn: product.specificationsEn?.[key]?.value ?? '',
      })),
    );
    this.adminProductMessage.set('');
    this.adminProductOpen.set(true);
  }

  protected toggleProductTag(tag: string): void {
    const tags = this.newProductTags().split(',').map((item) => item.trim()).filter(Boolean);
    const exists = tags.some((item) => item.toLocaleLowerCase() === tag.toLocaleLowerCase());
    this.newProductTags.set(exists
      ? tags.filter((item) => item.toLocaleLowerCase() !== tag.toLocaleLowerCase()).join(', ')
      : [...tags, tag].join(', '));
    this.adminProductMessage.set('');
  }

  protected isProductTagSelected(tag: string): boolean {
    return this.newProductTags().split(',').some((item) => item.trim().toLocaleLowerCase() === tag.toLocaleLowerCase());
  }

  protected addProductSpecification(): void {
    this.productSpecifications.update((items) => [...items, { key: '', value: '', keyEn: '', valueEn: '' }]);
  }

  protected updateProductSpecification(
    index: number,
    field: keyof ProductSpecification,
    event: Event,
  ): void {
    if (!(event.target instanceof HTMLInputElement)) return;
    const value = event.target.value;
    this.productSpecifications.update((items) =>
      items.map((item, itemIndex) => itemIndex === index
        ? { ...item, [field]: value }
        : item),
    );
    this.adminProductMessage.set('');
  }

  protected removeProductSpecification(index: number): void {
    this.productSpecifications.update((items) => items.filter((_, itemIndex) => itemIndex !== index));
    this.adminProductMessage.set('');
  }

  protected async importProductSpecifications(event: Event): Promise<void> {
    if (!(event.target instanceof HTMLInputElement)) return;
    const file = event.target.files?.[0];
    if (!file) return;
    event.target.value = '';

    try {
      const contents = await file.text();
      let parsed: unknown;
      try {
        parsed = JSON.parse(contents);
      } catch (error) {
        if (error instanceof SyntaxError) {
          this.adminProductMessage.set('Файл содержит некорректный JSON.');
          return;
        }
        throw error;
      }

      if (parsed === null || typeof parsed !== 'object' || Array.isArray(parsed)) {
        this.adminProductMessage.set('JSON должен быть объектом с названиями характеристик и строковыми значениями.');
        return;
      }
      const importedSpecifications: ProductSpecification[] = [];
      for (const [key, value] of Object.entries(parsed)) {
        if (!key.trim() || typeof value !== 'string' || !value.trim()) {
          this.adminProductMessage.set('Названия характеристик и их строковые значения не должны быть пустыми.');
          return;
        }
        importedSpecifications.push({ key, value, keyEn: '', valueEn: '' });
      }

      this.productSpecifications.set(importedSpecifications);
      this.adminProductMessage.set('');
    } catch (error) {
      console.error('Could not read product specifications JSON file.', error);
      this.adminProductMessage.set('Не удалось прочитать файл характеристик.');
    }
  }

  private getProductSpecifications(): Record<string, string> | null {
    const specifications: Record<string, string> = {};
    for (const { key, value } of this.productSpecifications()) {
      const normalizedKey = key.trim();
      const normalizedValue = value.trim();
      if (!normalizedKey && !normalizedValue) continue;
      if (!normalizedKey || !normalizedValue) {
        this.adminProductMessage.set('Заполните название и значение каждой характеристики.');
        return null;
      }
      if (Object.hasOwn(specifications, normalizedKey)) {
        this.adminProductMessage.set('Названия характеристик не должны повторяться.');
        return null;
      }
      specifications[normalizedKey] = normalizedValue;
    }
    return specifications;
  }

  private getProductSpecificationsEn(
    specifications: Record<string, string>,
  ): Record<string, { name: string; value: string }> | null {
    const translations: Record<string, { name: string; value: string }> = {};
    for (const { key, keyEn, valueEn } of this.productSpecifications()) {
      const sourceKey = key.trim();
      const translatedName = keyEn.trim();
      const translatedValue = valueEn.trim();
      if (!translatedName && !translatedValue) continue;
      if (!sourceKey || !Object.hasOwn(specifications, sourceKey) || !translatedName || !translatedValue) {
        this.adminProductMessage.set('Для перевода заполните английские название и значение характеристики.');
        return null;
      }
      translations[sourceKey] = { name: translatedName, value: translatedValue };
    }
    return translations;
  }

  protected canPreviewProductImage(): boolean {
    try {
      const url = new URL(this.newProductImageUrl());
      return url.protocol === 'https:' || url.protocol === 'http:';
    } catch (error) {
      if (error instanceof TypeError) return false;
      throw error;
    }
  }

  protected saveProduct(event: Event): void {
    event.preventDefault();
    const user = this.currentUser();
    if (!user || !this.isAdmin()) {
      this.adminProductMessage.set('Управлять товарами может только администратор.');
      return;
    }
    if (this.adminProductBusy()) return;

    const price = this.newProductPrice();
    const stockQuantity = this.newProductStock();
    const editingProduct = this.productBeingEdited();
    if (price === null || price <= 0 || !this.newProductCategoryId()
      || (!editingProduct && (stockQuantity === null || stockQuantity < 0))) {
      this.adminProductMessage.set(editingProduct
        ? 'Укажите категорию и цену больше нуля.'
        : 'Укажите категорию, цену больше нуля и остаток товара.');
      return;
    }
    const specifications = this.getProductSpecifications();
    if (specifications === null) return;
    const specificationsEn = this.getProductSpecificationsEn(specifications);
    if (specificationsEn === null) return;
    const productDetails: UpdateProductRequest = {
      name: this.newProductName().trim(),
      nameEn: this.newProductNameEn().trim(),
      description: this.newProductDescription().trim(),
      descriptionEn: this.newProductDescriptionEn().trim(),
      imageUrl: this.newProductImageUrl().trim() || null,
      specifications,
      specificationsEn,
      tags: this.newProductTags().split(',').map((tag) => tag.trim()).filter(Boolean),
      categoryId: this.newProductCategoryId(),
      price,
    };

    let request: Observable<ProductApiDto>;
    if (editingProduct) {
      request = this.productApi.updateProduct(String(editingProduct.id), productDetails, user.token);
    } else {
      if (stockQuantity === null) {
        this.adminProductMessage.set('Укажите категорию, цену больше нуля и остаток товара.');
        return;
      }
      request = this.productApi.createProduct({
        ...productDetails,
        stockQuantity,
      } satisfies CreateProductRequest, user.token);
    }

    this.adminProductBusy.set(true);
    this.adminProductMessage.set('');
    request.subscribe({
      next: () => {
        this.adminProductOpen.set(false);
        this.productBeingEdited.set(null);
        this.resetNewProductForm();
        this.loadProducts();
        this.showToast(editingProduct ? 'Товар обновлён' : 'Товар добавлен в каталог');
      },
      error: (error: HttpErrorResponse) => {
        this.adminProductMessage.set(this.getRequestError(
          error,
          editingProduct ? 'Не удалось сохранить изменения товара.' : 'Не удалось добавить товар.',
        ));
        this.adminProductBusy.set(false);
      },
      complete: () => this.adminProductBusy.set(false),
    });
  }

  protected closeProductCreation(): void {
    if (!this.adminProductBusy()) {
      this.adminProductOpen.set(false);
      this.productBeingEdited.set(null);
    }
  }

  protected openAccount(): void {
    if (this.currentUser()) {
      void this.router.navigateByUrl('/account');
      return;
    }

    this.authMessage.set('');
    this.accountOpen.set(true);
  }

  protected submitAuth(event: Event): void {
    event.preventDefault();
    if (this.authBusy()) return;

    const isRegister = this.authMode() === 'register';
    if (isRegister) {
      const passwordRequirements = this.getPasswordRequirements(this.password());
      if (passwordRequirements.length > 0) {
        this.authMessage.set(`Пароль должен содержать: ${passwordRequirements.join(', ')}.`);
        return;
      }
    }

    this.authBusy.set(true);
    this.authMessage.set('');
    const request = isRegister
      ? this.identityApi.register(
          this.firstName().trim(),
          this.lastName().trim(),
          this.userName().trim(),
          this.email().trim(),
          this.password(),
        )
      : this.identityApi.login(this.email().trim(), this.password());

    request.subscribe({
      next: (user) => {
        const guestItems = this.cartItems().filter((item) => item.synced !== true);
        this.currentUser.set(user);
        window.sessionStorage.setItem('forma.auth', JSON.stringify(user));
        this.syncGuestCart(guestItems);
        this.password.set('');
        this.userName.set('');
        this.authMessage.set('');
        this.accountOpen.set(false);
        void this.router.navigateByUrl('/');
      },
      error: (error: HttpErrorResponse) => {
        this.authMessage.set(this.getRequestError(error, 'Не удалось выполнить вход.'));
        this.authBusy.set(false);
      },
      complete: () => this.authBusy.set(false),
    });
  }

  protected toggleAuthMode(): void {
    this.authMode.update((mode) => (mode === 'login' ? 'register' : 'login'));
    this.authMessage.set('');
  }

  protected logout(): void {
    if (this.checkoutBusy()) {
      this.showToast('Дождитесь завершения оформления заказа');
      return;
    }
    window.sessionStorage.removeItem('forma.auth');
    this.currentUser.set(null);
    this.cartItems.set([]);
    this.cartOpen.set(false);
    this.cartError.set('');
    this.cartBusy.set(false);
    this.checkoutError.set('');
    this.placedOrder.set(null);
    void this.router.navigateByUrl('/');
    this.showToast('Вы вышли из аккаунта');
  }

  protected submitReview(submission: ProductReviewSubmission): void {
    const product = this.selectedProduct();
    const user = this.currentUser();
    if (!product || typeof product.id !== 'string') return;
    if (!user) {
      this.reviewMessage.set('Войдите в аккаунт, чтобы оставить отзыв.');
      return;
    }
    const productId = product.id;

    this.reviewBusy.set(true);
    this.reviewMessage.set('');
    this.productApi.submitReview(
      productId,
      submission.rating,
      submission.comment || null,
      user.token,
    ).subscribe({
      next: response => {
        const savedReview = { ...this.mapProductReview(response, user), isOwnReview: true };
        this.reviews.update((items) => {
          const currentIndex = items.findIndex((item) => item.id === savedReview.id);
          if (currentIndex < 0) return [savedReview, ...items];
          return items.map((item, index) => index === currentIndex ? savedReview : item);
        });
        this.reviewComment.set('');
        this.reviewRating.set(5);
        this.reviewMessage.set('Ваш отзыв сохранён.');
        this.reviewsLoading.set(true);
        this.loadProductReviews(productId, user);
        this.loadProducts();
      },
      error: (error: HttpErrorResponse) => {
        this.reviewMessage.set(this.getRequestError(error, 'Не удалось сохранить отзыв.'));
        this.reviewBusy.set(false);
      },
      complete: () => this.reviewBusy.set(false),
    });
  }

  protected selectRating(rating: number): void {
    this.reviewRating.set(rating);
  }

  protected roundedRating(rating?: number): number {
    return Math.round(rating ?? 0);
  }

  private loadProducts(): void {
    this.productApi.getProducts().subscribe({
      next: (items) => {
        const products = items.map((item) => ({
          id: item.id,
          name: item.name,
          nameEn: item.nameEn ?? '',
          category: item.categoryName,
          categoryEn: item.categoryNameEn ?? '',
          description: item.description,
          descriptionEn: item.descriptionEn ?? '',
          specifications: item.specifications ?? {},
          specificationsEn: item.specificationsEn ?? {},
          tags: item.tags ?? [],
          price: item.price,
          imageUrl: item.imageUrl ?? '',
          ratingAverage: item.ratingAverage ?? 0,
          reviewCount: item.reviewCount ?? 0,
        }));
        this.products.set(products);
        const selected = this.selectedProduct();
        if (selected) {
          this.selectedProduct.set(products.find((item) => item.id === selected.id) ?? selected);
        }
        this.cartItems.update((itemsInCart) => itemsInCart.map((item) => {
          const currentProduct = products.find((product) => product.id === item.product.id);
          return currentProduct ? { ...item, product: currentProduct } : item;
        }));
        this.productsError.set('');
        this.productsLoaded.set(true);
        if (this.currentUser() && !this.cartBusy()) {
          this.loadUserCart(this.cartItems().filter((item) => item.synced === false));
        }
      },
      error: () => {
        this.productsError.set('Не удалось загрузить каталог. Проверьте, что Product API запущен.');
        this.productsLoaded.set(true);
      },
    });
  }

  private loadUserCart(pendingItems: CartItem[] = []): void {
    const user = this.currentUser();
    if (!user) return;

    this.cartBusy.set(true);
    this.cartApi.getCart(user.token).subscribe({
      next: (cart) => {
        if (this.currentUser()?.userId !== user.userId) return;
        const products = this.products();
        const serverItems: CartItem[] = cart.items.map((item) => {
          const product = products.find((candidate) => candidate.id === item.productId) ?? {
            id: item.productId,
            name: item.productNameSnapshot,
            category: '',
            description: '',
            specifications: {},
            tags: [],
            price: item.unitPriceSnapshot,
            imageUrl: '',
          };
          return {
            product,
            quantity: item.quantity,
            available: item.isAvailable,
            synced: true,
          };
        });

        for (const pendingItem of pendingItems) {
          const existingIndex = serverItems.findIndex(
            (item) => item.product.id === pendingItem.product.id,
          );
          const pending = { ...pendingItem, synced: false };
          if (existingIndex < 0) {
            serverItems.push(pending);
          } else {
            serverItems[existingIndex] = {
              ...serverItems[existingIndex],
              ...pending,
              available: serverItems[existingIndex].available,
            };
          }
        }

        this.cartItems.set(serverItems);
        if (pendingItems.length === 0) this.cartError.set('');
      },
      error: (error: HttpErrorResponse) => {
        if (this.currentUser()?.userId !== user.userId) return;
        if (pendingItems.length > 0) this.cartItems.set(pendingItems);
        this.cartError.set(this.getRequestError(error, 'Не удалось загрузить корзину с сервера.'));
        console.error('Could not load the current user cart.', error);
        this.cartBusy.set(false);
      },
      complete: () => {
        if (this.currentUser()?.userId === user.userId) this.cartBusy.set(false);
      },
    });
  }

  private syncGuestCart(guestItems: CartItem[]): void {
    const user = this.currentUser();
    if (!user) return;
    if (guestItems.length === 0) {
      this.loadUserCart();
      return;
    }

    this.cartBusy.set(true);
    this.cartError.set('');
    this.cartApi.getCart(user.token).subscribe({
      next: (cart) => {
        if (this.currentUser()?.userId !== user.userId) {
          this.cartBusy.set(false);
          return;
        }
        const pendingItems: CartItem[] = [];
        const requests = guestItems.map((item) => {
          if (typeof item.product.id !== 'string') {
            pendingItems.push({ ...item, synced: false });
            return of(false);
          }

          const remoteItem = cart.items.find((candidate) => candidate.productId === item.product.id);
          const targetQuantity = item.quantity + (remoteItem?.quantity ?? 0);
          const request = remoteItem
            ? this.cartApi.updateItem(item.product.id, targetQuantity, user.token)
            : this.cartApi.addItem(item.product.id, targetQuantity, user.token);
          return request.pipe(
            map(() => true),
            catchError((error: HttpErrorResponse) => {
              pendingItems.push({ ...item, quantity: targetQuantity, synced: false });
              this.cartError.set(this.getRequestError(error, 'Не удалось перенести товары в корзину аккаунта.'));
              return of(false);
            }),
          );
        });

        from(requests).pipe(
          concatMap((request) => request),
          toArray(),
        ).subscribe({
          next: () => {
            if (this.currentUser()?.userId !== user.userId) {
              this.cartBusy.set(false);
              return;
            }
            this.cartItems.set(pendingItems);
            this.loadUserCart(pendingItems);
          },
          error: (error: HttpErrorResponse) => {
            if (this.currentUser()?.userId !== user.userId) {
              this.cartBusy.set(false);
              return;
            }
            this.cartItems.set(guestItems.map((item) => ({ ...item, synced: false })));
            this.cartError.set(this.getRequestError(error, 'Не удалось перенести корзину в аккаунт.'));
            this.cartBusy.set(false);
          },
        });
      },
      error: (error: HttpErrorResponse) => {
        if (this.currentUser()?.userId !== user.userId) {
          this.cartBusy.set(false);
          return;
        }
        this.cartItems.set(guestItems.map((item) => ({ ...item, synced: false })));
        this.cartError.set(this.getRequestError(error, 'Не удалось загрузить корзину аккаунта.'));
        this.cartBusy.set(false);
      },
    });
  }

  private mapProductReview(review: ProductReviewApiResponse, user: AuthResponse | null): ProductReview {
    const reviewerName = review.reviewerName ?? review.ReviewerName ?? '';
    const comment = typeof review.comment === 'string'
      ? review.comment
      : typeof review.Comment === 'string' ? review.Comment : null;
    return {
      id: review.id ?? review.Id ?? '',
      productId: review.productId ?? review.ProductId ?? '',
      isOwnReview: review.isOwnReview === true
        || review.IsOwnReview === true
        || this.isCurrentUsersReview(reviewerName, user),
      reviewerName,
      rating: review.rating ?? review.Rating ?? 0,
      comment,
      createdAtUtc: review.createdAtUtc ?? review.CreatedAtUtc ?? '',
      updatedAtUtc: review.updatedAtUtc ?? review.UpdatedAtUtc ?? '',
    };
  }

  private isOwnReview(review: ProductReview, user: AuthResponse): boolean {
    return review.isOwnReview || this.isCurrentUsersReview(review.reviewerName, user);
  }

  private isCurrentUsersReview(reviewerName: string, user: AuthResponse | null): boolean {
    if (!user) return false;
    const normalize = (value: string): string => value.trim().replace(/\s+/g, ' ').toLocaleLowerCase();
    const fullName = normalize(`${user.firstName} ${user.lastName}`);
    return fullName.length > 0 && normalize(reviewerName) === fullName;
  }

  private loadProductReviews(productId: string, user: AuthResponse | null): void {
    this.productApi.getReviews(productId, user?.token).subscribe({
      next: reviews => {
        if (this.selectedProduct()?.id !== productId) return;
        this.reviews.set(reviews.map(review => this.mapProductReview(review, user)));
      },
      error: () => {
        if (this.selectedProduct()?.id === productId)
          this.reviewMessage.set('Не удалось загрузить отзывы. Попробуйте позже.');
      },
      complete: () => {
        if (this.selectedProduct()?.id === productId) this.reviewsLoading.set(false);
      },
    });
  }

  private loadCategories(): void {
    this.productApi.getCategories().subscribe({
      next: (categories) => {
        this.categoryOptions.set(categories);
        if (!this.newProductCategoryId() && categories.length > 0) {
          this.newProductCategoryId.set(categories[0].id);
        }
      },
      error: (error: HttpErrorResponse) => {
        console.error('Could not load product categories from Product API.', error);
      },
    });
  }

  private resetNewProductForm(): void {
    this.newProductName.set('');
    this.newProductNameEn.set('');
    this.newProductDescription.set('');
    this.newProductDescriptionEn.set('');
    this.newProductImageUrl.set('');
    this.newProductTags.set('');
    this.productSpecifications.set([]);
    this.newProductPrice.set(null);
    this.newProductStock.set(null);
    this.newProductCategoryId.set(this.categoryOptions()[0]?.id ?? '');
    this.adminProductMessage.set('');
  }

  private readStoredUser(): AuthResponse | null {
    try {
      const value = window.sessionStorage.getItem('forma.auth');
      if (!value) return null;
      const user = JSON.parse(value) as AuthResponse;
      if (!user.token || new Date(user.expiresAtUtc) <= new Date()) {
        window.sessionStorage.removeItem('forma.auth');
        return null;
      }

      return user;
    } catch (error) {
      console.warn('Could not read the saved FORMA session.', error);
      window.sessionStorage.removeItem('forma.auth');
      return null;
    }
  }

  private isStandalonePage(url: string): boolean {
    return url.startsWith('/account') || url.startsWith('/admin');
  }

  private getRequestError(error: HttpErrorResponse, fallback: string): string {
    const body: unknown = error.error;
    if (typeof body === 'string' && body.trim()) {
      return this.translateIdentityError(body);
    }

    if (body && typeof body === 'object') {
      const response = body as Record<string, unknown>;
      const serverErrors = response['errors'];
      if (Array.isArray(serverErrors) && serverErrors.length > 0) {
        return serverErrors.map((message) => this.translateIdentityError(String(message))).join(' ');
      }
      if (serverErrors && typeof serverErrors === 'object') {
        const messages = Object.values(serverErrors)
          .flatMap((value) => Array.isArray(value) ? value : [value])
          .filter((value): value is string => typeof value === 'string');
        if (messages.length > 0) {
          return messages.map((message) => this.translateIdentityError(message)).join(' ');
        }
      }
      if (typeof response['error'] === 'string') {
        return this.translateIdentityError(response['error']);
      }
      if (typeof response['title'] === 'string') {
        return this.translateIdentityError(response['title']);
      }
    }
    if (error.status === 401) {
      return 'Неверное имя пользователя или электронная почта, либо пароль.';
    }
    if (error.status === 403) {
      return 'Доступ запрещён. Для этого действия нужна роль администратора.';
    }
    if (error.status === 400) {
      return 'Проверьте данные формы. Пароль должен соответствовать требованиям рядом с полем.';
    }
    if (error.status === 0) {
      return 'Не удалось подключиться к серверу. Проверьте, что API запущен.';
    }
    return fallback;
  }

  private getPasswordRequirements(password: string): string[] {
    const requirements: string[] = [];
    if (password.length < 6) requirements.push('не менее 6 символов');
    if (!/\p{Lu}/u.test(password)) requirements.push('заглавную букву');
    if (!/\p{Ll}/u.test(password)) requirements.push('строчную букву');
    if (!/\p{Nd}/u.test(password)) requirements.push('цифру');
    if (!/[^\p{L}\p{N}]/u.test(password)) requirements.push('спецсимвол');
    return requirements;
  }

  private translateIdentityError(message: string): string {
    if (message === 'Cannot create an order from an empty cart.') {
      return 'Корзина пуста. Добавьте товары перед оформлением заказа.';
    }
    if (message === 'There is not enough stock for the requested quantity.') {
      return 'На складе недостаточно товара для выбранного количества.';
    }
    if (message === 'The product is not available.') {
      return 'Один из товаров больше недоступен.';
    }
    if (message === 'Cart or product catalog is unavailable.') {
      return 'Не удалось связаться с корзиной или каталогом. Проверьте, что Cart API и Product API запущены.';
    }
    if (message === 'Inventory service is unavailable.') {
      return 'Сервис учёта остатков недоступен. Заказ не был подтверждён.';
    }
    if (message === 'This cart revision has already been checked out with different order details.') {
      return 'Эта версия корзины уже оформлена с другими данными. Обновите корзину и попробуйте снова.';
    }
    if (message === 'The cart changed after checkout started; its newer contents were kept.') {
      return 'Корзина изменилась во время оформления. Её актуальное содержимое сохранено; проверьте его и повторите попытку.';
    }
    if (message.startsWith('Product ') && message.endsWith(' is no longer available.')) {
      return 'Один из товаров больше недоступен. Обновите корзину и попробуйте снова.';
    }
    if (/Passwords must be at least 6 characters/i.test(message)) {
      return 'Пароль должен содержать не менее 6 символов.';
    }
    if (/Passwords must have at least one digit/i.test(message)) {
      return 'Добавьте в пароль хотя бы одну цифру.';
    }
    if (/Passwords must have at least one lowercase/i.test(message)) {
      return 'Добавьте в пароль хотя бы одну строчную букву.';
    }
    if (/Passwords must have at least one uppercase/i.test(message)) {
      return 'Добавьте в пароль хотя бы одну заглавную букву.';
    }
    if (/Passwords must have at least one non alphanumeric character/i.test(message)) {
      return 'Добавьте в пароль хотя бы один спецсимвол.';
    }
    if (/Email .* is already taken/i.test(message)) {
      return 'Аккаунт с такой электронной почтой уже зарегистрирован.';
    }
    if (/User name .* is already taken/i.test(message)) {
      return 'Это имя пользователя уже занято.';
    }
    if (message === 'You cannot report your own review.') {
      return 'Нельзя пожаловаться на собственный отзыв.';
    }
    if (message === 'You have already reported this review.') {
      return 'Вы уже отправили жалобу на этот отзыв.';
    }
    if (message === 'Please explain the other report reason.') {
      return 'Поясните, пожалуйста, причину жалобы.';
    }
    if (message === 'Select at least one valid report reason.') {
      return 'Выберите хотя бы одну корректную причину жалобы.';
    }
    if (message === 'Report reasons must be unique.') {
      return 'Удалите повторяющиеся причины жалобы.';
    }
    if (message === 'Select at least one valid report reason.') {
      return 'Выберите хотя бы одну корректную причину жалобы.';
    }
    if (/The Email field is required/i.test(message)) return 'Укажите электронную почту.';
    if (/The UserName field is required/i.test(message)) return 'Укажите имя пользователя.';
    if (/The Email field is not a valid e-mail address/i.test(message)) return 'Укажите корректный адрес электронной почты.';
    if (/regular expression|username.*invalid/i.test(message)) {
      return 'Имя пользователя может содержать латинские буквы, цифры и символы . _ + -';
    }
    if (/User name .* is already taken/i.test(message)) return 'Это имя пользователя уже занято.';
    if (/Email .* is already taken/i.test(message)) return 'Аккаунт с такой электронной почтой уже зарегистрирован.';
    if (/^[А-Яа-яЁё]/.test(message)) return message;
    return 'Не удалось выполнить запрос. Проверьте данные и попробуйте снова.';
  }

  private showToast(message: string): void {
    this.toastMessage.set(message);
    window.setTimeout(() => this.toastMessage.set(''), 2400);
  }
}
