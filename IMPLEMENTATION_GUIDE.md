# Phase 3 - Advanced Features Implementation Guide

## Tổng quan

Tài liệu này mô tả việc triển khai các tính năng nâng cao theo spec V3 Phase 3 (12.3) và các module mới trong spec 6.8.

---

## 1. Hệ thống Đánh giá & Xếp hạng (Reviews)

### Database Schema
- **reviews**: Lưu đánh giá của user với soft delete, moderation status
- **review_media**: Ảnh/video đính kèm review
- **review_helpful_votes**: Vote "hữu ích" cho review

### API Endpoints
```
POST   /api/reviews                    # Tạo review (chỉ khi order DELIVERED)
GET    /api/reviews/product/{id}       # Lấy reviews của sản phẩm (pagination)
GET    /api/reviews/{id}               # Chi tiết review
POST   /api/reviews/{id}/helpful       # Vote helpful
POST   /api/reviews/{id}/approve       # Admin duyệt review
POST   /api/reviews/{id}/reject        # Admin từ chối review
DELETE /api/reviews/{id}               # Soft delete review
```

### Key Features
- ✅ Chỉ được review sau khi order DELIVERED (kiểm tra ở DB)
- ✅ Review ẩn (status=pending) cho đến khi admin duyệt
- ✅ Trigger tự động cập nhật `average_rating` và `review_count` trong bảng products
- ✅ Outbox Pattern: Event `ReviewCreated` published khi tạo review
- ✅ Tag-based cache invalidation: `product_reviews:{productId}`

---

## 2. Danh sách Yêu thích & Xem Gần đây

### Database Schema
- **wishlists**: User wishlist với soft delete
- **user_recently_viewed**: Lưu 20 sản phẩm xem gần đây/user (UPSERT)

### API Endpoints
```
GET    /api/wishlist                   # Lấy danh sách yêu thích (pagination)
POST   /api/wishlist                   # Thêm vào wishlist
DELETE /api/wishlist/{productId}       # Xóa khỏi wishlist
POST   /api/wishlist/view              # Record product view
GET    /api/wishlist/recently-viewed   # Lấy recently viewed (cursor pagination)
```

### Key Features
- ✅ UPSERT cho cả wishlist và recently viewed
- ✅ Giới hạn 20 sản phẩm/user cho recently viewed
- ✅ Cursor pagination cho recently viewed
- ✅ Tag-based cache: `wishlist:{userId}`, `recently_viewed:{userId}`

---

## 3. Ví điện tử & Thẻ quà tặng

### Database Schema
- **wallets**: Số dư ví của user
- **wallet_transactions**: Lịch sử giao dịch (earn, spend, refund, expire, admin_adjust, gift_card_load)
- **gift_cards**: Gift card với code_hash (SHA256), balance, expires_at

### API Endpoints
```
GET    /api/wallet/balance             # Xem số dư
GET    /api/wallet/transactions        # Lịch sử giao dịch
POST   /api/gift-cards                 # Tạo gift card mới (mua)
POST   /api/gift-cards/redeem          # Áp dụng gift card (hash lookup, idempotent)
```

### Key Features
- ✅ Gift card code được hash bằng SHA256 trước khi lưu
- ✅ Idempotency key cho POST /gift-cards/redeem
- ✅ Outbox Pattern: Event `GiftCardPurchased`
- ✅ Tự động tạo wallet nếu chưa tồn tại khi redeem

---

## 4. Workflow Trả hàng & Hoàn tiền

### Database Schema
- **return_requests**: Yêu cầu trả hàng (requested → approved → shipped → received → completed)
- **return_items**: Chi tiết sản phẩm trả
- **return_media**: Ảnh chứng minh lỗi

### API Endpoints
```
POST   /api/returns                    # Tạo yêu cầu trả (idempotent)
GET    /api/returns/{id}               # Chi tiết return request
POST   /api/returns/{id}/approve       # Admin duyệt
POST   /api/returns/{id}/ship          # User xác nhận đã gửi
POST   /api/returns/{id}/receive       # Admin xác nhận nhận hàng
POST   /api/returns/{id}/refund        # Xử lý hoàn tiền
```

