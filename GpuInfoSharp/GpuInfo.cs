using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace GpuInfoSharp;

public static class GpuInfo {
	private static Thread?                  _monitoringThread;
	private static Mutex?                   _runningMutex;
	private static Channel<ChannelMessage>? _channel;

	private static ConcurrentBag<Gpu> _foundGpus;

	public static ReadOnlyCollection<Gpu> GetFoundGpus() => new ReadOnlyCollection<Gpu>(_foundGpus.ToList());

	public static int SampleDelay {
		[MethodImpl(MethodImplOptions.Synchronized)]
		get;
		[MethodImpl(MethodImplOptions.Synchronized)]
		set;
	} = 100;

	private enum ChannelMessage {
		Stop
	}

	private static Regex _linuxDrmCardRegex = new("^card[0-9]+$");
	
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

			foreach (DirectoryInfo dirInfo in dirs) {
				//Did we find a valid card?
				if (_linuxDrmCardRegex.IsMatch(dirInfo.Name)) {
					HandleValidLinuxDrmCard(dirInfo);
				}
			}
		}
		else {
			//TODO
		}
	}
	
	private static void HandleValidLinuxDrmCard(DirectoryInfo drmDir) {
		DirectoryInfo[] dirs = drmDir.GetDirectories();

		//Try to find the device folder
		DirectoryInfo? deviceDir = null;
		foreach (DirectoryInfo dir in dirs) {
			if (dir.Name == "device")
				deviceDir = dir;
		}
		
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
	
	private static void HandleAmdGpu(DirectoryInfo drmDir) {
		_foundGpus.Add(new AmdGpu("Unknown"));
	}
	private static void HandleIntelGpu(DirectoryInfo drmDir) {
		string drmPath   = drmDir.FullName;
		string labelPath = Path.Combine(drmPath, "device/label");
		
		string label = File.Exists(labelPath) ? File.ReadAllText(labelPath).Trim() : "Unknown Intel GPU, no `device/label`";
		
		_foundGpus.Add(new IntelGpu(label));
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

			foreach (Gpu gpu in _foundGpus) {
				
			}

			Thread.Sleep(SampleDelay);
		}

		//Release the mutex
		_runningMutex.ReleaseMutex();
	}
}
