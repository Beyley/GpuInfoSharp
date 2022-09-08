namespace GpuInfoSharp; 

public class AmdGpu : Gpu {
	public AmdGpu(string label) : base(GpuVendor.Amd, label) {
		
	}
}
