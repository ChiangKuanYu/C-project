namespace MyOrderMaster.Models
{
    public class Symbol
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string MarketType { get; set; }  
        public string PriceFormat { get; set; }
        public int Session1BeginTime { get; set; }
        public int Session1EndTime { get; set; }
        public int Session2BeginTime { get; set; }
        public int Session2EndTime { get; set; }
        public double PIP { get; set; }

        public string FullName
        {
            get { return $"{ID,-10} {Name}"; }
        }

        public Symbol()
        {

        }

        public Symbol(string id, string name, string code, string marketType, string format, double pip, int time1, int time2, int time3, int time4)
        {           
            ID = id;
            Name = name;
            Code = code;
            MarketType = marketType;
            PriceFormat = format;
            PIP = pip;
            Session1BeginTime = time1;
            Session1EndTime = time2;
            Session2BeginTime = time3;
            Session2EndTime = time4;
        }
    }
}
