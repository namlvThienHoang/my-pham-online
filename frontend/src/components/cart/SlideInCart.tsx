'use client';

import { createPortal } from 'react-dom';
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetFooter } from '@/components/ui/sheet';
import { Button } from '@/components/ui/button';
import { useCartStore } from '@/store/cartStore';
import { ShoppingCart, Trash2, Plus, Minus } from 'lucide-react';
import Link from 'next/link';
import { useEffect, useState } from 'react';

export function SlideInCart() {
  const [mounted, setMounted] = useState(false);
  const cart = useCartStore((state) => state.cart);
  const isOpen = useCartStore((state) => state.isOpen);
  const closeCart = useCartStore((state) => state.closeCart);
  const updateQuantity = useCartStore((state) => state.updateQuantity);
  const removeItem = useCartStore((state) => state.removeItem);

  useEffect(() => {
    setMounted(true);
  }, []);

  if (!mounted) return null;

  const items = cart?.items || [];
  const subtotal = cart?.subtotal || 0;
  const shippingFee = cart?.shippingFee || 0;
  const total = cart?.total || 0;

  return createPortal(
    <Sheet open={isOpen} onOpenChange={(open) => !open && closeCart()}>
      <SheetContent side="right" className="w-full sm:max-w-md flex flex-col p-0">
        <SheetHeader className="border-b p-4">
          <SheetTitle className="flex items-center gap-2">
            <ShoppingCart className="h-5 w-5" />
            Giỏ hàng ({items.length} sản phẩm)
          </SheetTitle>
        </SheetHeader>

        {/* Cart Items */}
        <div className="flex-1 overflow-y-auto p-4 space-y-4">
          {items.length === 0 ? (
            <div className="flex flex-col items-center justify-center h-full text-center space-y-4">
              <ShoppingCart className="h-16 w-16 text-muted-foreground" />
              <div>
                <p className="text-lg font-medium">Giỏ hàng trống</p>
                <p className="text-sm text-muted-foreground">
                  Thêm sản phẩm để bắt đầu mua sắm
                </p>
              </div>
              <Button asChild onClick={() => closeCart()}>
                <Link href="/products">Mua ngay</Link>
              </Button>
            </div>
          ) : (
            items.map((item) => (
              <div key={item.id} className="flex gap-4 border-b pb-4 last:border-0">
                <img
                  src={item.image}
                  alt={item.productName}
                  className="w-20 h-20 object-cover rounded-md bg-muted"
                />
                <div className="flex-1 min-w-0 space-y-2">
                  <h4 className="font-medium line-clamp-2">{item.productName}</h4>
                  <p className="text-sm text-muted-foreground">{item.variantName}</p>
                  <p className="text-primary font-bold">
                    {new Intl.NumberFormat('vi-VN', {
                      style: 'currency',
                      currency: 'VND',
                    }).format(item.unitPrice)}
                  </p>
                  
                  <div className="flex items-center gap-2">
                    <Button
                      variant="outline"
                      size="icon"
                      className="h-8 w-8"
                      onClick={() => updateQuantity(item.id, item.quantity - 1)}
                    >
                      <Minus className="h-4 w-4" />
                    </Button>
                    <span className="w-8 text-center text-sm font-medium">
                      {item.quantity}
                    </span>
                    <Button
                      variant="outline"
                      size="icon"
                      className="h-8 w-8"
                      onClick={() => updateQuantity(item.id, item.quantity + 1)}
                    >
                      <Plus className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8 ml-auto text-destructive hover:text-destructive"
                      onClick={() => removeItem(item.id)}
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              </div>
            ))
          )}
        </div>

        {/* Footer */}
        {items.length > 0 && (
          <>
            <div className="border-t p-4 space-y-3">
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">Tạm tính</span>
                <span className="font-medium">
                  {new Intl.NumberFormat('vi-VN', {
                    style: 'currency',
                    currency: 'VND',
                  }).format(subtotal)}
                </span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">Phí vận chuyển</span>
                <span className="font-medium">
                  {shippingFee === 0 ? (
                    <span className="text-green-600">Miễn phí</span>
                  ) : (
                    new Intl.NumberFormat('vi-VN', {
                      style: 'currency',
                      currency: 'VND',
                    }).format(shippingFee)
                  )}
                </span>
              </div>
              <div className="flex justify-between text-lg font-bold pt-2 border-t">
                <span>Tổng cộng</span>
                <span className="text-primary">
                  {new Intl.NumberFormat('vi-VN', {
                    style: 'currency',
                    currency: 'VND',
                  }).format(total)}
                </span>
              </div>
            </div>

            <SheetFooter className="p-4 border-t gap-2">
              <Button variant="outline" className="flex-1" onClick={closeCart} asChild>
                <Link href="/products">Mua thêm</Link>
              </Button>
              <Button className="flex-1" asChild>
                <Link href="/cart">Thanh toán</Link>
              </Button>
            </SheetFooter>
          </>
        )}
      </SheetContent>
    </Sheet>,
    document.body
  );
}
