using System;
using System.IO;
using System.Text;

namespace GpuInfoSharp;

public class LinuxAmdGpu : Gpu {
	private readonly DirectoryInfo _drmDir;

	private readonly FileStream? _vramTotal;
	private readonly FileStream? _vramUsed;
	private readonly FileStream? _utilization;
	private readonly FileStream? _temperature;

	public LinuxAmdGpu(DirectoryInfo drmDir) : base(GpuVendor.Amd) {
		this._drmDir = drmDir;

		string vramTotalPath   = Path.Combine(this._drmDir.FullName, "device/mem_info_vram_total");
		string vramUsedPath    = Path.Combine(this._drmDir.FullName, "device/mem_info_vram_used");
		string utilizationPath = Path.Combine(this._drmDir.FullName, "device/gpu_busy_percent");
		string temperaturePath = Path.Combine(this._drmDir.FullName, "device/hwmon/hwmon4/temp1_input");

		this._vramTotal   = File.Exists(vramTotalPath) ? File.OpenRead(vramTotalPath) : null;
		this._vramUsed    = File.Exists(vramUsedPath) ? File.OpenRead(vramUsedPath) : null;
		this._utilization = File.Exists(utilizationPath) ? File.OpenRead(utilizationPath) : null;
		this._temperature = File.Exists(temperaturePath) ? File.OpenRead(temperaturePath) : null;
	}

	private readonly byte[] _buf = new byte[128];
	public override GpuInfoSample CollectSample() {
		GpuInfoSample sample = new();

		if (this._vramTotal != null) {
			this._vramTotal.Position = 0;
			this._vramTotal.Flush();
			int    read  = this._vramTotal.Read(this._buf, 0, this._buf.Length);
			byte[] final = new byte[read];
			Array.Copy(this._buf, final, read);

			sample.TotalVram = ulong.Parse(Encoding.UTF8.GetString(final).Trim());
		}
		if (this._vramUsed != null) {
			this._vramUsed.Position = 0;
			this._vramUsed.Flush();
			int    read  = this._vramUsed.Read(this._buf, 0, this._buf.Length);
			byte[] final = new byte[read];
			Array.Copy(this._buf, final, read);

			sample.UsedVram = ulong.Parse(Encoding.UTF8.GetString(final).Trim());
		}
		if (this._utilization != null) {
			this._utilization.Position = 0;
			this._utilization.Flush();
			int    read  = this._utilization.Read(this._buf, 0, this._buf.Length);
			byte[] final = new byte[read];
			Array.Copy(this._buf, final, read);

			sample.Utilization = float.Parse(Encoding.UTF8.GetString(final).Trim()) / 100f;
		}
		if (this._temperature != null) {
			this._temperature.Position = 0;
			this._temperature.Flush();
			int    read  = this._temperature.Read(this._buf, 0, this._buf.Length);
			byte[] final = new byte[read];
			Array.Copy(this._buf, final, read);

			sample.Temperature = (short)(int.Parse(Encoding.UTF8.GetString(final).Trim()) / 1000);
		}

		return sample;
	}
}
