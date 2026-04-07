import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient, type ApiError } from '@/lib/api';
import type {
  Product,
  Cart,
  Order,
  PaginatedResult,
  SearchResult,
  SearchSuggestion,
  Wishlist,
  RecentlyViewedItem,
} from '@/types';

// Products
export function useProducts(params?: {
  categorySlug?: string;
  brandSlug?: string;
  minPrice?: number;
  maxPrice?: number;
  rating?: number;
  cursor?: string;
  limit?: number;
  sort?: string;
  order?: 'asc' | 'desc';
}) {
  return useQuery<PaginatedResult<Product>, ApiError>({
    queryKey: ['products', params],
    queryFn: async () => {
      const { data } = await apiClient.get('/products', { params });
      return data;
    },
  });
}

export function useProduct(slug: string) {
  return useQuery<Product, ApiError>({
    queryKey: ['product', slug],
    queryFn: async () => {
      const { data } = await apiClient.get(`/products/${slug}`);
      return data;
    },
    enabled: !!slug,
  });
}

export function useSearchSuggestions(query: string) {
  return useQuery<SearchSuggestion, ApiError>({
    queryKey: ['search-suggestions', query],
    queryFn: async () => {
      const { data } = await apiClient.get('/search/suggest', {
        params: { q: query },
      });
      return data;
    },
    enabled: query.length >= 2,
    staleTime: 5 * 60 * 1000,
  });
}

export function useSearch(params?: {
  q?: string;
  categorySlug?: string;
  brandSlug?: string;
  minPrice?: number;
  maxPrice?: number;
  rating?: number;
  cursor?: string;
  limit?: number;
}) {
  return useQuery<SearchResult, ApiError>({
    queryKey: ['search', params],
    queryFn: async () => {
      const { data } = await apiClient.get('/search', { params });
      return data;
    },
  });
}

// Cart
export function useCart() {
  return useQuery<Cart, ApiError>({
    queryKey: ['cart'],
    queryFn: async () => {
      const { data } = await apiClient.get('/carts/me');
      return data;
    },
  });
}

export function useAddToCart() {
  const queryClient = useQueryClient();

  return useMutation<Cart, ApiError, { productId: string; variantId: string; quantity: number }>({
    mutationFn: async ({ productId, variantId, quantity }) => {
      const { data } = await apiClient.post('/carts/me/items', {
        productId,
        variantId,
        quantity,
      });
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cart'] });
    },
  });
}

export function useUpdateCartItem() {
  const queryClient = useQueryClient();

  return useMutation<Cart, ApiError, { itemId: string; quantity: number }>({
    mutationFn: async ({ itemId, quantity }) => {
      const { data } = await apiClient.put(`/carts/me/items/${itemId}`, {
        quantity,
      });
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cart'] });
    },
  });
}

export function useRemoveFromCart() {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, { itemId: string }>({
    mutationFn: async ({ itemId }) => {
      await apiClient.delete(`/carts/me/items/${itemId}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cart'] });
    },
  });
}

export function useCheckout() {
  const queryClient = useQueryClient();

  return useMutation<Order, ApiError, {
    shippingAddressId: string;
    paymentMethod: string;
    notes?: string;
    useWalletAmount?: number;
    voucherCodes?: string[];
  }>({
    mutationFn: async (checkoutData) => {
      const { data } = await apiClient.post('/carts/me/checkout', checkoutData);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cart'] });
      queryClient.invalidateQueries({ queryKey: ['orders'] });
    },
  });
}

// Orders
export function useOrders(params?: {
  status?: string;
  cursor?: string;
  limit?: number;
}) {
  return useQuery<PaginatedResult<Order>, ApiError>({
    queryKey: ['orders', params],
    queryFn: async () => {
      const { data } = await apiClient.get('/orders/me', { params });
      return data;
    },
  });
}

export function useOrder(id: string) {
  return useQuery<Order, ApiError>({
    queryKey: ['order', id],
    queryFn: async () => {
      const { data } = await apiClient.get(`/orders/me/${id}`);
      return data;
    },
    enabled: !!id,
  });
}

export function useCancelOrder() {
  const queryClient = useQueryClient();

  return useMutation<Order, ApiError, { orderId: string; reason: string }>({
    mutationFn: async ({ orderId, reason }) => {
      const { data } = await apiClient.post(`/orders/me/${orderId}/cancel`, {
        reason,
      });
      return data;
    },
    onSuccess: (_, { orderId }) => {
      queryClient.invalidateQueries({ queryKey: ['order', orderId] });
      queryClient.invalidateQueries({ queryKey: ['orders'] });
    },
  });
}

export function usePartialCancelOrder() {
  const queryClient = useQueryClient();

  return useMutation<Order, ApiError, {
    orderId: string;
    itemIds: string[];
    reason: string;
  }>({
    mutationFn: async ({ orderId, itemIds, reason }) => {
      const { data } = await apiClient.post(`/orders/me/${orderId}/partial-cancel`, {
        itemIds,
        reason,
      });
      return data;
    },
    onSuccess: (_, { orderId }) => {
      queryClient.invalidateQueries({ queryKey: ['order', orderId] });
    },
  });
}

// Wishlist
export function useWishlist() {
  return useQuery<Wishlist, ApiError>({
    queryKey: ['wishlist'],
    queryFn: async () => {
      const { data } = await apiClient.get('/users/me/wishlist');
      return data;
    },
  });
}

export function useToggleWishlist() {
  const queryClient = useQueryClient();

  return useMutation<{ added: boolean }, ApiError, { productId: string }>({
    mutationFn: async ({ productId }) => {
      const { data } = await apiClient.post('/users/me/wishlist/toggle', {
        productId,
      });
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['wishlist'] });
    },
  });
}

// Recently Viewed
export function useRecentlyViewed() {
  return useQuery<RecentlyViewedItem[], ApiError>({
    queryKey: ['recently-viewed'],
    queryFn: async () => {
      const { data } = await apiClient.get('/users/me/recently-viewed');
      return data;
    },
  });
}

export function useTrackView() {
  return useMutation<void, ApiError, { productId: string }>({
    mutationFn: async ({ productId }) => {
      await apiClient.post('/users/me/recently-viewed', { productId });
    },
  });
}
