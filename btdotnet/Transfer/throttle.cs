using System;
using Threading = System.Threading;


namespace BitTorrent
{
	public delegate void RateChangeCallback(float newRate);

	/// <summary>
	/// Keeps track of the amount of data shifted in a certain timeframe and blocks to
	/// keep data flow constant
	/// </summary>
	public class Throttle
	{
		public float MaxRate = -1.0f;

		private float actualRate = 0.0f;

		public float ActualRate
		{
			get { return this.actualRate; }
		}


		public event RateChangeCallback RateChange;


		private System.Collections.Queue pastSegmentThroughput = new System.Collections.Queue();
		private int totalAmount = 0, segmentAmount = 0, totalSegmentQueueAmount = 0;
		private Threading.Timer segmentTimer = null;


		public Throttle()
		{
		}


		public void OnSegmentDone(object state)
		{
			pastSegmentThroughput.Enqueue(segmentAmount);
			totalSegmentQueueAmount += segmentAmount;

			if (pastSegmentThroughput.Count > 5)
				totalSegmentQueueAmount -= (int)pastSegmentThroughput.Dequeue();


			segmentAmount = 0;

			if (totalSegmentQueueAmount > 0)
				actualRate = ((float)totalSegmentQueueAmount) / 5000.0f;
			else
				actualRate = 0.0f;

			if (RateChange != null)
				RateChange(actualRate);
		}


		public void Start()
		{
			segmentTimer = new Threading.Timer(new Threading.TimerCallback(OnSegmentDone), null,
				1000, 1000);
		}


		public void Stop()
		{
			segmentTimer.Dispose();
		}


		public void AddData(int size)
		{
			totalAmount += size;
			segmentAmount += size;
		}


	}
}
