# BÁO CÁO ĐÁNH GIÁ TRẢI NGHIỆM NGƯỜI DÙNG (UX AUDIT)
## Dự án: my-pham-online - Thương mại Điện tử Ngành Mỹ phẩm

---

## PHẦN 1: KHẢO SÁT HIỆN TRẠNG UI/UX (Desktop & Mobile)

### 1.1 Công nghệ Frontend

| Thành phần | Công nghệ sử dụng | Phiên bản |
|------------|------------------|-----------|
| **Framework** | Next.js (App Router) | 15.0.3 |
| **UI Library** | React + shadcn/ui (Radix UI primitives) | React 18.3.1 |
| **Styling** | Tailwind CSS | 3.4.15 |
| **State Management** | Zustand | 5.0.1 |
| **Data Fetching** | TanStack Query (React Query) | 5.62.3 |
| **Icons** | Lucide React | 0.460.0 |
| **Form Handling** | React Hook Form + Zod | 7.53.2 |

**Đánh giá:** Stack công nghệ hiện đại, phù hợp cho việc xây dựng giao diện responsive và tối ưu hiệu năng.

### 1.2 Cấu trúc Layout Tổng thể

```
┌─────────────────────────────────────────────┐
│                 HEADER                       │
│  [Menu] [Logo] [Nav Desktop] [Search] [Icons]│
├─────────────────────────────────────────────┤
│                                              │
│              MAIN CONTENT                    │
│  • Hero Section                              │
│  • Product Listing (Grid 2/4 cols)           │
│  • Categories Grid                           │
│  • Brands Section                            │
│                                              │
├─────────────────────────────────────────────┤
│                 FOOTER                       │
│  [Info] [Links] [Contact] [Social]          │
└─────────────────────────────────────────────┘
```

**File tham chiếu:**
- `frontend/src/app/layout.tsx` - Root layout với font Inter
- `frontend/src/app/(customer)/layout.tsx` - Customer layout với Header/Footer
- `frontend/src/components/layout/Header.tsx` - Điều hướng chính
- `frontend/src/components/layout/Footer.tsx` - Chân trang

### 1.3 Đánh giá Responsive Hiện tại

| Breakpoint | Trạng thái | Ghi chú |
|------------|-----------|---------|
| **Mobile (< 768px)** | ✅ Cơ bản | Menu collapse thành hamburger, grid 2 cột |
| **Tablet (768px - 1024px)** | ✅ Tốt | Grid chuyển từ 2 → 4 cột |
| **Desktop (> 1024px)** | ✅ Tốt | Full layout với navigation đầy đủ |

**Viewport Meta Tag:** Được cấu hình mặc định bởi Next.js (kiểm tra trong `frontend/src/app/layout.tsx`).

**Media Queries (Tailwind):** Sử dụng các breakpoint chuẩn `sm`, `md`, `lg`, `xl`, `2xl`.

---

## PHẦN 2: PHÂN TÍCH ĐIỂM ĐAU (PAIN POINTS) VÀ ĐỀ XUẤT CẢI TIẾN

### 2.1 Thanh Điều hướng & Tìm kiếm (Navigation & Search)

#### Hiện trạng
**File:** `frontend/src/components/layout/Header.tsx`

```tsx
// Line 28-41: Navigation Desktop chỉ hiển thị trên md+
<nav className="hidden md:flex items-center gap-6 ml-6">
  <Link href="/products">Sản phẩm</Link>
  <Link href="/categories">Danh mục</Link>
  <Link href="/brands">Thương hiệu</Link>
  <Link href="/promotions">Khuyến mãi</Link>
</nav>

// Line 20-22: Mobile chỉ có nút hamburger không có chức năng
<Button variant="ghost" size="icon" className="md:hidden">
  <Menu className="h-5 w-5" />
</Button>
```

#### Vấn đề UX

| Vấn đề | Mức độ nghiêm trọng | Ảnh hưởng |
|--------|---------------------|-----------|
| 🔴 **Nút hamburger Mobile không hoạt động** | Cao | Người dùng mobile không thể truy cập menu |
| 🟡 **Không có Mega Menu cho Desktop** | Trung bình | Khó khám phá danh mục mỹ phẩm chi tiết |
| 🟡 **Không có phân loại nhanh "Son môi", "Kem chống nắng"** | Trung bình | Tăng thời gian tìm kiếm sản phẩm |
| 🟢 **Thanh tìm kiếm có suggestion** | Tốt | Hiển thị gợi ý khi gõ từ khóa |

#### Đề xuất Cải tiến

##### A. Mobile - Bottom Navigation Bar (Ưu tiên cao)

