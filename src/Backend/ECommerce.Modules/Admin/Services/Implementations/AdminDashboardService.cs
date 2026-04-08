using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Core.Entities;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Modules.Admin.DTOs;
using ECommerce.Modules.Admin.Services;

namespace ECommerce.Modules.Admin.Services.Implementations;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly ApplicationDbContext _context;
    
    public AdminDashboardService(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<DashboardStatsDto> GetDashboardStatsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);
        
        // Total revenue (from completed orders)
        var totalRevenue = await _context.Orders
            .Where(o => o.Status == "Delivered" && !o.IsDeleted)
            .SumAsync(o => o.TotalAmount, cancellationToken);
        
        // Total orders
        var totalOrders = await _context.Orders.CountAsync(o => !o.IsDeleted, cancellationToken);
        
        // Pending orders
        var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending" && !o.IsDeleted, cancellationToken);
        
        // Completed orders
        var completedOrders = await _context.Orders.CountAsync(o => o.Status == "Delivered" && !o.IsDeleted, cancellationToken);
        
        // Revenue growth (compare last 30 days vs previous 30 days)
        var currentPeriodRevenue = await _context.Orders
            .Where(o => o.Status == "Delivered" && o.CreatedAt >= thirtyDaysAgo && !o.IsDeleted)
            .SumAsync(o => o.TotalAmount, cancellationToken);
        
        var previousPeriodRevenue = await _context.Orders
            .Where(o => o.Status == "Delivered" && o.CreatedAt >= thirtyDaysAgo.AddDays(-30) && o.CreatedAt < thirtyDaysAgo && !o.IsDeleted)
            .SumAsync(o => o.TotalAmount, cancellationToken);
        
        var revenueGrowthPercentage = previousPeriodRevenue > 0 
            ? ((currentPeriodRevenue - previousPeriodRevenue) / previousPeriodRevenue) * 100 
            : 0;
        
        // Top products
        var topProducts = await _context.OrderItems
            .Where(oi => !oi.Order.IsDeleted)
            .GroupBy(oi => oi.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                SoldQuantity = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.TotalPrice)
            })
            .OrderByDescending(x => x.SoldQuantity)
            .Take(10)
            .ToListAsync(cancellationToken);
        
        var topProductsDtos = new List<TopProductDto>();
        foreach (var tp in topProducts)
        {
            var product = await _context.Products.FindAsync(new object[] { tp.ProductId }, cancellationToken);
            if (product != null)
            {
                topProductsDtos.Add(new TopProductDto
                {
                    ProductId = tp.ProductId,
                    Name = product.Name,
                    SoldQuantity = tp.SoldQuantity,
                    Revenue = tp.Revenue
                });
            }
        }
        
        // Low stock products
        var lowStockProducts = await _context.Products
            .Where(p => p.StockQuantity <= p.LowStockThreshold && p.IsActive && !p.IsDeleted)
            .Select(p => new LowStockProductDto
            {
                ProductId = p.Id,
                Name = p.Name,
                CurrentStock = p.StockQuantity,
                LowStockThreshold = p.LowStockThreshold
            })
            .OrderBy(p => p.CurrentStock)
            .Take(20)
            .ToListAsync(cancellationToken);
        
        // Expiring inventory (next 30 days)
        var expiringInventory = await _context.InventoryLots
            .Where(l => l.ExpiryDate <= now.AddDays(30) && l.Quantity > 0 && !l.IsDeleted)
            .Select(l => new
            {
                LotId = l.Id,
                ProductId = l.ProductId,
                BatchNumber = l.BatchNumber,
                ExpiryDate = l.ExpiryDate,
                RemainingQuantity = l.Quantity - l.ReservedQuantity
            })
            .OrderBy(l => l.ExpiryDate)
            .Take(20)
            .ToListAsync(cancellationToken);
        
        var expiringInventoryDtos = new List<ExpiringInventoryDto>();
        foreach (var ei in expiringInventory)
        {
            var product = await _context.Products.FindAsync(new object[] { ei.ProductId }, cancellationToken);
            expiringInventoryDtos.Add(new ExpiringInventoryDto
            {
                LotId = ei.LotId,
                ProductName = product?.Name ?? "Unknown",
                BatchNumber = ei.BatchNumber,
                ExpiryDate = ei.ExpiryDate,
                RemainingQuantity = ei.RemainingQuantity
            });
        }
        
        return new DashboardStatsDto
        {
            TotalRevenue = totalRevenue,
            TotalOrders = totalOrders,
            PendingOrders = pendingOrders,
            CompletedOrders = completedOrders,
            RevenueGrowthPercentage = Math.Round(revenueGrowthPercentage, 2),
            TopProducts = topProductsDtos,
            LowStockProducts = lowStockProducts,
            ExpiringInventory = expiringInventoryDtos,
            LastUpdated = now
        };
    }
}

// Authorization Handler for MFA + IP Whitelist
public class MfaRequirement : IAuthorizationRequirement
{
}

public class MfaAuthorizationHandler : AuthorizationHandler<MfaRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationDbContext _context;
    
    public MfaAuthorizationHandler(IHttpContextAccessor httpContextAccessor, ApplicationDbContext context)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
    }
    
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MfaRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return;
        
        var userId = context.User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return;
        
        var adminUserId = Guid.Parse(userId);
        
        // Check MFA
        var adminUser = await _context.AdminUsers.FindAsync(adminUserId);
        if (adminUser == null || !adminUser.IsMfaEnabled || !adminUser.IsActive)
            return;
        
        // Check IP whitelist
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString()
            ?? httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        
        if (!string.IsNullOrEmpty(clientIp) && adminUser.IpWhitelist.Any())
        {
            var isIpAllowed = adminUser.IpWhitelist.Any(ip => 
                ip == clientIp || 
                ip.Contains("*") && IsIpInCidr(clientIp, ip));
            
            if (!isIpAllowed)
                return;
        }
        
        // Check if MFA was recently verified (within 1 hour)
        if (adminUser.LastMfaVerification.HasValue && 
            adminUser.LastMfaVerification.Value.AddHours(1) > DateTime.UtcNow)
        {
            context.Succeed(requirement);
        }
    }
    
    private bool IsIpInCidr(string ip, string cidr)
    {
        // Simplified CIDR check - in production use proper library
        if (!cidr.Contains("/"))
            return ip == cidr;
        
        var parts = cidr.Split('/');
        if (parts.Length != 2)
            return false;
        
        var networkIp = parts[0];
        var prefixLength = int.Parse(parts[1]);
        
        // Convert IPs to uint32 and compare
        var ipBytes = System.Net.IPAddress.Parse(ip).GetAddressBytes();
        var networkBytes = System.Net.IPAddress.Parse(networkIp).GetAddressBytes();
        
        var ipUint = BitConverter.ToUInt32(ipBytes.Reverse().ToArray(), 0);
        var networkUint = BitConverter.ToUInt32(networkBytes.Reverse().ToArray(), 0);
        
        var mask = uint.MaxValue << (32 - prefixLength);
        
        return (ipUint & mask) == (networkUint & mask);
    }
}
