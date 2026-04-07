'use client';

import Link from 'next/link';
import { ShoppingCart, User, Heart, Menu, Search, Package } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { useCartStore } from '@/store/cartStore';
import { useState } from 'react';
import { useSearchSuggestions } from '@/hooks/useQueries';

export function Header() {
  const [searchQuery, setSearchQuery] = useState('');
  const { data: suggestions } = useSearchSuggestions(searchQuery);
  const cart = useCartStore((state) => state.cart);
  const itemCount = cart?.items.reduce((sum, item) => sum + item.quantity, 0) || 0;

  return (
    <header className="sticky top-0 z-50 w-full border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <div className="container flex h-16 items-center gap-4">
        <Button variant="ghost" size="icon" className="md:hidden">
          <Menu className="h-5 w-5" />
        </Button>

        <Link href="/" className="flex items-center gap-2">
          <span className="text-2xl font-bold text-primary">BeautyStore</span>
        </Link>

        <nav className="hidden md:flex items-center gap-6 ml-6">
          <Link href="/products" className="text-sm font-medium hover:text-primary">
            Sản phẩm
          </Link>
          <Link href="/categories" className="text-sm font-medium hover:text-primary">
            Danh mục
          </Link>
          <Link href="/brands" className="text-sm font-medium hover:text-primary">
            Thương hiệu
          </Link>
          <Link href="/promotions" className="text-sm font-medium hover:text-primary">
            Khuyến mãi
          </Link>
        </nav>

        <div className="flex-1 flex items-center max-w-md mx-auto">
          <div className="relative w-full">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input
              type="search"
              placeholder="Tìm kiếm sản phẩm..."
              className="pl-10"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
            />
            {suggestions && searchQuery.length >= 2 && (
              <div className="absolute top-full left-0 right-0 mt-2 bg-background border rounded-md shadow-lg p-4 z-50">
                {suggestions.products.length > 0 && (
                  <div className="space-y-2">
                    <p className="text-xs font-medium text-muted-foreground">Sản phẩm gợi ý</p>
                    {suggestions.products.slice(0, 5).map((product) => (
                      <Link
                        key={product.id}
                        href={`/products/${product.slug}`}
                        className="flex items-center gap-3 p-2 hover:bg-accent rounded-md"
                      >
                        <img
                          src={product.image}
                          alt={product.name}
                          className="w-10 h-10 object-cover rounded"
                        />
                        <div className="flex-1 min-w-0">
                          <p className="text-sm font-medium truncate">{product.name}</p>
                          <p className="text-xs text-muted-foreground">
                            {new Intl.NumberFormat('vi-VN', {
                              style: 'currency',
                              currency: 'VND',
                            }).format(product.price)}
                          </p>
                        </div>
                      </Link>
                    ))}
                  </div>
                )}
              </div>
            )}
          </div>
        </div>

        <div className="flex items-center gap-2">
          <Link href="/wishlist">
            <Button variant="ghost" size="icon">
              <Heart className="h-5 w-5" />
            </Button>
          </Link>
          <Link href="/cart">
            <Button variant="ghost" size="icon" className="relative">
              <ShoppingCart className="h-5 w-5" />
              {itemCount > 0 && (
                <span className="absolute -top-1 -right-1 h-5 w-5 bg-primary text-primary-foreground text-xs rounded-full flex items-center justify-center">
                  {itemCount}
                </span>
              )}
            </Button>
          </Link>
          <Link href="/orders">
            <Button variant="ghost" size="icon">
              <Package className="h-5 w-5" />
            </Button>
          </Link>
          <Link href="/profile">
            <Button variant="ghost" size="icon">
              <User className="h-5 w-5" />
            </Button>
          </Link>
        </div>
      </div>
    </header>
  );
}