```tsx
// File mới: frontend/src/components/layout/MobileNav.tsx
'use client';

import { Home, ShoppingBag, User, Heart, Menu } from 'lucide-react';
import Link from 'next/link';

export function MobileNav() {
  return (
    <nav className="fixed bottom-0 left-0 right-0 bg-background border-t z-50 md:hidden">
      <div className="grid grid-cols-5 h-16">
        <Link href="/" className="flex flex-col items-center justify-center gap-1">
          <Home className="h-5 w-5" />
          <span className="text-xs">Trang chủ</span>
        </Link>
        <Link href="/products" className="flex flex-col items-center justify-center gap-1">
          <ShoppingBag className="h-5 w-5" />
          <span className="text-xs">Sản phẩm</span>
        </Link>
        <Link href="/categories" className="flex flex-col items-center justify-center gap-1">
          <Menu className="h-5 w-5" />
          <span className="text-xs">Danh mục</span>
        </Link>
        <Link href="/wishlist" className="flex flex-col items-center justify-center gap-1">
          <Heart className="h-5 w-5" />
          <span className="text-xs">Yêu thích</span>
        </Link>
        <Link href="/profile" className="flex flex-col items-center justify-center gap-1">
          <User className="h-5 w-5" />
          <span className="text-xs">Tài khoản</span>
        </Link>
      </div>
    </nav>
  );
}
```

**Áp dụng trên Desktop:** Không hiển thị (sử dụng `md:hidden`).

##### B. Desktop - Mega Menu cho Danh mục Mỹ phẩm

```tsx
// Cải tiến Header.tsx - Thêm Mega Menu
const beautyCategories = [
  {
    name: 'Skincare',
    subcategories: [
      { name: 'Sửa rửa mặt', slug: 'sua-rua-mat' },
      { name: 'Toner', slug: 'toner' },
      { name: 'Serum', slug: 'serum' },
      { name: 'Kem dưỡng', slug: 'kem-duong' },
      { name: 'Kem chống nắng', slug: 'kem-chong-nang' },
    ]
  },
  {
    name: 'Makeup',
    subcategories: [
      { name: 'Son môi', slug: 'son-moi' },
      { name: 'Phấn nền', slug: 'phan-nen' },
      { name: 'Mascara', slug: 'mascara' },
      { name: 'Phấn mắt', slug: 'phan-mat' },
    ]
  },
  // ... thêm Haircare, Fragrance
];
```

**Thiết kế Mega Menu:**
```
┌─────────────────────────────────────────────────────────────┐
│  SKINCARE          │  MAKEUP            │  HAIRCARE         │
│  ├─ Sửa rửa mặt    │  ├─ Son môi        │  ├─ Dầu gội       │
│  ├─ Toner          │  ├─ Phấn nền       │  ├─ Dầu xả        │
│  ├─ Serum          │  ├─ Mascara        │  └─ Mặt nạ tóc    │
│  ├─ Kem dưỡng      │  └─ Phấn mắt       │                   │
│  └─ Kem chống nắng │                    │                   │
└─────────────────────────────────────────────────────────────┘
```

**Áp dụng trên Mobile:** Chuyển thành accordion expandable trong drawer menu.

---

### 2.2 Trang Danh sách Sản phẩm (PLP - Product Listing Page)

#### Hiện trạng
**File:** `frontend/src/app/(customer)/page.tsx` (Homepage có product grid)

```tsx
// Line 59-64: Grid sản phẩm cơ bản
<div className="grid grid-cols-2 md:grid-cols-4 gap-4">
  {featuredProducts?.data.map((product) => (
    <ProductCard key={product.id} product={product} />
  ))}
</div>
```

**Lưu ý quan trọng:** Dự án **CHƯA CÓ** trang danh sách sản phẩm riêng (`/products` page chưa tồn tại). Đây là lỗ hổng lớn về UX.

#### Vấn đề UX

| Vấn đề | Mức độ nghiêm trọng | Ảnh hưởng |
|--------|---------------------|-----------|
| 🔴 **Không có trang PLP dedicated** | Rất cao | Người dùng không thể browse toàn bộ sản phẩm |
| 🔴 **Không có bộ lọc (Filter)** | Rất cao | Không thể lọc theo Loại da, Giá, Thương hiệu |
| 🔴 **Không có sắp xếp (Sort)** | Rất cao | Không thể sort theo Bán chạy, Giá, Mới nhất |
| 🟡 **Product Card thiếu thông tin** | Trung bình | Thiếu badge "Chính hãng", "Freeship" |

#### Đề xuất Cải tiến

##### A. Tạo trang PLP với Filter Slide-out cho Mobile

**File mới:** `frontend/src/app/(customer)/products/page.tsx`

