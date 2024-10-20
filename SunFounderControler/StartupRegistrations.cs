﻿using SmartCar.Media;
using System.Text;

namespace SmartCar.SunFounderControler
{
	public static class StartupRegistrations
	{

		public static void ConfigureSunFounderControler(this WebApplicationBuilder builder)
		{
			builder.WebHost.UseUrls("http://+:8765");
			builder.Services.AddSingleton<ControlerHandler>();
		}
		public static void UseSunFounderControler(this WebApplication app)
		{
			app.UseWebSockets();

			var controlerHandler = app.Services.GetRequiredService<ControlerHandler>();

			CancellationTokenSource? cancelSource = null;
			app.Use(async (context, next) =>
			{
				if (context.Request.Path == "/" && context.WebSockets.IsWebSocketRequest)
				{
					using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
					Console.WriteLine($"Client connected:" + context.Connection.RemoteIpAddress);
					context.RequestAborted.Register(() => Console.WriteLine("Aborting"));
					cancelSource = new CancellationTokenSource();
					await controlerHandler.Handle(webSocket, cancelSource.Token);
				}
				else
				{
					await next(context);
				}
			});

			app.MapGet("/mjpg", async (HttpContext context, ICamera camera) =>
			{

				context.Response.ContentType = "multipart/x-mixed-replace; boundary=frame";
				context.Response.Headers.Append("Access-Control-Allow-Origin", "*");

				var cameraReader = await camera.CaptureTimelapse();
				var cancellationToken = context.RequestAborted;
				try
				{

					while (!cancellationToken.IsCancellationRequested)
					{
						byte[] frame = await cameraReader.Read();

						await context.Response.Body.WriteAsync(Encoding.ASCII.GetBytes("--frame\r\n"), cancellationToken);
						await context.Response.Body.WriteAsync(Encoding.ASCII.GetBytes("Content-Type: image/jpeg\r\n\r\n"), cancellationToken);
						await context.Response.Body.WriteAsync(frame, cancellationToken);
						await context.Response.Body.WriteAsync(Encoding.ASCII.GetBytes("\r\n"), cancellationToken);
						//send twice to fix chrome bug
						await context.Response.Body.WriteAsync(Encoding.ASCII.GetBytes("--frame\r\n"), cancellationToken);
						await context.Response.Body.WriteAsync(Encoding.ASCII.GetBytes("Content-Type: image/jpeg\r\n\r\n"), cancellationToken);
						await context.Response.Body.WriteAsync(frame, cancellationToken);
						await context.Response.Body.WriteAsync(Encoding.ASCII.GetBytes("\r\n"), cancellationToken);

						await context.Response.Body.FlushAsync(cancellationToken);

						await Task.Delay(100, cancellationToken);
					}
				}
				catch (Exception ex)
				{
					cancelSource?.Cancel();
					cameraReader.Stop();
					throw;
				}

				cameraReader.Stop();
				cancelSource?.Cancel();
			});

			app.MapGet("/mjpg.jpg", async (HttpContext context, ICamera camera) =>
			{
				byte[] frame = await camera.GetPictureAsJpeg();

				context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
				context.Response.ContentType = "image/jpeg";
				await context.Response.Body.WriteAsync(frame);
			});
		}
	}

}
