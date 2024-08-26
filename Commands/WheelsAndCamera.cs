namespace SmartCar.Commands;

public class WheelsAndCamera : ICommandProvider
{
	public WheelsAndCamera(PicarX.Picarx picarx)
	{
		Commands = [
			new Camera(picarx),
			new Right(picarx),
			new Left(picarx),
			new Forward(picarx),
			new Back(picarx)
		];
	}

	public ICommand[] Commands { get; }

	class Camera(PicarX.Picarx picarx) : CommandBase
	{
		public override string Name => "CAMERA";

		public override Task Execute(string[] parameters)
		{
			ParseParams<int, int>(parameters, out var pan, out var tilt);

			picarx.SetCamPanAngle(pan);
			picarx.SetCamTiltAngle(tilt);
			return Task.CompletedTask;
		}
	}

	class Right(PicarX.Picarx picarx) : CommandBase
	{
		public override string Name => "RIGHT";

		public override async Task Execute(string[] parameters)
		{
			ParseParams<int>(parameters, out var angle);
			await picarx.Turn(angle);
		}
	}
	class Left(PicarX.Picarx picarx) : CommandBase
	{
		public override string Name => "LEFT";

		public override async Task Execute(string[] parameters)
		{
			ParseParams<int>(parameters, out var angle);
			await picarx.Turn(-angle);
		}
	}
	class Forward(PicarX.Picarx picarx) : CommandBase
	{
		public override string Name => "FORWARD";

		public override async Task Execute(string[] parameters)
		{
			ParseParams<int>(parameters, out var distanceInCm);
			await picarx.DirectForward(distanceInCm);
		}
	}
	class Back(PicarX.Picarx picarx) : CommandBase
	{
		public override string Name => "BACK";

		public override async Task Execute(string[] parameters)
		{
			ParseParams<int>(parameters, out var distanceInCm);
			await picarx.DirectBack(distanceInCm);
		}
	}
}
