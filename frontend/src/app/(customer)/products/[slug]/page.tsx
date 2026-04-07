'use client';

import { useParams } from 'next/navigation';
import { useState } from 'react';
import Image from 'next/image';
import { ShoppingCart, Heart, Share2, ChevronRight } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { useProduct, useAddToCart, useToggleWishlist, useTrackView } from '@/hooks/useQueries';
import { useCartStore } from '@/store/cartStore';
import type { ProductVariant } from '@/types';

export default function ProductDetailPage() {
  const params = useParams();
  const slug = params.slug as string;
  const { data: product, isLoading } = useProduct(slug);
  const [selectedVariant, setSelectedVariant] = useState<ProductVariant | null>(null);
  const [quantity, setQuantity] = useState(1);
  const [selectedImage, setSelectedImage] = useState(0);
  
  const addToCart = useAddToCart();
  const toggleWishlist = useToggleWishlist();
  const trackView = useTrackView();
  const cart = useCartStore((state) => state.cart);

  if (isLoading) {
    return (
      <div className="container py-8">
        <div className="animate-pulse grid md:grid-cols-2 gap-8">
          <div className="aspect-square bg-muted rounded-lg" />
          <div className="space-y-4">
            <div className="h-8 bg-muted rounded w-3/4" />
            <div className="h-4 bg-muted rounded w-1/2" />
            <div className="h-12 bg-muted rounded" />
          </div>
        </div>
      </div>
    );
  }

  if (!product) {
    return (
      <div className="container py-8 text-center">
        <h1 className="text-2xl font-bold">Không tìm thấy sản phẩm</h1>
      </div>
    );
  }

  // Track view
  if (product) {
    trackView.mutate({ productId: product.id });
  }

  const variant = selectedVariant || product.variants[0];
  const discount = product.compareAtPrice
    ? Math.round(((product.compareAtPrice - product.price) / product.compareAtPrice) * 100)
    : 0;

  const handleAddToCart = async () => {
    if (variant) {
      await addToCart.mutateAsync({
        productId: product.id,
        variantId: variant.id,
        quantity,
      });
    }
  };

  return (
    <div className="container py-8">
      {/* Breadcrumb */}
      <nav className="flex items-center gap-2 text-sm text-muted-foreground mb-6">
        <a href="/" className="hover:text-primary">Trang chủ</a>
        <ChevronRight className="h-4 w-4" />
        <a href="/products" className="hover:text-primary">Sản phẩm</a>
        <ChevronRight className="h-4 w-4" />
        <span className="text-foreground">{product.name}</span>
      </nav>

      <div className="grid md:grid-cols-2 gap-8">
        {/* Images */}
        <div className="space-y-4">
          <div className="relative aspect-square overflow-hidden rounded-lg bg-muted">
            <img
              src={product.images[selectedImage]?.url || '/images/placeholder.jpg'}
              alt={product.name}
              className="object-cover w-full h-full"
            />
            {discount > 0 && (
              <span className="absolute top-4 left-4 bg-destructive text-destructive-foreground text-sm font-bold px-3 py-1.5 rounded">
                -{discount}%
              </span>
            )}
          </div>
          
          {product.images.length > 1 && (
            <div className="flex gap-2 overflow-x-auto">
              {product.images.map((image, index) => (
                <button
                  key={image.id}
                  onClick={() => setSelectedImage(index)}
                  className={`relative aspect-square w-20 flex-shrink-0 rounded-md overflow-hidden border-2 transition-colors ${
                    selectedImage === index ? 'border-primary' : 'border-transparent'
                  }`}
                >
                  <img
                    src={image.url}
                    alt={image.altText || product.name}
                    className="object-cover w-full h-full"
                  />
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Info */}
        <div className="space-y-6">
          <div>
            <h1 className="text-3xl font-bold mb-2">{product.name}</h1>
            
            {product.brand && (
              <a
                href={`/brands/${product.brand.slug}`}
                className="text-muted-foreground hover:text-primary"
              >
                {product.brand.name}
              </a>
            )}
          </div>

          <div className="flex items-center gap-4">
            <div className="flex items-center gap-1">
              {[...Array(5)].map((_, i) => (
                <svg
                  key={i}
                  className={`h-5 w-5 ${
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
            <span className="text-muted-foreground">
              {product.rating.toFixed(1)} ({product.reviewCount} đánh giá)
            </span>
          </div>

          <div className="space-y-2">
            <div className="flex items-baseline gap-3">
              <span className="text-3xl font-bold text-primary">
                {new Intl.NumberFormat('vi-VN', {
                  style: 'currency',
                  currency: 'VND',
                }).format(variant?.price || product.price)}
              </span>
              
              {product.compareAtPrice && (
                <span className="text-xl text-muted-foreground line-through">
                  {new Intl.NumberFormat('vi-VN', {
                    style: 'currency',
                    currency: 'VND',
                  }).format(product.compareAtPrice)}
                </span>
              )}
            </div>
            
            {!product.inStock && (
              <p className="text-destructive font-medium">Hiện đang hết hàng</p>
            )}
          </div>

          {product.description && (
            <div className="prose prose-sm max-w-none">
              <p>{product.description}</p>
            </div>
          )}

          {/* Variants */}
          {product.variants.length > 1 && (
            <div className="space-y-3">
              <label className="text-sm font-medium">Phiên bản</label>
              <div className="flex flex-wrap gap-2">
                {product.variants.map((v) => (
                  <button
                    key={v.id}
                    onClick={() => setSelectedVariant(v)}
                    className={`px-4 py-2 rounded-md border text-sm font-medium transition-colors ${
                      selectedVariant?.id === v.id || (!selectedVariant && v.id === product.variants[0].id)
                        ? 'border-primary bg-primary/10 text-primary'
                        : 'border-input hover:border-primary'
                    }`}
                  >
                    {v.name}
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* Quantity */}
          <div className="space-y-3">
            <label className="text-sm font-medium">Số lượng</label>
            <div className="flex items-center gap-2">
              <button
                onClick={() => setQuantity(Math.max(1, quantity - 1))}
                className="h-10 w-10 rounded-md border flex items-center justify-center hover:bg-accent"
              >
                -
              </button>
              <input
                type="number"
                value={quantity}
                onChange={(e) => setQuantity(Math.max(1, parseInt(e.target.value) || 1))}
                className="h-10 w-20 rounded-md border text-center"
                min="1"
                max={variant?.inventoryQuantity || product.inventoryQuantity}
              />
              <button
                onClick={() => setQuantity(Math.min(variant?.inventoryQuantity || product.inventoryQuantity, quantity + 1))}
                className="h-10 w-10 rounded-md border flex items-center justify-center hover:bg-accent"
              >
                +
              </button>
            </div>
            <p className="text-xs text-muted-foreground">
              Còn lại: {variant?.inventoryQuantity || product.inventoryQuantity} sản phẩm
            </p>
          </div>

          {/* Actions */}
          <div className="flex gap-3">
            <Button
              size="lg"
              className="flex-1"
              onClick={handleAddToCart}
              disabled={!product.inStock || addToCart.isPending}
            >
              <ShoppingCart className="h-5 w-5 mr-2" />
              {addToCart.isPending ? 'Đang thêm...' : 'Thêm vào giỏ'}
            </Button>
            
            <Button
              size="lg"
              variant="outline"
              onClick={() => toggleWishlist.mutate({ productId: product.id })}
            >
              <Heart className="h-5 w-5" />
            </Button>
            
            <Button size="lg" variant="outline">
              <Share2 className="h-5 w-5" />
            </Button>
          </div>

          {/* Attributes */}
          {product.attributes.length > 0 && (
            <Card>
              <CardContent className="p-4 space-y-2">
                <h3 className="font-semibold">Thông số</h3>
                {product.attributes.map((attr) => (
                  <div key={attr.name} className="flex justify-between text-sm">
                    <span className="text-muted-foreground">{attr.name}</span>
                    <span className="font-medium">{attr.value}</span>
                  </div>
                ))}
              </CardContent>
            </Card>
          )}
        </div>
      </div>

      {/* Reviews & Details Tabs */}
      <div className="mt-12">
        <div className="border-b">
          <div className="flex gap-8">
            <button className="pb-4 border-b-2 border-primary font-medium">
              Mô tả chi tiết
            </button>
            <button className="pb-4 border-b-2 border-transparent hover:border-muted-foreground text-muted-foreground">
              Đánh giá ({product.reviewCount})
            </button>
            <button className="pb-4 border-b-2 border-transparent hover:border-muted-foreground text-muted-foreground">
              Hỏi đáp
            </button>
          </div>
        </div>
        
        <div className="py-8 prose max-w-none">
          <p>Thông tin chi tiết sản phẩm đang được cập nhật...</p>
        </div>
      </div>
    </div>
  );
}
