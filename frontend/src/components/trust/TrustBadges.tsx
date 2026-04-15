'use client';

import { Shield, Truck, RotateCcw, Headphones, CreditCard, Award } from 'lucide-react';

const badges = [
  {
    icon: Shield,
    title: 'Chính hãng 100%',
    description: 'Cam kết sản phẩm chính hãng',
  },
  {
    icon: Truck,
    title: 'Freeship',
    description: 'Miễn phí vận chuyển đơn từ 500k',
  },
  {
    icon: RotateCcw,
    title: 'Đổi trả dễ dàng',
    description: 'Đổi trả trong 30 ngày',
  },
  {
    icon: Headphones,
    title: 'Hỗ trợ 24/7',
    description: 'Tư vấn tận tình mọi lúc',
  },
  {
    icon: CreditCard,
    title: 'Thanh toán linh hoạt',
    description: 'Nhiều phương thức thanh toán',
  },
  {
    icon: Award,
    title: 'Bảo hành uy tín',
    description: 'Bảo hành theo nhà sản xuất',
  },
];

export function TrustBadges() {
  return (
    <section className="bg-muted/50 py-8">
      <div className="container">
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4">
          {badges.map((badge) => {
            const Icon = badge.icon;
            return (
              <div
                key={badge.title}
                className="flex flex-col items-center text-center p-4 rounded-lg bg-background shadow-sm"
              >
                <Icon className="h-8 w-8 text-primary mb-3" />
                <h3 className="font-semibold text-sm mb-1">{badge.title}</h3>
                <p className="text-xs text-muted-foreground">{badge.description}</p>
              </div>
            );
          })}
        </div>
      </div>
    </section>
  );
}
