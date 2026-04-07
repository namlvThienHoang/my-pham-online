-- PostgreSQL initialization script for Beauty Commerce
-- Creates extensions, enums, functions, and triggers

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_partman";

-- Create ENUM types
DO $$ BEGIN
    CREATE TYPE order_status AS ENUM (
        'Pending', 'AwaitingPayment', 'PaymentPending', 'Paid', 'Confirmed',
        'Processing', 'PartiallyShipped', 'Shipped', 'OutForDelivery', 'Delivered',
        'Completed', 'Cancelled', 'Refunding', 'Refunded', 'ReturnRequested',
        'ReturnApproved', 'ReturnRejected'
    );
EXCEPTION
    WHEN duplicate_object THEN null;
END $$;

DO $$ BEGIN
    CREATE TYPE payment_status AS ENUM (
        'Pending', 'Authorized', 'Captured', 'Failed', 'Refunded', 'PartiallyRefunded', 'Cancelled'
    );
EXCEPTION
    WHEN duplicate_object THEN null;
END $$;

DO $$ BEGIN
    CREATE TYPE payment_method AS ENUM (
        'CreditCard', 'DebitCard', 'BankTransfer', 'COD', 'Wallet', 'GiftCard', 'StoreCredit'
    );
EXCEPTION
    WHEN duplicate_object THEN null;
END $$;

DO $$ BEGIN
    CREATE TYPE inventory_status AS ENUM (
        'Available', 'Reserved', 'Committed', 'Damaged', 'Expired', 'InTransit'
    );
EXCEPTION
    WHEN duplicate_object THEN null;
END $$;

DO $$ BEGIN
    CREATE TYPE shipment_status AS ENUM (
        'Pending', 'PickedUp', 'InTransit', 'OutForDelivery', 'Delivered', 'Failed', 'Returned', 'Lost'
    );
EXCEPTION
    WHEN duplicate_object THEN null;
END $$;

DO $$ BEGIN
    CREATE TYPE user_role AS ENUM (
        'Customer', 'Admin', 'Staff', 'Vendor'
    );
EXCEPTION
    WHEN duplicate_object THEN null;
END $$;

DO $$ BEGIN
    CREATE TYPE saga_status AS ENUM (
        'Pending', 'Running', 'Completed', 'Compensating', 'Compensated', 'Failed'
    );
EXCEPTION
    WHEN duplicate_object THEN null;
END $$;

-- Function to generate UUID v7 (timestamp-based)
CREATE OR REPLACE FUNCTION gen_uuid_v7()
RETURNS UUID AS $$
DECLARE
    timestamp BIGINT;
    uuid_bytes BYTEA;
    random_bytes BYTEA;
BEGIN
    -- Get current timestamp in milliseconds
    timestamp := EXTRACT(EPOCH FROM CLOCK_TIMESTAMP()) * 1000;
    
    -- Generate random bytes for remaining part
    random_bytes := gen_random_bytes(10);
    
    -- Build UUID v7: 6 bytes timestamp + 1 byte version + 9 bytes random
    uuid_bytes := 
        set_byte('\x00000000000000000000000000000000'::bytea, 0, (timestamp >> 40)::integer & 255) ||
        set_byte('\x00000000000000000000000000000000'::bytea, 1, (timestamp >> 32)::integer & 255) ||
        set_byte('\x00000000000000000000000000000000'::bytea, 2, (timestamp >> 24)::integer & 255) ||
        set_byte('\x00000000000000000000000000000000'::bytea, 3, (timestamp >> 16)::integer & 255) ||
        set_byte('\x00000000000000000000000000000000'::bytea, 4, (timestamp >> 8)::integer & 255) ||
        set_byte('\x00000000000000000000000000000000'::bytea, 5, timestamp::integer & 255) ||
        set_byte('\x00000000000000000000000000000000'::bytea, 6, 7) || -- Version 7
        substring(random_bytes from 1 for 9);
    
    RETURN encode(uuid_bytes, 'hex')::uuid;
END;
$$ LANGUAGE plpgsql;

-- Simpler UUID v7 function using built-in functions
CREATE OR REPLACE FUNCTION gen_uuid_v7_simple()
RETURNS UUID AS $$
DECLARE
    result UUID;
BEGIN
    -- Use gen_random_uuid() and modify the version bits
    result := gen_random_uuid();
    -- Set version to 7 (bits 4-7 of byte 6)
    result := encode(
        set_byte(
            decode(substring(result::text, 1, 14), 'hex'),
            6,
            b'01110000'::bit(8)::integer
        ) ||
        decode(substring(result::text, 15), 'hex'),
        'hex'
    )::uuid;
    RETURN result;
END;
$$ LANGUAGE plpgsql;

-- Trigger function to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create index on deleted_at for soft delete filtering
CREATE INDEX IF NOT EXISTS idx_deleted_at ON users (deleted_at);

-- Grant permissions
GRANT ALL PRIVILEGES ON DATABASE beauty_commerce TO beauty_user;