```tsx
'use client';

import { useState } from 'react';
import { Filter, SlidersHorizontal, X } from 'lucide-react';
import { ProductCard } from '@/components/products/ProductCard';
import { useProducts } from '@/hooks/useQueries';

export default function ProductsPage() {
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [filters, setFilters] = useState({
    category: '',
    brand: '',
    minPrice: 0,
    maxPrice: 1000000,
    skinType: '', // Loại da: Dầu, Khô, Hỗn hợp
    concern: '',  // Công dụng: Trị mụn, Dưỡng ẩm, Chống lão hóa
  });

  const { data, isLoading } = useProducts(filters);

  return (
    <div className="container py-8">
      {/* Header với Sort và Filter Toggle */}
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl font-bold">Tất cả sản phẩm</h1>
        <div className="flex gap-2">
          {/* Mobile Filter Button */}
          <button 
            className="md:hidden flex items-center gap-2 px-4 py-2 border rounded-lg"
            onClick={() => setFiltersOpen(true)}
          >
            <SlidersHorizontal className="h-4 w-4" />
            Lọc
          </button>
          
          {/* Sort Dropdown */}
          <select className="border rounded-lg px-3 py-2 text-sm">
            <option value="popular">Bán chạy nhất</option>
            <option value="price_asc">Giá tăng dần</option>
            <option value="price_desc">Giá giảm dần</option>
            <option value="newest">Mới nhất</option>
            <option value="rating">Đánh giá cao</option>
          </select>
        </div>
      </div>

      <div className="flex gap-6">
        {/* Desktop Sidebar Filter */}
        <aside className="hidden md:block w-64 space-y-6">
          <FilterSection title="Danh mục">
            {/* Category checkboxes */}
          </FilterSection>
          <FilterSection title="Loại da">
            {['Da dầu', 'Da khô', 'Da hỗn hợp', 'Da nhạy cảm'].map(type => (
              <label key={type} className="flex items-center gap-2">
                <input type="checkbox" value={type} />
                <span className="text-sm">{type}</span>
              </label>
            ))}
          </FilterSection>
          <FilterSection title="Công dụng">
            {['Trị mụn', 'Dưỡng ẩm', 'Trắng da', 'Chống lão hóa'].map(use => (
              <label key={use} className="flex items-center gap-2">
                <input type="checkbox" value={use} />
                <span className="text-sm">{use}</span>
              </label>
            ))}
          </FilterSection>
          <FilterSection title="Giá">
            <input type="range" min="0" max="1000000" className="w-full" />
          </FilterSection>
        </aside>

        {/* Product Grid */}
        <main className="flex-1">
          <div className="grid grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
            {data?.data.map(product => (
              <ProductCard key={product.id} product={product} />
            ))}
          </div>
        </main>
      </div>

      {/* Mobile Filter Slide-out */}
      {filtersOpen && (
        <div className="fixed inset-0 z-50 md:hidden">
          <div className="absolute inset-0 bg-black/50" onClick={() => setFiltersOpen(false)} />
          <div className="absolute right-0 top-0 bottom-0 w-full max-w-sm bg-background p-6 overflow-y-auto">
            <div className="flex justify-between items-center mb-6">
              <h2 className="text-lg font-bold">Bộ lọc</h2>
              <button onClick={() => setFiltersOpen(false)}>
                <X className="h-5 w-5" />
              </button>
            </div>
            {/* Filter content same as desktop sidebar */}
            <button 
              className="w-full bg-primary text-primary-foreground py-3 rounded-lg mt-6"
              onClick={() => setFiltersOpen(false)}
            >
              Áp dụng lọc
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
```

**Áp dụng trên Desktop:** Sidebar filter cố định bên trái.

##### B. Cải tiến Product Card

**File:** `frontend/src/components/products/ProductCard.tsx`

```tsx
// Thêm vào phần đầu card (Line 55-66)
{product.inStock && (
  <span className="absolute top-2 right-2 bg-green-500 text-white text-xs font-bold px-2 py-1 rounded">
    Chính hãng
  </span>
)}

{product.tags?.includes('freeship') && (
  <span className="absolute bottom-2 left-2 bg-blue-500 text-white text-xs font-bold px-2 py-1 rounded">
    Freeship
  </span>
)}

// Cải tiến nút Add to Cart (Line 126-142)
<Button
  className="w-full mt-2 h-10 text-base font-semibold"
  onClick={handleAddToCart}
  disabled={!product.inStock || isAdding}
  size="lg" // Tăng kích thước cho mobile
>
  {isAdding ? (
    'Đang thêm...'
  ) : product.inStock ? (
    <>
      <ShoppingCart className="h-5 w-5 mr-2" />
      Thêm vào giỏ
    </>
  ) : (
    'Hết hàng'
  )}
</Button>
```

---

### 2.3 Trang Chi tiết Sản phẩm (PDP - Product Detail Page)

#### Hiện trạng
**File:** `frontend/src/app/(customer)/products/[slug]/page.tsx`

```tsx
// Line 236-258: Action buttons
<div className="flex gap-3">
  <Button size="lg" className="flex-1" onClick={handleAddToCart}>
    <ShoppingCart className="h-5 w-5 mr-2" />
    Thêm vào giỏ
  </Button>
  <Button size="lg" variant="outline">
    <Heart className="h-5 w-5" />
  </Button>
  <Button size="lg" variant="outline">
    <Share2 className="h-5 w-5" />
  </Button>
</div>
```

#### Vấn đề UX

