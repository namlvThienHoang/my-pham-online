'use client';

import { useState, useEffect } from 'react';
import { BanknotesIcon, ArrowDownTrayIcon, ArrowUpTrayIcon } from '@heroicons/react/24/outline';

interface WalletTransaction {
  id: string;
  type: string;
  amount: number;
  balanceAfter: number;
  description: string | null;
  createdAt: string;
}

export default function WalletPage() {
  const [balance, setBalance] = useState<number>(0);
  const [transactions, setTransactions] = useState<WalletTransaction[]>([]);
  const [loading, setLoading] = useState(true);
  const [giftCardCode, setGiftCardCode] = useState('');
  const [redeeming, setRedeeming] = useState(false);

  useEffect(() => {
    fetchWalletData();
  }, []);

  const fetchWalletData = async () => {
    setLoading(true);
    try {
      const userId = 'current-user-id'; // Replace with actual user ID
      
      // Fetch balance
      const balanceRes = await fetch(`/api/wallet/balance?userId=${userId}`);
      const balanceData = await balanceRes.json();
      setBalance(balanceData.balance || 0);
      
      // Fetch transactions
      const transRes = await fetch(`/api/wallet/transactions?userId=${userId}&page=1&pageSize=20`);
      const transData = await transRes.json();
      setTransactions(transData.transactions || []);
    } catch (error) {
      console.error('Failed to fetch wallet data:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleRedeemGiftCard = async (e: React.FormEvent) => {
    e.preventDefault();
    setRedeeming(true);

    try {
      const res = await fetch('/api/gift-cards/redeem', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          code: giftCardCode.trim(),
          userId: 'current-user-id',
        }),
      });

      const data = await res.json();

      if (!res.ok) {
        throw new Error(data.message || 'Có lỗi xảy ra');
      }

      alert(data.message);
      setGiftCardCode('');
      fetchWalletData();
    } catch (err: any) {
      alert(err.message);
    } finally {
      setRedeeming(false);
    }
  };

  const getTypeLabel = (type: string) => {
    const labels: Record<string, string> = {
      earn: 'Tích lũy',
      spend: 'Chi tiêu',
      refund: 'Hoàn tiền',
      expire: 'Hết hạn',
      admin_adjust: 'Điều chỉnh',
      gift_card_load: 'Nạp từ gift card',
    };
    return labels[type] || type;
  };

  const getTypeColor = (type: string) => {
    const positiveTypes = ['earn', 'refund', 'gift_card_load', 'admin_adjust'];
    return positiveTypes.includes(type) ? 'text-green-600' : 'text-red-600';
  };

  const getTypeIcon = (type: string) => {
    const positiveTypes = ['earn', 'refund', 'gift_card_load', 'admin_adjust'];
    return positiveTypes.includes(type) ? (
      <ArrowDownTrayIcon className="w-5 h-5 text-green-600" />
    ) : (
      <ArrowUpTrayIcon className="w-5 h-5 text-red-600" />
    );
  };

  if (loading) {
    return <div className="text-center py-12">Đang tải...</div>;
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-3xl font-bold mb-6">Ví của tôi</h1>

      {/* Balance Card */}
      <div className="bg-gradient-to-r from-blue-500 to-blue-600 rounded-lg p-6 text-white mb-8">
        <div className="flex items-center gap-3 mb-2">
          <BanknotesIcon className="w-8 h-8" />
          <span className="text-lg">Số dư hiện tại</span>
        </div>
        <div className="text-4xl font-bold">
          {balance.toLocaleString('vi-VN')}₫
        </div>
      </div>

      <div className="grid md:grid-cols-2 gap-6">
        {/* Gift Card Redeem */}
        <div className="border rounded-lg p-6">
          <h2 className="text-xl font-bold mb-4">Nạp tiền từ Gift Card</h2>
          <form onSubmit={handleRedeemGiftCard}>
            <input
              type="text"
              value={giftCardCode}
              onChange={(e) => setGiftCardCode(e.target.value)}
              placeholder="Nhập mã gift card (XXXX-XXXX-XXXX-XXXX)"
              className="w-full border rounded px-3 py-2 mb-4 uppercase tracking-wider"
              maxLength={19}
              required
            />
            <button
              type="submit"
              disabled={redeeming || !giftCardCode}
              className="w-full bg-blue-500 text-white py-2 rounded hover:bg-blue-600 disabled:opacity-50"
            >
              {redeeming ? 'Đang xử lý...' : 'Nạp ngay'}
            </button>
          </form>
        </div>

        {/* Quick Info */}
        <div className="border rounded-lg p-6">
          <h2 className="text-xl font-bold mb-4">Thông tin</h2>
          <ul className="space-y-2 text-gray-700">
            <li>• Số dư ví có thể sử dụng để thanh toán đơn hàng</li>
            <li>• Hoàn tiền từ trả hàng sẽ được cộng vào ví</li>
            <li>• Gift card chỉ có thể sử dụng một lần</li>
            <li>• Kiểm tra kỹ mã gift card trước khi nạp</li>
          </ul>
        </div>
      </div>

      {/* Transaction History */}
      <div className="mt-8">
        <h2 className="text-2xl font-bold mb-4">Lịch sử giao dịch</h2>
        
        {transactions.length === 0 ? (
          <div className="text-center py-8 text-gray-500">Chưa có giao dịch nào</div>
        ) : (
          <div className="border rounded-lg overflow-hidden">
            <table className="w-full">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-4 py-3 text-left text-sm font-medium text-gray-500">Loại</th>
                  <th className="px-4 py-3 text-left text-sm font-medium text-gray-500">Mô tả</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">Số tiền</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">Số dư sau</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">Ngày</th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {transactions.map((tx) => (
                  <tr key={tx.id} className="hover:bg-gray-50">
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-2">
                        {getTypeIcon(tx.type)}
                        <span className="text-sm font-medium">{getTypeLabel(tx.type)}</span>
                      </div>
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-600">{tx.description}</td>
                    <td className={`px-4 py-3 text-right font-medium ${getTypeColor(tx.type)}`}>
                      {tx.type === 'spend' || tx.type === 'expire' ? '-' : '+'}
                      {tx.amount.toLocaleString('vi-VN')}₫
                    </td>
                    <td className="px-4 py-3 text-right text-sm text-gray-600">
                      {tx.balanceAfter.toLocaleString('vi-VN')}₫
                    </td>
                    <td className="px-4 py-3 text-right text-sm text-gray-500">
                      {new Date(tx.createdAt).toLocaleDateString('vi-VN', {
                        day: '2-digit',
                        month: '2-digit',
                        year: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit',
                      })}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
