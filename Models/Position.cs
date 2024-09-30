namespace MyOrderMaster.Models
{ 
    public class Position
    {
        public string MarketType { get; set; }
        public string Account { get; set; }
        public string ContractID { get; set; }
        public int Size { get; set; }
        public double AvgCost { get; set; }
        public double TickValue { get; set; }
    }
}
