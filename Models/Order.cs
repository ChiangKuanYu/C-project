using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace MyOrderMaster.Models
{
    public class Order : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        } 
       
        private string keyNo;                           // 委託序號
        public string KeyNo
        {
            get { return keyNo; }
            set
            {
                if (keyNo != value)
                {
                    keyNo = value;
                    RaisePropertyChanged("KeyNo");
                }
            }
        }
        public string AccountNo { get; set; }           // 交易帳號
        public string MarketType { get; set; }          // TS:證券 TA:盤後 TL:零股 TF:期貨 TO:選擇權 OF:海期 OO:海選 OS:複委託

        public string OrderType { get; set; }           // 市價單/限價單/停損單/停損限價單/收市單
        
        public string OrderDate { get; set; }           // 委託日期

        private string orderTime;                       // 委託時間
        public string OrderTime
        {
            get { return orderTime; }
            set
            {
                if (orderTime != value)
                {
                    orderTime = value;
                    RaisePropertyChanged("OrderTime");
                }
            }
        }

        public string Symbol { get; set; }              // 委託商品代號 

        public string BuySell { get; set; }             // 買進或賣出
        public int OrderQty { get; set; }               // 委託數量: TS TA張數/ TL股數/ TF TO 口數
        public double OrderPrice { get; set; }          // 委託價格

        private string orderStatus;                     // 委託狀態
        public string OrderStatus
        {
            get { return orderStatus; }
            set
            {
                if (orderStatus != value)
                {
                    orderStatus = value;
                    RaisePropertyChanged("OrderStatus");
                }
            }
        }      

        private string statusChangeTime;                // 最近一次委託狀態發生的時間
        public string StatusChangeTime
        {
            get { return statusChangeTime; }
            set
            {
                if (statusChangeTime != value)
                {
                    statusChangeTime = value;
                    RaisePropertyChanged("StatusChangeTime");
                }
            }
        }

        private int dealQty;                            // 成交數量
        public int DealQty            
        {
            get { return dealQty; }
            set
            {
                if (dealQty != value)
                {
                    dealQty = value;
                    RaisePropertyChanged("DealQty");
                }
            }
        }
        
        private int remainQty;                          // 尚未成交的數量
        public int RemainQty
        {
            get { return remainQty; }
            set
            {
                if (remainQty != value)
                {
                    remainQty = value;
                    RaisePropertyChanged("RemainQty");
                }
            }
        }
        
        private double dealPrice;                       // 平均成交價
        public double DealPrice
        {
            get { return dealPrice; }
            set
            {
                if (dealPrice != value)
                {
                    dealPrice = value;
                    RaisePropertyChanged("DealPrice");
                }
            }
        }

        public double TriggerPrice { get; set; }        // 停損單的觸發價格, 限價單則為空白

        private string orderNo;                         // 委託書號    
        public string OrderNo
        {
            get { return orderNo; }
            set
            {
                if (orderNo != value)
                {
                    orderNo = value;
                    RaisePropertyChanged("OrderNo");
                }
            }
        }

        private string okSeq;                           // 成交序號
        public string OKSeq
        {
            get { return okSeq; }
            set
            {
                if (okSeq != value)
                {
                    okSeq = value;
                    RaisePropertyChanged("OKSeq");
                }
            }
        }      
    }
}