| Vấn đề | Mức độ nghiêm trọng | Ảnh hưởng |
|--------|---------------------|-----------|
| 🔴 **Không có Sticky Add to Cart trên Mobile** | Cao | Người dùng phải cuộn lên để mua |
| 🟡 **Không có Image Zoom** | Trung bình | Khó xem chi tiết sản phẩm mỹ phẩm |
| 🟡 **Thông tin mô tả bị ẩn dưới tabs** | Trung bình | Người dùng có thể bỏ sót thông tin |
| 🟡 **Thiếu Trust Signals gần CTA** | Trung bình | Giảm tỷ lệ chuyển đổi |

#### Đề xuất Cải tiến

##### A. Sticky Add to Cart Bar cho Mobile

```tsx
// Thêm vào cuối file products/[slug]/page.tsx
'use client';

import { useEffect, useState } from 'react';

// Trong component ProductDetailPage:
const [showStickyBar, setShowStickyBar] = useState(false);

useEffect(() => {
  const handleScroll = () => {
    // Show sticky bar when user scrolls past the main CTA
    const mainCta = document.querySelector('[data-main-cta]');
    if (mainCta) {
      const rect = mainCta.getBoundingClientRect();
      setShowStickyBar(rect.top < -100);
    }
  };
  
  window.addEventListener('scroll', handleScroll);
  return () => window.removeEventListener('scroll', handleScroll);
}, []);

// Render Sticky Bar
{showStickyBar && (
  <div className="fixed bottom-0 left-0 right-0 bg-background border-t p-4 z-40 md:hidden safe-area-pb">
    <div className="flex gap-2">
      <div className="flex-1">
        <p className="text-sm font-bold text-primary">
          {new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND',
          }).format(variant?.price || product.price)}
        </p>
      </div>
      <Button 
        className="flex-1 h-12 text-base font-semibold"
        size="lg"
        onClick={handleAddToCart}
        disabled={!product.inStock}
      >
        <ShoppingCart className="h-5 w-5 mr-2" />
        Thêm vào giỏ
      </Button>
    </div>
  </div>
)}
```

**CSS bổ sung trong `globals.css`:**
```css
.safe-area-pb {
  padding-bottom: env(safe-area-inset-bottom);
}
```

**Áp dụng trên Desktop:** Không hiển thị (`md:hidden`).

##### B. Image Zoom Functionality

```tsx
// Thêm dependency: npm install react-medium-image-zoom
import ImageZoom from 'react-medium-image-zoom';
import 'react-medium-image-zoom/dist/styles.css';

// Thay thế img tag (Line 84-93)
<ImageZoom>
  <img
    src={product.images[selectedImage]?.url || '/images/placeholder.jpg'}
    alt={product.name}
    className="object-cover w-full h-full cursor-zoom-in"
  />
</ImageZoom>
```

##### C. Trust Signals Block gần CTA

```tsx
// Thêm sau action buttons (Line 258)
<div className="space-y-3 p-4 bg-muted/50 rounded-lg">
  <div className="flex items-center gap-3 text-sm">
    <CheckCircle className="h-5 w-5 text-green-500" />
    <span>Cam kết chính hãng 100%</span>
  </div>
  <div className="flex items-center gap-3 text-sm">
    <Truck className="h-5 w-5 text-blue-500" />
    <span>Miễn phí vận chuyển từ 500.000đ</span>
  </div>
  <div className="flex items-center gap-3 text-sm">
    <RefreshCcw className="h-5 w-5 text-orange-500" />
    <span>Đổi trả trong 30 ngày</span>
  </div>
  <div className="flex items-center gap-3 text-sm">
    <Shield className="h-5 w-5 text-purple-500" />
    <span>Bảo hành bởi thương hiệu</span>
  </div>
</div>
```

---

### 2.4 Quy trình Giỏ hàng & Thanh toán (Cart & Checkout Flow)

#### Hiện trạng
**File:** `frontend/src/app/(customer)/cart/page.tsx`

```tsx
// Line 53-59: Checkout handler đơn giản
const handleCheckout = async () => {
  await checkout.mutateAsync({
    shippingAddressId: 'default',
    paymentMethod: 'COD',
  });
};
```

#### Vấn đề UX

| Vấn đề | Mức độ nghiêm trọng | Ảnh hưởng |
|--------|---------------------|-----------|
| 🔴 **Chuyển hướng rời trang khi Add to Cart** | Cao | Gián đoạn trải nghiệm mua sắm |
| 🟡 **Không có Slide-in Cart** | Trung bình | Phải tải lại trang để xem giỏ |
| 🟡 **Form địa chỉ có thể dài trên Mobile** | Trung bình | Tăng tỷ lệ abandon checkout |

#### Đề xuất Cải tiến

##### A. Slide-in Cart (Giỏ hàng bay)

**File mới:** `frontend/src/components/cart/SlideInCart.tsx`

