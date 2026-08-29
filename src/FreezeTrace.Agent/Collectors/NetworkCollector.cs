using System.Net.NetworkInformation;

namespace FreezeTrace.Agent.Collectors;

internal static class NetworkCollector
{
    public static (long Received, long Sent) ReadTotals()
    {
        long received = 0;
        long sent = 0;

        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up ||
                nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;

            try
            {
                var stats = nic.GetIPv4Statistics();
                received += stats.BytesReceived;
                sent += stats.BytesSent;
            }
            catch (NetworkInformationException)
            {
                // Adapter disappeared during enumeration.
            }
        }

        return (received, sent);
    }
}
