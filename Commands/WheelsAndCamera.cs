using System.Threading;
using SmartCar.ChatGpt;

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
			new Back(picarx),
			new Stop(picarx),
		];
	}

	public ICommand[] Commands { get; }

	class Camera(PicarX.Picarx picarx) : CommandBase
	{
		public override string Name => "CAMERA";

		public override async Task<CommandResult> Execute(string[] parameters, CancellationToken ct)
		{
			ParseParams<int, int>(parameters, out var pan, out var tilt);

			picarx.SetCamPanAngle(pan);
			picarx.SetCamTiltAngle(tilt);
			await Task.Delay(500, ct);
			return CommandResult.OK;
		}
	}

	class Right(PicarX.Picarx picarx) : CommandBase
	{
		public override string Name => "RIGHT";

		public override async Task<CommandResult> Execute(string[] parameters, CancellationToken ct)
		{
			ParseParams<int>(parameters, out var angle);
			await picarx.Turn(angle, ct);
			return CommandResult.OK;
		}
	}
	class Left(PicarX.Picarx picarx) : CommandBase
	{
		public override string Name => "LEFT";

		public override async Task<CommandResult> Execute(string[] parameters, CancellationToken ct)
		{
			ParseParams<int>(parameters, out var angle);
			await picarx.Turn(-angle, ct);
			return CommandResult.OK;
		}
	}
	class Forward(PicarX.Picarx picarx) : CommandBase
	{
		public override string Name => "FORWARD";

		public override async Task<CommandResult> Execute(string[] parameters, CancellationToken ct)
		{
			ParseParams<int>(parameters, out var distanceInCm);
			var completed = await picarx.DirectForward(distanceInCm, ct);
			return completed ? CommandResult.OK : CommandResult.OBSTACLE;
		}
	}
	class Back(PicarX.Picarx picarx) : CommandBase
	{
		public override string Name => "BACK";

		public override async Task<CommandResult> Execute(string[] parameters, CancellationToken ct)
		{
			ParseParams<int>(parameters, out var distanceInCm);
			await picarx.DirectBack(distanceInCm, ct);
			return CommandResult.OK;
		}
	}
	class Stop(PicarX.Picarx picarx) : CommandBase
	{
		public override string Name => "STOP";

		public override async Task<CommandResult> Execute(string[] parameters, CancellationToken ct)
		{
			picarx.Stop();
			return CommandResult.OK;
		}
	}
}
