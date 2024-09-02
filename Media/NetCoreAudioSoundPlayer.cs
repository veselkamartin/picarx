﻿namespace SmartCar.Media;

public class NetCoreAudioSoundPlayer : ISoundPlayer
{
	public async Task PlaySoundOnSpeaker(byte[] data)
	{
		//NAudio (Windows only):
		//var stream = streamData.ToStream();
		//using var provider = new Mp3FileReader(stream);
		//using var outputDevice = new NAudio.Wave. WaveOutEvent();
		//outputDevice.Init(provider);
		//outputDevice.Play();
		//while (outputDevice.PlaybackState == PlaybackState.Playing)
		//{
		//    await Task.Delay(100);
		//}

		//NetCoreAudio:
		var tempPath = Path.GetTempPath();
		var tempFile = Path.Join(tempPath, Guid.NewGuid().ToString() + ".wav");
		await File.WriteAllBytesAsync(tempFile, data);
		var player = new NetCoreAudio.Player();
		await player.Play(tempFile);
		while (player.Playing)
		{
			await Task.Delay(100);
		}
	}
}
