using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
		builder.Services.AddSingleton(s => new OpenAIClient(openAiApiKey));// var client = new OpenAIClient(openAiApiKey);
		builder.Services.AddSingleton<ChatGptStt>();//var stt = new ChatGptStt(client);

		builder.Services.AddSingleton<ISoundPlayer, OpenTkSoundPlayer>();//( var soundPlayer = new OpenTkSoundPlayer(factory.CreateLogger<OpenTkSoundPlayer>());
		builder.Services.AddSingleton<OpenTkSoundRecorder>();// using var recorder = new OpenTkSoundRecorder(factory.CreateLogger<OpenTkSoundRecorder>());
		builder.Services.AddSingleton<SpeachInput>();// var soundInput = new SpeachInput(recorder, soundPlayer, stt, factory.CreateLogger<SpeachInput>());
													 ////test
													 //while (true)
													 //{
													 //	var testSound = await soundInput.Read();
													 //	Console.WriteLine(testSound);
													 //	Console.ReadKey();
													 //}
		builder.Services.AddSingleton(s =>
		{
			var factory = s.GetRequiredService<ILoggerFactory>();
			return new PicarX.Picarx(factory, ControllerBase.GetGpioController(factory), bus: ControllerBase.CreateI2cBus(1, factory));
		});
		//var px = new PicarX.Picarx(factory, ControllerBase.GetGpioController(factory), bus: ControllerBase.CreateI2cBus(1, factory));

		if (Environment.OSVersion.Platform == PlatformID.Win32NT)
		{
			builder.Services.AddSingleton<ICamera, EmguCvCamera>();
		}
		else
		{
			builder.Services.AddSingleton<ICamera, IotBindingsCamera>();
		}
		//using ICamera camera = Environment.OSVersion.Platform == PlatformID.Win32NT ?
		//	new EmguCvCamera(factory.CreateLogger<EmguCvCamera>()) :
		//	new IotBindingsCamera(factory.CreateLogger<IotBindingsCamera>());

		//var tts = new CognitiveServicesTts(azureSpeakKey, soundPlayer, factory.CreateLogger<CognitiveServicesTts>());
		builder.Services.AddSingleton<ITextPlayer>(s => new CognitiveServicesTts(azureSpeakKey, s.GetRequiredService<ISoundPlayer>(), s.GetRequiredService<ILogger<CognitiveServicesTts>>()));
		//var tts = new ChatGptTts(client, soundPlayer);
		builder.Services.AddSingleton<ICommandProvider, WheelsAndCamera>();
		builder.Services.AddSingleton<ICommandProvider, Speak>();
		//ICommandProvider[] commandProviders = [new WheelsAndCamera(px), new Speak(tts)];
		builder.Services.AddSingleton<ChatResponseParser>();
		//var parser = new ChatResponseParser(commandProviders, factory.CreateLogger<ChatResponseParser>());
		builder.Services.AddSingleton<PicarX.StateProvider>();
		//var stateProvider = new PicarX.StateProvider(px);
		builder.Services.AddSingleton<ChatGpt.ChatGpt>();
		//builder.Services.AddHostedService<ChatHost>();
		//var chat = new ChatGpt.ChatGpt(client, factory.CreateLogger<ChatGpt.ChatGpt>(), parser, camera, soundInput, stateProvider);
		var app = builder.Build();
		Console.WriteLine("Initialized");

		//app.UseSunFounderControler();
		await app.StartAsync();

		var camera=app.Services.GetRequiredService<ICamera>();
		var cameraReader = await camera.CaptureTimelapse();
		await cameraReader.Read();
		cameraReader.Stop();

		//var chat = app.Services.GetRequiredService<ChatGpt.ChatGpt>();
		//await chat.StartAsync();
		//ControllerBase.SetTest();
		//new KeyboardControl(px).Run();
		Console.WriteLine("Running");
		await app.WaitForShutdownAsync();
	}
}