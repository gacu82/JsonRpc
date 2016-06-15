namespace JsonRpc.Host
{
    /// <summary>
    /// A class for keeping execution times of particular request handling stages
    /// </summary>
    public class Diagnostics
    {
        public long ReadingTime { get; set; }
        public long ProcessingTime { get; set; }
        public long WritingTime { get; set; }
        public long TotalTime
        {
            get
            {
                return this.ReadingTime + this.ProcessingTime + this.WritingTime;
            }
        }  
    }
}
