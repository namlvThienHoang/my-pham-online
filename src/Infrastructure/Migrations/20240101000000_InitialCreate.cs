using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeautyCommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable required extensions
            migrationBuilder.Sql(@"
                CREATE EXTENSION IF NOT EXISTS ""uuid-ossp"";
                CREATE EXTENSION IF NOT EXISTS ""pgcrypto"";
            ");

            // Create ENUM types
            migrationBuilder.Sql(@"
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

                DO $$ BEGIN
                    CREATE TYPE gender AS ENUM (
                        'Male', 'Female', 'Other', 'PreferNotToSay'
                    );
                EXCEPTION
                    WHEN duplicate_object THEN null;
                END $$;

                DO $$ BEGIN
                    CREATE TYPE skin_type AS ENUM (
                        'Normal', 'Dry', 'Oily', 'Combination', 'Sensitive'
                    );
                EXCEPTION
                    WHEN duplicate_object THEN null;
                END $$;

                DO $$ BEGIN
                    CREATE TYPE voucher_type AS ENUM (
                        'Percentage', 'FixedAmount', 'FreeShipping', 'BuyXGetY'
                    );
                EXCEPTION
                    WHEN duplicate_object THEN null;
                END $$;

                DO $$ BEGIN
                    CREATE TYPE refund_reason AS ENUM (
                        'CustomerRequest', 'DefectiveProduct', 'WrongItem', 'LateDelivery', 'Other'
                    );
                EXCEPTION
                    WHEN duplicate_object THEN null;
                END $$;

                DO $$ BEGIN
                    CREATE TYPE refund_status AS ENUM (
                        'Pending', 'Approved', 'Rejected', 'Processing', 'Completed', 'Failed'
                    );
                EXCEPTION
                    WHEN duplicate_object THEN null;
                END $$;

                DO $$ BEGIN
                    CREATE TYPE invoice_status AS ENUM (
                        'Draft', 'Issued', 'Sent', 'Paid', 'Cancelled'
                    );
                EXCEPTION
                    WHEN duplicate_object THEN null;
                END $$;

                DO $$ BEGIN
                    CREATE TYPE stock_movement_type AS ENUM (
                        'Inbound', 'Outbound', 'Reservation', 'Cancellation', 'Adjustment', 'Damaged', 'Expired', 'Return'
                    );
                EXCEPTION
                    WHEN duplicate_object THEN null;
                END $$;

                DO $$ BEGIN
                    CREATE TYPE wallet_transaction_type AS ENUM (
                        'Credit', 'Debit', 'Refund', 'Adjustment'
                    );
                EXCEPTION
                    WHEN duplicate_object THEN null;
                END $$;

                DO $$ BEGIN
                    CREATE TYPE address_type AS ENUM (
                        'Home', 'Office', 'Other'
                    );
                EXCEPTION
                    WHEN duplicate_object THEN null;
                END $$;
            ");

            // Create function to update updated_at
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION update_updated_at_column()
                RETURNS TRIGGER AS $$
                BEGIN
                    NEW.updated_at = CURRENT_TIMESTAMP;
                    NEW.row_version = OLD.row_version + 1;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
            ");

            // Users table
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Role = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsEmailVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsPhoneVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    MfaEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    MfaSecret = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<ulong>(type: "bigint", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "Email",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_users_deleted_at",
                table: "users",
                column: "DeletedAt");

            // User Addresses table
            migrationBuilder.CreateTable(
                name: "user_addresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AddressLine1 = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AddressLine2 = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Ward = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    District = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false, defaultValue: "VN"),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Type = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<ulong>(type: "bigint", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_addresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_addresses_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_addresses_UserId_IsDefault",
                table: "user_addresses",
                columns: new[] { "UserId", "IsDefault" });

            // Skin Profiles table
            migrationBuilder.CreateTable(
                name: "skin_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Gender = table.Column<int>(type: "integer", nullable: false),
                    Age = table.Column<int>(type: "integer", nullable: false),
                    SkinType = table.Column<int>(type: "integer", nullable: false),
                    HasAcne = table.Column<bool>(type: "boolean", nullable: false),
                    HasDarkSpots = table.Column<bool>(type: "boolean", nullable: false),
                    HasWrinkles = table.Column<bool>(type: "boolean", nullable: false),
                    IsSensitive = table.Column<bool>(type: "boolean", nullable: false),
                    Concerns = table.Column<string>(type: "text", nullable: true),
                    CurrentProducts = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<ulong>(type: "bigint", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skin_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_skin_profiles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_skin_profiles_UserId",
                table: "skin_profiles",
                column: "UserId");

            // Brands table
            migrationBuilder.CreateTable(
                name: "brands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Position = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<ulong>(type: "bigint", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_brands", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_brands_slug",
                table: "brands",
                column: "Slug",
                unique: true,
                filter: "deleted_at IS NULL");

            // Categories table
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ParentCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Position = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    MetaTitle = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    MetaDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<ulong>(type: "bigint", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_categories_categories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_categories_ParentCategoryId",
                table: "categories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_categories_slug",
                table: "categories",
                column: "Slug",
                unique: true,
                filter: "deleted_at IS NULL");

            // Products table
            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Sku = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ShortDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CompareAtPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    StockQuantity = table.Column<int>(type: "integer", nullable: false),
                    LowStockThreshold = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    TrackInventory = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AllowBackorder = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsVirtual = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Weight = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Length = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Width = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Height = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    MetaTitle = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    MetaDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MetaKeywords = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Slug = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ViewCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    SalesCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    RatingAverage = table.Column<decimal>(type: "numeric(3,2)", nullable: false, defaultValue: 0m),
                    RatingCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<ulong>(type: "bigint", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_products_brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_products_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_products_BrandId",
                table: "products",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_products_CategoryId",
                table: "products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_products_Sku",
                table: "products",
                column: "Sku",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_products_Slug",
                table: "products",
                column: "Slug",
                unique: true,
                filter: "deleted_at IS NULL");

            // Continue in next part due to length...
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "user_addresses");
            migrationBuilder.DropTable(name: "skin_profiles");
            migrationBuilder.DropTable(name: "products");
            migrationBuilder.DropTable(name: "categories");
            migrationBuilder.DropTable(name: "brands");
            migrationBuilder.DropTable(name: "users");
            
            migrationBuilder.Sql("DROP TYPE IF EXISTS order_status;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS payment_status;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS payment_method;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS inventory_status;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS shipment_status;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS user_role;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS saga_status;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS gender;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS skin_type;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS voucher_type;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS refund_reason;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS refund_status;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS invoice_status;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS stock_movement_type;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS wallet_transaction_type;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS address_type;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS update_updated_at_column();");
        }
    }
}
