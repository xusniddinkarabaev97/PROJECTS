using System;
using Polly;
using Polly.Retry;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GzsBilling.Infrastructure.Resilience;

/// <summary>
/// Centralised factory for Polly v8 resilience pipelines used across the billing system.
/// Pipelines combine retry (exponential backoff + jitter), circuit breaker, and per-service timeouts.
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Creates a ResiliencePipelineBuilder pre-configured with an exponential-backoff
    /// retry strategy. Callers can chain additional strategies and call .Build().
    /// </summary>
    public static ResiliencePipelineBuilder CreateExponentialBackoff(
        int maxRetries = 5,
        double baseDelayMs = 500,
        ILogger? logger = null)
    {
        var log = logger ?? NullLogger.Instance;
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = maxRetries,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromMilliseconds(baseDelayMs),
                UseJitter = true,
                MaxDelay = TimeSpan.FromSeconds(60),
                ShouldHandle = new PredicateBuilder()
                    .Handle<Exception>(),
                OnRetry = args =>
                {
                    log.LogWarning(
                        "Retry attempt {AttemptNumber} after {RetryDelayMs:F0}ms. " +
                        "Exception: {ExceptionType} - {ExceptionMessage}",
                        args.AttemptNumber + 1,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.GetType().Name,
                        args.Outcome.Exception?.Message);
                    return default;
                }
            });
    }

    /// <summary>
    /// Builds CircuitBreakerStrategyOptions with the supplied thresholds.
    /// Default: 50% failure ratio, min 10 throughput, 30s break, 60s sampling window.
    /// </summary>
    public static CircuitBreakerStrategyOptions CreateCircuitBreakerOptions(
        double failureThreshold = 0.5,
        int minimumThroughput = 10,
        int breakDurationSeconds = 30,
        int samplingDurationSeconds = 60)
    {
        return new CircuitBreakerStrategyOptions
        {
            FailureRatio = failureThreshold,
            MinimumThroughput = minimumThroughput,
            BreakDuration = TimeSpan.FromSeconds(breakDurationSeconds),
            SamplingDuration = TimeSpan.FromSeconds(samplingDurationSeconds),
            ShouldHandle = new PredicateBuilder()
                .Handle<Exception>(),
            OnOpened = args => default,
            OnClosed = args => default,
            OnHalfOpened = args => default
        };
    }

    public static TimeSpan PaymentSystemTimeout => TimeSpan.FromSeconds(10);
    public static TimeSpan ErspEcbTimeout => TimeSpan.FromSeconds(5);
    public static TimeSpan InternalMicroserviceTimeout => TimeSpan.FromSeconds(2);
    public static TimeSpan WebhookCallbackTimeout => TimeSpan.FromSeconds(3);
    public static TimeSpan RedisCacheTimeout => TimeSpan.FromMilliseconds(500);

    public static ResiliencePipeline CreatePaymentSystemPipeline(ILogger logger)
    {
        return new ResiliencePipelineBuilder()
            .AddTimeout(PaymentSystemTimeout)
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 5,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromMilliseconds(500),
                UseJitter = true,
                MaxDelay = TimeSpan.FromSeconds(60),
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .Handle<Exception>(),
                OnRetry = args =>
                {
                    logger.LogWarning(
                        "Payment system retry {Attempt}/{MaxAttempts} after {DelayMs:F0}ms. " +
                        "Error: {Error}",
                        args.AttemptNumber + 1, 5,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message);
                    return default;
                }
            })
            .AddCircuitBreaker(CreateCircuitBreakerOptions(
                failureThreshold: 0.5, breakDurationSeconds: 30, samplingDurationSeconds: 60))
            .Build();
    }

    public static ResiliencePipeline CreateErspEcbPipeline(ILogger logger)
    {
        return new ResiliencePipelineBuilder()
            .AddTimeout(ErspEcbTimeout)
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromMilliseconds(500),
                UseJitter = true,
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .Handle<Exception>(),
                OnRetry = args =>
                {
                    logger.LogWarning(
                        "ERSP/ECB retry {Attempt}/{MaxAttempts} after {DelayMs:F0}ms. " +
                        "Error: {Error}",
                        args.AttemptNumber + 1, 3,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message);
                    return default;
                }
            })
            .AddCircuitBreaker(CreateCircuitBreakerOptions(
                failureThreshold: 0.5, breakDurationSeconds: 30))
            .Build();
    }

    public static ResiliencePipeline CreateInternalMicroservicePipeline(ILogger logger)
    {
        return new ResiliencePipelineBuilder()
            .AddTimeout(InternalMicroserviceTimeout)
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromMilliseconds(200),
                UseJitter = true,
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .Handle<Exception>(),
                OnRetry = args =>
                {
                    logger.LogWarning(
                        "Internal service retry {Attempt}/{MaxAttempts} after {DelayMs:F0}ms. " +
                        "Error: {Error}",
                        args.AttemptNumber + 1, 3,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message);
                    return default;
                }
            })
            .AddCircuitBreaker(CreateCircuitBreakerOptions(
                failureThreshold: 0.5, breakDurationSeconds: 30))
            .Build();
    }
}
