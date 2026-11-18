import { Component, inject, input, output } from '@angular/core';
import { LanguageService } from './language.service';

@Component({
  selector: 'app-review-moderation-actions',
  template: `
    <div class="review-actions">
      @if (isAdmin()) {
        <button class="review-action-button review-hide-button" type="button" [disabled]="busy()" (click)="hideRequested.emit()">
          {{ language.translate('Скрыть отзыв') }}
        </button>
      } @else if (!isOwnReview()) {
        <button class="review-action-button" type="button" (click)="reportRequested.emit()">
          {{ language.translate('Пожаловаться') }}
        </button>
      }
    </div>
  `,
  styleUrl: './review-moderation-actions.component.scss',
})
export class ReviewModerationActionsComponent {
  protected readonly language = inject(LanguageService);
  readonly isAdmin = input(false);
  readonly isOwnReview = input(false);
  readonly busy = input(false);
  readonly reportRequested = output<void>();
  readonly hideRequested = output<void>();
}
