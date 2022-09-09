using System.Collections.ObjectModel;

namespace GpuInfoSharp.Sample;

public static class Program {
	public static unsafe void Main(string[] args) {
		GpuInfo.StartMonitoring();

		ReadOnlyCollection<Gpu> gpus = GpuInfo.GetFoundGpus();
		foreach (Gpu foundGpu in gpus)
			Console.WriteLine(@$"Found GPU: Label:{foundGpu.Label}, Vendor:{foundGpu.Vendor}
	Link Speed: {foundGpu.LinkSpeed} 
	Link Width: {foundGpu.LinkWidth}
	Max Link Speed: {foundGpu.MaxLinkSpeed}
	Max Link Width: {foundGpu.MaxLinkWidth}");

		const int milisWaitForSamples = 1500;
		Console.WriteLine($" --- Waiting {milisWaitForSamples}ms for samples... ---");
		Thread.Sleep(milisWaitForSamples);
		
		gpus = GpuInfo.GetFoundGpus();
		foreach (Gpu gpu in gpus) {
			Console.WriteLine($"gpu {gpu.Vendor}:");
			UnsafeLinkedList<GpuInfoSample>.Node* node = gpu.SampleInfo.FirstElement;
		
			int i = 0;
			while (node != null) {
				Console.WriteLine($"	{i}: temp: {node->Contents.Temperature}C, usage: {node->Contents.Utilization:P}, vram: {node->Contents.UsedVram / 1024f / 1024f:N4}mb/{node->Contents.TotalVram / 1024f / 1024f:N4}mb");
		
				node = node->NextNode;
				i++;
			}
		}

		GpuInfo.StopMonitoring();
	}
}
