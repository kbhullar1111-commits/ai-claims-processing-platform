using Azure.Messaging.ServiceBus;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Commands.CreateNotification;

namespace NotificationService.Infrastructure.Messaging;

public sealed class NotificationMessagePump(
    ServiceBusClient busClient,
    NotificationMessagingOptions options,
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationMessagePump> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Starting notification message pump. Queue={Queue}, ClaimSubmittedTopic={Topic}, Subscription={Subscription}, MaxConcurrentCalls={MaxConcurrentCalls}",
            options.NotificationServiceQueue,
            options.ClaimSubmittedTopic,
            options.ClaimSubmittedSubscription,
            options.MaxConcurrentCalls);

        var processorOptions = new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = options.MaxConcurrentCalls,
            PrefetchCount = options.PrefetchCount,
            MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(options.MaxAutoLockRenewalMinutes)
        };

        var claimSubmittedProcessor = busClient.CreateProcessor(
            options.ClaimSubmittedTopic,
            options.ClaimSubmittedSubscription,
            processorOptions);

        var requestDocumentsProcessor = busClient.CreateProcessor(
            options.NotificationServiceQueue,
            processorOptions);

        claimSubmittedProcessor.ProcessErrorAsync += args =>
        {
            logger.LogError(
                args.Exception,
                "ClaimSubmitted processor error. Entity={EntityPath}, ErrorSource={ErrorSource}",
                args.EntityPath,
                args.ErrorSource);
            return Task.CompletedTask;
        };

        requestDocumentsProcessor.ProcessErrorAsync += args =>
        {
            logger.LogError(
                args.Exception,
                "RequestDocuments processor error. Entity={EntityPath}, ErrorSource={ErrorSource}",
                args.EntityPath,
                args.ErrorSource);
            return Task.CompletedTask;
        };

        claimSubmittedProcessor.ProcessMessageAsync += args =>
            HandleClaimSubmittedMessageAsync(args, options, scopeFactory, logger);

        requestDocumentsProcessor.ProcessMessageAsync += args =>
            HandleRequestDocumentsMessageAsync(args, options, logger);

        await claimSubmittedProcessor.StartProcessingAsync(stoppingToken);
        await requestDocumentsProcessor.StartProcessingAsync(stoppingToken);

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
            await claimSubmittedProcessor.StopProcessingAsync(CancellationToken.None);
            await requestDocumentsProcessor.StopProcessingAsync(CancellationToken.None);
            await claimSubmittedProcessor.DisposeAsync();
            await requestDocumentsProcessor.DisposeAsync();
        }
    }

    private static async Task HandleClaimSubmittedMessageAsync(
        ProcessMessageEventArgs args,
        NotificationMessagingOptions options,
        IServiceScopeFactory scopeFactory,
        ILogger logger)
    {
        var rawBody = args.Message.Body.ToString();

        if (!MassTransitInterop.TryDeserializeClaimSubmitted(rawBody, out var evt, out var envelopeMessageId))
        {
            var payloadPreview = rawBody.Length > 300 ? rawBody[..300] : rawBody;

            logger.LogWarning(
                "Skipping unsupported ClaimSubmitted payload. Topic={Topic}, Subscription={Subscription}, MessageId={MessageId}, ContentType={ContentType}, Subject={Subject}, PayloadPreview={PayloadPreview}",
                options.ClaimSubmittedTopic,
                options.ClaimSubmittedSubscription,
                args.Message.MessageId,
                args.Message.ContentType,
                args.Message.Subject,
                payloadPreview);

            await args.DeadLetterMessageAsync(
                args.Message,
                "UnsupportedPayload",
                "Message could not be deserialized as ClaimSubmitted or MassTransit envelope.");
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var eventId = ResolveEventId(args.Message, envelopeMessageId, evt!.ClaimId);

            logger.LogInformation(
                "Received ClaimSubmitted event. ClaimId={ClaimId}, CustomerId={CustomerId}, EventId={EventId}",
                evt.ClaimId,
                evt.CustomerId,
                eventId);

            var parameters = new Dictionary<string, string>
            {
                { "ClaimId", evt.ClaimId.ToString() }
            };

            var command = new CreateNotificationCommand(
                eventId,
                evt.CustomerId,
                "ClaimSubmitted",
                parameters);

            await ExecuteWithRetryAsync(
                ct => mediator.Send(command, ct),
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
                    "Moving ClaimSubmitted message to DLQ after delivery attempts exhausted. MessageId={MessageId}, DeliveryCount={DeliveryCount}, Topic={Topic}, Subscription={Subscription}",
                    args.Message.MessageId,
                    args.Message.DeliveryCount,
                    options.ClaimSubmittedTopic,
                    options.ClaimSubmittedSubscription);

                await args.DeadLetterMessageAsync(
                    args.Message,
                    "ProcessingFailed",
                    $"ClaimSubmitted processing failed after {args.Message.DeliveryCount} deliveries.");
                return;
            }

            logger.LogWarning(
                ex,
                "ClaimSubmitted processing failed; broker retry will continue. MessageId={MessageId}, DeliveryCount={DeliveryCount}, Topic={Topic}, Subscription={Subscription}",
                args.Message.MessageId,
                args.Message.DeliveryCount,
                options.ClaimSubmittedTopic,
                options.ClaimSubmittedSubscription);

            throw;
        }
    }

    private static async Task HandleRequestDocumentsMessageAsync(
        ProcessMessageEventArgs args,
        NotificationMessagingOptions options,
        ILogger logger)
    {
        var rawBody = args.Message.Body.ToString();

        if (!MassTransitInterop.TryDeserializeRequestDocuments(rawBody, out var message, out var envelopeMessageId))
        {
            if (MassTransitInterop.TryDeserializeClaimSubmitted(rawBody, out var claimSubmitted, out _))
            {
                logger.LogInformation(
                    "ClaimSubmitted received on notification queue during migration overlap; skipping queue handler. Queue={Queue}, MessageId={MessageId}, ClaimId={ClaimId}",
                    options.NotificationServiceQueue,
                    args.Message.MessageId,
                    claimSubmitted!.ClaimId);

                await args.CompleteMessageAsync(args.Message, args.CancellationToken);
                return;
            }

            var payloadPreview = rawBody.Length > 300 ? rawBody[..300] : rawBody;

            logger.LogWarning(
                "Skipping unsupported RequestDocuments payload. Queue={Queue}, MessageId={MessageId}, ContentType={ContentType}, Subject={Subject}, PayloadPreview={PayloadPreview}",
                options.NotificationServiceQueue,
                args.Message.MessageId,
                args.Message.ContentType,
                args.Message.Subject,
                payloadPreview);

            await args.DeadLetterMessageAsync(
                args.Message,
                "UnsupportedPayload",
                "Message could not be deserialized as RequestDocuments or MassTransit envelope.");
            return;
        }

        try
        {
            var eventId = ResolveEventId(args.Message, envelopeMessageId, message!.ClaimId);

            logger.LogInformation(
                "RequestDocuments received. ClaimId={ClaimId}, CustomerId={CustomerId}, RequiredDocuments={RequiredDocuments}, EventId={EventId}",
                message.ClaimId,
                message.CustomerId,
                string.Join(", ", message.Documents),
                eventId);

            await args.CompleteMessageAsync(args.Message, args.CancellationToken);
        }
        catch (Exception ex) when (!args.CancellationToken.IsCancellationRequested)
        {
            if (args.Message.DeliveryCount >= options.MaxDeliveryAttemptsBeforeDeadLetter)
            {
                logger.LogError(
                    ex,
                    "Moving RequestDocuments message to DLQ after delivery attempts exhausted. MessageId={MessageId}, DeliveryCount={DeliveryCount}, Queue={Queue}",
                    args.Message.MessageId,
                    args.Message.DeliveryCount,
                    options.NotificationServiceQueue);

                await args.DeadLetterMessageAsync(
                    args.Message,
                    "ProcessingFailed",
                    $"RequestDocuments logging failed after {args.Message.DeliveryCount} deliveries.");
                return;
            }

            logger.LogWarning(
                ex,
                "RequestDocuments processing failed; broker retry will continue. MessageId={MessageId}, DeliveryCount={DeliveryCount}, Queue={Queue}",
                args.Message.MessageId,
                args.Message.DeliveryCount,
                options.NotificationServiceQueue);

            throw;
        }
    }

    private static Guid ResolveEventId(ServiceBusReceivedMessage message, Guid? envelopeMessageId, Guid fallbackClaimId)
    {
        if (!string.IsNullOrWhiteSpace(message.MessageId) && Guid.TryParse(message.MessageId, out var parsedMessageId))
        {
            return parsedMessageId;
        }

        if (envelopeMessageId.HasValue && envelopeMessageId.Value != Guid.Empty)
        {
            return envelopeMessageId.Value;
        }

        if (!string.IsNullOrWhiteSpace(message.CorrelationId) && Guid.TryParse(message.CorrelationId, out var parsedCorrelationId))
        {
            return parsedCorrelationId;
        }

        return fallbackClaimId;
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
                    "Transient message handling failure on attempt {Attempt}. Retrying in {DelayMs}ms.",
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
