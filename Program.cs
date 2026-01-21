using OpenAI;
using SmartCar.Azure;
using SmartCar.ChatGpt;
using SmartCar.Commands;
using SmartCar.Media;
using SmartCar.SunFounderControler;
using static Emgu.CV.VideoCapture;

namespace SmartCar;

class Program
{
	static async Task Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);
		builder.Logging
			//using ILoggerFactory factory = LoggerFactory.Create(builder => builder
			.SetMinimumLevel(LogLevel.Debug)
			.AddFilter(typeof(RobotHat.Pwm).FullName, LogLevel.None)
			.AddFilter(typeof(RobotHat.Motor).FullName, LogLevel.None)
			.AddFilter(typeof(PicarX.Servo).FullName, LogLevel.None)
			.AddFilter(typeof(PicarX.Ultrasonic).FullName, LogLevel.None)
			.AddFilter(typeof(PicarX.Picarx).FullName, LogLevel.Information)
			.AddFilter(typeof(ChatGpt.ChatGpt).FullName, LogLevel.Information)
			.AddFilter(typeof(EmguCvCamera).FullName, LogLevel.Warning)
			.AddFilter("TestController", LogLevel.None)
			.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss"; });
		//ILogger logger = factory.CreateLogger("Program");

		Console.WriteLine("Starting");
		builder.ConfigureSunFounderControler();

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
		builder.Services.AddSingleton(s => new OpenAIClient(openAiApiKey));
		builder.Services.AddSingleton<ChatGptStt>();
		builder.Services.AddSingleton<ISoundPlayer, OpenTkSoundPlayer>();
		builder.Services.AddSingleton<OpenTkSoundRecorder>();
		builder.Services.AddSingleton<ISpeachInput, SpeachInput>();
		//builder.Services.AddSingleton<ISpeachInput, ConsoleInput>();
		
		builder.Services.AddSingleton(s =>
		{
			var factory = s.GetRequiredService<ILoggerFactory>();
			return new PicarX.Picarx(factory, ControllerBase.GetGpioController(factory), bus: ControllerBase.CreateI2cBus(1, factory));
		});

		if (Environment.OSVersion.Platform == PlatformID.Win32NT)
		{
			builder.Services.AddSingleton<ICamera, EmguCvCamera>();
		}
		else
		{
			builder.Services.AddSingleton<ICamera, IotBindingsCamera>();
		}

		builder.Services.AddSingleton<ITextPlayer>(s => new CognitiveServicesTts(azureSpeakKey, s.GetRequiredService<ISoundPlayer>(), s.GetRequiredService<ILogger<CognitiveServicesTts>>()));
		//builder.Services.AddSingleton<ITextPlayer, ChatGptTts>();
		builder.Services.AddSingleton<ICommandProvider, WheelsAndCamera>();
		builder.Services.AddSingleton<ICommandProvider, Speak>();
		builder.Services.AddSingleton<ChatResponseParser>();
		builder.Services.AddSingleton<PicarX.StateProvider>();
		builder.Services.AddSingleton<ChatGpt.ChatGpt>();
		builder.Services.AddHostedService<ChatHost>();
		var app = builder.Build();
		Console.WriteLine("Initialized");
		
		var soundPlayer = app.Services.GetRequiredService<ISoundPlayer>(); 
		await soundPlayer.PlayWavOnSpeaker(File.ReadAllBytes("Sounds/bells-logo.wav"));
		//var soundInput = app.Services.GetRequiredService<ISpeachInput>();
		////test
		//while (true)
		//{
		//	var testSound = await soundInput.Read(CancellationToken.None);
		//	Console.WriteLine(testSound);
		//	Console.ReadKey();
		//}

		//app.UseSunFounderControler();
		await app.StartAsync();

		//var camera=app.Services.GetRequiredService<ICamera>();
		//var cameraReader = await camera.CaptureTimelapse();
		//await cameraReader.Read();
		//cameraReader.Stop();

		Console.WriteLine("Running");
		await app.WaitForShutdownAsync();
	}
}