```tsx
'use client';

import { createPortal } from 'react-dom';
import { X, Trash2, Plus, Minus } from 'lucide-react';
import { useCartStore } from '@/store/cartStore';
import { Button } from '@/components/ui/button';

export function SlideInCart() {
  const { isOpen, closeCart, cart } = useCartStore();
  
  if (!isOpen || typeof window === 'undefined') return null;

  return createPortal(
    <div className="fixed inset-0 z-50">
      {/* Backdrop */}
      <div 
        className="absolute inset-0 bg-black/50 transition-opacity"
        onClick={closeCart}
      />
      
      {/* Cart Panel */}
      <div className="absolute right-0 top-0 bottom-0 w-full max-w-md bg-background shadow-xl flex flex-col">
        {/* Header */}
        <div className="flex justify-between items-center p-4 border-b">
          <h2 className="text-lg font-bold">Giỏ hàng ({cart?.items.length || 0})</h2>
          <button onClick={closeCart}>
            <X className="h-5 w-5" />
          </button>
        </div>
        
        {/* Items */}
        <div className="flex-1 overflow-y-auto p-4 space-y-4">
          {cart?.items.map(item => (
            <div key={item.id} className="flex gap-4">
              <img src={item.image} alt={item.productName} className="w-20 h-20 object-cover rounded" />
              <div className="flex-1">
                <p className="font-medium line-clamp-2">{item.productName}</p>
                <p className="text-sm text-muted-foreground">{item.options.map(o => o.value).join(', ')}</p>
                <div className="flex justify-between items-center mt-2">
                  <div className="flex items-center gap-2">
                    <Button variant="outline" size="icon" className="h-8 w-8">
                      <Minus className="h-3 w-3" />
                    </Button>
                    <span className="text-sm w-8 text-center">{item.quantity}</span>
                    <Button variant="outline" size="icon" className="h-8 w-8">
                      <Plus className="h-3 w-3" />
                    </Button>
                  </div>
                  <p className="font-bold text-primary">
                    {new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(item.totalPrice)}
                  </p>
                </div>
              </div>
            </div>
          ))}
        </div>
        
        {/* Footer */}
        <div className="border-t p-4 space-y-4">
          <div className="flex justify-between text-lg font-bold">
            <span>Tổng cộng:</span>
            <span className="text-primary">
              {new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(cart?.total || 0)}
            </span>
          </div>
          <Button className="w-full h-12 text-base" size="lg">
            Thanh toán ngay
          </Button>
          <Link href="/cart" className="block text-center text-sm text-primary hover:underline">
            Xem giỏ hàng đầy đủ →
          </Link>
        </div>
      </div>
    </div>,
    document.body
  );
}
```

**Cập nhật `frontend/src/store/cartStore.ts`:**
```tsx
import { create } from 'zustand';

interface CartState {
  cart: Cart | null;
  isOpen: boolean;
  openCart: () => void;
  closeCart: () => void;
  // ... existing actions
}

export const useCartStore = create<CartState>((set) => ({
  cart: null,
  isOpen: false,
  openCart: () => set({ isOpen: true }),
  closeCart: () => set({ isOpen: false }),
  // ... existing actions
}));
```

**Cập nhật Header.tsx để mở Slide-in Cart:**
```tsx
// Line 93-101
<Link href="/cart" onClick={(e) => { e.preventDefault(); useCartStore.getState().openCart(); }}>
  <Button variant="ghost" size="icon" className="relative">
    <ShoppingCart className="h-5 w-5" />
    {/* ... badge */}
  </Button>
</Link>
```

**Áp dụng trên Desktop:** Same functionality, panel rộng hơn (max-w-md).

---

### 2.5 Yếu tố Tin cậy & Chuyển đổi (Trust Signals & Conversion)

#### Hiện trạng
**File:** `frontend/src/components/layout/Footer.tsx`

```tsx
// Line 10-13: Chỉ có mô tả ngắn
<p className="text-sm text-muted-foreground">
  Cửa hàng mỹ phẩm trực tuyến uy tín, chất lượng cao. Cam kết sản phẩm chính hãng 100%.
</p>
```

#### Vấn đề UX

| Vấn đề | Mức độ nghiêm trọng | Ảnh hưởng |
|--------|---------------------|-----------|
| 🟡 **Trust signals chỉ ở Footer** | Trung bình | Người dùng có thể không cuộn tới |
| 🟡 **Không có review images trên PDP** | Trung bình | Giảm độ tin cậy đánh giá |
| 🟡 **Không hiển thị "Đã bán" số lượng** | Thấp | Mất yếu tố social proof |

#### Đề xuất Cải tiến

##### A. Trust Badges Block (Thêm vào Homepage và PDP)

**Component mới:** `frontend/src/components/trust/TrustBadges.tsx`

