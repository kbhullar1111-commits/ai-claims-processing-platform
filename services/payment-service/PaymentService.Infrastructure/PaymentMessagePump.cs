using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentService.Application;

namespace PaymentService.Infrastructure;

public sealed class PaymentMessagePump(
    ServiceBusClient busClient,
    PaymentMessagingOptions options,
    IPaymentProcessor paymentProcessor,
    PaymentProcessedPublisher publisher,
    ILogger<PaymentMessagePump> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Starting payment message pump. Queue={Queue}, PaymentProcessedTopic={Topic}, MaxConcurrentCalls={MaxConcurrentCalls}",
            options.PaymentServiceQueue,
            options.PaymentProcessedTopic,
            options.MaxConcurrentCalls);

        var processor = busClient.CreateProcessor(options.PaymentServiceQueue, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = options.MaxConcurrentCalls,
            PrefetchCount = options.PrefetchCount,
            MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(options.MaxAutoLockRenewalMinutes)
        });

        processor.ProcessErrorAsync += args =>
        {
            logger.LogError(
                args.Exception,
                "Service Bus processor error. Entity={EntityPath}, ErrorSource={ErrorSource}",
                args.EntityPath,
                args.ErrorSource);

            return Task.CompletedTask;
        };

        processor.ProcessMessageAsync += async args =>
        {
            var rawBody = args.Message.Body.ToString();

            logger.LogInformation(
                "Payment queue message received. Queue={Queue}, MessageId={MessageId}, DeliveryCount={DeliveryCount}, ContentType={ContentType}, Subject={Subject}, CorrelationId={CorrelationId}",
                options.PaymentServiceQueue,
                args.Message.MessageId,
                args.Message.DeliveryCount,
                args.Message.ContentType,
                args.Message.Subject,
                args.Message.CorrelationId);

            if (!MassTransitInterop.TryDeserializeProcessPayment(rawBody, out var command, out var correlationId, out var conversationId))
            {
                var payloadPreview = rawBody.Length > 300
                    ? rawBody[..300]
                    : rawBody;

                logger.LogWarning(
                    "Skipping unsupported message payload on queue {QueueName}. MessageId={MessageId}, ContentType={ContentType}, Subject={Subject}, PayloadPreview={PayloadPreview}",
                    options.PaymentServiceQueue,
                    args.Message.MessageId,
                    args.Message.ContentType,
                    args.Message.Subject,
                    payloadPreview);

                await args.DeadLetterMessageAsync(
                    args.Message,
                    "UnsupportedPayload",
                    "Message could not be deserialized as ProcessPayment or MassTransit envelope.");
                return;
            }

            try
            {
                var processed = await paymentProcessor.ProcessAsync(command!, args.CancellationToken);
                await ExecuteWithRetryAsync(
                    ct => publisher.PublishAsync(processed, correlationId, conversationId, ct),
                    options.HandlerRetryMaxAttempts,
                    options.HandlerRetryBaseDelayMs,
                    options.HandlerRetryMaxDelaySeconds,
                    logger,
                    args.CancellationToken);

                await args.CompleteMessageAsync(args.Message, args.CancellationToken);
            }
            catch (Exception ex) when (!args.CancellationToken.IsCancellationRequested)
            {
                if (args.Message.DeliveryCount >= options.MaxDeliveryAttemptsBeforeDeadLetter)
                {
                    logger.LogError(
                        ex,
                        "Moving message to DLQ after delivery attempts exhausted. MessageId={MessageId}, DeliveryCount={DeliveryCount}, Queue={Queue}",
                        args.Message.MessageId,
                        args.Message.DeliveryCount,
                        options.PaymentServiceQueue);

                    await args.DeadLetterMessageAsync(
                        args.Message,
                        "ProcessingFailed",
                        $"Payment processing failed after {args.Message.DeliveryCount} deliveries.");
                    return;
                }

                logger.LogWarning(
                    ex,
                    "Message processing failed; broker retry will continue. MessageId={MessageId}, DeliveryCount={DeliveryCount}, Queue={Queue}",
                    args.Message.MessageId,
                    args.Message.DeliveryCount,
                    options.PaymentServiceQueue);

                throw;
            }
        };

        await processor.StartProcessingAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Service is stopping.
        }
        finally
        {
            await processor.StopProcessingAsync(CancellationToken.None);
            await processor.DisposeAsync();
        }
    }

    private static async Task ExecuteWithRetryAsync(
        Func<CancellationToken, Task> action,
        int maxAttempts,
        int baseDelayMs,
        int maxDelaySeconds,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < Math.Max(maxAttempts, 1))
        {
            attempt++;

            try
            {
                await action(cancellationToken);
                return;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested && IsTransient(ex))
            {
                lastException = ex;
                if (attempt >= maxAttempts)
                {
                    break;
                }

                var delayMs = Math.Min(
                    baseDelayMs * (int)Math.Pow(2, attempt - 1),
                    maxDelaySeconds * 1000);

                logger.LogWarning(
                    ex,
                    "Transient publish failure on attempt {Attempt}. Retrying in {DelayMs}ms.",
                    attempt,
                    delayMs);

                await Task.Delay(delayMs, cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException("Retry operation failed without captured exception.");
    }

    private static bool IsTransient(Exception exception)
    {
        return exception switch
        {
            ServiceBusException sbEx => sbEx.IsTransient,
            TimeoutException => true,
            TaskCanceledException => true,
            OperationCanceledException => false,
            _ => false
        };
    }
}
