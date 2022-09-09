using System;
using System.IO;
using System.Text;

namespace GpuInfoSharp;

public class LinuxAmdGpu : Gpu {
	private readonly DirectoryInfo _drmDir;
	private readonly FileStream?   _vramTotal;
	private readonly FileStream?   _vramUsed;
	public LinuxAmdGpu(DirectoryInfo drmDir) : base(GpuVendor.Amd) {
		this._drmDir = drmDir;

		string vramTotalPath = Path.Combine(this._drmDir.FullName, "device/mem_info_vram_total");
		string vramUsedPath  = Path.Combine(this._drmDir.FullName, "device/mem_info_vram_used");

		this._vramTotal = File.Exists(vramTotalPath) ? File.OpenRead(vramTotalPath) : null;
		this._vramUsed  = File.Exists(vramUsedPath) ? File.OpenRead(vramUsedPath) : null;
	}
	public override GpuInfoSample CollectSample() {
		GpuInfoSample sample = new();

		byte[] buf = new byte[128];
		if (this._vramTotal != null) {
			this._vramTotal.Position = 0;
			this._vramTotal.Flush();
			int    read  = this._vramTotal.Read(buf, 0, buf.Length);
			byte[] final = new byte[read];
			Array.Copy(buf, final, read);

			sample.TotalVram = ulong.Parse(Encoding.UTF8.GetString(final).Trim());
		}
		if (this._vramUsed != null) {
			this._vramUsed.Position = 0;
			this._vramUsed.Flush();
			int    read  = this._vramUsed.Read(buf, 0, buf.Length);
			byte[] final = new byte[read];
			Array.Copy(buf, final, read);

			sample.UsedVram = ulong.Parse(Encoding.UTF8.GetString(final).Trim());
		}

		return sample;
	}
}
