
namespace SmartCar.Media
{
	public interface ISoundPlayer
	{
		Task PlaySoundOnSpeaker(byte[] data);
	}
}