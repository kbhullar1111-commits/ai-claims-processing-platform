namespace ClaimsService.Application.Sagas;

public class ClaimProcessingSagaRoutingOptions
{
    public string ClaimsServiceQueue { get; set; } = "claims-service";

    public string NotificationServiceQueue { get; set; } = "notification-service";

    public string FraudServiceQueue { get; set; } = "fraud-service";

    public string PaymentServiceQueue { get; set; } = "payment-service";
}