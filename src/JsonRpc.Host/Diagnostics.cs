using System;

namespace JsonRpc.Host
{
    /// <summary>
    /// A class for keeping execution times of particular request handling stages
    /// </summary>
    public class Diagnostics
    {
        public string RequestId { get; set; }
        public DateTime StartDate { get; set; }
        public long ReadTime { get; set; }
        public long ProcessTime { get; set; }
        public long WriteTime { get; set; }
        public long TotalTime => this.ReadTime + this.ProcessTime + this.WriteTime;
    }
}
