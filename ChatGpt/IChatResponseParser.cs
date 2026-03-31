namespace SmartCar.ChatGpt;

/// <summary>
/// Interface for parsing streaming text output from the model.
/// </summary>
public interface IChatResponseParser
{
	/// <summary>
	/// Adds streaming text to the parser buffer and processes it.
	/// </summary>
	/// <param name="text">Text chunk from model output stream</param>
	Task Add(string text);

	/// <summary>
	/// Processes any remaining buffered text and finalizes the current batch.
	/// </summary>
	Task Finish();
}
