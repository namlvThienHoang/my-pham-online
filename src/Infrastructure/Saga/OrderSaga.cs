namespace BeautyEcommerce.Infrastructure.Saga;

using Microsoft.EntityFrameworkCore;
using BeautyEcommerce.Infrastructure.Persistence;
using BeautyEcommerce.Domain.Entities;
using BeautyEcommerce.Domain.Interfaces;
using BeautyEcommerce.Domain.Enums;

/// <summary>
/// Saga repository implementation with compensation idempotency support
/// </summary>
public class SagaRepository : ISagaRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<SagaRepository> _logger;

    public SagaRepository(AppDbContext dbContext, ILogger<SagaRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<OrderSagaState?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.OrderSagaStates
            .Include(s => s.CompensationLogs)
            .FirstOrDefaultAsync(s => s.OrderId == orderId, cancellationToken);
    }

    public async Task<OrderSagaState> CreateAsync(OrderSagaState state, CancellationToken cancellationToken = default)
    {
        await _dbContext.OrderSagaStates.AddAsync(state, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return state;
    }

    public async Task UpdateAsync(OrderSagaState state, CancellationToken cancellationToken = default)
    {
        state.UpdatedAt = DateTime.UtcNow;
        state.RowVersion++;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddCompensationLogAsync(SagaCompensationLog log, CancellationToken cancellationToken = default)
    {
        await _dbContext.SagaCompensationLogs.AddAsync(log, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> HasCompensatedAsync(Guid sagaStateId, string stepName, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SagaCompensationLogs
            .AnyAsync(l => l.SagaStateId == sagaStateId && l.StepName == stepName && l.Success, cancellationToken);
    }
}

/// <summary>
/// Order Saga orchestrator implementing the Saga pattern with compensating transactions
/// </summary>
public class OrderSaga
{
    private readonly ISagaRepository _sagaRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IPaymentService _paymentService;
    private readonly IShipmentService _shipmentService;
    private readonly ILogger<OrderSaga> _logger;

    private static readonly List<string> Steps = new()
    {
        "ReserveInventory",
        "ProcessPayment",
        "CreateShipment",
        "ConfirmOrder"
    };

    public OrderSaga(
        ISagaRepository sagaRepository,
        IInventoryService inventoryService,
        IPaymentService paymentService,
        IShipmentService shipmentService,
        ILogger<OrderSaga> logger)
    {
        _sagaRepository = sagaRepository;
        _inventoryService = inventoryService;
        _paymentService = paymentService;
        _shipmentService = shipmentService;
        _logger = logger;
    }

    public async Task<bool> ExecuteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var sagaState = await _sagaRepository.GetByOrderIdAsync(orderId, cancellationToken);
        
        if (sagaState == null)
        {
            sagaState = new OrderSagaState
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                Status = SagaStatus.Pending,
                CurrentStep = Steps[0],
                StepOrder = 0
            };
            await _sagaRepository.CreateAsync(sagaState, cancellationToken);
        }

        if (sagaState.Status == SagaStatus.Completed)
        {
            _logger.LogInformation("Saga for order {OrderId} already completed", orderId);
            return true;
        }

        sagaState.Status = SagaStatus.Running;
        await _sagaRepository.UpdateAsync(sagaState, cancellationToken);

        try
        {
            for (int i = sagaState.StepOrder; i < Steps.Count; i++)
            {
                var stepName = Steps[i];
                sagaState.CurrentStep = stepName;
                sagaState.StepOrder = i;
                await _sagaRepository.UpdateAsync(sagaState, cancellationToken);

                var success = await ExecuteStepAsync(stepName, orderId, sagaState.Id, cancellationToken);
                
                if (!success)
                {
                    throw new SagaException($"Step {stepName} failed for order {orderId}");
                }
            }

            sagaState.Status = SagaStatus.Completed;
            sagaState.CompletedAt = DateTime.UtcNow;
            await _sagaRepository.UpdateAsync(sagaState, cancellationToken);

            _logger.LogInformation("Saga completed successfully for order {OrderId}", orderId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Saga failed for order {OrderId}, starting compensation", orderId);
            await CompensateAsync(sagaState.Id, cancellationToken);
            return false;
        }
    }

    private async Task<bool> ExecuteStepAsync(string stepName, Guid orderId, Guid sagaStateId, CancellationToken cancellationToken)
    {
        // Check if already compensated (idempotency)
        if (await _sagaRepository.HasCompensatedAsync(sagaStateId, stepName, cancellationToken))
        {
            _logger.LogWarning("Step {StepName} already compensated for saga {SagaStateId}, skipping", stepName, sagaStateId);
            return true;
        }

        return stepName switch
        {
            "ReserveInventory" => await ReserveInventoryAsync(orderId, sagaStateId, cancellationToken),
            "ProcessPayment" => await ProcessPaymentAsync(orderId, sagaStateId, cancellationToken),
            "CreateShipment" => await CreateShipmentAsync(orderId, sagaStateId, cancellationToken),
            "ConfirmOrder" => await ConfirmOrderAsync(orderId, sagaStateId, cancellationToken),
            _ => throw new InvalidOperationException($"Unknown step: {stepName}")
        };
    }

    private async Task<bool> ReserveInventoryAsync(Guid orderId, Guid sagaStateId, CancellationToken cancellationToken)
    {
        try
        {
            await _inventoryService.ReserveForOrderAsync(orderId, cancellationToken);
            
            var log = new SagaCompensationLog
            {
                Id = Guid.NewGuid(),
                SagaStateId = sagaStateId,
                StepName = "ReserveInventory",
                Action = "Reserve",
                Success = true,
                ExecutedAt = DateTime.UtcNow
            };
            await _sagaRepository.AddCompensationLogAsync(log, cancellationToken);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reserve inventory for order {OrderId}", orderId);
            return false;
        }
    }

    private async Task<bool> ProcessPaymentAsync(Guid orderId, Guid sagaStateId, CancellationToken cancellationToken)
    {
        try
        {
            await _paymentService.ProcessPaymentAsync(orderId, cancellationToken);
            
            var log = new SagaCompensationLog
            {
                Id = Guid.NewGuid(),
                SagaStateId = sagaStateId,
                StepName = "ProcessPayment",
                Action = "Charge",
                Success = true,
                ExecutedAt = DateTime.UtcNow
            };
            await _sagaRepository.AddCompensationLogAsync(log, cancellationToken);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process payment for order {OrderId}", orderId);
            return false;
        }
    }

    private async Task<bool> CreateShipmentAsync(Guid orderId, Guid sagaStateId, CancellationToken cancellationToken)
    {
        try
        {
            await _shipmentService.CreateShipmentAsync(orderId, cancellationToken);
            
            var log = new SagaCompensationLog
            {
                Id = Guid.NewGuid(),
                SagaStateId = sagaStateId,
                StepName = "CreateShipment",
                Action = "Create",
                Success = true,
                ExecutedAt = DateTime.UtcNow
            };
            await _sagaRepository.AddCompensationLogAsync(log, cancellationToken);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create shipment for order {OrderId}", orderId);
            return false;
        }
    }

    private async Task<bool> ConfirmOrderAsync(Guid orderId, Guid sagaStateId, CancellationToken cancellationToken)
    {
        // Mark order as confirmed in database
        var log = new SagaCompensationLog
        {
            Id = Guid.NewGuid(),
            SagaStateId = sagaStateId,
            StepName = "ConfirmOrder",
            Action = "Confirm",
            Success = true,
            ExecutedAt = DateTime.UtcNow
        };
        await _sagaRepository.AddCompensationLogAsync(log, cancellationToken);
        return true;
    }

    private async Task CompensateAsync(Guid sagaStateId, CancellationToken cancellationToken)
    {
        var sagaState = await _sagaRepository.GetByOrderIdAsync(sagaStateId, cancellationToken);
        if (sagaState == null) return;

        sagaState.Status = SagaStatus.Compensating;
        await _sagaRepository.UpdateAsync(sagaState, cancellationToken);

        // Compensate in reverse order
        for (int i = sagaState.StepOrder; i >= 0; i--)
        {
            var stepName = Steps[i];
            
            // Skip if already compensated (idempotency check)
            if (await _sagaRepository.HasCompensatedAsync(sagaStateId, stepName, cancellationToken))
            {
                _logger.LogInformation("Step {StepName} already compensated, skipping", stepName);
                continue;
            }

            try
            {
                await CompensateStepAsync(stepName, sagaStateId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Compensation failed for step {StepName}", stepName);
            }
        }

        sagaState.Status = SagaStatus.Compensated;
        sagaState.CompensatedAt = DateTime.UtcNow;
        await _sagaRepository.UpdateAsync(sagaState, cancellationToken);
    }

    private async Task CompensateStepAsync(string stepName, Guid sagaStateId, CancellationToken cancellationToken)
    {
        var log = new SagaCompensationLog
        {
            Id = Guid.NewGuid(),
            SagaStateId = sagaStateId,
            StepName = stepName,
            Action = GetCompensatingAction(stepName),
            Success = false,
            ExecutedAt = DateTime.UtcNow
        };

        try
        {
            switch (stepName)
            {
                case "ReserveInventory":
                    await _inventoryService.ReleaseReservationAsync(sagaStateId, cancellationToken);
                    break;
                case "ProcessPayment":
                    await _paymentService.RefundPaymentAsync(sagaStateId, cancellationToken);
                    break;
                case "CreateShipment":
                    await _shipmentService.CancelShipmentAsync(sagaStateId, cancellationToken);
                    break;
            }

            log.Success = true;
            _logger.LogInformation("Successfully compensated step {StepName} for saga {SagaStateId}", stepName, sagaStateId);
        }
        catch (Exception ex)
        {
            log.Error = ex.Message;
            _logger.LogError(ex, "Failed to compensate step {StepName} for saga {SagaStateId}", stepName, sagaStateId);
            throw;
        }
        finally
        {
            await _sagaRepository.AddCompensationLogAsync(log, cancellationToken);
        }
    }

    private static string GetCompensatingAction(string stepName) => stepName switch
    {
        "ReserveInventory" => "Release",
        "ProcessPayment" => "Refund",
        "CreateShipment" => "Cancel",
        "ConfirmOrder" => "Cancel",
        _ => "Unknown"
    };
}

public class SagaException : Exception
{
    public SagaException(string message) : base(message) { }
}

// Service interfaces for Saga dependencies
public interface IInventoryService
{
    Task ReserveForOrderAsync(Guid orderId, CancellationToken cancellationToken);
    Task ReleaseReservationAsync(Guid sagaStateId, CancellationToken cancellationToken);
}

public interface IPaymentService
{
    Task ProcessPaymentAsync(Guid orderId, CancellationToken cancellationToken);
    Task RefundPaymentAsync(Guid sagaStateId, CancellationToken cancellationToken);
}

public interface IShipmentService
{
    Task CreateShipmentAsync(Guid orderId, CancellationToken cancellationToken);
    Task CancelShipmentAsync(Guid sagaStateId, CancellationToken cancellationToken);
}
