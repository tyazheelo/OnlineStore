import { DecimalPipe } from '@angular/common';
import { Component, inject, input, output } from '@angular/core';
import { LanguageService } from './language.service';
import { ProductReview } from './review-moderation.component';
import { ReviewModerationActionsComponent } from './review-moderation-actions.component';
import { Product } from './product.models';

export interface ProductReviewSubmission {
  rating: number;
  comment: string;
}

@Component({
  selector: 'app-product-detail',
  imports: [DecimalPipe, ReviewModerationActionsComponent],
  templateUrl: './product-detail.component.html',
  styleUrl: './product-detail.component.scss',
})
export class ProductDetailComponent {
  private readonly language = inject(LanguageService);
  readonly product = input.required<Product>();
  readonly reviews = input<ProductReview[]>([]);
  readonly reviewsLoading = input(false);
  readonly isAdmin = input(false);
  readonly isAuthenticated = input(false);
  readonly moderationBusy = input(false);
  readonly reviewBusy = input(false);
  readonly reviewRating = input(5);
  readonly reviewComment = input('');
  readonly reviewMessage = input('');

  readonly closed = output<void>();
  readonly addToCart = output<Product>();
  readonly editProduct = output<Product>();
  readonly reportReview = output<ProductReview>();
  readonly hideReview = output<ProductReview>();
  readonly reviewSubmitted = output<ProductReviewSubmission>();
  readonly ratingChanged = output<number>();
  readonly commentChanged = output<string>();
  readonly accountRequested = output<void>();

  protected readonly stars = [1, 2, 3, 4, 5];

  protected t(text: string): string {
    return this.language.translate(text);
  }

  protected productName(): string {
    const product = this.product();
    return this.language.current() === 'en' && product.nameEn?.trim()
      ? product.nameEn
      : product.name;
  }

  protected productDescription(): string {
    const product = this.product();
    return this.language.current() === 'en' && product.descriptionEn?.trim()
      ? product.descriptionEn
      : product.description;
  }

  protected productCategory(): string {
    const product = this.product();
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

  protected roundedRating(rating?: number): number {
    return Math.round(rating ?? 0);
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

  protected formatReviewDate(value: string): string {
    return new Intl.DateTimeFormat(this.language.current() === 'ru' ? 'ru-RU' : 'en-US', {
      dateStyle: 'medium',
    }).format(new Date(value));
  }

  protected getSpecifications(product: Product): { key: string; value: string }[] {
    return Object.entries(product.specifications).map(([key, value]) => {
      const translation = this.language.current() === 'en' ? product.specificationsEn?.[key] : undefined;
      return {
        key: translation?.name?.trim() || key,
        value: translation?.value?.trim() || value,
      };
    });
  }

  protected close(): void {
    this.closed.emit();
  }

  protected submitReview(event: Event): void {
    event.preventDefault();
    this.reviewSubmitted.emit({
      rating: this.reviewRating(),
      comment: this.reviewComment().trim(),
    });
  }

  protected onCommentInput(event: Event): void {
    if (event.target instanceof HTMLTextAreaElement) {
      this.commentChanged.emit(event.target.value);
    }
  }
}
