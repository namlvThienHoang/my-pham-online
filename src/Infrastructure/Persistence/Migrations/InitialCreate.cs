namespace BeautyEcommerce.Infrastructure.Persistence.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <summary>
/// Initial migration creating all tables from SPEC V3
/// </summary>
[DbContext(typeof(AppDbContext))]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Enable required extensions
        migrationBuilder.Sql(@"
            CREATE EXTENSION IF NOT EXISTS ""uuid-ossp"";
            CREATE EXTENSION IF NOT EXISTS ""pg_partman"";
        ");

        // Create UUID v7 generation function
        migrationBuilder.Sql(@"
            CREATE OR REPLACE FUNCTION gen_uuid_v7() RETURNS uuid AS $$
            DECLARE
                timestamp_bytes bytea;
                random_bytes bytea;
                result_bytes bytea;
            BEGIN
                -- Get current timestamp in milliseconds (8 bytes, but we use 6)
                timestamp_bytes := decode(lpad(to_hex(EXTRACT(EPOCH FROM clock_timestamp()) * 1000)::text, 12, '0'), 'hex');
                
                -- Generate 10 random bytes
                random_bytes := gen_random_bytes(10);
                
                -- Construct UUID v7: 6 bytes timestamp + 2 bytes version+variant + 10 bytes random
                result_bytes := substring(timestamp_bytes from 3 for 6) || 
                               E'\\x07' || 
                               substring(random_bytes from 1 for 9);
                
                RETURN encode(result_bytes, 'hex')::uuid;
            END;
            $$ LANGUAGE plpgsql VOLATILE;
        ");

        // Create trigger function for updated_at
        migrationBuilder.Sql(@"
            CREATE OR REPLACE FUNCTION update_updated_at_column()
            RETURNS TRIGGER AS $$
            BEGIN
                NEW.updated_at = NOW();
                RETURN NEW;
            END;
            $$ language 'plpgsql';
        ");

        // Create ENUM types
        migrationBuilder.Sql(@"
            DO $$ BEGIN
                CREATE TYPE order_status AS ENUM ('Pending', 'AwaitingPayment', 'PaymentPending', 'Paid', 'Confirmed', 'Processing', 'PartiallyShipped', 'Shipped', 'OutForDelivery', 'Delivered', 'Completed', 'Cancelled', 'Refunding', 'Refunded', 'ReturnRequested', 'ReturnApproved', 'ReturnRejected');
            EXCEPTION
                WHEN duplicate_object THEN null;
            END $$;

            DO $$ BEGIN
                CREATE TYPE payment_status AS ENUM ('Pending', 'Authorized', 'Captured', 'Failed', 'Refunded', 'PartiallyRefunded', 'Cancelled');
            EXCEPTION
                WHEN duplicate_object THEN null;
            END $$;

            DO $$ BEGIN
                CREATE TYPE payment_method AS ENUM ('CreditCard', 'DebitCard', 'BankTransfer', 'COD', 'Wallet', 'GiftCard', 'StoreCredit');
            EXCEPTION
                WHEN duplicate_object THEN null;
            END $$;

            DO $$ BEGIN
                CREATE TYPE inventory_status AS ENUM ('Available', 'Reserved', 'Committed', 'Damaged', 'Expired', 'InTransit');
            EXCEPTION
                WHEN duplicate_object THEN null;
            END $$;

            DO $$ BEGIN
                CREATE TYPE shipment_status AS ENUM ('Pending', 'PickedUp', 'InTransit', 'OutForDelivery', 'Delivered', 'Failed', 'Returned', 'Lost');
            EXCEPTION
                WHEN duplicate_object THEN null;
            END $$;

            DO $$ BEGIN
                CREATE TYPE user_role AS ENUM ('Customer', 'Admin', 'Staff', 'Vendor');
            EXCEPTION
                WHEN duplicate_object THEN null;
            END $$;

            DO $$ BEGIN
                CREATE TYPE saga_status AS ENUM ('Pending', 'Running', 'Completed', 'Compensating', 'Compensated', 'Failed');
            EXCEPTION
                WHEN duplicate_object THEN null;
            END $$;
        ");

        // Users table
        migrationBuilder.Sql(@"
            CREATE TABLE users (
                id uuid PRIMARY KEY DEFAULT gen_uuid_v7(),
                email varchar(256) NOT NULL,
                password_hash varchar(512) NOT NULL,
                full_name varchar(256) NOT NULL,
                phone_number varchar(32),
                role user_role DEFAULT 'Customer',
                is_email_verified boolean DEFAULT false,
                is_phone_verified boolean DEFAULT false,
                mfa_enabled boolean DEFAULT false,
                mfa_secret varchar(256),
                last_login_at timestamptz,
                avatar_url text,
                created_at timestamptz DEFAULT NOW(),
                updated_at timestamptz DEFAULT NOW(),
                deleted_at timestamptz,
                row_version bigint DEFAULT 0
            );

            CREATE UNIQUE INDEX ix_users_email ON users(email) WHERE deleted_at IS NULL;
            CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON users FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
        ");

        // User addresses table
        migrationBuilder.Sql(@"
            CREATE TABLE user_addresses (
                id uuid PRIMARY KEY DEFAULT gen_uuid_v7(),
                user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                full_name varchar(256) NOT NULL,
                phone_number varchar(32) NOT NULL,
                address_line1 varchar(500) NOT NULL,
                address_line2 varchar(500),
                ward varchar(256) NOT NULL,
                district varchar(256) NOT NULL,
                city varchar(256) NOT NULL,
                country varchar(256) DEFAULT 'VN',
                postal_code varchar(32),
                is_default boolean DEFAULT false,
                type varchar(50) DEFAULT 'Home',
                created_at timestamptz DEFAULT NOW(),
                updated_at timestamptz DEFAULT NOW(),
                deleted_at timestamptz,
                row_version bigint DEFAULT 0
            );

            CREATE INDEX ix_user_addresses_user_id ON user_addresses(user_id);
            CREATE TRIGGER update_user_addresses_updated_at BEFORE UPDATE ON user_addresses FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
        ");

        // Skin profiles table
        migrationBuilder.Sql(@"
            CREATE TABLE skin_profiles (
                id uuid PRIMARY KEY DEFAULT gen_uuid_v7(),
                user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                profile_name varchar(256) NOT NULL,
                gender varchar(50) NOT NULL,
                age integer NOT NULL,
                skin_type varchar(50) NOT NULL,
                has_acne boolean DEFAULT false,
                has_dark_spots boolean DEFAULT false,
                has_wrinkles boolean DEFAULT false,
                is_sensitive boolean DEFAULT false,
                concerns text,
                current_products text,
                created_at timestamptz DEFAULT NOW(),
                updated_at timestamptz DEFAULT NOW(),
                deleted_at timestamptz,
                row_version bigint DEFAULT 0
            );

            CREATE INDEX ix_skin_profiles_user_id ON skin_profiles(user_id);
            CREATE TRIGGER update_skin_profiles_updated_at BEFORE UPDATE ON skin_profiles FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
        ");

        // Categories table
        migrationBuilder.Sql(@"
            CREATE TABLE categories (
                id uuid PRIMARY KEY DEFAULT gen_uuid_v7(),
                name varchar(256) NOT NULL,
                description text,
                slug varchar(256) NOT NULL,
                parent_category_id uuid REFERENCES categories(id),
                image_url text,
                position integer DEFAULT 0,
                is_published boolean DEFAULT true,
                meta_title varchar(500),
                meta_description text,
                created_at timestamptz DEFAULT NOW(),
                updated_at timestamptz DEFAULT NOW(),
                deleted_at timestamptz,
                row_version bigint DEFAULT 0
            );

            CREATE UNIQUE INDEX ix_categories_slug ON categories(slug) WHERE deleted_at IS NULL;
            CREATE INDEX ix_categories_parent ON categories(parent_category_id);
            CREATE TRIGGER update_categories_updated_at BEFORE UPDATE ON categories FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
        ");

        // Brands table
        migrationBuilder.Sql(@"
            CREATE TABLE brands (
                id uuid PRIMARY KEY DEFAULT gen_uuid_v7(),
                name varchar(256) NOT NULL,
                description text,
                slug varchar(256) NOT NULL,
                logo_url text,
                website_url text,
                is_published boolean DEFAULT true,
                position integer DEFAULT 0,
                created_at timestamptz DEFAULT NOW(),
                updated_at timestamptz DEFAULT NOW(),
                deleted_at timestamptz,
                row_version bigint DEFAULT 0
            );

            CREATE UNIQUE INDEX ix_brands_slug ON brands(slug) WHERE deleted_at IS NULL;
            CREATE TRIGGER update_brands_updated_at BEFORE UPDATE ON brands FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
        ");

        // Products table
        migrationBuilder.Sql(@"
            CREATE TABLE products (
                id uuid PRIMARY KEY DEFAULT gen_uuid_v7(),
                sku varchar(100) NOT NULL,
                name varchar(500) NOT NULL,
                description text,
                short_description text,
                category_id uuid NOT NULL REFERENCES categories(id),
                brand_id uuid NOT NULL REFERENCES brands(id),
                price numeric(18,2) NOT NULL,
                compare_at_price numeric(18,2),
                cost numeric(18,2) NOT NULL,
                stock_quantity integer DEFAULT 0,
                low_stock_threshold integer DEFAULT 10,
                track_inventory boolean DEFAULT true,
                allow_backorder boolean DEFAULT false,
                is_published boolean DEFAULT false,
                is_featured boolean DEFAULT false,
                is_virtual boolean DEFAULT false,
                weight numeric(18,4) DEFAULT 0,
                length numeric(18,4),
                width numeric(18,4),
                height numeric(18,4),
                meta_title varchar(500),
                meta_description text,
                meta_keywords text,
                slug varchar(500) NOT NULL,
                view_count integer DEFAULT 0,
                sales_count integer DEFAULT 0,
                rating_average numeric(3,2) DEFAULT 0,
                rating_count integer DEFAULT 0,
                published_at timestamptz,
                created_at timestamptz DEFAULT NOW(),
                updated_at timestamptz DEFAULT NOW(),
                deleted_at timestamptz,
                row_version bigint DEFAULT 0
            );

            CREATE UNIQUE INDEX ix_products_sku ON products(sku) WHERE deleted_at IS NULL;
            CREATE UNIQUE INDEX ix_products_slug ON products(slug) WHERE deleted_at IS NULL;
            CREATE INDEX ix_products_category_id ON products(category_id);
            CREATE INDEX ix_products_brand_id ON products(brand_id);
            CREATE INDEX ix_products_published ON products(is_published) WHERE is_published = true AND deleted_at IS NULL;
            CREATE TRIGGER update_products_updated_at BEFORE UPDATE ON products FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
        ");

        // Continue with more tables in next part...
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS update_products_updated_at ON products");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS update_brands_updated_at ON brands");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS update_categories_updated_at ON categories");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS update_skin_profiles_updated_at ON skin_profiles");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS update_user_addresses_updated_at ON user_addresses");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS update_users_updated_at ON users");
        
        migrationBuilder.DropTable(name: "products", schema: "public");
        migrationBuilder.DropTable(name: "brands", schema: "public");
        migrationBuilder.DropTable(name: "categories", schema: "public");
        migrationBuilder.DropTable(name: "skin_profiles", schema: "public");
        migrationBuilder.DropTable(name: "user_addresses", schema: "public");
        migrationBuilder.DropTable(name: "users", schema: "public");
        
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS gen_uuid_v7()");
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS update_updated_at_column()");
    }
}
