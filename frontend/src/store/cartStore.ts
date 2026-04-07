import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { Cart, CartItem, ProductVariant } from '@/types';

interface CartStore {
  cart: Cart | null;
  isLoading: boolean;
  error: string | null;
  setCart: (cart: Cart | null) => void;
  addItem: (productId: string, variant: ProductVariant, quantity: number) => void;
  updateQuantity: (itemId: string, quantity: number) => void;
  removeItem: (itemId: string) => void;
  clearCart: () => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
}

export const useCartStore = create<CartStore>()(
  persist(
    (set, get) => ({
      cart: null,
      isLoading: false,
      error: null,

      setCart: (cart) => set({ cart }),

      addItem: (productId, variant, quantity) => {
        const { cart } = get();
        if (!cart) return;

        const existingItem = cart.items.find(
          (item) => item.variantId === variant.id
        );

        let newItems: CartItem[];
        if (existingItem) {
          newItems = cart.items.map((item) =>
            item.variantId === variant.id
              ? { ...item, quantity: item.quantity + quantity }
              : item
          );
        } else {
          newItems = [
            ...cart.items,
            {
              id: crypto.randomUUID(),
              productId,
              variantId: variant.id,
              productName: '', // Will be populated from API
              variantName: variant.name,
              productSlug: '',
              image: '',
              quantity,
              unitPrice: variant.price,
              totalPrice: variant.price * quantity,
              options: variant.options,
              inventoryAvailable: variant.inventoryQuantity,
            },
          ];
        }

        set({
          cart: {
            ...cart,
            items: newItems,
            subtotal: newItems.reduce((sum, item) => sum + item.totalPrice, 0),
            total:
              newItems.reduce((sum, item) => sum + item.totalPrice, 0) -
              cart.discount +
              cart.shippingFee +
              cart.tax,
          },
        });
      },

      updateQuantity: (itemId, quantity) => {
        const { cart } = get();
        if (!cart) return;

        if (quantity <= 0) {
          get().removeItem(itemId);
          return;
        }

        const newItems = cart.items.map((item) =>
          item.id === itemId ? { ...item, quantity } : item
        );

        set({
          cart: {
            ...cart,
            items: newItems,
            subtotal: newItems.reduce((sum, item) => sum + item.totalPrice, 0),
            total:
              newItems.reduce((sum, item) => sum + item.totalPrice, 0) -
              cart.discount +
              cart.shippingFee +
              cart.tax,
          },
        });
      },

      removeItem: (itemId) => {
        const { cart } = get();
        if (!cart) return;

        const newItems = cart.items.filter((item) => item.id !== itemId);

        set({
          cart: {
            ...cart,
            items: newItems,
            subtotal: newItems.reduce((sum, item) => sum + item.totalPrice, 0),
            total:
              newItems.reduce((sum, item) => sum + item.totalPrice, 0) -
              cart.discount +
              cart.shippingFee +
              cart.tax,
          },
        });
      },

      clearCart: () => {
        const { cart } = get();
        if (!cart) return;

        set({
          cart: {
            ...cart,
            items: [],
            subtotal: 0,
            total: 0,
          },
        });
      },

      setLoading: (isLoading) => set({ isLoading }),
      setError: (error) => set({ error }),
    }),
    {
      name: 'cart-storage',
      partialize: (state) => ({ cart: state.cart }),
    }
  )
);
