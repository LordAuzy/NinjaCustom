using System;
using System.Collections.Generic;
using System.Linq;

namespace NinjaTrader.Custom.Strategies.DAustin.Common
{
    public class FixedSizeFifo<T>
    {
        private readonly Queue<T> queue;
        public int MaxSize { get; }

        public FixedSizeFifo(int maxSize)
        {
            if (maxSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxSize), "Max size must be greater than zero.");
            MaxSize = maxSize;
            queue = new Queue<T>(maxSize);
        }

        /// <summary>
        /// Adds a new item to the list. If the list is full, the oldest item is automatically removed.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(T item)
        {
            queue.Enqueue(item); // Add the new item to the back (rear).

            // If the queue exceeds the maximum size, remove the oldest item from the front.
            while (queue.Count > MaxSize)
            {
                queue.Dequeue(); // Remove the oldest item (first in).
            }
        }
        public T Get(int index)
        {
            return queue.ElementAt(index);
        }

        public T GetFromMostRecent(int index)
        {
            int actualIndex = queue.Count - 1 - index;
            if (actualIndex < 0)
            {
                actualIndex = 0;
            }
            return queue.ElementAt(actualIndex);
        }

        public void Clear()
        {
            queue.Clear();
        }

        /// <summary>
        /// Returns the current number of items in the list.
        /// </summary>
        public int Count => queue.Count;

        /// <summary>
        /// Allows iteration through the items in FIFO order.
        /// </summary>
        public IEnumerator<T> GetEnumerator() => queue.GetEnumerator();
    }

    public class ValueHistory : FixedSizeFifo<double>
    {
        #region Properties
        public bool IsReady { get { return (Count == MaxSize); } }
        #endregion

        #region constructors
        public ValueHistory(int maxSize) : base(maxSize) 
        { 
        
        }
        #endregion

        #region PublicMethods
        public double Average()
        {
            double average = 0;

            foreach (double range in this)
            {
                average += range;
            }

            average = average / Count;

            return average;
        }
        #endregion
    }
}