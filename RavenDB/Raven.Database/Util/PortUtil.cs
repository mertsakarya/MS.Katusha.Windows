using System.Linq;
using System.Net.NetworkInformation;
using NLog;

namespace Raven.Database.Util
{
	public static class PortUtil
	{
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		const int DefaultPort = 8080;

		public static int GetPort(string portStr)
		{
			if (portStr == "*" || string.IsNullOrWhiteSpace(portStr))
			{
				var autoPort = FindPort();
				if (autoPort != DefaultPort)
				{
					logger.Info("Default port {0} was not available, so using available port {1}", DefaultPort, autoPort);
				}
				return autoPort;
			}

			int port;
			if (int.TryParse(portStr, out port) == false)
				return DefaultPort;

			return port;
		}

		private static int FindPort()
		{
			var activeTcpListeners = IPGlobalProperties
				.GetIPGlobalProperties()
				.GetActiveTcpListeners();

			for (var port = DefaultPort; port < DefaultPort + 1024; port++)
			{
				var portCopy = port;
				if (activeTcpListeners.All(endPoint => endPoint.Port != portCopy))
					return port;
			}

			return DefaultPort;
		}
	}
}