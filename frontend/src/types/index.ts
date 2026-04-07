export interface Product {
  id: string;
  name: string;
  slug: string;
  description?: string;
  price: number;
  compareAtPrice?: number;
  currency: string;
  images: ProductImage[];
  variants: ProductVariant[];
  categories: Category[];
  brand?: Brand;
  rating: number;
  reviewCount: number;
  inStock: boolean;
  inventoryQuantity: number;
  tags: string[];
  attributes: ProductAttribute[];
  createdAt: string;
  updatedAt: string;
}

export interface ProductImage {
  id: string;
  url: string;
  altText?: string;
  position: number;
  isPrimary: boolean;
}

export interface ProductVariant {
  id: string;
  sku: string;
  name: string;
  price: number;
  compareAtPrice?: number;
  inventoryQuantity: number;
  options: VariantOption[];
}

export interface VariantOption {
  optionId: string;
  optionName: string;
  value: string;
}

export interface ProductAttribute {
  name: string;
  value: string;
}

export interface Category {
  id: string;
  name: string;
  slug: string;
  parentId?: string;
  level: number;
  path: string;
  image?: string;
  productCount: number;
}

export interface Brand {
  id: string;
  name: string;
  slug: string;
  logo?: string;
  description?: string;
}

export interface Cart {
  id: string;
  items: CartItem[];
  subtotal: number;
  discount: number;
  shippingFee: number;
  tax: number;
  total: number;
  currency: string;
  appliedVouchers: Voucher[];
  usedWalletAmount: number;
  usedGiftCardAmount: number;
  expiresAt: string;
}

export interface CartItem {
  id: string;
  productId: string;
  variantId: string;
  productName: string;
  variantName: string;
  productSlug: string;
  image: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  options: VariantOption[];
  inventoryAvailable: number;
}

export interface Order {
  id: string;
  orderNumber: string;
  status: OrderStatus;
  customerId: string;
  items: OrderItem[];
  subtotal: number;
  discount: number;
  shippingFee: number;
  tax: number;
  total: number;
  currency: string;
  shippingAddress: Address;
  billingAddress?: Address;
  paymentMethod: PaymentMethod;
  paymentStatus: PaymentStatus;
  shipments: Shipment[];
  refunds: Refund[];
  notes?: string;
  cancellationReason?: string;
  createdAt: string;
  updatedAt: string;
}

export type OrderStatus = 
  | 'Pending'
  | 'CodAwaitingConfirmation'
  | 'Confirmed'
  | 'Processing'
  | 'ReadyToShip'
  | 'PartiallyShipped'
  | 'Shipped'
  | 'PartiallyDelivered'
  | 'Delivered'
  | 'Completed'
  | 'Cancelled'
  | 'Refunding'
  | 'PartiallyRefunded'
  | 'Refunded'
  | 'ReturnRequested'
  | 'ReturnApproved'
  | 'ReturnRejected';

export type PaymentStatus = 
  | 'Pending'
  | 'Authorized'
  | 'Captured'
  | 'PartiallyRefunded'
  | 'Refunded'
  | 'Failed'
  | 'Cancelled';

export interface OrderItem {
  id: string;
  productId: string;
  variantId: string;
  productName: string;
  variantName: string;
  productSlug: string;
  image: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  options: VariantOption[];
}

export interface Shipment {
  id: string;
  trackingNumber?: string;
  carrier: string;
  status: ShipmentStatus;
  shippedAt?: string;
  deliveredAt?: string;
  estimatedDeliveryDate?: string;
}

export type ShipmentStatus = 
  | 'Pending'
  | 'PickedUp'
  | 'InTransit'
  | 'OutForDelivery'
  | 'Delivered'
  | 'Failed'
  | 'Returned';

export interface Refund {
  id: string;
  amount: number;
  reason: string;
  status: RefundStatus;
  refundMethod: string;
  createdAt: string;
  processedAt?: string;
}

export type RefundStatus = 'Pending' | 'Approved' | 'Rejected' | 'Processed';

export interface Address {
  fullName: string;
  phone: string;
  addressLine1: string;
  addressLine2?: string;
  ward?: string;
  district: string;
  city: string;
  country: string;
  postalCode?: string;
  isDefault: boolean;
}

export interface User {
  id: string;
  email: string;
  fullName: string;
  phone?: string;
  avatar?: string;
  roles: string[];
  emailVerified: boolean;
  mfaEnabled: boolean;
  createdAt: string;
}

export interface Voucher {
  id: string;
  code: string;
  name: string;
  description?: string;
  discountType: 'Percentage' | 'FixedAmount' | 'FreeShipping';
  discountValue: number;
  minOrderValue?: number;
  maxDiscountAmount?: number;
  usageLimit?: number;
  usedCount: number;
  validFrom: string;
  validTo: string;
  applicableProducts?: string[];
  applicableCategories?: string[];
  stackingRule: 'None' | 'WithOtherVouchers' | 'WithWalletOnly';
}

export interface Wallet {
  id: string;
  userId: string;
  balance: number;
  currency: string;
  transactions: WalletTransaction[];
}

export interface WalletTransaction {
  id: string;
  type: 'Credit' | 'Debit';
  amount: number;
  balanceAfter: number;
  reason: string;
  referenceType?: string;
  referenceId?: string;
  createdAt: string;
}

export interface Review {
  id: string;
  productId: string;
  userId: string;
  userName: string;
  userAvatar?: string;
  rating: number;
  title?: string;
  content: string;
  images: ReviewImage[];
  helpfulCount: number;
  notHelpfulCount: number;
  verifiedPurchase: boolean;
  sellerResponse?: SellerResponse;
  createdAt: string;
  updatedAt: string;
}

export interface ReviewImage {
  id: string;
  url: string;
}

export interface SellerResponse {
  content: string;
  respondedAt: string;
  responderName: string;
}

export interface Wishlist {
  id: string;
  userId: string;
  items: WishlistItem[];
  updatedAt: string;
}

export interface WishlistItem {
  id: string;
  productId: string;
  productName: string;
  productSlug: string;
  image: string;
  price: number;
  inStock: boolean;
  addedAt: string;
}

export interface RecentlyViewedItem {
  id: string;
  productId: string;
  productName: string;
  productSlug: string;
  image: string;
  price: number;
  viewedAt: string;
}

export interface SearchResult {
  products: Product[];
  total: number;
  facets: SearchFacets;
  cursor?: string;
  hasMore: boolean;
}

export interface SearchFacets {
  categories: FacetCount[];
  brands: FacetCount[];
  priceRanges: FacetCount[];
  ratings: FacetCount[];
  attributes: Record<string, FacetCount[]>;
}

export interface FacetCount {
  value: string;
  count: number;
}

export interface SearchSuggestion {
  query: string;
  products: ProductSuggestion[];
  categories: CategorySuggestion[];
}

export interface ProductSuggestion {
  id: string;
  name: string;
  slug: string;
  image: string;
  price: number;
}

export interface CategorySuggestion {
  id: string;
  name: string;
  slug: string;
}

export interface PaginationParams {
  cursor?: string;
  limit?: number;
  sort?: string;
  order?: 'asc' | 'desc';
}

export interface PaginatedResult<T> {
  data: T[];
  cursor?: string;
  hasMore: boolean;
  total?: number;
}
