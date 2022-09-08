namespace GpuInfoSharp; 

public abstract class Gpu {
	public readonly GpuVendor Vendor;
	public readonly string    Label;
	
	internal Gpu(GpuVendor vendor, string label) {
		this.Vendor = vendor;
		this.Label  = label;
	}
}
