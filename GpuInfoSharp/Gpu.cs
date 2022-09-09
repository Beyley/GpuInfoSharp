namespace GpuInfoSharp;

public abstract class Gpu {
	public readonly GpuVendor Vendor;

	public UnsafeLinkedList<GpuInfoSample> SampleInfo = new();

	internal Gpu(GpuVendor vendor) {
		this.Vendor = vendor;
	}

	public string Label {
		get;
		internal set;
	}

	public string LinkSpeed {
		get;
		internal set;
	}

	public string LinkWidth {
		get;
		internal set;
	}

	public string MaxLinkSpeed {
		get;
		internal set;
	}

	public string MaxLinkWidth {
		get;
		internal set;
	}

	public abstract GpuInfoSample CollectSample();
}
