import Link from 'next/link';
import { ShoppingCart, Heart } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import type { Product } from '@/types';
import { useAddToCart, useToggleWishlist } from '@/hooks/useQueries';
import { useState } from 'react';

interface ProductCardProps {
  product: Product;
}

export function ProductCard({ product }: ProductCardProps) {
  const [isAdding, setIsAdding] = useState(false);
  const addToCart = useAddToCart();
  const toggleWishlist = useToggleWishlist();

  const handleAddToCart = async (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsAdding(true);
    try {
      const variant = product.variants[0];
      if (variant) {
        await addToCart.mutateAsync({
          productId: product.id,
          variantId: variant.id,
          quantity: 1,
        });
      }
    } finally {
      setIsAdding(false);
    }
  };

  const handleToggleWishlist = async (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    await toggleWishlist.mutateAsync({ productId: product.id });
  };

  const discount = product.compareAtPrice
    ? Math.round(((product.compareAtPrice - product.price) / product.compareAtPrice) * 100)
    : 0;

  return (
    <Link href={`/products/${product.slug}`}>
      <Card className="group overflow-hidden transition-all hover:shadow-lg">
        <div className="relative aspect-square overflow-hidden bg-muted">
          <img
            src={product.images[0]?.url || '/images/placeholder.jpg'}
            alt={product.name}
            className="object-cover w-full h-full group-hover:scale-105 transition-transform duration-300"
          />
          
          {discount > 0 && (
            <span className="absolute top-2 left-2 bg-destructive text-destructive-foreground text-xs font-bold px-2 py-1 rounded">
              -{discount}%
            </span>
          )}
          
          {!product.inStock && (
            <span className="absolute top-2 right-2 bg-muted-foreground text-muted-foreground-foreground text-xs font-bold px-2 py-1 rounded">
              Hết hàng
            </span>
          )}

          <div className="absolute bottom-2 right-2 flex gap-2 opacity-0 group-hover:opacity-100 transition-opacity">
            <Button
              size="icon"
              variant="secondary"
              className="h-8 w-8 rounded-full"
              onClick={handleToggleWishlist}
            >
              <Heart className="h-4 w-4" />
            </Button>
          </div>
        </div>

        <CardContent className="p-4 space-y-2">
          <h3 className="font-medium line-clamp-2 group-hover:text-primary transition-colors">
            {product.name}
          </h3>
          
          <div className="flex items-center gap-2">
            <span className="text-lg font-bold text-primary">
              {new Intl.NumberFormat('vi-VN', {
                style: 'currency',
                currency: 'VND',
              }).format(product.price)}
            </span>
            
            {product.compareAtPrice && (
              <span className="text-sm text-muted-foreground line-through">
                {new Intl.NumberFormat('vi-VN', {
                  style: 'currency',
                  currency: 'VND',
                }).format(product.compareAtPrice)}
              </span>
            )}
          </div>

          {product.rating > 0 && (
            <div className="flex items-center gap-1">
              <div className="flex">
                {[...Array(5)].map((_, i) => (
                  <svg
                    key={i}
                    className={`h-3 w-3 ${
                      i < Math.floor(product.rating)
                        ? 'text-yellow-400 fill-yellow-400'
                        : 'text-muted-foreground'
                    }`}
                    viewBox="0 0 20 20"
                  >
                    <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                  </svg>
                ))}
              </div>
              <span className="text-xs text-muted-foreground">
                ({product.reviewCount})
              </span>
            </div>
          )}

          <Button
            className="w-full mt-2"
            onClick={handleAddToCart}
            disabled={!product.inStock || isAdding}
            size="sm"
          >
            {isAdding ? (
              'Đang thêm...'
            ) : product.inStock ? (
              <>
                <ShoppingCart className="h-4 w-4 mr-2" />
                Thêm vào giỏ
              </>
            ) : (
              'Hết hàng'
            )}
          </Button>
        </CardContent>
      </Card>
    </Link>
  );
}
