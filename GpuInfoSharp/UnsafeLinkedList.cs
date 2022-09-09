using System;
using System.Runtime.InteropServices;

namespace GpuInfoSharp;

public unsafe class UnsafeLinkedList <T> where T : unmanaged {
	public Node* FirstElement;
	public Node* LastElement;

	public int Length { get; private set; }

	public Node* AppendToEnd(T contents) {
		Node* node = (Node*)Marshal.AllocHGlobal(sizeof(Node));

		node->Contents = contents;
		node->NextNode = null;
		node->PrevNode = null;

		lock (this) {
			if (this.FirstElement == null || this.LastElement == null) {
				this.FirstElement = node;
				this.LastElement  = node;
				return node;
			}

			this.LastElement->NextNode = node;

			this.LastElement = node;

			this.Length++;
		}

		return node;
	}

	public Node* AppendToStart(T contents) {
		Node* node = (Node*)Marshal.AllocHGlobal(sizeof(Node));

		node->Contents = contents;
		node->NextNode = null;
		node->PrevNode = null;

		lock (this) {
			if (this.FirstElement == null || this.LastElement == null) {
				this.FirstElement = node;
				this.LastElement  = node;
				return node;
			}

			this.FirstElement->PrevNode = node;

			this.FirstElement = node;

			this.Length++;
		}

		return node;
	}

	public void RemoveEnd() {
		lock (this) {
			Node* newLast = this.LastElement->PrevNode;

			this.LastElement->PrevNode->NextNode = null;

			Marshal.FreeHGlobal((IntPtr)this.LastElement);

			this.LastElement = newLast;

			this.Length--;
		}
	}

	public void RemoveStart() {
		lock (this) {
			Node* newFirst = this.FirstElement->NextNode;

			this.FirstElement->NextNode->PrevNode = null;

			Marshal.FreeHGlobal((IntPtr)this.FirstElement);

			this.FirstElement = newFirst;

			this.Length--;
		}
	}

	public struct Node {
		public T Contents;

		public Node* NextNode;
		public Node* PrevNode;
	}
}
