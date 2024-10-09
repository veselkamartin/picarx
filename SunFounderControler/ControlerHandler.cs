using System.Net.Sockets;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using SmartCar.PicarX;

namespace SmartCar.SunFounderControler;

public class ControlerHandler
{
	private readonly ILogger<ControlerHandler> _logger;
	private readonly Picarx _px;

	public ControlerHandler(ILogger<ControlerHandler> logger, Picarx px)
	{
		_logger = logger;
		_px = px;
	}

	public async Task Handle(WebSocket webSocket)
	{

		var response = new ControlerMessage();

		var host = Dns.GetHostEntry(Dns.GetHostName());
		_logger.LogInformation("IP addresses: {ip}", string.Join(",", host.AddressList.Select(i => i.ToString() + " " + i.AddressFamily)));
		var ip = host.AddressList.First(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToString();
		_logger.LogInformation("My ip:" + ip);

		response.Data["video"] = $"http://{ip}:8765/mjpg";
		var sendBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));
		await webSocket.SendAsync(sendBytes, WebSocketMessageType.Text, true, CancellationToken.None);

		var buffer = new byte[1024 * 4];
		WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
		string? lastMessage = null;
		while (!result.CloseStatus.HasValue)
		{
			//await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);
			var recMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
			if (recMessage != lastMessage)
			{
				_logger.LogInformation("Received: " + recMessage);
				var recMessageValue = JsonSerializer.Deserialize<ControlerMessage>(recMessage) ?? throw new Exception("Message cannot be deserialized");
				await ProcessMessage(recMessageValue, response);
			}
			lastMessage = recMessage;
			result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
		}
		await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
		_logger.LogInformation("Closed");
	}

	private async Task ProcessMessage(ControlerMessage message, ControlerMessage response)
	{
		// --- send data ---
		//response.Set("A", speed);

		//var grayscale_data = _px.get_grayscale_data();
		// response.Set("D", grayscale_data);

		var distance = _px.GetDistance();
		response.Set("F", distance);

		// --- control ---

		// // horn
		if (message.GetBool("M"))
		{
			//horn();
		}

		// speaker
		//if message.Get('J') != None:
		//    speak=message.Get('J')
		//    print(f'speaker: {speak}')
		//if speak in ["forward"]:
		//    px.forward(speed)
		//elif speak in ["backward"]:
		//    px.backward(speed)
		//elif speak in ["left"]:
		//    px.set_dir_servo_angle(-30)
		//    px.forward(60)
		//    sleep(1.2)
		//    px.set_dir_servo_angle(0)
		//    px.forward(speed)
		//elif speak in ["right", "white", "rice"]:
		//    px.set_dir_servo_angle(30)
		//    px.forward(60)
		//    sleep(1.2)
		//    px.set_dir_servo_angle(0)
		//    px.forward(speed)
		//elif speak in ["stop"]:
		//    px.stop()

		// line_track and avoid_obstacles
		//line_track_switch = message.Get('I')
		//avoid_obstacles_switch = message.Get('E')
		//if line_track_switch == True:
		//    speed = LINE_TRACK_SPEED
		//    line_track()
		//elif avoid_obstacles_switch == True:
		//    speed = AVOID_OBSTACLES_SPEED
		//    avoid_obstacles()

		// joystick moving
		//if line_track_switch != True and avoid_obstacles_switch != True:
		var Joystick_K_Val = message.GetIntArray("K");
		if (Joystick_K_Val is [int angle, int speed])
		{
			var dir_angle = Map(angle, -100, 100, -30, 30);
			_px.SetDirServoAngle(dir_angle);
			if (speed > 0)
			{
				_px.Forward(speed);
			}
			else if (speed < 0)
			{
				speed = -speed;
				_px.Backward(speed);
			}
			else
			{
				_px.Stop();
			}
		}

		// camera servos control
		var Joystick_Q_Val = message.GetIntArray("Q");
		if (Joystick_Q_Val is [int pan, int tilt] && (pan != 0 || tilt != 0))
		{
			pan = Math.Clamp(pan, -90, 90);
			tilt = Math.Clamp(tilt, -35, 65);
			_px.SetCamPanAngle(pan);
			_px.SetCamTiltAngle(tilt);
		}
		// image recognition
		//if message.Get('N') == True:
		//    Vilib.color_detect(DETECT_COLOR)
		//else:
		//    Vilib.color_detect("close")

		//if message.Get('O') == True:
		//    Vilib.face_detect_switch(True)  
		//else:
		//    Vilib.face_detect_switch(False)  

		//if message.Get('P') == True:
		//    Vilib.object_detect_switch(True) 
		//else:
		//    Vilib.object_detect_switch(False)
		//    

	}
	private double Map(double x, double in_min, double in_max, double out_min, double out_max)
	{
		return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
	}

	class ControlerMessage
	{
		public string Name { get; set; } = "PicarWin";
		public string Type { get; set; } = "None";
		public string Check { get; set; } = "SunFounder Controller";

		[JsonExtensionData]
		public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

		public void Set(string key, object value)
		{
			Data[key] = value;
		}
		public object? Get(string key)
		{
			return Data.GetValueOrDefault(key);
		}
		public bool GetBool(string key)
		{
			var v = Data.GetValueOrDefault(key);
			return (v as bool?) ?? false;
		}
		public int[] GetIntArray(string key)
		{
			var v = Data.GetValueOrDefault(key);
			return (v as int[]) ?? [0, 0];
		}
	}
}

