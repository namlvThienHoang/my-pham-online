'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { Home, ShoppingBag, User, Heart, Package } from 'lucide-react';
import { cn } from '@/lib/utils';

export function MobileBottomNav() {
  const pathname = usePathname();

  const navItems = [
    { href: '/', icon: Home, label: 'Trang chủ' },
    { href: '/products', icon: ShoppingBag, label: 'Sản phẩm' },
    { href: '/wishlist', icon: Heart, label: 'Yêu thích' },
    { href: '/orders', icon: Package, label: 'Đơn hàng' },
    { href: '/profile', icon: User, label: 'Tài khoản' },
  ];

  return (
    <nav className="fixed bottom-0 left-0 right-0 bg-background border-t z-50 md:hidden safe-area-pb">
      <div className="grid grid-cols-5 h-16">
        {navItems.map((item) => {
          const isActive = pathname === item.href;
          const Icon = item.icon;

          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                'flex flex-col items-center justify-center gap-1 transition-colors',
                isActive ? 'text-primary' : 'text-muted-foreground hover:text-foreground'
              )}
            >
              <Icon className="h-5 w-5" />
              <span className="text-[10px] font-medium">{item.label}</span>
            </Link>
          );
        })}
      </div>
    </nav>
  );
}
