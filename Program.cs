using Microsoft.Extensions.Logging;
using OpenAI;
using SmartCar.Azure;
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
			.AddFilter(typeof(RobotHat.Pwm).FullName, LogLevel.None)
			.AddFilter(typeof(RobotHat.Motor).FullName, LogLevel.None)
			.AddFilter(typeof(PicarX.Servo).FullName, LogLevel.None)
			.AddFilter(typeof(PicarX.Ultrasonic).FullName, LogLevel.None)
			.AddFilter(typeof(PicarX.Picarx).FullName, LogLevel.None)
			.AddFilter(typeof(ChatGpt.ChatGpt).FullName, LogLevel.Information)
			.AddFilter("TestController", LogLevel.None)
			.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss"; }));
		ILogger logger = factory.CreateLogger("Program");

		Console.WriteLine("Starting");

		var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY", EnvironmentVariableTarget.Machine) ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
		if (string.IsNullOrEmpty(openAiApiKey))
		{
			Console.WriteLine("Environment variable OPENAI_API_KEY not set");
			return;
		}
		var azureSpeakKey = Environment.GetEnvironmentVariable("AZURE_SPEACH_KEY", EnvironmentVariableTarget.Machine) ?? Environment.GetEnvironmentVariable("AZURE_SPEACH_KEY");
		if (string.IsNullOrEmpty(azureSpeakKey))
		{
			Console.WriteLine("Environment variable AZURE_SPEACH_KEY not set");
			return;
		}
		var client = new OpenAIClient(openAiApiKey);
		var stt = new ChatGptStt(client);

		using var soundPlayer = new OpenTkSoundPlayer(factory.CreateLogger<OpenTkSoundPlayer>());
		using var recorder = new OpenTkSoundRecorder(factory.CreateLogger<OpenTkSoundRecorder>());
		var soundInput = new SpeachInput(recorder, soundPlayer, stt, factory.CreateLogger<SpeachInput>());
		////test
		//while (true)
		//{
		//	var testSound = await soundInput.Read();
		//	Console.WriteLine(testSound);
		//	Console.ReadKey();
		//}
		var px = new PicarX.Picarx(factory, ControllerBase.GetGpioController(factory), bus: ControllerBase.CreateI2cBus(1, factory));

		using ICamera camera = Environment.OSVersion.Platform == PlatformID.Win32NT ?
			new EmguCvCamera(factory.CreateLogger<EmguCvCamera>()) :
			new IotBindingsCamera(factory.CreateLogger<IotBindingsCamera>());

		var tts = new CognitiveServicesTts(azureSpeakKey, soundPlayer, factory.CreateLogger<CognitiveServicesTts>());
		//var tts = new ChatGptTts(client, soundPlayer);
		ICommandProvider[] commandProviders = [new WheelsAndCamera(px), new Speak(tts)];
		var parser = new ChatResponseParser(commandProviders, factory.CreateLogger<ChatResponseParser>());
		var stateProvider = new PicarX.StateProvider(px);
		var chat = new ChatGpt.ChatGpt(client, factory.CreateLogger<ChatGpt.ChatGpt>(), parser, camera, soundInput, stateProvider);
		Console.WriteLine("Initialized");
		await chat.StartAsync();
		//ControllerBase.SetTest();
		//new KeyboardControl(px).Run();
	}
}