namespace SmartCar.PicarX;

public class StateProvider
{
	private readonly Picarx _px;

	public StateProvider(Picarx px)
	{
		_px = px;
	}

	public Task<string> GetState()
	{
		var distance = (int)_px.GetDistance();
		var state = $"MAX_FORWARD {distance}";
		return Task.FromResult(state);
	}
}
