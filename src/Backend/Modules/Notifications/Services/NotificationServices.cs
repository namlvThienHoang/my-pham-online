using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Polly;
using Polly.CircuitBreaker;

namespace ECommerce.Modules.Notifications.Services;

public interface IEmailSender
{
    Task SendEmailAsync(string to, string subject, string body);
}

public interface ISmsSender
{
    Task SendSmsAsync(string phoneNumber, string message);
}

/// <summary>
/// AWS SES Email Sender với Circuit Breaker
/// </summary>
public class SesEmailSender : IEmailSender
{
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker;
    
    public SesEmailSender()
    {
        _circuitBreaker = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromMinutes(1)
            );
    }
    
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        await _circuitBreaker.ExecuteAsync(async () =>
        {
            // TODO: Implement AWS SES SDK call
            // var client = new AmazonSimpleEmailServiceClient();
            // await client.SendEmailAsync(...);
            
            Console.WriteLine($"[SES] Sending email to {to}: {subject}");
            await Task.Delay(100); // Simulate API call
        });
    }
}

/// <summary>
/// ESMS SMS Sender với Circuit Breaker và Fallback
/// </summary>
public class EsmsSender : ISmsSender
{
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker;
    private readonly IEmailSender _emailSender; // Fallback
    
    public EsmsSender(IEmailSender emailSender)
    {
        _emailSender = emailSender;
        _circuitBreaker = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromMinutes(2)
            );
    }
    
    public async Task SendSmsAsync(string phoneNumber, string message)
    {
        try
        {
            await _circuitBreaker.ExecuteAsync(async () =>
            {
                // TODO: Implement ESMS API call (Vietnam SMS provider)
                // var httpClient = new HttpClient();
                // await httpClient.PostAsync("https://api.esms.vn/...", ...);
                
                Console.WriteLine($"[ESMS] Sending SMS to {phoneNumber}: {message}");
                await Task.Delay(100); // Simulate API call
                
                // Simulate failure for demo
                throw new Exception("SMS service unavailable");
            });
        }
        catch (BrokenCircuitException)
        {
            // Circuit is open, fallback to email
            Console.WriteLine($"[Fallback] SMS circuit broken, sending email instead to {phoneNumber}");
            await _emailSender.SendEmailAsync(phoneNumber.Replace("+", ""), "SMS Fallback", message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] SMS failed: {ex.Message}");
            throw;
        }
    }
}