### Refund Methods
- **original**: Hoàn về phương thức thanh toán ban đầu
- **store_credit**: Cộng vào ví điện tử
- **gift_card**: Cấp gift card mới

### Key Features
- ✅ Idempotency cho POST /returns
- ✅ Outbox Pattern: Events `ReturnRequested`, `RefundProcessed`
- ✅ Tích hợp với wallet_transactions khi refund
- ✅ Upload ảnh chứng minh lỗi

---

## 5. Hệ thống Thông báo (Email/SMS)

### Database Schema
- **notifications**: Hàng đợi thông báo với retry logic

### Circuit Breaker Pattern (Polly)
```csharp
// Email (AWS SES) - 5 failures → 1 minute break
Policy.Handle<Exception>()
      .CircuitBreakerAsync(5, TimeSpan.FromMinutes(1));

// SMS (ESMS) - 3 failures → 2 minutes break + Fallback to Email
Policy.Handle<Exception>()
      .CircuitBreakerAsync(3, TimeSpan.FromMinutes(2));
```

### Retry Policy
- Email fail → Retry 5 lần exponential backoff
- SMS fail → Fallback sang Email
- Sau 5 retries → Dead letter queue

### Notification Channels
- **Email**: AWS SES
- **SMS**: ESMS (Vietnam provider)
- **Push**: (Future implementation)

---

## 6. Frontend Next.js 15 Components

### Trang Sản phẩm (`/products/[id]`)
- `ProductReviews.tsx`: Hiển thị danh sách review, filter theo rating, vote helpful
- `ReviewForm.tsx`: Form gửi review với upload ảnh/video

### Trang Wishlist (`/wishlist`)
- `page.tsx`: Danh sách yêu thích với infinite scroll/pagination

### Trang Ví (`/wallet`)
- `page.tsx`: Số dư, lịch sử giao dịch, form nạp gift card

### Form Trả hàng (`/returns`)
- `ReturnForm.tsx`: Form tạo yêu cầu trả hàng với chọn sản phẩm, lý do, upload ảnh

---

## 7. Security & Best Practices

### Implemented
- ✅ Soft delete cho reviews, wishlists
- ✅ Row versioning cho optimistic concurrency
- ✅ UUID v7 cho primary keys
- ✅ Idempotency keys cho critical operations
- ✅ Hash storage cho gift card codes
- ✅ Transaction wrapping cho multi-table operations
- ✅ Tag-based cache invalidation

### Pending Implementation
- [ ] Security headers middleware
- [ ] MFA for admin users
- [ ] Refresh token family tracking
- [ ] Rate limiting
- [ ] Input validation/sanitization

---

## 8. Folder Structure

```
src/
├── Backend/
│   ├── Modules/
│   │   ├── Reviews/
│   │   │   ├── Entities/
│   │   │   ├── Application/
│   │   │   │   ├── Commands/
│   │   │   │   ├── Queries/
│   │   │   │   └── Handlers/
│   │   │   ├── Infrastructure/
│   │   │   └── API/
│   │   ├── Wishlist/
│   │   ├── Wallet/
│   │   ├── Returns/
│   │   └── Notifications/
│   └── Shared/
│       ├── Events/
│       ├── Helpers/
│       └── Interfaces/
├── Frontend/
│   └── app/
│       ├── products/[id]/
│       ├── wishlist/
│       ├── wallet/
│       └── returns/
└── Database/
    └── Migrations/
```

---

## 9. Testing Recommendations

### Unit Tests
- GiftCardHelper.GenerateGiftCardCode()
- GiftCardHelper.HashGiftCardCode()
- Review creation validation logic

### Integration Tests
- Full return request workflow
- Gift card redemption with idempotency
- Wallet balance updates on refund

### E2E Tests
- Create review → Admin approve → Appears on product page
- Add to wishlist → View wishlist → Remove
- Request return → Admin approve → Refund processed

---

## 10. Deployment Checklist

- [ ] Run database migrations
- [ ] Configure Redis connection
- [ ] Setup AWS SES credentials
- [ ] Setup ESMS API credentials
- [ ] Configure Hangfire for background jobs
- [ ] Enable HTTPS and security headers
- [ ] Setup monitoring/alerting for circuit breakers
- [ ] Configure log aggregation
