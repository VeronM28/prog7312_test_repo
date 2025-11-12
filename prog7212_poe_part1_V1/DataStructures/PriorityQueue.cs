// DataStructures/PriorityQueue.cs

using prog7212_poe_part1_V1.Models;
using System;
using System.Collections.Generic;

namespace prog7212_poe_part1_V1.DataStructures
{
    public class RequestPriorityQueue
    {
        private List<ServiceRequestModel> heap;
        private bool isMaxHeap;

        public int Count => heap.Count;

        public RequestPriorityQueue(bool maxHeap = true)
        {
            heap = new List<ServiceRequestModel>();
            isMaxHeap = maxHeap;
        }

        public void Enqueue(ServiceRequestModel request)
        {
            heap.Add(request);
            int currentIndex = heap.Count - 1;
            HeapifyUp(currentIndex);
        }

        public ServiceRequestModel Dequeue()
        {
            if (heap.Count == 0)
                throw new InvalidOperationException("Queue is empty");

            var firstItem = heap[0];
            heap[0] = heap[heap.Count - 1];
            heap.RemoveAt(heap.Count - 1);
            HeapifyDown(0);

            return firstItem;
        }

        public ServiceRequestModel Peek()
        {
            if (heap.Count == 0)
                throw new InvalidOperationException("Queue is empty");
            return heap[0];
        }

        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                if (Compare(heap[index], heap[parentIndex]) > 0)
                {
                    Swap(index, parentIndex);
                    index = parentIndex;
                }
                else
                {
                    break;
                }
            }
        }

        private void HeapifyDown(int index)
        {
            while (true)
            {
                int leftChild = 2 * index + 1;
                int rightChild = 2 * index + 2;
                int largest = index;

                if (leftChild < heap.Count && Compare(heap[leftChild], heap[largest]) > 0)
                    largest = leftChild;

                if (rightChild < heap.Count && Compare(heap[rightChild], heap[largest]) > 0)
                    largest = rightChild;

                if (largest != index)
                {
                    Swap(index, largest);
                    index = largest;
                }
                else
                {
                    break;
                }
            }
        }

        private int Compare(ServiceRequestModel a, ServiceRequestModel b)
        {
            int comparison = a.PriorityScore.CompareTo(b.PriorityScore);
            return isMaxHeap ? comparison : -comparison;
        }

        private void Swap(int i, int j)
        {
            var temp = heap[i];
            heap[i] = heap[j];
            heap[j] = temp;
        }

        // Explanation of Heap Role:
        // The Priority Queue (implemented as a binary heap) ensures that high-priority service requests
        // are processed first. This is crucial for municipal services where emergency requests
        // (like water main breaks) need immediate attention.
        // Example: When an admin checks for pending requests, they see the most critical ones first,
        // improving response time for urgent matters.
    }
}