'use client';

import { useEffect, useState } from 'react';
import { ShoppingCart } from 'lucide-react';
import { Button } from '@/components/ui/button';
import type { ProductVariant } from '@/types';

interface StickyAddToCartProps {
  variant: ProductVariant;
  inStock: boolean;
  onAddToCart: () => void;
  isAdding: boolean;
}

export function StickyAddToCart({ variant, inStock, onAddToCart, isAdding }: StickyAddToCartProps) {
  const [isVisible, setIsVisible] = useState(false);
  const [ctaRef, setCtaRef] = useState<HTMLDivElement | null>(null);

  useEffect(() => {
    const handleScroll = () => {
      if (!ctaRef) return;

      const ctaRect = ctaRef.getBoundingClientRect();
      // Show sticky bar when CTA scrolls out of view
      setIsVisible(ctaRect.top < -100);
    };

    window.addEventListener('scroll', handleScroll, { passive: true });
    return () => window.removeEventListener('scroll', handleScroll);
  }, [ctaRef]);

  if (!isVisible) return null;

  return (
    <div className="fixed bottom-0 left-0 right-0 bg-background border-t p-4 z-40 md:hidden safe-area-pb">
      <div className="container flex items-center gap-4">
        <div className="flex-1">
          <p className="text-sm text-muted-foreground">Tổng cộng:</p>
          <p className="text-lg font-bold text-primary">
            {new Intl.NumberFormat('vi-VN', {
              style: 'currency',
              currency: 'VND',
            }).format(variant.price)}
          </p>
        </div>
        <Button
          size="lg"
          className="flex-1 max-w-[200px]"
          onClick={onAddToCart}
          disabled={!inStock || isAdding}
        >
          <ShoppingCart className="h-4 w-4 mr-2" />
          {isAdding ? 'Đang thêm...' : 'Thêm vào giỏ'}
        </Button>
      </div>
    </div>
  );
}

// Hook to capture CTA ref
export function useStickyCTA() {
  const [ref, setRef] = useState<HTMLDivElement | null>(null);
  return [ref, setRef] as const;
}
