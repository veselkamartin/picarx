using System.Net.Sockets;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Serialization;

namespace SmartCar.SunFounderControler;

public class ControlerHandler
{
	public async Task Handle(WebSocket webSocket)
	{

		var message = new ControlerMessage();

		var host = Dns.GetHostEntry(Dns.GetHostName());
		var ip = host.AddressList.First(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToString();
		Console.WriteLine("My ip:" + ip);

		message.Data["video"] = $"http://{ip}:8765/mjpg";
		string v = System.Text.Json.JsonSerializer.Serialize(message);
		var b = Encoding.UTF8.GetBytes(v);
		await webSocket.SendAsync(b, WebSocketMessageType.Text, true, CancellationToken.None);

		var buffer = new byte[1024 * 4];
		WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
		while (!result.CloseStatus.HasValue)
		{
			//await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);
			var recMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);

			Console.WriteLine("Received: " + recMessage);
			result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
		}
		await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
		Console.WriteLine("Closed");
	}
	class ControlerMessage
	{
		public string Name { get; set; } = "PicarWin";
		public string Type { get; set; } = "None";
		public string Check { get; set; } = "SunFounder Controller";
		[JsonExtensionData]
		public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

	}
}

