'use client';

import { useState, useEffect } from 'react';
import { HeartIcon as HeartOutline } from '@heroicons/react/24/outline';
import { HeartIcon as HeartSolid } from '@heroicons/react/24/solid';
import Link from 'next/link';

interface WishlistItem {
  id: string;
  productId: string;
  productName: string;
  productImage: string | null;
  price: number | null;
  variantId: string | null;
  variantName: string | null;
  addedAt: string;
}

export default function WishlistPage() {
  const [items, setItems] = useState<WishlistItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(false);

  useEffect(() => {
    fetchWishlist();
  }, [page]);

  const fetchWishlist = async () => {
    setLoading(true);
    try {
      // Get user ID from session/auth context
      const userId = 'current-user-id'; // Replace with actual user ID
      
      const res = await fetch(`/api/wishlist?userId=${userId}&page=${page}&pageSize=20`);
      const data = await res.json();
      
      if (page === 1) {
        setItems(data.items || []);
      } else {
        setItems((prev) => [...prev, ...(data.items || [])]);
      }
      
      setHasMore(data.hasMore);
    } catch (error) {
      console.error('Failed to fetch wishlist:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleRemove = async (productId: string) => {
    try {
      const userId = 'current-user-id';
      const res = await fetch(`/api/wishlist/${productId}?userId=${userId}`, {
        method: 'DELETE',
      });
      
      if (res.ok) {
        setItems(items.filter((item) => item.productId !== productId));
      }
    } catch (error) {
      console.error('Failed to remove from wishlist:', error);
    }
  };

  const loadMore = () => {
    setPage(page + 1);
  };

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-3xl font-bold mb-6">Danh sách yêu thích</h1>

      {items.length === 0 && !loading ? (
        <div className="text-center py-12">
          <HeartOutline className="w-24 h-24 mx-auto text-gray-300 mb-4" />
          <p className="text-gray-500 text-lg">Chưa có sản phẩm nào trong danh sách yêu thích</p>
          <Link href="/products" className="mt-4 inline-block text-blue-500 hover:underline">
            Khám phá sản phẩm
          </Link>
        </div>
      ) : (
        <>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            {items.map((item) => (
              <div
                key={item.id}
                className="border rounded-lg overflow-hidden hover:shadow-lg transition-shadow"
              >
                <Link href={`/products/${item.productId}`}>
                  <div className="aspect-square bg-gray-100">
                    {item.productImage ? (
                      <img
                        src={item.productImage}
                        alt={item.productName}
                        className="w-full h-full object-cover"
                      />
                    ) : (
                      <div className="flex items-center justify-center h-full text-gray-400">
                        No Image
                      </div>
                    )}
                  </div>
                </Link>
                
                <div className="p-4">
                  <Link href={`/products/${item.productId}`}>
                    <h3 className="font-medium line-clamp-2 mb-2 hover:text-blue-500">
                      {item.productName}
                    </h3>
                  </Link>
                  
                  {item.variantName && (
                    <p className="text-sm text-gray-500 mb-2">{item.variantName}</p>
                  )}
                  
                  <div className="flex items-center justify-between">
                    <span className="text-lg font-bold text-red-500">
                      {item.price ? `${item.price.toLocaleString('vi-VN')}₫` : 'Liên hệ'}
                    </span>
                    
                    <button
                      onClick={() => handleRemove(item.productId)}
                      className="text-red-500 hover:text-red-700"
                      title="Xóa khỏi danh sách yêu thích"
                    >
                      <HeartSolid className="w-6 h-6" />
                    </button>
                  </div>
                  
                  <p className="text-xs text-gray-400 mt-2">
                    Thêm ngày: {new Date(item.addedAt).toLocaleDateString('vi-VN')}
                  </p>
                </div>
              </div>
            ))}
          </div>

          {hasMore && (
            <div className="text-center mt-8">
              <button
                onClick={loadMore}
                disabled={loading}
                className="px-6 py-2 bg-blue-500 text-white rounded hover:bg-blue-600 disabled:opacity-50"
              >
                {loading ? 'Đang tải...' : 'Xem thêm'}
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
