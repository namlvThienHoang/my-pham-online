'use client';

import { useCart, useRemoveFromCart, useUpdateCartItem, useCheckout } from '@/hooks/useQueries';
import { useCartStore } from '@/store/cartStore';
import Link from 'next/link';
import { Trash2, Plus, Minus, ArrowLeft } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

export default function CartPage() {
  const { data: cart, isLoading } = useCart();
  const removeFromCart = useRemoveFromCart();
  const updateCartItem = useUpdateCartItem();
  const checkout = useCheckout();

  if (isLoading) {
    return (
      <div className="container py-8">
        <div className="animate-pulse space-y-4">
          <div className="h-8 bg-muted rounded w-1/4" />
          <div className="h-32 bg-muted rounded" />
        </div>
      </div>
    );
  }

  if (!cart || cart.items.length === 0) {
    return (
      <div className="container py-8 text-center">
        <div className="max-w-md mx-auto space-y-6">
          <ShoppingCart className="h-24 w-24 mx-auto text-muted-foreground" />
          <h1 className="text-2xl font-bold">Giỏ hàng trống</h1>
          <p className="text-muted-foreground">
            Giỏ hàng của bạn đang trống. Hãy thêm sản phẩm để mua sắm ngay!
          </p>
          <Link href="/products">
            <Button>Mua sắm ngay</Button>
          </Link>
        </div>
      </div>
    );
  }

  const handleUpdateQuantity = async (itemId: string, quantity: number) => {
    await updateCartItem.mutateAsync({ itemId, quantity });
  };

  const handleRemove = async (itemId: string) => {
    await removeFromCart.mutateAsync({ itemId });
  };

  const handleCheckout = async () => {
    // Default checkout - will be enhanced with address selection
    await checkout.mutateAsync({
      shippingAddressId: 'default',
      paymentMethod: 'COD',
    });
  };

  return (
    <div className="container py-8">
      <div className="flex items-center gap-2 mb-6">
        <Link href="/products">
          <Button variant="ghost" size="icon">
            <ArrowLeft className="h-5 w-5" />
          </Button>
        </Link>
        <h1 className="text-3xl font-bold">Giỏ hàng ({cart.items.length} sản phẩm)</h1>
      </div>

      <div className="grid lg:grid-cols-3 gap-8">
        {/* Cart Items */}
        <div className="lg:col-span-2 space-y-4">
          {cart.items.map((item) => (
            <Card key={item.id}>
              <CardContent className="p-4">
                <div className="flex gap-4">
                  <img
                    src={item.image}
                    alt={item.productName}
                    className="w-24 h-24 object-cover rounded-md"
                  />
                  
                  <div className="flex-1 space-y-2">
                    <Link
                      href={`/products/${item.productSlug}`}
                      className="font-medium hover:text-primary line-clamp-2"
                    >
                      {item.productName}
                    </Link>
                    
                    {item.options.length > 0 && (
                      <p className="text-sm text-muted-foreground">
                        {item.options.map((opt) => opt.value).join(', ')}
                      </p>
                    )}
                    
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <Button
                          variant="outline"
                          size="icon"
                          className="h-8 w-8"
                          onClick={() => handleUpdateQuantity(item.id, item.quantity - 1)}
                          disabled={updateCartItem.isPending}
                        >
                          <Minus className="h-4 w-4" />
                        </Button>
                        
                        <span className="w-12 text-center">{item.quantity}</span>
                        
                        <Button
                          variant="outline"
                          size="icon"
                          className="h-8 w-8"
                          onClick={() => handleUpdateQuantity(item.id, item.quantity + 1)}
                          disabled={updateCartItem.isPending || item.quantity >= item.inventoryAvailable}
                        >
                          <Plus className="h-4 w-4" />
                        </Button>
                      </div>
                      
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleRemove(item.id)}
                        disabled={removeFromCart.isPending}
                      >
                        <Trash2 className="h-4 w-4 text-destructive" />
                      </Button>
                    </div>
                  </div>
                  
                  <div className="text-right space-y-2">
                    <p className="font-bold text-primary">
                      {new Intl.NumberFormat('vi-VN', {
                        style: 'currency',
                        currency: 'VND',
                      }).format(item.totalPrice)}
                    </p>
                    <p className="text-xs text-muted-foreground">
                      {new Intl.NumberFormat('vi-VN', {
                        style: 'currency',
                        currency: 'VND',
                      }).format(item.unitPrice)} x {item.quantity}
                    </p>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>

        {/* Order Summary */}
        <div className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Tổng đơn hàng</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">Tạm tính</span>
                <span>{new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(cart.subtotal)}</span>
              </div>
              
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">Giảm giá</span>
                <span className="text-green-600">
                  -{new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(cart.discount)}
                </span>
              </div>
              
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">Phí vận chuyển</span>
                <span>
                  {cart.shippingFee === 0 ? 'Miễn phí' : new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(cart.shippingFee)}
                </span>
              </div>
              
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">Thuế</span>
                <span>{new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(cart.tax)}</span>
              </div>
              
              <div className="border-t pt-3 flex justify-between font-bold text-lg">
                <span>Tổng cộng</span>
                <span className="text-primary">
                  {new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(cart.total)}
                </span>
              </div>
              
              <Button 
                className="w-full mt-4" 
                size="lg"
                onClick={handleCheckout}
                disabled={checkout.isPending}
              >
                {checkout.isPending ? 'Đang xử lý...' : 'Tiến hành thanh toán'}
              </Button>
              
              <div className="text-xs text-muted-foreground text-center">
                Miễn phí vận chuyển cho đơn hàng từ 500.000đ
              </div>
            </CardContent>
          </Card>

          {/* Voucher Section */}
          <Card>
            <CardContent className="p-4 space-y-3">
              <h3 className="font-semibold">Mã giảm giá</h3>
              <div className="flex gap-2">
                <Input placeholder="Nhập mã giảm giá" />
                <Button variant="outline">Áp dụng</Button>
              </div>
              {cart.appliedVouchers.length > 0 && (
                <div className="space-y-2">
                  {cart.appliedVouchers.map((voucher) => (
                    <div key={voucher.id} className="flex justify-between text-sm bg-green-50 p-2 rounded">
                      <span>{voucher.name}</span>
                      <span className="text-green-600 font-medium">
                        -{new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(
                          voucher.discountType === 'Percentage' 
                            ? Math.min(voucher.discountValue * cart.subtotal / 100, voucher.maxDiscountAmount || Infinity)
                            : voucher.discountValue
                        )}
                      </span>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}

function ShoppingCart({ className }: { className?: string }) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <circle cx="8" cy="21" r="1" />
      <circle cx="19" cy="21" r="1" />
      <path d="M2.05 2.05h2l2.66 12.42a2 2 0 0 0 2 1.58h9.78a2 2 0 0 0 1.95-1.57l1.65-7.43H5.12" />
    </svg>
  );
}
