import Link from 'next/link';
import { Facebook, Instagram, Mail, Phone, MapPin } from 'lucide-react';

export function Footer() {
  return (
    <footer className="border-t bg-background">
      <div className="container py-12">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-8">
          <div className="space-y-4">
            <h3 className="text-lg font-bold text-primary">BeautyStore</h3>
            <p className="text-sm text-muted-foreground">
              Cửa hàng mỹ phẩm trực tuyến uy tín, chất lượng cao. Cam kết sản phẩm chính hãng 100%.
            </p>
            <div className="flex gap-4">
              <Link href="#" className="text-muted-foreground hover:text-primary">
                <Facebook className="h-5 w-5" />
              </Link>
              <Link href="#" className="text-muted-foreground hover:text-primary">
                <Instagram className="h-5 w-5" />
              </Link>
            </div>
          </div>

          <div className="space-y-4">
            <h4 className="font-semibold">Hỗ trợ khách hàng</h4>
            <ul className="space-y-2 text-sm text-muted-foreground">
              <li><Link href="/help/faq" className="hover:text-primary">Câu hỏi thường gặp</Link></li>
              <li><Link href="/help/shipping" className="hover:text-primary">Chính sách giao hàng</Link></li>
              <li><Link href="/help/returns" className="hover:text-primary">Chính sách đổi trả</Link></li>
              <li><Link href="/help/warranty" className="hover:text-primary">Chính sách bảo hành</Link></li>
            </ul>
          </div>

          <div className="space-y-4">
            <h4 className="font-semibold">Về chúng tôi</h4>
            <ul className="space-y-2 text-sm text-muted-foreground">
              <li><Link href="/about" className="hover:text-primary">Giới thiệu</Link></li>
              <li><Link href="/contact" className="hover:text-primary">Liên hệ</Link></li>
              <li><Link href="/terms" className="hover:text-primary">Điều khoản dịch vụ</Link></li>
              <li><Link href="/privacy" className="hover:text-primary">Chính sách bảo mật</Link></li>
            </ul>
          </div>

          <div className="space-y-4">
            <h4 className="font-semibold">Liên hệ</h4>
            <ul className="space-y-3 text-sm text-muted-foreground">
              <li className="flex items-center gap-2">
                <Phone className="h-4 w-4" />
                <span>1900 xxxx</span>
              </li>
              <li className="flex items-center gap-2">
                <Mail className="h-4 w-4" />
                <span>support@beautystore.vn</span>
              </li>
              <li className="flex items-start gap-2">
                <MapPin className="h-4 w-4 mt-0.5" />
                <span>123 Đường ABC, Quận 1, TP. Hồ Chí Minh</span>
              </li>
            </ul>
          </div>
        </div>

        <div className="mt-8 pt-8 border-t text-center text-sm text-muted-foreground">
          <p>&copy; {new Date().getFullYear()} BeautyStore. Tất cả quyền được bảo lưu.</p>
        </div>
      </div>
    </footer>
  );
}
