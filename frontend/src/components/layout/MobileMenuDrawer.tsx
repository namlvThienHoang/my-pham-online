'use client';

import Link from 'next/link';
import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/components/ui/sheet';
import { Button } from '@/components/ui/button';
import { X, User, Package, Heart, Settings, LogOut } from 'lucide-react';

interface MobileMenuDrawerProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

const menuItems = [
  { href: '/products', label: 'Sản phẩm' },
  { href: '/categories', label: 'Danh mục' },
  { href: '/brands', label: 'Thương hiệu' },
  { href: '/promotions', label: 'Khuyến mãi' },
];

const accountItems = [
  { href: '/profile', icon: User, label: 'Tài khoản' },
  { href: '/orders', icon: Package, label: 'Đơn hàng' },
  { href: '/wishlist', icon: Heart, label: 'Yêu thích' },
  { href: '/settings', icon: Settings, label: 'Cài đặt' },
  { href: '/logout', icon: LogOut, label: 'Đăng xuất' },
];

export function MobileMenuDrawer({ open, onOpenChange }: MobileMenuDrawerProps) {
  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="left" className="w-[300px] sm:w-[400px]">
        <SheetHeader className="border-b pb-4 mb-4">
          <SheetTitle className="text-xl font-bold text-primary">
            BeautyStore
          </SheetTitle>
        </SheetHeader>

        <nav className="space-y-6">
          {/* Main Menu */}
          <div className="space-y-2">
            {menuItems.map((item) => (
              <Link
                key={item.href}
                href={item.href}
                onClick={() => onOpenChange(false)}
                className="block py-3 px-4 text-lg font-medium hover:bg-accent rounded-md transition-colors"
              >
                {item.label}
              </Link>
            ))}
          </div>

          {/* Account Section */}
          <div className="space-y-2 pt-4 border-t">
            <p className="px-4 text-sm font-medium text-muted-foreground">
              Tài khoản
            </p>
            {accountItems.map((item) => {
              const Icon = item.icon;
              return (
                <Link
                  key={item.href}
                  href={item.href}
                  onClick={() => onOpenChange(false)}
                  className="flex items-center gap-3 py-3 px-4 hover:bg-accent rounded-md transition-colors"
                >
                  <Icon className="h-5 w-5 text-muted-foreground" />
                  <span className="font-medium">{item.label}</span>
                </Link>
              );
            })}
          </div>

          {/* Contact Button */}
          <div className="pt-4 border-t">
            <Button variant="outline" className="w-full" asChild>
              <Link href="/contact" onClick={() => onOpenChange(false)}>
                Liên hệ hỗ trợ
              </Link>
            </Button>
          </div>
        </nav>
      </SheetContent>
    </Sheet>
  );
}
