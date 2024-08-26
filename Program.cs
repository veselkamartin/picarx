using Microsoft.Extensions.Logging;
using OpenAI;
using SmartCar.ChatGpt;
using SmartCar.Commands;
using SmartCar.Media;

namespace SmartCar;

class Program
{
	static async Task Main(string[] args)
	{
		using ILoggerFactory factory = LoggerFactory.Create(builder => builder
			.SetMinimumLevel(LogLevel.Debug)
			.AddFilter("PicarX.RobotHat.Pwm", LogLevel.None)
			.AddFilter("PicarX.PicarX.Servo", LogLevel.None)
			.AddFilter("PicarX.ChatGpt.ChatGpt", LogLevel.Debug)
			.AddFilter("TestController", LogLevel.None)
			.AddSimpleConsole(o => { o.SingleLine = true; }));
		ILogger logger = factory.CreateLogger("Program");

		Console.WriteLine("Sound");

		SoundRecorder.Test();
		Console.WriteLine("Starting");

		var OpenAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY", EnvironmentVariableTarget.Machine) ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
		if (string.IsNullOrEmpty(OpenAiApiKey))
		{
			Console.WriteLine("Environment variable OPENAI_API_KEY not set");
			return;
		}
		var px = new PicarX.Picarx(factory, ControllerBase.GetGpioController(factory), bus: ControllerBase.CreateI2cBus(1, factory));
		using var camera = new Camera(factory.CreateLogger<Camera>());
		var client = new OpenAIClient(OpenAiApiKey);
		var tts = new ChatGptTts(client);
		ICommandProvider[] commandProviders = [new WheelsAndCamera(px), new Speak(tts)];
		var parser = new ChatResponseParser(px, commandProviders, factory.CreateLogger<ChatResponseParser>());
		var chat = new ChatGpt.ChatGpt(client, factory.CreateLogger<ChatGpt.ChatGpt>(), parser, camera);
		Console.WriteLine("Initialized");
		await chat.StartAsync();
		//ControllerBase.SetTest();
		//new KeyboardControl(px).Run();
	}
}
