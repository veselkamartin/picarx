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

	public Task<int> GetDistance()
	{
		var distance = (int)_px.GetDistance();
		return Task.FromResult(distance);
	}
}
