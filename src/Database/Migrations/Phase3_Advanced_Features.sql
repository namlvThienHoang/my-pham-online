-- Migration: Phase3_Advanced_Features.sql
-- Yêu cầu: PostgreSQL 15+, Extension "uuid-ossp" hoặc sử dụng gen_random_uuid()

-- 1. HỆ THỐNG ĐÁNH GIÁ & XẾP HẠNG (REVIEWS)
CREATE TABLE reviews (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id),
    product_id UUID NOT NULL REFERENCES products(id),
    order_item_id UUID NOT NULL REFERENCES order_items(id), -- Đảm bảo chỉ review khi mua
    rating INT NOT NULL CHECK (rating BETWEEN 1 AND 5),
    title VARCHAR(255),
    content TEXT,
    status VARCHAR(50) DEFAULT 'pending', -- pending, approved, rejected
    is_deleted BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    row_version INT DEFAULT 0,
    CONSTRAINT unique_user_product_review UNIQUE (user_id, product_id, order_item_id)
);

CREATE INDEX idx_reviews_product_status ON reviews(product_id, status) WHERE is_deleted = FALSE;

CREATE TABLE review_media (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    review_id UUID NOT NULL REFERENCES reviews(id) ON DELETE CASCADE,
    media_url TEXT NOT NULL,
    media_type VARCHAR(50) NOT NULL, -- image, video
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE review_helpful_votes (
    review_id UUID NOT NULL REFERENCES reviews(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id),
    voted_at TIMESTAMPTZ DEFAULT NOW(),
    PRIMARY KEY (review_id, user_id)
);

-- Trigger cập nhật average_rating vào bảng products (Cách 1: Trigger)
-- Cách 2 (Khuyến khích): Dùng Outbox Event ReviewCreated -> Update Product via Saga/Handler
CREATE OR REPLACE FUNCTION update_product_rating() RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' AND NEW.status = 'approved' THEN
        UPDATE products 
        SET average_rating = (SELECT AVG(rating) FROM reviews WHERE product_id = NEW.product_id AND status = 'approved'),
            review_count = (SELECT COUNT(*) FROM reviews WHERE product_id = NEW.product_id AND status = 'approved')
        WHERE id = NEW.product_id;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_update_product_rating
AFTER INSERT OR UPDATE OF status ON reviews
FOR EACH ROW EXECUTE FUNCTION update_product_rating();


-- 2. DANH SÁCH YÊU THÍCH & XEM GẦN ĐÂY
CREATE TABLE wishlists (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    product_id UUID NOT NULL REFERENCES products(id),
    variant_id UUID REFERENCES product_variants(id),
    added_at TIMESTAMPTZ DEFAULT NOW(),
    is_deleted BOOLEAN DEFAULT FALSE,
    CONSTRAINT unique_wishlist_item UNIQUE (user_id, product_id, variant_id)
);
CREATE INDEX idx_wishlists_user ON wishlists(user_id) WHERE is_deleted = FALSE;

CREATE TABLE user_recently_viewed (
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    product_id UUID NOT NULL REFERENCES products(id),
    viewed_at TIMESTAMPTZ DEFAULT NOW(),
    PRIMARY KEY (user_id, product_id)
);
-- Index để lấy danh sách mới nhất nhanh
CREATE INDEX idx_recently_viewed_user_time ON user_recently_viewed(user_id, viewed_at DESC);


-- 3. VÍ ĐIỆN TỬ & THẺ QUÀ TẶNG
CREATE TYPE wallet_transaction_type AS ENUM ('earn', 'spend', 'refund', 'expire', 'admin_adjust', 'gift_card_load');

CREATE TABLE wallets (
    user_id UUID PRIMARY KEY REFERENCES users(id),
    balance DECIMAL(18, 2) DEFAULT 0.00,
    currency VARCHAR(3) DEFAULT 'VND',
    row_version INT DEFAULT 0
);

CREATE TABLE wallet_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    wallet_id UUID NOT NULL REFERENCES wallets(user_id),
    type wallet_transaction_type NOT NULL,
    amount DECIMAL(18, 2) NOT NULL,
    balance_after DECIMAL(18, 2) NOT NULL,
    reference_id UUID, -- OrderID, ReturnID, GiftCardID
    description TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX idx_wallet_trans_wallet ON wallet_transactions(wallet_id, created_at DESC);

CREATE TABLE gift_cards (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code_hash VARCHAR(64) NOT NULL UNIQUE, -- Hash của code (SHA256)
    original_code VARCHAR(20) NOT NULL, -- Chỉ lưu tạm lúc tạo, sau đó xóa hoặc mã hóa riêng
    initial_balance DECIMAL(18, 2) NOT NULL,
    current_balance DECIMAL(18, 2) NOT NULL,
    expires_at TIMESTAMPTZ,
    status VARCHAR(50) DEFAULT 'active', -- active, used, expired, cancelled
    purchased_by_user_id UUID REFERENCES users(id),
    created_at TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX idx_gift_cards_hash ON gift_cards(code_hash);


-- 4. WORKFLOW TRẢ HÀNG & HOÀN TIỀN
CREATE TYPE return_status AS ENUM ('requested', 'approved', 'rejected', 'shipped', 'received', 'completed', 'cancelled');

CREATE TABLE return_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id),
    order_id UUID NOT NULL REFERENCES orders(id),
    status return_status DEFAULT 'requested',
    refund_method VARCHAR(50), -- original, store_credit, gift_card
    total_refund_amount DECIMAL(18, 2),
    admin_note TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE return_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    return_request_id UUID NOT NULL REFERENCES return_requests(id) ON DELETE CASCADE,
    order_item_id UUID NOT NULL REFERENCES order_items(id),
    quantity INT NOT NULL,
    reason TEXT,
    refund_amount DECIMAL(18, 2) NOT NULL
);

CREATE TABLE return_media (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    return_request_id UUID NOT NULL REFERENCES return_requests(id) ON DELETE CASCADE,
    media_url TEXT NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW()
);


-- 5. HỆ THỐNG THÔNG BÁO
CREATE TYPE notification_channel AS ENUM ('email', 'sms', 'push');
CREATE TYPE notification_status AS ENUM ('pending', 'sent', 'failed', 'dead_letter');

CREATE TABLE notifications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id),
    type VARCHAR(100) NOT NULL,
    channel notification_channel NOT NULL,
    recipient VARCHAR(255) NOT NULL, -- Email or Phone
    subject VARCHAR(255),
    content TEXT NOT NULL,
    status notification_status DEFAULT 'pending',
    retry_count INT DEFAULT 0,
    last_error TEXT,
    sent_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX idx_notifications_pending ON notifications(status, created_at) WHERE status = 'pending';

-- OUTBOX TABLE (Chung cho các module)
CREATE TABLE outbox_messages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    aggregate_type VARCHAR(100) NOT NULL,
    aggregate_id UUID NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    payload JSONB NOT NULL,
    occurred_on TIMESTAMPTZ DEFAULT NOW(),
    processed_at TIMESTAMPTZ,
    error_message TEXT
);
CREATE INDEX idx_outbox_unprocessed ON outbox_messages(processed_at) WHERE processed_at IS NULL;
