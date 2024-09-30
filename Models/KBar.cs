namespace MyOrderMaster.Models
{
    public class KBar
    {
        //public string Symbol { get; set; }　　// 商品代號       
        public int TimeStamp { get; set; }      // KBar結束時點 hhmm    
        public double Open { get; set; }  
        public double High { get; set; }　
        public double Low { get; set; }　　 
        public double Close { get; set; }
        public int Volume { get; set; }

        public KBar() {}

        public KBar(int timeStamp, double open, double high, double low, double close, int volume)
        {
            TimeStamp = timeStamp;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
        }        
    }
}
