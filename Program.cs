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
		using var soundPlayer = new OpenTkSoundPlayer();
		var recorder = new SoundRecorder();
		const int sampleRate = 44100;
		const float toneLengthSeconds = 0.5f;
		short[] sine = new short[(int)(sampleRate * toneLengthSeconds)];
		FillSine(sine, 400, sampleRate, 0.5f);
		Console.WriteLine("Play sine");
		await soundPlayer.PlaySoundOnSpeaker(sine, sampleRate);

		var recordedData = recorder.Record();
		var max=recordedData.Max();
		var min=recordedData.Min();
		var gain =(float)short.MaxValue/ Math.Max(max, Math.Abs(min));
		Console.WriteLine($"Min {min} max {max} gain {gain}");

		await soundPlayer.PlaySoundOnSpeaker(sine, sampleRate);
		await soundPlayer.PlaySoundOnSpeaker(recordedData, recorder.SampleRate);
		await soundPlayer.PlaySoundOnSpeaker(sine, sampleRate);
		for (int i = 0; i < recordedData.Length; i++)
		{
			recordedData[i]=(short)(recordedData[i] * gain);
		}
		await soundPlayer.PlaySoundOnSpeaker(recordedData, recorder.SampleRate);
		await soundPlayer.PlaySoundOnSpeaker(sine, sampleRate);


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

		var tts = new ChatGptTts(client, soundPlayer);
		ICommandProvider[] commandProviders = [new WheelsAndCamera(px), new Speak(tts)];
		var parser = new ChatResponseParser(px, commandProviders, factory.CreateLogger<ChatResponseParser>());
		var chat = new ChatGpt.ChatGpt(client, factory.CreateLogger<ChatGpt.ChatGpt>(), parser, camera);
		Console.WriteLine("Initialized");
		await chat.StartAsync();
		//ControllerBase.SetTest();
		//new KeyboardControl(px).Run();
	}
	public static void FillSine(short[] buffer, float frequency, float sampleRate, float gain = 1f)
	{
		for (int i = 0; i < buffer.Length; i++)
		{
			buffer[i] = (short)(MathF.Sin(i * frequency * MathF.PI * 2 / sampleRate) * gain * short.MaxValue);
		}
	}
}
