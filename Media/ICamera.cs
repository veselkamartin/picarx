namespace SmartCar.Media
{
	public interface ICamera : IDisposable
	{
		Task<byte[]> GetPictureAsJpeg();
	}
}