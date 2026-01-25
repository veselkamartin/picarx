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
			new Back(picarx)
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
            await picarx.Turn(angle);
            return CommandResult.OK;
        }
	}
	class Left(PicarX.Picarx picarx) : CommandBase
	{
		public override string Name => "LEFT";

        public override async Task<CommandResult> Execute(string[] parameters, CancellationToken ct)
        {
            ParseParams<int>(parameters, out var angle);
            await picarx.Turn(-angle);
            return CommandResult.OK;
        }
	}
	class Forward(PicarX.Picarx picarx) : CommandBase
	{
		public override string Name => "FORWARD";

        public override async Task<CommandResult> Execute(string[] parameters, CancellationToken ct)
        {
            ParseParams<int>(parameters, out var distanceInCm);
            await picarx.DirectForward(distanceInCm);
            return CommandResult.OK;
        }
	}
	class Back(PicarX.Picarx picarx) : CommandBase
	{
		public override string Name => "BACK";

        public override async Task<CommandResult> Execute(string[] parameters, CancellationToken ct)
        {
            ParseParams<int>(parameters, out var distanceInCm);
            await picarx.DirectBack(distanceInCm);
            return CommandResult.OK;
        }
	}
}