```tsx
import { Shield, Truck, RefreshCcw, Award, CheckCircle, Leaf } from 'lucide-react';

export function TrustBadges() {
  const badges = [
    { icon: Shield, title: 'Chính hãng 100%', desc: 'Cam kết hoàn tiền nếu phát hiện hàng giả' },
    { icon: Truck, title: 'Freeship từ 500k', desc: 'Miễn phí vận chuyển toàn quốc' },
    { icon: RefreshCcw, title: 'Đổi trả 30 ngày', desc: 'Áp dụng cho mọi sản phẩm' },
    { icon: Award, title: 'Bảo hành chính hãng', desc: 'Bảo hành bởi nhà sản xuất' },
    { icon: CheckCircle, title: 'Kiểm tra trước khi nhận', desc: 'Được kiểm tra hàng trước khi thanh toán' },
    { icon: Leaf, title: 'Thành phần an toàn', desc: '100% thành phần tự nhiên, không độc hại' },
  ];

  return (
    <section className="py-12 bg-muted/30">
      <div className="container">
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-6">
          {badges.map((badge) => (
            <div key={badge.title} className="flex flex-col items-center text-center gap-3">
              <div className="h-14 w-14 rounded-full bg-primary/10 flex items-center justify-center">
                <badge.icon className="h-7 w-7 text-primary" />
              </div>
              <div>
                <h3 className="font-semibold text-sm">{badge.title}</h3>
                <p className="text-xs text-muted-foreground mt-1">{badge.desc}</p>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
```

**Vị trí chèn:** Sau Hero section trong `frontend/src/app/(customer)/page.tsx`.

**Áp dụng trên Mobile:** Grid 2 cột, icon nhỏ hơn.

##### B. Customer Reviews với Images trên PDP

```tsx
// Thêm section đánh giá trong products/[slug]/page.tsx
<section className="mt-12 border-t pt-8">
  <h2 className="text-2xl font-bold mb-6">
    Đánh giá từ khách hàng ({product.reviewCount})
  </h2>
  
  {/* Review Summary */}
  <div className="flex items-center gap-6 mb-8">
    <div className="text-center">
      <p className="text-5xl font-bold">{product.rating.toFixed(1)}</p>
      <div className="flex gap-1 my-2">
        {[...Array(5)].map((_, i) => (
          <Star key={i} className={`h-5 w-5 ${i < Math.floor(product.rating) ? 'fill-yellow-400 text-yellow-400' : 'text-gray-300'}`} />
        ))}
      </div>
      <p className="text-sm text-muted-foreground">{product.reviewCount} đánh giá</p>
    </div>
    
    {/* Rating Distribution */}
    <div className="flex-1 space-y-2">
      {[5, 4, 3, 2, 1].map(star => (
        <div key={star} className="flex items-center gap-2">
          <span className="text-sm w-3">{star}</span>
          <Star className="h-3 w-3 fill-yellow-400 text-yellow-400" />
          <div className="flex-1 h-2 bg-muted rounded-full overflow-hidden">
            <div className="h-full bg-yellow-400" style={{ width: `${getPercentage(star)}%` }} />
          </div>
          <span className="text-xs text-muted-foreground w-8">{getCount(star)}</span>
        </div>
      ))}
    </div>
  </div>
  
  {/* Review List with Images */}
  <div className="grid md:grid-cols-2 gap-6">
    {reviews?.map(review => (
      <div key={review.id} className="border rounded-lg p-4 space-y-3">
        <div className="flex items-center gap-3">
          <img src={review.userAvatar} alt={review.userName} className="w-10 h-10 rounded-full" />
          <div>
            <p className="font-medium">{review.userName}</p>
            <div className="flex gap-1">
              {[...Array(5)].map((_, i) => (
                <Star key={i} className={`h-3 w-3 ${i < review.rating ? 'fill-yellow-400 text-yellow-400' : 'text-gray-300'}`} />
              ))}
            </div>
          </div>
          {review.verifiedPurchase && (
            <span className="text-xs bg-green-100 text-green-700 px-2 py-1 rounded">
              Đã mua hàng
            </span>
          )}
        </div>
        
        <p className="text-sm">{review.content}</p>
        
        {/* Review Images */}
        {review.images.length > 0 && (
          <div className="flex gap-2">
            {review.images.map(img => (
              <img 
                key={img.id} 
                src={img.url} 
                alt="Review" 
                className="w-20 h-20 object-cover rounded-lg cursor-pointer hover:opacity-80"
              />
            ))}
          </div>
        )}
        
        <p className="text-xs text-muted-foreground">{formatDate(review.createdAt)}</p>
      </div>
    ))}
  </div>
</section>
```

---

## PHẦN 3: GỢI Ý TÍNH NĂNG TRẢI NGHIỆM MỚI (NEW UX FEATURES)

### 3.1 Tính năng 1: Quiz Chẩn đoán Loại Da (Skin Type Quiz)

**Mô tả:** Giao diện trắc nghiệm 5-7 câu hỏi để gợi ý bộ sản phẩm phù hợp với loại da và mối quan tâm của người dùng.

**File mới:** `frontend/src/app/(customer)/skin-quiz/page.tsx`

