'use client';

import { useState } from 'react';
import { ArrowUpTrayIcon, XMarkIcon } from '@heroicons/react/24/outline';

interface ReturnItem {
  orderItemId: string;
  productName: string;
  quantity: number;
  maxRefundAmount: number;
}

interface OrderItem {
  id: string;
  productName: string;
  quantity: number;
  unitPrice: number;
}

interface ReturnFormProps {
  orderId: string;
  userId: string;
  orderItems: OrderItem[];
  onSuccess: () => void;
  onClose: () => void;
}

export default function ReturnForm({ orderId, userId, orderItems, onSuccess, onClose }: ReturnFormProps) {
  const [selectedItems, setSelectedItems] = useState<Map<string, { quantity: number; reason: string; refundAmount: number }>>(new Map());
  const [refundMethod, setRefundMethod] = useState('original');
  const [mediaFiles, setMediaFiles] = useState<File[]>([]);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleItemSelect = (itemId: string, checked: boolean) => {
    if (checked) {
      const item = orderItems.find((i) => i.id === itemId);
      if (item) {
        setSelectedItems(
          new Map(selectedItems).set(itemId, {
            quantity: 1,
            reason: '',
            refundAmount: item.unitPrice,
          })
        );
      }
    } else {
      const newMap = new Map(selectedItems);
      newMap.delete(itemId);
      setSelectedItems(newMap);
    }
  };

  const handleQuantityChange = (itemId: string, quantity: number) => {
    const itemData = selectedItems.get(itemId);
    if (itemData) {
      const item = orderItems.find((i) => i.id === itemId);
      const maxQty = item?.quantity || 1;
      setSelectedItems(
        new Map(selectedItems).set(itemId, {
          ...itemData,
          quantity: Math.min(Math.max(1, quantity), maxQty),
        })
      );
    }
  };

  const handleReasonChange = (itemId: string, reason: string) => {
    const itemData = selectedItems.get(itemId);
    if (itemData) {
      setSelectedItems(new Map(selectedItems).set(itemId, { ...itemData, reason }));
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (selectedItems.size === 0) {
      setError('Vui lòng chọn ít nhất một sản phẩm');
      return;
    }

    // Validate all items have reason
    for (const [id, data] of selectedItems.entries()) {
      if (!data.reason.trim()) {
        setError(`Vui lòng nhập lý do trả cho tất cả sản phẩm`);
        return;
      }
    }

    setSubmitting(true);
    setError(null);

    try {
      // Upload media if any
      const mediaUrls: string[] = [];
      for (const file of mediaFiles) {
        // TODO: Implement actual file upload
        mediaUrls.push(`https://example.com/uploads/${file.name}`);
      }

      const items = Array.from(selectedItems.entries()).map(([orderItemId, data]) => ({
        orderItemId,
        quantity: data.quantity,
        reason: data.reason,
        refundAmount: data.refundAmount,
      }));

      const res = await fetch('/api/returns', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          userId,
          orderId,
          items,
          refundMethod,
          mediaUrls: mediaUrls.length > 0 ? mediaUrls : undefined,
        }),
      });

      const data = await res.json();

      if (!res.ok) {
        throw new Error(data.message || 'Có lỗi xảy ra');
      }

      onSuccess();
      onClose();
    } catch (err: any) {
      setError(err.message);
    } finally {
      setSubmitting(false);
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) {
      setMediaFiles(Array.from(e.target.files).slice(0, 5));
    }
  };

  const totalRefund = Array.from(selectedItems.values()).reduce(
    (sum, item) => sum + item.refundAmount * item.quantity,
    0
  );

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 overflow-y-auto">
      <div className="bg-white rounded-lg p-6 w-full max-w-2xl m-4">
        <div className="flex justify-between items-center mb-4">
          <h2 className="text-xl font-bold">Yêu cầu trả hàng</h2>
          <button onClick={onClose} className="text-gray-500 hover:text-gray-700">
            <XMarkIcon className="w-6 h-6" />
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          {/* Items Selection */}
          <div className="mb-4">
            <h3 className="font-medium mb-2">Chọn sản phẩm cần trả</h3>
            <div className="space-y-2 max-h-60 overflow-y-auto border rounded p-2">
              {orderItems.map((item) => {
                const isSelected = selectedItems.has(item.id);
                const itemData = selectedItems.get(item.id);

                return (
                  <div key={item.id} className={`p-3 rounded ${isSelected ? 'bg-blue-50 border border-blue-200' : 'bg-gray-50'}`}>
                    <label className="flex items-start gap-2 cursor-pointer">
                      <input
                        type="checkbox"
                        checked={isSelected}
                        onChange={(e) => handleItemSelect(item.id, e.target.checked)}
                        className="mt-1"
                      />
                      <div className="flex-1">
                        <div className="font-medium">{item.productName}</div>
                        <div className="text-sm text-gray-500">
                          SL: {item.quantity} × {item.unitPrice.toLocaleString('vi-VN')}₫
                        </div>

                        {isSelected && itemData && (
                          <div className="mt-2 space-y-2">
                            <div className="flex items-center gap-2">
                              <span className="text-sm">Số lượng:</span>
                              <input
                                type="number"
                                min="1"
                                max={item.quantity}
                                value={itemData.quantity}
                                onChange={(e) => handleQuantityChange(item.id, parseInt(e.target.value) || 1)}
                                className="w-16 border rounded px-2 py-1 text-sm"
                              />
                            </div>
                            <textarea
                              placeholder="Lý do trả hàng *"
                              value={itemData.reason}
                              onChange={(e) => handleReasonChange(item.id, e.target.value)}
                              className="w-full border rounded px-2 py-1 text-sm"
                              rows={2}
                              required
                            />
                          </div>
                        )}
                      </div>
                    </label>
                  </div>
                );
              })}
            </div>
          </div>

          {/* Refund Method */}
          <div className="mb-4">
            <label className="block font-medium mb-2">Phương thức hoàn tiền</label>
            <select
              value={refundMethod}
              onChange={(e) => setRefundMethod(e.target.value)}
              className="w-full border rounded px-3 py-2"
            >
              <option value="original">Về phương thức thanh toán ban đầu</option>
              <option value="store_credit">Tích vào ví điện tử</option>
              <option value="gift_card">Nhận gift card</option>
            </select>
          </div>

          {/* Media Upload */}
          <div className="mb-4">
            <label className="block font-medium mb-2">Ảnh chứng minh (tùy chọn)</label>
            <input
              type="file"
              accept="image/*"
              multiple
              onChange={handleFileChange}
              className="w-full border rounded px-3 py-2"
            />
            {mediaFiles.length > 0 && (
              <div className="flex gap-2 mt-2 flex-wrap">
                {mediaFiles.map((file, index) => (
                  <div key={index} className="relative">
                    <img
                      src={URL.createObjectURL(file)}
                      alt={file.name}
                      className="w-16 h-16 object-cover rounded"
                    />
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Total Refund */}
          <div className="mb-4 p-3 bg-gray-50 rounded">
            <div className="flex justify-between items-center">
              <span className="font-medium">Tổng tiền hoàn dự kiến:</span>
              <span className="text-xl font-bold text-red-500">
                {totalRefund.toLocaleString('vi-VN')}₫
              </span>
            </div>
            <p className="text-xs text-gray-500 mt-1">
              * Số tiền thực tế có thể thay đổi sau khi kiểm tra sản phẩm
            </p>
          </div>

          {/* Error */}
          {error && <div className="mb-4 text-red-500 text-sm">{error}</div>}

          {/* Submit */}
          <div className="flex gap-2 justify-end">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 border rounded hover:bg-gray-100"
              disabled={submitting}
            >
              Hủy
            </button>
            <button
              type="submit"
              className="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600 disabled:opacity-50"
              disabled={submitting || selectedItems.size === 0}
            >
              {submitting ? 'Đang gửi...' : 'Gửi yêu cầu'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
