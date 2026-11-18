import { Component, effect, inject, input, output, signal } from '@angular/core';
import { LanguageService } from './language.service';

export interface ProductReview {
  id: string;
  productId: string;
  isOwnReview: boolean;
  reviewerName: string;
  rating: number;
  comment: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface ProductReviewReport {
  id: string;
  reviewId: string;
  productId: string;
  productName: string;
  reviewerName: string;
  rating: number;
  comment: string | null;
  reviewCreatedAtUtc: string;
  reporterUserId: string;
  reasons: string[];
  otherDetails: string | null;
  reportedAtUtc: string;
}

export interface ReviewReportSubmission {
  reasons: string[];
  otherDetails: string | null;
}

export interface ResolveReviewReportRequest {
  report: ProductReviewReport;
  hideReview: boolean;
}

@Component({
  selector: 'app-review-moderation',
  templateUrl: './review-moderation.component.html',
  styleUrl: './review-moderation.component.scss',
})
export class ReviewModerationComponent {
  private readonly language = inject(LanguageService);
  readonly reportReview = input<ProductReview | null>(null);
  readonly reportOpen = input(false);
  readonly reportBusy = input(false);
  readonly reportMessage = input('');

  protected readonly reportReasons = signal<string[]>([]);
  protected readonly reportOtherDetails = signal('');
  protected readonly validationMessage = signal('');
  readonly reportSubmitted = output<ReviewReportSubmission>();
  readonly reportClosed = output<void>();
  readonly reportMessageCleared = output<void>();

  constructor() {
    effect(() => {
      if (this.reportOpen()) {
        this.reportReasons.set([]);
        this.reportOtherDetails.set('');
        this.validationMessage.set('');
      }
    });
  }

  protected t(text: string): string {
    return this.language.translate(text);
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

  protected onReportReasonChange(event: Event, reason: string): void {
    if (!(event.target instanceof HTMLInputElement)) return;
    const checked = event.target.checked;
    this.reportReasons.update((reasons) => checked
      ? [...new Set([...reasons, reason])]
      : reasons.filter((item) => item !== reason));
    this.validationMessage.set('');
    this.reportMessageCleared.emit();
  }

  protected onReportDetailsInput(event: Event): void {
    if (!(event.target instanceof HTMLTextAreaElement)) return;
    this.reportOtherDetails.set(event.target.value);
    this.validationMessage.set('');
    this.reportMessageCleared.emit();
  }

  protected submitReport(event: Event): void {
    event.preventDefault();
    if (this.reportBusy()) return;
    if (!this.reportReasons().length) {
      this.validationMessage.set('Выберите хотя бы одну причину жалобы.');
      return;
    }
    if (this.reportReasons().includes('Other') && !this.reportOtherDetails().trim()) {
      this.validationMessage.set('Поясните, пожалуйста, причину жалобы.');
      return;
    }

    this.validationMessage.set('');
    this.reportSubmitted.emit({
      reasons: this.reportReasons(),
      otherDetails: this.reportOtherDetails().trim() || null,
    });
  }

  protected closeReport(): void {
    this.reportClosed.emit();
  }

}
