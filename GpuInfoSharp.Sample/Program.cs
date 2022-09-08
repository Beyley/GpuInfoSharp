using System.Collections.ObjectModel;

namespace GpuInfoSharp.Sample;

public static class Program {
	public static void Main(string[] args) {
		GpuInfo.StartMonitoring();

		ReadOnlyCollection<Gpu> gpus = GpuInfo.GetFoundGpus();
		foreach (Gpu foundGpu in gpus) {
			Console.WriteLine($"Found GPU: {foundGpu.Label}:{foundGpu.Vendor}");
		}
		
		GpuInfo.StopMonitoring();
	}
}