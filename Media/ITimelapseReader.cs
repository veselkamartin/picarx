
namespace SmartCar.Media
{
	public interface ITimelapseReader
	{
		Task<byte[]> Read();
		void Stop();
	}
}