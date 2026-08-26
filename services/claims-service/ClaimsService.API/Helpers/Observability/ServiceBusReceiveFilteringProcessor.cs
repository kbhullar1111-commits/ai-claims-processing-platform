using System.Diagnostics;
using OpenTelemetry;

namespace ClaimsService.API.Observability;

public sealed class ServiceBusReceiveFilteringProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        if (activity.DisplayName.Equals(
                "ServiceBusReceiver.Receive",
                StringComparison.Ordinal))
        {
             activity.IsAllDataRequested = false;
        }
    }
}