import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiConfigurationService } from './api-configuration.service';
import { ProductReviewReport, ReviewReportSubmission } from './review-moderation.component';

export interface ProductApiDto {
  id: string;
  name: string;
  nameEn?: string;
  categoryName: string;
  categoryNameEn?: string;
  description: string;
  descriptionEn?: string;
  specifications: Record<string, string>;
  specificationsEn?: Record<string, { name: string; value: string }>;
  tags: string[];
  price: number;
  imageUrl: string | null;
  ratingAverage: number;
  reviewCount: number;
}

export interface ProductCategoryDto {
  id: string;
  name: string;
  nameEn?: string;
}

export interface CreateProductRequest {
  name: string;
  nameEn: string;
  description: string;
  descriptionEn: string;
  imageUrl: string | null;
  specifications: Record<string, string>;
  specificationsEn: Record<string, { name: string; value: string }>;
  tags: string[];
  categoryId: string;
  price: number;
  stockQuantity: number;
}

export type UpdateProductRequest = Omit<CreateProductRequest, 'stockQuantity'>;

export interface ProductReviewApiResponse {
  id?: string;
  Id?: string;
  productId?: string;
  ProductId?: string;
  isOwnReview?: boolean;
  IsOwnReview?: boolean;
  reviewerName?: string;
  ReviewerName?: string;
  rating?: number;
  Rating?: number;
  comment?: string | null;
  Comment?: string | null;
  createdAtUtc?: string;
  CreatedAtUtc?: string;
  updatedAtUtc?: string;
  UpdatedAtUtc?: string;
}

@Injectable({ providedIn: 'root' })
export class ProductApiService {
  private readonly http = inject(HttpClient);
  private readonly api = inject(ApiConfigurationService);

  getProducts(): Observable<ProductApiDto[]> {
    return this.http.get<ProductApiDto[]>(this.api.productApiUrl);
  }

  getCategories(): Observable<ProductCategoryDto[]> {
    return this.http.get<ProductCategoryDto[]>(`${this.api.productApiUrl}/categories`);
  }

  createCategory(name: string, nameEn: string, token: string): Observable<ProductCategoryDto> {
    return this.http.post<ProductCategoryDto>(
      `${this.api.productApiUrl}/categories`,
      { name, nameEn },
      { headers: this.auth(token) },
    );
  }

  createProduct(product: CreateProductRequest, token: string): Observable<ProductApiDto> {
    return this.http.post<ProductApiDto>(this.api.productApiUrl, product, {
      headers: this.auth(token),
    });
  }

  updateProduct(
    id: string,
    product: UpdateProductRequest,
    token: string,
  ): Observable<ProductApiDto> {
    return this.http.put<ProductApiDto>(`${this.api.productApiUrl}/${id}`, product, {
      headers: this.auth(token),
    });
  }

  submitReview(
    productId: string,
    rating: number,
    comment: string | null,
    token: string,
  ): Observable<ProductReviewApiResponse> {
    return this.http.put<ProductReviewApiResponse>(
      `${this.api.productApiUrl}/${productId}/reviews`,
      { rating, comment },
      { headers: this.auth(token) },
    );
  }

  getReviews(productId: string, token?: string): Observable<ProductReviewApiResponse[]> {
    return this.http.get<ProductReviewApiResponse[]>(
      `${this.api.productApiUrl}/${productId}/reviews`,
      { headers: token ? this.auth(token) : undefined },
    );
  }

  submitReviewReport(
    productId: string,
    reviewId: string,
    submission: ReviewReportSubmission,
    token: string,
  ): Observable<void> {
    return this.http.post<void>(
      `${this.api.productApiUrl}/${productId}/reviews/${reviewId}/reports`,
      submission,
      { headers: this.auth(token) },
    );
  }

  getReviewReports(token: string): Observable<ProductReviewReport[]> {
    return this.http.get<ProductReviewReport[]>(`${this.api.productApiUrl}/review-reports`, {
      headers: this.auth(token),
    });
  }

  resolveReviewReport(reportId: string, hideReview: boolean, token: string): Observable<void> {
    return this.http.put<void>(
      `${this.api.productApiUrl}/review-reports/${reportId}/resolve`,
      { hideReview },
      { headers: this.auth(token) },
    );
  }

  hideReview(reviewId: string, token: string): Observable<void> {
    return this.http.delete<void>(`${this.api.productApiUrl}/reviews/${reviewId}`, {
      headers: this.auth(token),
    });
  }

  private auth(token: string): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }
}
