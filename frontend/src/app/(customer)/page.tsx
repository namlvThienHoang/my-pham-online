import { ProductCard } from '@/components/products/ProductCard';
import { useProducts } from '@/hooks/useQueries';

export default function HomePage() {
  const { data: featuredProducts, isLoading } = useProducts({ limit: 8, sort: 'createdAt', order: 'desc' });

  return (
    <div className="space-y-12">
      {/* Hero Section */}
      <section className="relative h-[400px] md:h-[500px] bg-gradient-to-r from-primary/10 to-primary/5 rounded-lg overflow-hidden">
        <div className="absolute inset-0 flex items-center">
          <div className="container">
            <div className="max-w-xl space-y-6">
              <h1 className="text-4xl md:text-6xl font-bold text-foreground">
                Làm đẹp hoàn hảo
              </h1>
              <p className="text-lg text-muted-foreground">
                Khám phá bộ sưu tập mỹ phẩm cao cấp từ các thương hiệu hàng đầu thế giới
              </p>
              <div className="flex gap-4">
                <a
                  href="/products"
                  className="inline-flex items-center justify-center px-6 py-3 bg-primary text-primary-foreground rounded-md font-medium hover:bg-primary/90 transition-colors"
                >
                  Mua ngay
                </a>
                <a
                  href="/promotions"
                  className="inline-flex items-center justify-center px-6 py-3 border border-input bg-background rounded-md font-medium hover:bg-accent transition-colors"
                >
                  Khuyến mãi
                </a>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Featured Products */}
      <section className="container">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-2xl font-bold">Sản phẩm mới</h2>
          <a href="/products" className="text-primary hover:underline text-sm font-medium">
            Xem tất cả →
          </a>
        </div>
        
        {isLoading ? (
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            {[...Array(8)].map((_, i) => (
              <div key={i} className="animate-pulse space-y-4">
                <div className="aspect-square bg-muted rounded-lg" />
                <div className="h-4 bg-muted rounded w-3/4" />
                <div className="h-4 bg-muted rounded w-1/2" />
              </div>
            ))}
          </div>
        ) : (
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            {featuredProducts?.data.map((product) => (
              <ProductCard key={product.id} product={product} />
            ))}
          </div>
        )}
      </section>

      {/* Categories */}
      <section className="container">
        <h2 className="text-2xl font-bold mb-6">Danh mục phổ biến</h2>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          {[
            { name: 'Skincare', image: '/images/skincare.jpg' },
            { name: 'Makeup', image: '/images/makeup.jpg' },
            { name: 'Haircare', image: '/images/haircare.jpg' },
            { name: 'Fragrance', image: '/images/fragrance.jpg' },
          ].map((category) => (
            <a
              key={category.name}
              href={`/categories/${category.name.toLowerCase()}`}
              className="group relative aspect-square rounded-lg overflow-hidden bg-muted"
            >
              <div className="absolute inset-0 flex items-center justify-center">
                <span className="text-xl font-semibold group-hover:text-primary transition-colors">
                  {category.name}
                </span>
              </div>
            </a>
          ))}
        </div>
      </section>

      {/* Brands */}
      <section className="container">
        <h2 className="text-2xl font-bold mb-6">Thương hiệu hàng đầu</h2>
        <div className="grid grid-cols-3 md:grid-cols-6 gap-4">
          {['La Roche-Posay', 'CeraVe', 'The Ordinary', 'Innisfree', 'Clinique', 'Estée Lauder'].map((brand) => (
            <a
              key={brand}
              href={`/brands/${brand.toLowerCase().replace(/\s+/g, '-')}`}
              className="flex items-center justify-center p-4 border rounded-lg hover:border-primary transition-colors"
            >
              <span className="text-sm font-medium text-center">{brand}</span>
            </a>
          ))}
        </div>
      </section>
    </div>
  );
}
