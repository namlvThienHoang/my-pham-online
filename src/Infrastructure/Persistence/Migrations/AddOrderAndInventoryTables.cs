namespace BeautyEcommerce.Infrastructure.Persistence.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <summary>
/// Migration part 2: Orders, Inventory, Outbox, Saga tables
/// </summary>
[DbContext(typeof(AppDbContext))]
public partial class AddOrderAndInventoryTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Orders table
        migrationBuilder.Sql(@"
            CREATE TABLE orders (
                id uuid PRIMARY KEY DEFAULT gen_uuid_v7(),
                order_number varchar(50) NOT NULL,
                user_id uuid NOT NULL REFERENCES users(id),
                status order_status DEFAULT 'Pending',
                subtotal numeric(18,2) NOT NULL,
                discount_amount numeric(18,2) DEFAULT 0,
                tax_amount numeric(18,2) DEFAULT 0,
                shipping_amount numeric(18,2) DEFAULT 0,
                total numeric(18,2) NOT NULL,
                currency_code varchar(3) DEFAULT 'VND',
                payment_method payment_method,
                payment_status payment_status DEFAULT 'Pending',
                payment_intent_id varchar(256),
                transaction_id varchar(256),
                paid_at timestamptz,
                is_cod boolean DEFAULT false,
                cod_confirmed boolean DEFAULT false,
                cod_confirm_attempts integer DEFAULT 0,
                cod_confirmed_at timestamptz,
                customer_email varchar(256) NOT NULL,
                customer_name varchar(256) NOT NULL,
                customer_phone varchar(32) NOT NULL,
                shipping_address_line1 varchar(500) NOT NULL,
                shipping_address_line2 varchar(500),
                shipping_ward varchar(256) NOT NULL,
                shipping_district varchar(256) NOT NULL,
                shipping_city varchar(256) NOT NULL,
                shipping_country varchar(256) DEFAULT 'VN',
                shipping_postal_code varchar(32),
                billing_address_line1 varchar(500),
                billing_address_line2 varchar(500),
                billing_ward varchar(256),
                billing_district varchar(256),
                billing_city varchar(256),
                billing_country varchar(256),
                billing_postal_code varchar(32),
                customer_note text,
                internal_note text,
                shipment_id uuid,
                tracking_number varchar(256),
                carrier_code varchar(100),
                shipped_at timestamptz,
                delivered_at timestamptz,
                cancelled_at timestamptz,
                cancellation_reason text,
                voucher_code varchar(100),
                wallet_amount_used numeric(18,2) DEFAULT 0,
                gift_card_amount_used numeric(18,2) DEFAULT 0,
                gift_card_code varchar(100),
                created_at timestamptz DEFAULT NOW(),
                updated_at timestamptz DEFAULT NOW(),
                deleted_at timestamptz,
                row_version bigint DEFAULT 0
            );

            CREATE UNIQUE INDEX ix_orders_order_number ON orders(order_number) WHERE deleted_at IS NULL;
            CREATE INDEX ix_orders_user_id ON orders(user_id);
            CREATE INDEX ix_orders_status ON orders(status);
            CREATE INDEX ix_orders_created_at ON orders(created_at);
            CREATE INDEX ix_orders_user_created ON orders(user_id, created_at, id);
            CREATE TRIGGER update_orders_updated_at BEFORE UPDATE ON orders FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
        ");

        // Order items table
        migrationBuilder.Sql(@"
            CREATE TABLE order_items (
                id uuid PRIMARY KEY DEFAULT gen_uuid_v7(),
                order_id uuid NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
                product_id uuid NOT NULL REFERENCES products(id),
                variant_id uuid REFERENCES product_variants(id),
                lot_id uuid REFERENCES inventory_lots(id),
                product_name varchar(500) NOT NULL,
                product_sku varchar(100) NOT NULL,
                variant_name varchar(256),
                quantity integer NOT NULL,
                unit_price numeric(18,2) NOT NULL,
                discount_amount numeric(18,2) DEFAULT 0,
                tax_amount numeric(18,2) DEFAULT 0,
                total_price numeric(18,2) NOT NULL,
                fulfilled_quantity integer DEFAULT 0,
                returned_quantity integer DEFAULT 0,
                cancelled_quantity integer DEFAULT 0,
                custom_data text,
                created_at timestamptz DEFAULT NOW(),
                updated_at timestamptz DEFAULT NOW(),
                deleted_at timestamptz,
                row_version bigint DEFAULT 0
            );

            CREATE INDEX ix_order_items_order_id ON order_items(order_id);
            CREATE INDEX ix_order_items_product_id ON order_items(product_id);
            CREATE TRIGGER update_order_items_updated_at BEFORE UPDATE ON order_items FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
        ");

        // Inventory lots table
        migrationBuilder.Sql(@"
            CREATE TABLE inventory_lots (
                id uuid PRIMARY KEY DEFAULT gen_uuid_v7(),
                product_id uuid NOT NULL REFERENCES products(id) ON DELETE CASCADE,
                variant_id uuid REFERENCES product_variants(id),
                lot_number varchar(100) NOT NULL,
                quantity integer NOT NULL,
                available_quantity integer NOT NULL,
                reserved_quantity integer DEFAULT 0,
                manufacture_date timestamptz NOT NULL,
                expiry_date timestamptz NOT NULL,
                unit_cost numeric(18,2) NOT NULL,
                supplier_name varchar(256),
                status inventory_status DEFAULT 'Available',
                created_at timestamptz DEFAULT NOW(),
                updated_at timestamptz DEFAULT NOW(),
                deleted_at timestamptz,
                row_version bigint DEFAULT 0
            );

            CREATE INDEX ix_inventory_lots_product_variant ON inventory_lots(product_id, variant_id);
            CREATE INDEX ix_inventory_lots_expiry ON inventory_lots(expiry_date);
            CREATE INDEX ix_inventory_lots_available ON inventory_lots(product_id, variant_id, available_quantity) 
                WHERE available_quantity > 0 AND status = 'Available' AND deleted_at IS NULL;
            CREATE TRIGGER update_inventory_lots_updated_at BEFORE UPDATE ON inventory_lots FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
        ");

        // Stock movements table
        migrationBuilder.Sql(@"
            CREATE TABLE stock_movements (
                id uuid PRIMARY KEY DEFAULT gen_uuid_v7(),
                product_id uuid NOT NULL REFERENCES products(id) ON DELETE CASCADE,
                variant_id uuid REFERENCES product_variants(id),
                lot_id uuid REFERENCES inventory_lots(id),
                quantity_change integer NOT NULL,
                quantity_before integer NOT NULL,
                quantity_after integer NOT NULL,
                type varchar(50) NOT NULL,
                reference_type varchar(100),
                reference_id uuid,
                reason text,
                performed_by uuid NOT NULL,
                created_at timestamptz DEFAULT NOW(),
                updated_at timestamptz DEFAULT NOW(),
                deleted_at timestamptz,
                row_version bigint DEFAULT 0
            );

            CREATE INDEX ix_stock_movements_product_id ON stock_movements(product_id);
            CREATE INDEX ix_stock_movements_reference ON stock_movements(reference_id);
            CREATE INDEX ix_stock_movements_created_at ON stock_movements(created_at);
            CREATE TRIGGER update_stock_movements_updated_at BEFORE UPDATE ON stock_movements FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
        ");

        // Outbox messages table
        migrationBuilder.Sql(@"
            CREATE TABLE outbox_messages (
                id uuid PRIMARY KEY DEFAULT gen_uuid_v7(),
                type varchar(256) NOT NULL,
                content text NOT NULL,
                processed_at timestamptz,
                error_at timestamptz,
                error varchar(4000),
                retry_count integer DEFAULT 0,
                worker_id uuid,
                lease_expires_at timestamptz,
                is_dead_letter boolean DEFAULT false,
                created_at timestamptz DEFAULT NOW(),
                updated_at timestamptz DEFAULT NOW(),
                deleted_at timestamptz,
                row_version bigint DEFAULT 0
            );

            CREATE INDEX ix_outbox_messages_unprocessed ON outbox_messages(processed_at, is_dead_letter) 
                WHERE processed_at IS NULL AND is_dead_letter = false;
            CREATE INDEX ix_outbox_messages_lease ON outbox_messages(lease_expires_at);
            CREATE TRIGGER update_outbox_messages_updated_at BEFORE UPDATE ON outbox_messages FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
        ");

        // Order saga state table
        migrationBuilder.Sql(@"
            CREATE TABLE order_saga_state (
                id uuid PRIMARY KEY DEFAULT gen_uuid_v7(),
                order_id uuid NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
                status saga_status DEFAULT 'Pending',
                current_step varchar(100) NOT NULL,
                step_order integer DEFAULT 0,
                last_error varchar(4000),
                completed_at timestamptz,
                compensated_at timestamptz,
                created_at timestamptz DEFAULT NOW(),
                updated_at timestamptz DEFAULT NOW(),
                deleted_at timestamptz,
                row_version bigint DEFAULT 0
            );

            CREATE UNIQUE INDEX ix_order_saga_state_order_id ON order_saga_state(order_id);
            CREATE TRIGGER update_order_saga_state_updated_at BEFORE UPDATE ON order_saga_state FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
        ");

        // Saga compensation log table
        migrationBuilder.Sql(@"
            CREATE TABLE saga_compensation_log (
                id uuid PRIMARY KEY DEFAULT gen_uuid_v7(),
                saga_state_id uuid NOT NULL REFERENCES order_saga_state(id) ON DELETE CASCADE,
                step_name varchar(100) NOT NULL,
                action varchar(100) NOT NULL,
                request_data text,
                response_data text,
                success boolean DEFAULT false,
                error varchar(4000),
                executed_at timestamptz DEFAULT NOW(),
                created_at timestamptz DEFAULT NOW(),
                updated_at timestamptz DEFAULT NOW(),
                deleted_at timestamptz,
                row_version bigint DEFAULT 0
            );

            CREATE INDEX ix_saga_compensation_log_saga_step ON saga_compensation_log(saga_state_id, step_name);
            CREATE TRIGGER update_saga_compensation_log_updated_at BEFORE UPDATE ON saga_compensation_log FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
        ");

        // Audit logs table
        migrationBuilder.Sql(@"
            CREATE TABLE audit_logs (
                id uuid PRIMARY KEY DEFAULT gen_uuid_v7(),
                entity_type varchar(256) NOT NULL,
                entity_id uuid NOT NULL,
                action varchar(50) NOT NULL,
                old_values text,
                new_values text,
                performed_by uuid NOT NULL,
                ip_address varchar(45),
                user_agent varchar(1000),
                created_at timestamptz DEFAULT NOW(),
                updated_at timestamptz DEFAULT NOW(),
                deleted_at timestamptz,
                row_version bigint DEFAULT 0
            );

            CREATE INDEX ix_audit_logs_entity ON audit_logs(entity_id);
            CREATE INDEX ix_audit_logs_entity_type ON audit_logs(entity_type);
            CREATE INDEX ix_audit_logs_created_at ON audit_logs(created_at);
            CREATE INDEX ix_audit_logs_performed_by ON audit_logs(performed_by);
            CREATE TRIGGER update_audit_logs_updated_at BEFORE UPDATE ON audit_logs FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS update_audit_logs_updated_at ON audit_logs");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS update_saga_compensation_log_updated_at ON saga_compensation_log");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS update_order_saga_state_updated_at ON order_saga_state");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS update_outbox_messages_updated_at ON outbox_messages");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS update_stock_movements_updated_at ON stock_movements");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS update_inventory_lots_updated_at ON inventory_lots");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS update_order_items_updated_at ON order_items");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS update_orders_updated_at ON orders");

        migrationBuilder.DropTable(name: "audit_logs", schema: "public");
        migrationBuilder.DropTable(name: "saga_compensation_log", schema: "public");
        migrationBuilder.DropTable(name: "order_saga_state", schema: "public");
        migrationBuilder.DropTable(name: "outbox_messages", schema: "public");
        migrationBuilder.DropTable(name: "stock_movements", schema: "public");
        migrationBuilder.DropTable(name: "inventory_lots", schema: "public");
        migrationBuilder.DropTable(name: "order_items", schema: "public");
        migrationBuilder.DropTable(name: "orders", schema: "public");
    }
}
