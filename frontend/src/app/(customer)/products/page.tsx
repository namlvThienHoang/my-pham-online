'use client';

import { useState } from 'react';
import { Filter, SlidersHorizontal, ChevronDown } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetTrigger } from '@/components/ui/sheet';
import { ProductCard } from '@/components/products/ProductCard';
import { useProducts } from '@/hooks/useQueries';
import type { Category } from '@/types';

// Mock categories for filter
const mockCategories: Category[] = [
  { id: '1', name: 'Skincare', slug: 'skincare', level: 1, path: '/skincare', productCount: 120 },
  { id: '2', name: 'Makeup', slug: 'makeup', level: 1, path: '/makeup', productCount: 85 },
  { id: '3', name: 'Haircare', slug: 'haircare', level: 1, path: '/haircare', productCount: 45 },
  { id: '4', name: 'Fragrance', slug: 'fragrance', level: 1, path: '/fragrance', productCount: 30 },
];

const skinTypes = ['Da thường', 'Da nhạy cảm', 'Da dầu', 'Da khô', 'Da hỗn hợp'];
const concerns = ['Chống lão hóa', 'Trị mụn', 'Dưỡng ẩm', 'Làm sáng da', 'Se khít lỗ chân lông'];

interface Filters {
  category?: string;
  skinType?: string;
  concern?: string;
  minPrice?: number;
  maxPrice?: number;
}

export default function ProductsPage() {
  const [filters, setFilters] = useState<Filters>({});
  const [sortBy, setSortBy] = useState('createdAt');
  const [mobileFilterOpen, setMobileFilterOpen] = useState(false);

  const { data: products, isLoading } = useProducts({
    categorySlug: filters.category,
    limit: 20,
    sort: sortBy,
    order: 'desc',
  });

  const FilterContent = () => (
    <div className="space-y-6">
      {/* Categories */}
      <div>
        <h3 className="font-semibold mb-3">Danh mục</h3>
        <div className="space-y-2">
          <label className="flex items-center gap-2 cursor-pointer">
            <input
              type="radio"
              name="category"
              checked={!filters.category}
              onChange={() => setFilters({ ...filters, category: undefined })}
              className="text-primary"
            />
            <span>Tất cả</span>
          </label>
          {mockCategories.map((cat) => (
            <label key={cat.id} className="flex items-center gap-2 cursor-pointer">
              <input
                type="radio"
                name="category"
                checked={filters.category === cat.slug}
                onChange={() => setFilters({ ...filters, category: cat.slug })}
                className="text-primary"
              />
              <span>{cat.name}</span>
            </label>
          ))}
        </div>
      </div>

      {/* Skin Type */}
      <div>
        <h3 className="font-semibold mb-3">Loại da</h3>
        <div className="space-y-2">
          <label className="flex items-center gap-2 cursor-pointer">
            <input
              type="radio"
              name="skinType"
              checked={!filters.skinType}
              onChange={() => setFilters({ ...filters, skinType: undefined })}
              className="text-primary"
            />
            <span>Tất cả</span>
          </label>
          {skinTypes.map((type) => (
            <label key={type} className="flex items-center gap-2 cursor-pointer">
              <input
                type="radio"
                name="skinType"
                checked={filters.skinType === type}
                onChange={() => setFilters({ ...filters, skinType: type })}
                className="text-primary"
              />
              <span>{type}</span>
            </label>
          ))}
        </div>
      </div>

      {/* Concerns */}
      <div>
        <h3 className="font-semibold mb-3">Công dụng</h3>
        <div className="space-y-2">
          {concerns.map((concern) => (
            <label key={concern} className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={filters.concern === concern}
                onChange={(e) =>
                  setFilters({ ...filters, concern: e.target.checked ? concern : undefined })
                }
                className="text-primary rounded"
              />
              <span>{concern}</span>
            </label>
          ))}
        </div>
      </div>

      {/* Price Range */}
      <div>
        <h3 className="font-semibold mb-3">Giá</h3>
        <div className="flex items-center gap-2">
          <input
            type="number"
            placeholder="Từ"
            value={filters.minPrice || ''}
            onChange={(e) =>
              setFilters({ ...filters, minPrice: e.target.value ? Number(e.target.value) : undefined })
            }
            className="w-full px-3 py-2 border rounded-md text-sm"
          />
          <span>-</span>
          <input
            type="number"
            placeholder="Đến"
            value={filters.maxPrice || ''}
            onChange={(e) =>
              setFilters({ ...filters, maxPrice: e.target.value ? Number(e.target.value) : undefined })
            }
            className="w-full px-3 py-2 border rounded-md text-sm"
          />
        </div>
      </div>

      <Button className="w-full" onClick={() => setMobileFilterOpen(false)}>
        Áp dụng bộ lọc
      </Button>
    </div>
  );

  return (
    <div className="container py-8">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold">Tất cả sản phẩm</h1>
        
        <div className="flex items-center gap-2">
          {/* Mobile Filter Button */}
          <Sheet open={mobileFilterOpen} onOpenChange={setMobileFilterOpen}>
            <SheetTrigger asChild>
              <Button variant="outline" className="md:hidden" size="sm">
                <Filter className="h-4 w-4 mr-2" />
                Lọc
              </Button>
            </SheetTrigger>
            <SheetContent side="right" className="w-[300px] sm:w-[400px] overflow-y-auto">
              <SheetHeader>
                <SheetTitle>Bộ lọc sản phẩm</SheetTitle>
              </SheetHeader>
              <div className="mt-6">
                <FilterContent />
              </div>
            </SheetContent>
          </Sheet>

          {/* Sort Dropdown (Desktop) */}
          <div className="relative hidden md:block">
            <select
              value={sortBy}
              onChange={(e) => setSortBy(e.target.value)}
              className="appearance-none px-4 py-2 pr-8 border rounded-md text-sm font-medium bg-background hover:bg-accent cursor-pointer"
            >
              <option value="createdAt">Mới nhất</option>
              <option value="price">Giá tăng dần</option>
              <option value="-price">Giá giảm dần</option>
              <option value="rating">Đánh giá cao</option>
              <option value="name">Tên A-Z</option>
            </select>
            <ChevronDown className="absolute right-2 top-1/2 -translate-y-1/2 h-4 w-4 pointer-events-none" />
          </div>
        </div>
      </div>

      <div className="flex gap-6">
        {/* Desktop Sidebar Filter */}
        <aside className="hidden md:block w-64 flex-shrink-0">
          <Card>
            <CardContent className="p-4">
              <div className="flex items-center gap-2 mb-4">
                <SlidersHorizontal className="h-5 w-5" />
                <h2 className="font-semibold">Bộ lọc</h2>
              </div>
              <FilterContent />
            </CardContent>
          </Card>
        </aside>

        {/* Product Grid */}
        <div className="flex-1">
          {isLoading ? (
            <div className="grid grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
              {[...Array(8)].map((_, i) => (
                <div key={i} className="animate-pulse space-y-4">
                  <div className="aspect-square bg-muted rounded-lg" />
                  <div className="h-4 bg-muted rounded w-3/4" />
                  <div className="h-4 bg-muted rounded w-1/2" />
                </div>
              ))}
            </div>
          ) : (
            <div className="grid grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
              {products?.data.map((product) => (
                <ProductCard key={product.id} product={product} />
              ))}
            </div>
          )}

          {!isLoading && products?.data.length === 0 && (
            <div className="text-center py-12">
              <p className="text-muted-foreground">Không tìm thấy sản phẩm nào</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
