namespace GpuInfoSharp;

public class LinuxIntelGpu : Gpu {
	public LinuxIntelGpu() : base(GpuVendor.Intel) {}
	public override GpuInfoSample CollectSample() {
		return new GpuInfoSample { TotalVram = 0, UsedVram = 0 };
	}
}
