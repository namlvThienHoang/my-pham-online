export const environment = {
  production: false,
  apiUrl: process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api',
  wsUrl: process.env.NEXT_PUBLIC_WS_URL || 'http://localhost:5000',
};

export const siteConfig = {
  name: 'BeautyStore',
  description: 'Cửa hàng mỹ phẩm trực tuyến uy tín, chất lượng cao',
  url: process.env.NEXT_PUBLIC_SITE_URL || 'http://localhost:3000',
  ogImage: '/images/og.jpg',
  links: {
    facebook: 'https://facebook.com/beautystore',
    instagram: 'https://instagram.com/beautystore',
  },
};

export const navConfig = {
  mainNav: [
    { title: 'Sản phẩm', href: '/products' },
    { title: 'Danh mục', href: '/categories' },
    { title: 'Thương hiệu', href: '/brands' },
    { title: 'Khuyến mãi', href: '/promotions' },
  ],
  footerNav: [
    { title: 'Về chúng tôi', href: '/about' },
    { title: 'Liên hệ', href: '/contact' },
    { title: 'Điều khoản', href: '/terms' },
    { title: 'Bảo mật', href: '/privacy' },
  ],
};
