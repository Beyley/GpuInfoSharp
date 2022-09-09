using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;

namespace GpuInfoSharp;

public static class GpuInfo {
	private static Thread?                  _monitoringThread;
	private static Mutex?                   _runningMutex;
	private static Channel<ChannelMessage>? _channel;

	private static ConcurrentBag<Gpu> _foundGpus;

	private static readonly Regex _linuxDrmCardRegex = new("^card[0-9]+$");

	public static int SampleDelay {
		[MethodImpl(MethodImplOptions.Synchronized)]
		get;
		[MethodImpl(MethodImplOptions.Synchronized)]
		set;
	} = 100;

	public static ReadOnlyCollection<Gpu> GetFoundGpus() {
		return new(_foundGpus.ToList());
	}

	public static void StartMonitoring() {
		if (_monitoringThread != null)
			throw new InvalidOperationException();

		_foundGpus = new ConcurrentBag<Gpu>();
		FindGpus();

		_channel          = Channel.CreateUnbounded<ChannelMessage>();
		_runningMutex     = new Mutex(true, "GpuInfoRunningMutex");
		_monitoringThread = new Thread(MonitoringThreadRun);

		_runningMutex.ReleaseMutex();

		_monitoringThread.Start();
	}

	private static void FindGpus() {
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
			DirectoryInfo info = new("/sys/class/drm/");

			IEnumerable<DirectoryInfo> dirs = info.EnumerateDirectories();

			foreach (DirectoryInfo dirInfo in dirs)
				//Did we find a valid card?
				if (_linuxDrmCardRegex.IsMatch(dirInfo.Name))
					HandleValidLinuxDrmCard(dirInfo);
		}
		//TODO
	}

	private static void HandleValidLinuxDrmCard(DirectoryInfo drmDir) {
		DirectoryInfo[] dirs = drmDir.GetDirectories();

		//Try to find the device folder
		DirectoryInfo? deviceDir = null;
		foreach (DirectoryInfo dir in dirs)
			if (dir.Name == "device")
				deviceDir = dir;

		//If we didnt find a device folder, just ignore this card
		if (deviceDir == null)
			return;

		string vendorPath = Path.Combine(deviceDir.FullName, "vendor");

		if (File.Exists(vendorPath)) {
			string vendorString = File.ReadAllText(vendorPath).Trim();

			int vendor = Convert.ToInt32(vendorString, 16);

			switch (vendor) {
				case 0x1002: {
					//Amd GPU vendor seems to always be 0x1002, so we'll use that 
					HandleAmdGpu(drmDir);
					break;
				}
				case 0x8086: {
					//8086 might be used by more things... need to look into this more
					HandleIntelGpu(drmDir);
					break;
				}
				default:
					//Unknown GPU vendor!
					return;
			}
		}
	}

	private static string? GetLinkSpeed(DirectoryInfo drmDir) {
		string  linkSpeedPath = Path.Combine(drmDir.FullName, "device/current_link_speed");
		string? linkSpeed     = File.Exists(linkSpeedPath) ? File.ReadAllText(linkSpeedPath).Trim() : null;

		return linkSpeed;
	}

	private static string? GetLinkWidth(DirectoryInfo drmDir) {
		string  linkWidthPath = Path.Combine(drmDir.FullName, "device/current_link_width");
		string? linkWidth     = File.Exists(linkWidthPath) ? File.ReadAllText(linkWidthPath).Trim() : null;

		return linkWidth;
	}

	private static void HandleAmdGpu(DirectoryInfo drmDir) {
		string productNamePath = Path.Combine(drmDir.FullName, "device/product_name");
		string productName     = File.Exists(productNamePath) ? File.ReadAllText(productNamePath).Trim() : "Unknown Product Name";

		string productNumberPath = Path.Combine(drmDir.FullName, "device/product_number");
		string productNumber     = File.Exists(productNumberPath) ? File.ReadAllText(productNumberPath).Trim() : "Unknown Product Number";

		_foundGpus.Add(new AmdGpu {
			Label     = $"Name: {productName}, Product Number: {productNumber}",
			LinkSpeed = GetLinkSpeed(drmDir) ?? "Unknown Link Speed!",
			LinkWidth = GetLinkWidth(drmDir) ?? "Unknown Link Width!"
		});
	}
	private static void HandleIntelGpu(DirectoryInfo drmDir) {
		string drmPath   = drmDir.FullName;
		string labelPath = Path.Combine(drmPath, "device/label");

		string label = File.Exists(labelPath) ? File.ReadAllText(labelPath).Trim() : "Unknown Intel GPU, no `device/label`";

		_foundGpus.Add(new IntelGpu {
			Label     = label,
			LinkSpeed = GetLinkSpeed(drmDir) ?? "Unknown Link Speed!",
			LinkWidth = GetLinkWidth(drmDir) ?? "Unknown Link Width!"
		});
	}

	public static void StopMonitoring() {
		Debug.Assert(_runningMutex != null, "_runningMutex != null");
		Debug.Assert(_channel      != null, "_channel != null");

		_channel!.Writer.TryWrite(ChannelMessage.Stop);
		_channel.Writer.Complete();

		//Wait until the thread releases the mutex...
		_runningMutex!.WaitOne();
		//Release it then dispose it
		_runningMutex.ReleaseMutex();
		_runningMutex.Dispose();

		_monitoringThread = null;
		_runningMutex     = null;
		_channel          = null;
	}


	private static void MonitoringThreadRun(object obj) {
		Debug.Assert(_runningMutex != null, "_runningMutex != null");
		Debug.Assert(_channel      != null, "_channel != null");

		//Get the mutex
		_runningMutex!.WaitOne();

		for (;;) {
			if (_channel!.Reader.TryRead(out ChannelMessage message))
				if (message == ChannelMessage.Stop)
					break;

			foreach (Gpu gpu in _foundGpus) {}

			Thread.Sleep(SampleDelay);
		}

		//Release the mutex
		_runningMutex.ReleaseMutex();
	}

	private enum ChannelMessage {
		Stop
	}
}
