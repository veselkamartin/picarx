

namespace PicarX.Commands;

[Serializable]
internal class InvalidCommandException : Exception
{
	public InvalidCommandException()
	{
	}

	public InvalidCommandException(string? message) : base(message)
	{
	}

	public InvalidCommandException(string? message, Exception? innerException) : base(message, innerException)
	{
	}
}