```tsx
'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { ChevronRight, Sparkles } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';

const questions = [
  {
    id: 'skinType',
    question: 'Loại da của bạn là gì?',
    options: [
      { value: 'oily', label: 'Da dầu', image: '/images/skin-oily.jpg' },
      { value: 'dry', label: 'Da khô', image: '/images/skin-dry.jpg' },
      { value: 'combination', label: 'Da hỗn hợp', image: '/images/skin-combo.jpg' },
      { value: 'sensitive', label: 'Da nhạy cảm', image: '/images/skin-sensitive.jpg' },
    ]
  },
  {
    id: 'concern',
    question: 'Mối quan tâm chính của bạn?',
    options: [
      { value: 'acne', label: 'Trị mụn' },
      { value: 'whitening', label: 'Làm trắng' },
      { value: 'anti-aging', label: 'Chống lão hóa' },
      { value: 'moisturizing', label: 'Dưỡng ẩm' },
      { value: 'pores', label: 'Se khít lỗ chân lông' },
    ]
  },
  {
    id: 'budget',
    question: 'Ngân sách của bạn?',
    options: [
      { value: 'low', label: 'Dưới 500.000đ' },
      { value: 'medium', label: '500.000đ - 1.500.000đ' },
      { value: 'high', label: 'Trên 1.500.000đ' },
    ]
  },
];

export default function SkinQuizPage() {
  const [currentQuestion, setCurrentQuestion] = useState(0);
  const [answers, setAnswers] = useState<Record<string, string>>({});
  const router = useRouter();

  const handleAnswer = (value: string) => {
    setAnswers({ ...answers, [questions[currentQuestion].id]: value });
    
    if (currentQuestion < questions.length - 1) {
      setCurrentQuestion(currentQuestion + 1);
    } else {
      // Submit and show results
      router.push(`/skin-quiz/results?${new URLSearchParams(answers)}`);
    }
  };

  const progress = ((currentQuestion + 1) / questions.length) * 100;

  return (
    <div className="container py-12 max-w-2xl mx-auto">
      {/* Progress Bar */}
      <div className="mb-8">
        <div className="flex justify-between text-sm mb-2">
          <span>Câu hỏi {currentQuestion + 1}/{questions.length}</span>
          <span>{Math.round(progress)}%</span>
        </div>
        <div className="h-2 bg-muted rounded-full overflow-hidden">
          <div 
            className="h-full bg-primary transition-all duration-300"
            style={{ width: `${progress}%` }}
          />
        </div>
      </div>

      {/* Question Card */}
      <Card>
        <CardContent className="p-8">
          <h1 className="text-2xl font-bold mb-6">
            {questions[currentQuestion].question}
          </h1>
          
          <div className="grid gap-4">
            {questions[currentQuestion].options.map(option => (
              <button
                key={option.value}
                onClick={() => handleAnswer(option.value)}
                className="w-full p-4 border-2 rounded-lg hover:border-primary hover:bg-primary/5 transition-all text-left flex items-center gap-4"
              >
                {option.image && (
                  <img src={option.image} alt={option.label} className="w-16 h-16 object-cover rounded" />
                )}
                <span className="font-medium">{option.label}</span>
                <ChevronRight className="ml-auto h-5 w-5 text-muted-foreground" />
              </button>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Trust Message */}
      <div className="mt-8 text-center text-sm text-muted-foreground">
        <Sparkles className="h-4 w-4 inline mr-2" />
        Nhận gợi ý sản phẩm phù hợp chỉ trong 2 phút
      </div>
    </div>
  );
}
```

**Kết quả Quiz Page:** `frontend/src/app/(customer)/skin-quiz/results/page.tsx`

```tsx
// Hiển thị bộ sản phẩm được gợi ý dựa trên answers
// Include: "Bộ sản phẩm cho da dầu trị mụn" + CTA "Mua trọn bộ"
```

**Áp dụng trên Mobile:** Full-width cards, touch-friendly buttons.

**Lợi ích CRO:**
- Tăng thời gian trên site
- Personalization → Tăng conversion rate
- Cross-sell bộ sản phẩm thay vì单品

---

### 3.2 Tính năng 2: Virtual Try-On (Thử son ảo bằng Camera)

**Mô tả:** Sử dụng camera điện thoại để thử màu son trực tiếp trên môi người dùng (AR filter).

**File mới:** `frontend/src/components/virtual-tryon/VirtualTryOn.tsx`

