'use client';

import { useState, useEffect } from 'react';
import { StarIcon, ThumbUpIcon } from '@heroicons/react/24/outline';
import { StarIcon as StarIconSolid } from '@heroicons/react/24/solid';

interface Review {
  id: string;
  userId: string;
  userName: string;
  rating: number;
  title: string | null;
  content: string | null;
  status: string;
  createdAt: string;
  helpfulCount: number;
  media: Array<{ id: string; mediaUrl: string; mediaType: string }>;
}

interface ProductReviewsProps {
  productId: string;
  averageRating: number;
  totalReviews: number;
}

export default function ProductReviews({ productId, averageRating, totalReviews }: ProductReviewsProps) {
  const [reviews, setReviews] = useState<Review[]>([]);
  const [loading, setLoading] = useState(true);
  const [ratingFilter, setRatingFilter] = useState<number | null>(null);

  useEffect(() => {
    fetchReviews();
  }, [productId, ratingFilter]);

  const fetchReviews = async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams({
        page: '1',
        pageSize: '20',
        sortBy: 'created_at',
        order: 'desc',
      });
      if (ratingFilter) params.append('ratingFilter', ratingFilter.toString());

      const res = await fetch(`/api/reviews/product/${productId}?${params}`);
      const data = await res.json();
      setReviews(data.reviews || []);
    } catch (error) {
      console.error('Failed to fetch reviews:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleHelpfulVote = async (reviewId: string) => {
    try {
      const res = await fetch(`/api/reviews/${reviewId}/helpful`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userId: 'current-user-id' }), // Replace with actual user ID
      });
      if (res.ok) {
        fetchReviews();
      }
    } catch (error) {
      console.error('Failed to vote helpful:', error);
    }
  };

  return (
    <div className="mt-8">
      <h2 className="text-2xl font-bold mb-4">Đánh giá sản phẩm</h2>
      
      {/* Rating Summary */}
      <div className="flex items-center gap-4 mb-6">
        <div className="text-4xl font-bold">{averageRating.toFixed(1)}</div>
        <div className="flex">
          {[1, 2, 3, 4, 5].map((star) => (
            <StarIconSolid
              key={star}
              className={`w-6 h-6 ${
                star <= Math.round(averageRating) ? 'text-yellow-400' : 'text-gray-300'
              }`}
            />
          ))}
        </div>
        <span className="text-gray-600">({totalReviews} đánh giá)</span>
      </div>

      {/* Rating Filter */}
      <div className="flex gap-2 mb-4">
        <button
          onClick={() => setRatingFilter(null)}
          className={`px-3 py-1 rounded ${!ratingFilter ? 'bg-blue-500 text-white' : 'bg-gray-200'}`}
        >
          Tất cả
        </button>
        {[5, 4, 3, 2, 1].map((rating) => (
          <button
            key={rating}
            onClick={() => setRatingFilter(rating)}
            className={`px-3 py-1 rounded ${ratingFilter === rating ? 'bg-blue-500 text-white' : 'bg-gray-200'}`}
          >
            {rating} ★
          </button>
        ))}
      </div>

      {/* Reviews List */}
      {loading ? (
        <div className="text-center py-8">Đang tải...</div>
      ) : reviews.length === 0 ? (
        <div className="text-center py-8 text-gray-500">Chưa có đánh giá nào</div>
      ) : (
        <div className="space-y-4">
          {reviews.map((review) => (
            <div key={review.id} className="border rounded-lg p-4">
              <div className="flex items-center justify-between mb-2">
                <div className="flex items-center gap-2">
                  <span className="font-semibold">{review.userName}</span>
                  <div className="flex">
                    {[1, 2, 3, 4, 5].map((star) => (
                      <StarIconSolid
                        key={star}
                        className={`w-4 h-4 ${
                          star <= review.rating ? 'text-yellow-400' : 'text-gray-300'
                        }`}
                      />
                    ))}
                  </div>
                </div>
                <span className="text-sm text-gray-500">
                  {new Date(review.createdAt).toLocaleDateString('vi-VN')}
                </span>
              </div>
              
              {review.title && <h3 className="font-medium mb-1">{review.title}</h3>}
              <p className="text-gray-700 mb-3">{review.content}</p>
              
              {/* Media */}
              {review.media.length > 0 && (
                <div className="flex gap-2 mb-3 overflow-x-auto">
                  {review.media.map((m) => (
                    m.mediaType === 'image' ? (
                      <img
                        key={m.id}
                        src={m.mediaUrl}
                        alt="Review media"
                        className="w-24 h-24 object-cover rounded cursor-pointer hover:opacity-75"
                      />
                    ) : (
                      <video
                        key={m.id}
                        src={m.mediaUrl}
                        controls
                        className="w-48 h-24 object-cover rounded"
                      />
                    )
                  ))}
                </div>
              )}
              
              {/* Helpful Vote */}
              <button
                onClick={() => handleHelpfulVote(review.id)}
                className="flex items-center gap-1 text-sm text-gray-600 hover:text-blue-500"
              >
                <ThumbUpIcon className="w-4 h-4" />
                Hữu ích ({review.helpfulCount})
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
