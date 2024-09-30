namespace MyOrderMaster.Models
{
    public class Tick
    {
        //public string Symbol { get; set; }　　　　　//商品代號        
        public int TickPtr { get; set; }
        public int TickTime { get; set; }　          // 成交時間 hhmmss
        public double TickPrice { get; set; }　　　　// 成交價格
        public int TickQty { get; set; }　　　　　   // 成交單量
        public int TickSimulate { get; set; }

        public double TickBid { get; set; }　　　　// 買價
        public double TickAsk { get; set; }　　　　// 賣價

        public Tick(int ptr, int time, int price, int qty, int simulate, int bid, int ask)
        {
            TickPtr = ptr;
            TickTime = time;
            TickPrice = price / 100.0;
            TickQty = qty;
            TickSimulate = simulate;
            TickBid = bid / 100.0;
            TickAsk = ask / 100.0;
        }
    }
}
