export interface Product {
  id: string | number;
  name: string;
  nameEn?: string;
  category: string;
  categoryEn?: string;
  description: string;
  descriptionEn?: string;
  specifications: Record<string, string>;
  specificationsEn?: Record<string, { name: string; value: string }>;
  tags: string[];
  price: number;
  imageUrl: string;
  ratingAverage?: number;
  reviewCount?: number;
}

export interface CartItem {
  product: Product;
  quantity: number;
  available?: boolean;
  synced?: boolean;
}