```tsx
'use client';

import { useRef, useState } from 'react';
import { Camera, Upload, X } from 'lucide-react';
import { Button } from '@/components/ui/button';

interface VirtualTryOnProps {
  productColor: string; // Hex color của son
  productName: string;
}

export function VirtualTryOn({ productColor, productName }: VirtualTryOnProps) {
  const videoRef = useRef<HTMLVideoElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [stream, setStream] = useState<MediaStream | null>(null);
  const [capturedImage, setCapturedImage] = useState<string | null>(null);

  const startCamera = async () => {
    try {
      const mediaStream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: 'user' }
      });
      setStream(mediaStream);
      if (videoRef.current) {
        videoRef.current.srcObject = mediaStream;
      }
    } catch (err) {
      console.error('Không thể truy cập camera:', err);
      alert('Vui lòng cấp quyền truy cập camera để thử màu son');
    }
  };

  const stopCamera = () => {
    if (stream) {
      stream.getTracks().forEach(track => track.stop());
      setStream(null);
    }
  };

  const capturePhoto = () => {
    if (videoRef.current && canvasRef.current) {
      const video = videoRef.current;
      const canvas = canvasRef.current;
      canvas.width = video.videoWidth;
      canvas.height = video.videoHeight;
      const ctx = canvas.getContext('2d');
      if (ctx) {
        ctx.drawImage(video, 0, 0);
        setCapturedImage(canvas.toDataURL('image/png'));
      }
      stopCamera();
    }
  };

  return (
    <div className="space-y-4">
      <h3 className="font-semibold">Thử màu son ảo</h3>
      
      {!stream && !capturedImage && (
        <div className="flex gap-2">
          <Button onClick={startCamera} className="flex-1">
            <Camera className="h-4 w-4 mr-2" />
            Dùng camera
          </Button>
          <Button variant="outline" className="flex-1">
            <Upload className="h-4 w-4 mr-2" />
            Tải ảnh lên
          </Button>
        </div>
      )}

      {stream && (
        <div className="relative">
          <video
            ref={videoRef}
            autoPlay
            playsInline
            className="w-full rounded-lg"
          />
          {/* AR Overlay - Simplified version */}
          <div 
            className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-32 h-16 rounded-full opacity-50 pointer-events-none"
            style={{ backgroundColor: productColor, mixBlendMode: 'multiply' }}
          />
          
          <div className="absolute bottom-4 left-0 right-0 flex justify-center gap-4">
            <Button variant="destructive" size="icon" onClick={stopCamera}>
              <X className="h-5 w-5" />
            </Button>
            <Button onClick={capturePhoto}>
              Chụp ảnh
            </Button>
          </div>
        </div>
      )}

      {capturedImage && (
        <div className="relative">
          <img src={capturedImage} alt="Captured" className="w-full rounded-lg" />
          <Button 
            variant="outline" 
            size="sm" 
            className="absolute top-2 right-2"
            onClick={() => setCapturedImage(null)}
          >
            Thử lại
          </Button>
        </div>
      )}

      <canvas ref={canvasRef} className="hidden" />
      
      <p className="text-xs text-muted-foreground">
        💡 Mẹo: Sử dụng ở nơi có ánh sáng tốt để kết quả chính xác nhất
      </p>
    </div>
  );
}
```

**Tích hợp vào PDP:**
```tsx
// Trong products/[slug]/page.tsx, thêm vào section variants
{product.categories?.some(c => c.slug.includes('son')) && (
  <VirtualTryOn 
    productColor="#E91E63" // Lấy từ variant
    productName={product.name}
  />
)}
```

**Áp dụng trên Desktop:** Có thể upload ảnh thay vì camera.

**Lợi ích CRO:**
- Giảm tỷ lệ hoàn trả (người dùng tự tin hơn về màu)
- Tăng engagement time
- Yếu tố viral (share ảnh thử son)

---

## TỔNG KẾT VÀ LỘ TRÌNH THỰC HIỆN

### Ưu tiên实施 (Priority Matrix)

| Tính năng | Tác động UX | Độ phức tạp | Ưu tiên |
|-----------|-------------|-------------|---------|
| **Mobile Bottom Navigation** | ⭐⭐⭐⭐⭐ | Thấp | 🔴 P0 |
| **Fix Hamburger Menu** | ⭐⭐⭐⭐⭐ | Thấp | 🔴 P0 |
| **PLP với Filter/Sort** | ⭐⭐⭐⭐⭐ | Trung bình | 🔴 P0 |
| **Sticky Add to Cart (Mobile)** | ⭐⭐⭐⭐ | Thấp | 🟡 P1 |
| **Slide-in Cart** | ⭐⭐⭐⭐ | Trung bình | 🟡 P1 |
| **Trust Badges** | ⭐⭐⭐ | Thấp | 🟡 P1 |
| **Skin Type Quiz** | ⭐⭐⭐⭐ | Cao | 🟢 P2 |
| **Virtual Try-On** | ⭐⭐⭐⭐ | Rất cao | 🟢 P2 |
| **Mega Menu Desktop** | ⭐⭐⭐ | Trung bình | 🟢 P2 |

### Estimated Timeline

- **Tuần 1-2:** Fix critical mobile issues (hamburger menu, bottom nav)
- **Tuần 3-4:** Build PLP với filters + cải tiến Product Card
- **Tuần 5:** Sticky ATC + Slide-in Cart
- **Tuần 6-7:** Skin Quiz + Trust signals
- **Tuần 8+:** Virtual Try-On (R&D)

---

*Báo cáo này tập trung hoàn toàn vào trải nghiệm người dùng cuối (Frontend UI/UX), không bao gồm thay đổi logic backend. Tất cả đề xuất đều khả thi với stack công nghệ hiện tại (Next.js 15 + Tailwind CSS + shadcn/ui).*
