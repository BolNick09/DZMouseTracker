using System.Collections.Concurrent;
using System.Threading;

namespace DZMouseTracker
{
    public partial class FrmMouseTracker : Form
    {
        public class MouseTracker
        {
            public Point currentPosition;
            public DateTime currentDateTime;

            public MouseTracker(Point currentPosition, DateTime currentDateTime)
            {
                this.currentPosition = currentPosition;
                this.currentDateTime = currentDateTime;
            }
            public override string ToString()
            {
                return $"X = {currentPosition.X} Y = {currentPosition.Y} Time = {currentDateTime}";
            }

        }
        public class MouseTrackerProducer
        {
            private BlockingCollection<MouseTracker> queue;
            private SemaphoreSlim semaphore;

            public MouseTrackerProducer(BlockingCollection<MouseTracker> queue, 
                                        SemaphoreSlim semaphore)
            {
                this.queue = queue;
                this.semaphore = semaphore;
            }
            public void TrackMouse(object sender, MouseEventArgs e)
            {
                MouseTracker mouseTracker = new MouseTracker(e.Location, DateTime.Now);
                queue.Add(mouseTracker);
                semaphore.Release();
                Thread.Sleep(100);
            }
        }

        public class MouseTrackerConsumer
        {

            private BlockingCollection<MouseTracker> queue;
            private SemaphoreSlim semaphore;
            private string filePath;

            public MouseTrackerConsumer(BlockingCollection<MouseTracker> queue, SemaphoreSlim semaphore, string filePath)
            {
                this.queue = queue;
                this.semaphore = semaphore;
                this.filePath = filePath;
            }

            public void StartConsuming()
            {
                while (true)
                {
                    semaphore.Wait(); 

                    if (queue.TryTake(out var mouseTracker, Timeout.Infinite))
                        File.AppendAllText(filePath, mouseTracker.ToString() + Environment.NewLine);
                    Thread.Sleep(100);
                }
            }
        }

        private BlockingCollection<MouseTracker> queue = new BlockingCollection<MouseTracker>();
        private SemaphoreSlim semaphore = new SemaphoreSlim(0, 4); 
        private MouseTrackerProducer producer;
        private MouseTrackerConsumer consumer;

        public FrmMouseTracker()
        {
            InitializeComponent();
            producer = new MouseTrackerProducer(queue, semaphore);
            consumer = new MouseTrackerConsumer(queue, semaphore, "Log.txt");

            var consumerThread = new Thread(consumer.StartConsuming);
            consumerThread.IsBackground = true; 
            consumerThread.Start();
            MouseMove += new MouseEventHandler(producer.TrackMouse);
        }

    }
}
