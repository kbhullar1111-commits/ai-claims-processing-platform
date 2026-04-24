using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace document_processor_function;

public class BlobCreatedFunction
{
    private readonly ILogger _logger;
    private readonly ServiceBusClient _busClient;
    private readonly IConfiguration _config;

    public BlobCreatedFunction(
    ILogger<BlobCreatedFunction> logger,
    ServiceBusClient busClient,
    IConfiguration config)
    {
        _logger = logger;
        _busClient = busClient;
        _config = config;
    }

    [Function("BlobCreatedFunction")]
    public async Task Run([EventGridTrigger] string eventGridEvent)
    {
        try{
            _logger.LogInformation("Blob event received: {event}",
                eventGridEvent);

            using var doc = JsonDocument.Parse(eventGridEvent);

            var root = doc.RootElement;

            if (!root.TryGetProperty("eventType", out var eventTypeProp) ||
                eventTypeProp.GetString() != "Microsoft.Storage.BlobCreated")
            {
                _logger.LogWarning(
                    "Ignoring unsupported event type: {EventType}",
                    eventTypeProp.GetString());

                return;
            }

            if (root.TryGetProperty("data", out var data) &&
                data.TryGetProperty("url", out var urlProp))
            {
                var blobUrl = urlProp.GetString();

                _logger.LogInformation("Blob URL: {url}", blobUrl);

                var uri = new Uri(blobUrl!);

                var segments = uri.AbsolutePath
                    .Trim('/')
                    .Split('/');

                // Example:
                // claim-documents / claims / claimId / docType / file

                if (segments.Length >= 5)
                {
                    var claimIdSegment = segments[2];
                    var documentType = segments[3];
                    var fileName = segments[4];
                    var topic = _config["DocumentUploadedTopic"] ?? "document-uploaded-raw";

                    if (!Guid.TryParse(claimIdSegment, out var claimId))
                    {
                        _logger.LogWarning(
                            "Skipping BlobCreated event because claimId path segment is not a valid GUID. Path={Path}, ClaimIdSegment={ClaimIdSegment}",
                            uri.AbsolutePath,
                            claimIdSegment);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(documentType))
                    {
                        _logger.LogWarning(
                            "Skipping BlobCreated event because document type path segment is empty. Path={Path}",
                            uri.AbsolutePath);
                        return;
                    }

                    await using var sender =
                    _busClient.CreateSender(topic);

                    var payload = JsonSerializer.Serialize(new
                    {
                        DocumentId = Guid.NewGuid(),
                        ClaimId = claimId,
                        DocumentType = documentType,
                        UploadedAt = DateTime.UtcNow
                    });

                    var message = new ServiceBusMessage(payload)
                    {
                        ContentType = "application/json",
                        Subject = "DocumentUploaded",
                        MessageId = Guid.NewGuid().ToString()
                    };

                    await sender.SendMessageAsync(message);

                    _logger.LogInformation(
                        "Published DocumentUploaded for ClaimId={claimId}, Type={type}, File={file}",
                        claimId,
                        documentType,
                        fileName);
                }
                else
                {
                    _logger.LogWarning(
                        "Skipping BlobCreated event because blob path does not match expected structure. Path={Path}",
                        uri.AbsolutePath);
                }
            }
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error processing BlobCreated event");
        }

    }
}
