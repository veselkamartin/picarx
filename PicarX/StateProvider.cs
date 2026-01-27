namespace SmartCar.PicarX;

public class StateProvider
{
	private readonly Picarx _px;
	private bool _isExecuting;

	public StateProvider(Picarx px)
	{
		_px = px;
	}

	public bool IsExecuting
	{
		get => _isExecuting;
		set => _isExecuting = value;
	}

	public Task<string> GetState()
	{
		var distance = (int)_px.GetDistance();
		var state = $"MAX_FORWARD {distance}";
		return Task.FromResult(state);
	}
}
