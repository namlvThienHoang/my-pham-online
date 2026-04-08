using System;
using System.Security.Cryptography;
using System.Text;

namespace ECommerce.Shared.Helpers;

public static class GiftCardHelper
{
    /// <summary>
    /// Tạo code gift card ngẫu nhiên (16 ký tự, format: XXXX-XXXX-XXXX-XXXX)
    /// </summary>
    public static string GenerateGiftCardCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Loại bỏ ký tự dễ nhầm (I, 1, O, 0)
        var result = new StringBuilder();
        
        using var rng = RandomNumberGenerator.Create();
        var data = new byte[16];
        rng.GetBytes(data);
        
        for (int i = 0; i < 16; i++)
        {
            if (i > 0 && i % 4 == 0)
                result.Append('-');
            
            result.Append(chars[data[i] % chars.Length]);
        }
        
        return result.ToString();
    }
    
    /// <summary>
    /// Hash code gift card để lưu trữ an toàn (SHA256)
    /// </summary>
    public static string HashGiftCardCode(string code)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(code.ToUpperInvariant());
        var hash = sha256.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
    
    /// <summary>
    /// Validate format gift card code
    /// </summary>
    public static bool IsValidGiftCardCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;
            
        var pattern = @"^[A-HJ-NP-Z2-9]{4}-[A-HJ-NP-Z2-9]{4}-[A-HJ-NP-Z2-9]{4}-[A-HJ-NP-Z2-9]{4}$";
        return System.Text.RegularExpressions.Regex.IsMatch(code.ToUpperInvariant(), pattern);
    }
}
