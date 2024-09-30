
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Windows.Forms;
using SKCOMLib;
using MyOrderMaster.Models;
using System.Threading;

namespace MyOrderMaster
{
    public partial class MainForm : Form
    {
        #region Variables
        SKCenterLib skCenter = new SKCenterLib();
        SKOrderLib skOrder = new SKOrderLib();
        SKReplyLib skReply = new SKReplyLib();
        SKQuoteLib skQuote = new SKQuoteLib();

        DataTable portfolioQuoteTable;
        List<Portfolio> portfolioList;
        List<Symbol> symbolList;
        List<Symbol> domSymbolList;
        List<Symbol> orderSymbolList;

        List<Tick> ticks = new List<Tick>();
        BindingList<KBar> kBars = new BindingList<KBar>();
        BindingList<Order> orderBook = new BindingList<Order>();       
        BindingList<Position> openPositions = new BindingList<Position>();
               
        DateTime chartLastUpdatedTime;

        bool loginSuccessful = false;
        bool comboBoxPopulated = false;
        bool stopOrderReplyCompleted = false;
        int stopReplyPass = 1;
        bool replyCompleted = false;
                
        int portfolioIndex;
        string nearMonth;
        string stockID;
        string TDate = DateTime.Now.Date.ToString("yyyy-MM-dd");

        Symbol specificSymbol = new Symbol();
        Symbol previousSpecificSymbol = new Symbol();
        short specificStockIdx;
        double specificDenom;
        double specificRef;
        int firstSpecificLiveTick;
        int session1BeginTime;
        int session1EndTime;
        int session2BeginTime;
        int session2EndTime;

        int[] domLadder = new int[40];        
        Symbol domSymbol = new Symbol();
        Symbol previousDOMSymbol = new Symbol();
        short domStockIdx;
        double domDenom;
        int firstDOMLiveTick;
        double domBestBid = 99999;
        double domBestAsk = -1;
        int domLastTickRow = -1;
      
        #endregion

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            portfolioQuoteTable = CreatePortfolioQuoteTable();
            InitializeDataGridViews();            
            PopulateComboBoxes();
            InitializeChart();
            HookupEventHandlers();
            TBarsOnlyCheckBox.Checked = IsTBarsOnly();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            User.GetUserInfo();
            if (User.ID != string.Empty)
                LogIntoSystem();
            else
                SetUser();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.PortfolioIndex = portfolioIndex;
            Properties.Settings.Default.SpecificID = specificSymbol.ID;
            Properties.Settings.Default.DOMID = domSymbol.ID;
            Properties.Settings.Default.NearMonth = nearMonth;
            Properties.Settings.Default.Save();
        }

        #region Initialization Routines       
        private void InitializeDataGridViews()
        {
            InitializePortfolioQuoteGrid();
            InitializeOrderBookGrid();
            InitializeOpenPositionGrid();

            SetSpecificQuoteGrid();
            SetBest5Grid();
            SetTradeInfoGrid();
            SetDOMGrid();
        }
        
        private void PopulateComboBoxes()
        {
            portfolioList = GetPortfolioList();
            symbolList = GetSymbolList().OrderBy(s => s.ID).ToList();
            orderSymbolList = symbolList.Where(s => s.ID != "TSEA" && s.ID != "OTCA").ToList();
            //domSymbolList = symbolList.Where(s => s.ID != "TSEA" && s.ID != "OTCA").ToList();
            domSymbolList = symbolList.Where(s => s.MarketType == "TF").ToList();         

            comboBoxPopulated = false;
            PopupatePortfolioComboBox();
            PopulateSpecificComboBox();
            PopulateOrderSymbolComboBox();
            PopulateDOMComboBoxes();
            comboBoxPopulated = true;
        }
        
        private void InitializeChart()
        {
            chart.Series["Prices"].XValueMember = "TimeStamp";
            chart.Series["Prices"].YValueMembers = "High,Low,Open,Close";
            chart.Series["Volume"].XValueMember = "TimeStamp";
            chart.Series["Volume"].YValueMembers = "Volume";            
            chart.Series["Prices"]["MaximumPixelPointWidth"] = "20";
            chart.Series["Volume"]["MaximumPixelPointWidth"] = "20";
            chart.DataSource = kBars;           
        }
        
        private void HookupEventHandlers()
        {
            skOrder.OnAccount += OnReceiveAccount;
            //skOrder.OnOpenInterest += OnReceiveFuturePosition;
            //skOrder.OnFutureRights += OnReceiveFutureEquity;
            //skOrder.OnStopLossReport += OnReceiveStopLossReport;

            //skReply.OnComplete += OnReceiveReplyComplete;
            //skReply.OnConnect += OnReceiveReplyConnect;
            //skReply.OnNewData += OnReceiveReplyData;
            //skReply.OnDisconnect += OnReceiveReplyDisconnect;
            //skReply.OnReplyClear += OnReceiveReplyClear;

            //skQuote.OnConnection += OnReceiveQuoteConnect;
            //SKQuote.OnNotifyServerTime += OnReceiveServerTime;
            //skQuote.OnNotifyQuote += OnReceiveQuote;
            //skQuote.OnNotifyHistoryTicks += OnReceiveHistoryTicks;
            //skQuote.OnNotifyTicks += OnReceiveTicks;
            //skQuote.OnNotifyBest5 += OnReceiveBest5;
            //skQuote.OnNotifyFutureTradeInfo += OnReceiveFutureTradeInfo;
        }

        private bool IsTBarsOnly()
        {
            DateTime now = DateTime.Now;
            int hhmm = now.Hour * 100 + now.Minute;
            if (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday || hhmm < 800 || hhmm > 1440)
                return false;
            else
                return true;
        }

        void m_pSKReply_OnAnnouncement(string strUserID, string bstrMessage, out short nConfirmCode)
        {
            nConfirmCode = -1;
        }

        private void LogIntoSystem()
        {
            bool isFirst = true;
            if (isFirst == true)
            {
                // 註冊公告事件
                skReply.OnReplyMessage += new _ISKReplyLibEvents_OnReplyMessageEventHandler(this.m_pSKReply_OnAnnouncement);

                isFirst = false;
            }

            loginSuccessful = false;
            int code = skCenter.SKCenterLib_Login(User.ID, User.Password);
            WriteMessage("API", "Login", code);

            if (code != 0)                 
                MessageBox.Show("無法登入, 請確定帳號是否正確及網路是否正常", "錯誤訊息");
            else
            {                
                code = code + skOrder.SKOrderLib_Initialize();
                WriteMessage("Order", "Initialize", code);
                int code2 = skOrder.ReadCertByID(User.ID);
                WriteMessage("Order", "ReadCertificate", code2);
                int code3 = skOrder.GetUserAccount();
                WriteMessage("Order", "GetUserAccount", code3);
                if ((code + code2 + code3) != 0)
                    return;

                loginSuccessful = true;  
                UpdateFutureEquity();
                UpdateFuturePosition();
                
                ReplenishStopOrders();
                while (!stopOrderReplyCompleted)
                    Application.DoEvents();
                ConnectReplyServer();                
                while (!replyCompleted)
                    Application.DoEvents();
                ConnectQuoteServer();
            }
        }

        private void SetUser()
        {
            SetUserButton.Enabled = false;
            SetUserForm accountForm = new SetUserForm();
            accountForm.OnAccountReset += OnSetUser;
            accountForm.Show();            
        }
        #endregion

        #region Initialization Helper - DataSource Preparation Routines       
        private DataTable CreatePortfolioQuoteTable()
        {
            DataTable table = new DataTable();

            table.Columns.Add("SymbolID", typeof(String));
            table.Columns.Add("SymbolName", typeof(String));
            table.Columns.Add("SymbolCode", typeof(String));
            table.Columns.Add("PriceFormat", typeof(String));
            table.Columns.Add("Stockidx", typeof(Int16));
            table.Columns.Add("Bid", typeof(Double));
            table.Columns.Add("Ask", typeof(Double));
            table.Columns.Add("Deal", typeof(Double));
            table.Columns.Add("UpDown", typeof(Double));
            table.Columns.Add("UpDownPercent", typeof(Double));
            table.Columns.Add("TickQty", typeof(Int32));
            table.Columns.Add("TotalQty", typeof(Int32));
            table.Columns.Add("High", typeof(Double));
            table.Columns.Add("Low", typeof(Double));
            table.Columns.Add("Open", typeof(Double));
            table.Columns.Add("RefPrice", typeof(Double));
            table.Columns.Add("BidQty", typeof(Int32));
            table.Columns.Add("AskQty", typeof(Int32));
            table.Columns.Add("Simulate", typeof(Int32));
            table.Columns.Add("LastDeal", typeof(Double));
            table.Columns.Add("LastBid", typeof(Double));
            table.Columns.Add("LastAsk", typeof(Double));
            table.PrimaryKey = new DataColumn[] { table.Columns["SymbolCode"] };

            return table;
        }

        private List<Portfolio> GetPortfolioList()
        {
            List<Portfolio> portfolios = new List<Portfolio>();

            string[] lines = File.ReadAllLines(@"Assets\PortfolioList.txt");
            foreach (string line in lines)
            {
                string[] separated = line.Split('|');
                Portfolio portfolio = new Portfolio()
                {
                    ID = "[ " + separated[0] + " " + separated[1] + " ]",
                    Items = separated[2]
                };
                portfolios.Add(portfolio);
            }

            return portfolios;
        }

        private List<Symbol> GetSymbolList()
        {
            List<Symbol> symbols = new List<Symbol>();
            LoadStockList(symbols);
            LoadFutureContracts(symbols);
            LoadOptionList(symbols);
            return symbols;
        }

        private void LoadStockList(List<Symbol> symbols)
        {
            string[] lines = File.ReadAllLines(@"Assets\StockList.txt");
            foreach (string line in lines)
            {
                if (line != "" && !line.Contains("Symbol"))
                {
                    string[] separated = line.Split(',');
                    symbols.Add(new Symbol(separated[0].Trim(), separated[1].Trim(), separated[0].Trim(), "TS", "N2", 0.0, 0901, 1330, 0901, 1330));
                }
            }
        }
        
        private void LoadFutureContracts(List<Symbol> symbols)
        {
            nearMonth = Properties.Settings.Default.NearMonth;
            int month1 = int.Parse(nearMonth);
            int month2 = month1 == 12 ? 1 : month1 + 1;
            string str2 = month2.ToString("00");
            int month3 = (month2 + 3) / 3 * 3;
            month3 = month3 % 12 == 0 ? 12 : month3 % 12;
            string str3 = month3.ToString("00");
            int month4 = (month3 + 3) / 3 * 3;
            month4 = month4 % 12 == 0 ? 12 : month4 % 12;
            string str4 = month4.ToString("00");
            int month5 = (month4 + 3) / 3 * 3;
            month5 = month5 % 12 == 0 ? 12 : month5 % 12;
            string str5 = month5.ToString("00");

            symbols.Add(new Symbol("TX00", "台指近", $"TX{nearMonth}", "TF", "N0", 1, 1501, 500, 0846, 1345));
            symbols.Add(new Symbol("TX0N", $"台指{str2}", $"TX{str2}", "TF", "N0", 1, 1501, 500, 0846, 1345));
            symbols.Add(new Symbol("TX0X", $"台指{str3}", $"TX{str3}", "TF", "N0", 1, 1501, 500, 0846, 1345));
            symbols.Add(new Symbol("TX0Y", $"台指{str4}", $"TX{str4}", "TF", "N0", 1, 1501, 500, 0846, 1345));
            symbols.Add(new Symbol("TX0Z", $"台指{str5}", $"TX{str5}", "TF", "N0", 1, 1501, 500, 0846, 1345));

            symbols.Add(new Symbol("MTX00", "小台近", $"MTX{nearMonth}", "TF", "N0", 1, 1501, 500, 0846, 1345));
            symbols.Add(new Symbol("MTX0N", $"小台{str2}", $"MTX{str2}", "TF", "N0", 1, 1501, 500, 0846, 1345));
            symbols.Add(new Symbol("MTX0X", $"小台{str3}", $"MTX{str3}", "TF", "N0", 1, 1501, 500, 0846, 1345));
            symbols.Add(new Symbol("MTX0Y", $"小台{str4}", $"MTX{str4}", "TF", "N0", 1, 1501, 500, 0846, 1345));
            symbols.Add(new Symbol("MTX0Z", $"小台{str5}", $"MTX{str5}", "TF", "N0", 1, 1501, 500, 0846, 1345));
        }

        private void LoadOptionList(List<Symbol> symbols)
        {
            string[] lines = File.ReadAllLines(@"Assets\OptionList.txt");
            foreach (string line in lines)
            {
                if (line != "" && !line.Contains("Symbol"))
                {
                    string[] separated = line.Split(',');
                    symbols.Add(new Symbol(separated[0].Trim(), separated[1].Trim(), separated[0].Trim(), "TO", "N2", 0.0, 1501, 500, 846, 1345));
                }
            }
        }

        private void PopupatePortfolioComboBox()
        {
            PortfolioComboBox.DataSource = portfolioList;
            PortfolioComboBox.DisplayMember = "ID";
            PortfolioComboBox.ValueMember = "Items";
            PortfolioComboBox.SelectedIndex = -1;
        }
        
        private void PopulateSpecificComboBox()
        {
            SpecificComboBox.DataSource = symbolList;
            SpecificComboBox.DisplayMember = "FullName";
            SpecificComboBox.ValueMember = "ID";
            SpecificComboBox.SelectedIndex = -1;
        }

        private void PopulateOrderSymbolComboBox()
        {            
            OrderSymbolComboBox.DataSource = orderSymbolList;
            OrderSymbolComboBox.DisplayMember = "ID";
            OrderSymbolComboBox.ValueMember = "ID";
            OrderSymbolComboBox.SelectedIndex = -1;
        }
        
        private void PopulateDOMComboBoxes()
        {
            DOMComboBox.DataSource = domSymbolList;
            DOMComboBox.DisplayMember = "FullName";
            DOMComboBox.ValueMember = "ID";
            DOMComboBox.SelectedIndex = -1;
        }
        #endregion

        #region Initialization Helper - DataGridView DataTemplate Setters        
        private void InitializePortfolioQuoteGrid()
        {
            PortfolioQuoteGrid.DoubleBuffered(true);

            PortfolioQuoteGrid.AutoGenerateColumns = false;
            PortfolioQuoteGrid.DataSource = portfolioQuoteTable;
            PortfolioQuoteGrid.Columns["SymbolID"].DataPropertyName = "SymbolID"; //ID
            PortfolioQuoteGrid.Columns["SymbolName"].DataPropertyName = "SymbolName"; //股票名稱

            PortfolioQuoteGrid.Columns["Bid"].DataPropertyName = "Bid"; //買進
            PortfolioQuoteGrid.Columns["Ask"].DataPropertyName = "Ask"; //賣出
            PortfolioQuoteGrid.Columns["Deal"].DataPropertyName = "Deal"; //成交

            PortfolioQuoteGrid.Columns["UpOrDown"].DataPropertyName = "UpDown"; //漲跌
            PortfolioQuoteGrid.Columns["UpOrDownPercent"].DataPropertyName = "UpDownPercent"; //漲跌幅

            PortfolioQuoteGrid.Columns["TickQty"].DataPropertyName = "TickQty"; //ticks單量
            PortfolioQuoteGrid.Columns["TotalQty"].DataPropertyName = "TotalQty"; //成交量

            PortfolioQuoteGrid.Columns["High"].DataPropertyName = "High"; //最高價
            PortfolioQuoteGrid.Columns["Low"].DataPropertyName = "Low"; //最低價

            PortfolioQuoteGrid.Columns["BidSize"].DataPropertyName = "BidQty"; //買量
            PortfolioQuoteGrid.Columns["AskSize"].DataPropertyName = "AskQty"; //賣量

            // Invisible Columns
            PortfolioQuoteGrid.Columns["SymbolCode"].DataPropertyName = "SymbolCode";
            PortfolioQuoteGrid.Columns["PriceFormat"].DataPropertyName = "PriceFormat";
            PortfolioQuoteGrid.Columns["RefPrice"].DataPropertyName = "RefPrice";
            PortfolioQuoteGrid.Columns["Simulate"].DataPropertyName = "Simulate";
            PortfolioQuoteGrid.Columns["LastDeal"].DataPropertyName = "LastDeal";
            PortfolioQuoteGrid.Columns["LastBid"].DataPropertyName = "LastBid";
            PortfolioQuoteGrid.Columns["LastAsk"].DataPropertyName = "LastAsk";
        }
          
        private void InitializeOrderBookGrid()
        {
            OrderBookGrid.AutoGenerateColumns = false;
            //OrderBookGrid.DataSource = orderBook;
            OrderBookGrid.Columns["KeyNo"].DataPropertyName = "KeyNo";
            OrderBookGrid.Columns["AccountNo"].DataPropertyName = "AccountNo";
            OrderBookGrid.Columns["Market"].DataPropertyName = "MarketType";
            OrderBookGrid.Columns["OrderType"].DataPropertyName = "OrderType";
            OrderBookGrid.Columns["OrderDate"].DataPropertyName = "OrderDate";
            OrderBookGrid.Columns["OrderTime"].DataPropertyName = "OrderTime";
            OrderBookGrid.Columns["Symbol"].DataPropertyName = "Symbol";
            OrderBookGrid.Columns["BuySell"].DataPropertyName = "BuySell";
            OrderBookGrid.Columns["OrderQty"].DataPropertyName = "OrderQty";
            OrderBookGrid.Columns["OrderPrice"].DataPropertyName = "OrderPrice";
            OrderBookGrid.Columns["OrderStatus"].DataPropertyName = "OrderStatus";
            OrderBookGrid.Columns["StatusChangeTime"].DataPropertyName = "StatusChangeTime";
            OrderBookGrid.Columns["DealQty"].DataPropertyName = "DealQty";
            OrderBookGrid.Columns["RemainQty"].DataPropertyName = "RemainQty";
            OrderBookGrid.Columns["DealPrice"].DataPropertyName = "DealPrice";
            OrderBookGrid.Columns["TriggerPrice"].DataPropertyName = "TriggerPrice";
            OrderBookGrid.Columns["OrderNo"].DataPropertyName = "OrderNo";
            OrderBookGrid.Columns["OKSeq"].DataPropertyName = "OKSeq";
        }
        
        private void InitializeOpenPositionGrid()
        {
            OpenPositionGrid.AutoGenerateColumns = false;
            OpenPositionGrid.DataSource = openPositions;
            OpenPositionGrid.Columns["MarketType"].DataPropertyName = "MarketType";
            OpenPositionGrid.Columns["Account"].DataPropertyName = "Account";
            OpenPositionGrid.Columns["ContractID"].DataPropertyName = "ContractID";
            OpenPositionGrid.Columns["PositionSize"].DataPropertyName = "Size";
            OpenPositionGrid.Columns["AvgCost"].DataPropertyName = "AvgCost";
            OpenPositionGrid.Columns["TickValue"].DataPropertyName = "TickValue";
        }
         
        private void SetSpecificQuoteGrid()
        {
            SpecificQuoteGrid.ColumnHeadersVisible = false;
            SpecificQuoteGrid.Rows.Add(0,0.0, 0.0, 0.0, 0);
        }
        
        private void SetTradeInfoGrid()
        {
            TradeInfoGrid.Rows.Add("每筆均買", 0.0, 0.0);
            TradeInfoGrid.Rows.Add("每筆均賣", 0.0, 0.0);
            TradeInfoGrid.Rows.Add("買賣力差", 0.0, 0.0);
            TradeInfoGrid.Rows.Add("買賣筆差", 0, 0);
            TradeInfoGrid.Rows.Add("買賣口差", 0, 0);
            TradeInfoGrid.Rows.Add("成交力差", 0, 0);
            TradeInfoGrid.Rows[3].DefaultCellStyle.Format = "N0";
            TradeInfoGrid.Rows[4].DefaultCellStyle.Format = "N0";
            TradeInfoGrid.Rows[5].DefaultCellStyle.Format = "N0";
        }

        private void SetDOMGrid()
        {
            DOMGrid.RowCount = 0;
            for (int i = 0; i < 40; i++)
            {
                DOMGrid.Rows.Add("", 0, "", 0, 0, 0, 0, 0, "","");
                domLadder[i] = 0;
            }
        }

        private void SetBest5Grid()
        {
            Best5Grid.Rows.Clear();
            for (int i = 0; i < 5; i++)
            {
                Best5Grid.Rows.Add(0, 0.0, 0.0, 0);
            }
        }
        #endregion

        #region Quote StartUp Routines
        private void StartQuoting()
        {
            if (IsMonthMappingChecked())
            {                                
                PortfolioComboBox.SelectedIndex = Properties.Settings.Default.PortfolioIndex;
                SpecificComboBox.SelectedValue = Properties.Settings.Default.SpecificID;
                DOMComboBox.SelectedValue = Properties.Settings.Default.DOMID;
                int code = skQuote.SKQuoteLib_RequestFutureTradeInfo(1, "TX00");
                WriteMessage("Quote", "Request TradeInfo for TX00", code);
                code = skQuote.SKQuoteLib_RequestFutureTradeInfo(2, "MTX00");
                WriteMessage("Quote", "Request TradeInfo for MTX00", code);
            }
            else
                MessageBox.Show("Can't Get Current Contract Month"); 
        }
    
        private bool IsMonthMappingChecked()
        {
            string month;
            SKSTOCK stock = new SKSTOCK();
            int code = skQuote.SKQuoteLib_GetStockByIndex(2, 1, ref stock);
            if (code == 0)
            {
                month = stock.bstrStockNo.Substring(2, 2);
                if (month != nearMonth)
                {
                    nearMonth = month;
                    Properties.Settings.Default.NearMonth = nearMonth;
                    ChangeMonthMapping(month, nearMonth);                    
                }
                return true;
            }
            else
            {
                MessageBox.Show("Can't Cnfirm the Near Month");
                return false;
            }
        }

        private void ChangeMonthMapping(string oldMonth, string newMonth)
        {
            foreach (Symbol item in symbolList)
            {
                if (item.MarketType == "TF")
                    item.Code = item.Code.Replace(oldMonth, newMonth);                
            }
            foreach (Symbol item in orderSymbolList)
            {
                if (item.MarketType == "TF")
                    item.Code = item.Code.Replace(oldMonth, newMonth);
            }
            foreach (Symbol item in domSymbolList)
            {
                if (item.MarketType == "TF")
                    item.Code = item.Code.Replace(oldMonth, newMonth);
            }
            PopulateComboBoxes();
        }
        #endregion

        #region User Command Event Handlers
        private void SetUserButton_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {            
            SetUser();
        }

        private void SetPortfolioButton_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {           
            SetPortfolioButton.Enabled = false;
            SetPortfolioForm formSetPortfolio = new SetPortfolioForm() { Symbols = symbolList, CurrentIndex = portfolioIndex};
            formSetPortfolio.OnPortfolioSetting += OnSetPortfolio;
            formSetPortfolio.Show();
        }
        
        private void PortfolioComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {          
            if (!comboBoxPopulated || PortfolioComboBox.SelectedIndex == -1)
                return;

            portfolioIndex = PortfolioComboBox.SelectedIndex;
            WriteMessage("User", $"Portfolio Selection Changed, SelectedIndex = {portfolioIndex}");
            portfolioQuoteTable.Clear();

            string quoteList = "TSEA,OTCA"; //加權指數、櫃買指數
            int code;
            // 1. 把一籃子報價股票之基本資料(股號,股名,昨收價等)放入PortfolioQuoteTable中
            foreach (var id in PortfolioComboBox.SelectedValue.ToString().Split(','))
            {
                Symbol symbol = symbolList.FirstOrDefault(s => s.ID == id);
                if (symbol != null)
                {
                    string stockNo = symbol.Code;
                    SKSTOCK stock = new SKSTOCK();
                    code = skQuote.SKQuoteLib_GetStockByNo(stockNo, ref stock);
                    if (code == 0)
                    {
                        PutSymbolToPortfolioQuoteTable(symbol, stock);
                        quoteList = quoteList + "," + stockNo;
                    }
                }
            }

            // 2.這一步才是對Server提出持續報價需求, 當Server有新報價時會Raise OnNotifyQuote通知接收           
            code = skQuote.SKQuoteLib_RequestStocks(1, quoteList);  
            WriteMessage("Quote", $"Request Quotes for  : {quoteList}", code);
        }
        
        private void SpecificComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!comboBoxPopulated || SpecificComboBox.SelectedIndex == -1)
                return;

            specificSymbol = (Symbol)SpecificComboBox.SelectedItem;
            if (specificSymbol == previousSpecificSymbol)
                return;
           
            WriteMessage("User", $"Specific Symbol Selection Changed, Selected Symbol = {specificSymbol.ID}");

            SpecificQuoteGrid.Columns["ClosePrice"].DefaultCellStyle.Format = specificSymbol.PriceFormat;
            SpecificQuoteGrid.Columns["UpDown"].DefaultCellStyle.Format = specificSymbol.PriceFormat == "N2" ? "▲ #,##0.00;▼ #,##0.00" : "▲ #,##0;▼ #,##0";

            TicksGrid.Rows.Clear();
            ticks.Clear();
            firstSpecificLiveTick = -1;
            TicksGrid.Columns["TickPrice"].DefaultCellStyle.Format = specificSymbol.PriceFormat;

            SetBest5Grid();
            Best5Grid.Columns["AskPrice"].DefaultCellStyle.Format = specificSymbol.PriceFormat;
            Best5Grid.Columns["BidPrice"].DefaultCellStyle.Format = specificSymbol.PriceFormat;
                        
            session1BeginTime = specificSymbol.Session1BeginTime;
            session1EndTime = specificSymbol.Session1EndTime;
            session2BeginTime = specificSymbol.Session2BeginTime;
            session2EndTime = specificSymbol.Session2EndTime;

            chart.ChartAreas["PriceArea"].AxisY.LabelStyle.Format = specificSymbol.PriceFormat;
            kBars.Clear();
            chart.DataBind();

            SKSTOCK stock = new SKSTOCK();
            int code = skQuote.SKQuoteLib_GetStockByNo(specificSymbol.Code, ref stock);
            if (code == 0)
            {
                specificStockIdx = stock.sStockIdx;
                stockID = stock.bstrStockNo;
                specificDenom = Math.Pow(10, stock.sDecimal);
                specificRef = stock.nRef / specificDenom;
                double upDown = stock.nClose == 0 ? 0 : (stock.nClose - stock.nRef) / specificDenom;

                SpecificQuoteGrid.Rows[0].Cells["ClosePrice"].Value = stock.nClose / specificDenom;
                SpecificQuoteGrid.Rows[0].Cells["UpDown"].Value = upDown;
                SpecificQuoteGrid.Rows[0].Cells["UpDownPercent"].Value = upDown / stock.nRef;
                SpecificQuoteGrid.Rows[0].Cells["TQty"].Value = stock.nTQty;
                
                code = skQuote.SKQuoteLib_RequestStocks(2, specificSymbol.Code);
                WriteMessage("Quote", $"Request Quote for Specific Symbol : {specificSymbol.ID}", code);
            }

            if (previousSpecificSymbol != null && previousSpecificSymbol.Code != domSymbol.Code)
            {
                code = skQuote.SKQuoteLib_CancelRequestTicks(previousSpecificSymbol.Code);
                WriteMessage("Quote", $"Cancel TicksRequest for Previous SpecificSymbol : {previousSpecificSymbol.ID}", code);
            }

            if (specificSymbol.Code == domSymbol.Code)
            {
                code = skQuote.SKQuoteLib_CancelRequestTicks(domSymbol.Code);
                WriteMessage("Quote", $"Cancel TicksRequest for DOMSymbol : {domSymbol.ID}", code);
                //如不取消, 因DOM Symbol 已Request了Tick, 新的Specific Symbol 無法收到歷史資料, 收錄之 ticks 將不完整
            }

            code = skQuote.SKQuoteLib_RequestTicks(2, specificSymbol.Code);
            WriteMessage("Quote", $"Request Ticks for New specificSymbol : {specificSymbol.ID}", code);

            previousSpecificSymbol = specificSymbol;
        }
               
        private void DOMComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!comboBoxPopulated || DOMComboBox.SelectedIndex == -1)
                return;
            
            domSymbol = (Symbol)DOMComboBox.SelectedItem;
            if (domSymbol == previousDOMSymbol)
                return;

            WriteMessage("User", $"DOM Symbol Selection Changed, Selected Symbol = {domSymbol.ID}");

            firstDOMLiveTick = -1;            
            SKSTOCK stock = new SKSTOCK();
            int code = skQuote.SKQuoteLib_GetStockByNo(domSymbol.Code, ref stock);
            short marketNo = 0;
            if (code == 0)
            {
                domStockIdx = stock.sStockIdx;
                marketNo = short.Parse(stock.bstrMarketNo);
                domDenom = (int)Math.Pow(10, stock.sDecimal);
                DOMGrid.Columns["Price"].DefaultCellStyle.Format = domSymbol.PriceFormat;
            }

            if (previousDOMSymbol != null && previousDOMSymbol.Code != specificSymbol.Code)
                code = skQuote.SKQuoteLib_CancelRequestTicks(previousDOMSymbol.Code);

            SetDOMGrid();
            if (domSymbol.Code != specificSymbol.Code)
            {
                code = skQuote.SKQuoteLib_RequestTicks(3, domSymbol.Code);
                firstDOMLiveTick = -1;
                WriteMessage("Quote", $"Request Ticks for DOM Symbol = {domSymbol.ID}", code);
            }
            else
            {
                // 當DOM標的 = specific 標的, 不自行Request五檔, 而是利用現成的Specific Request,以免重複
                // 但盤後非交易時段(非初次Request)不會有新資料進來,  DOM不會Update, 為避免此種情形, 所以主動去抓五檔, 並用最新SpecificSymbol的tick update DOM
                if (ticks.Count > 0)
                {
                    SKBEST5 best5 = new SKBEST5();
                    code = skQuote.SKQuoteLib_GetBest5(marketNo, domStockIdx, ref best5);
                    WriteMessage("Quote", "GetBest5", code);
                    if (code == 0)
                    {
                        UpdateDOMWithBest5(best5);
                        UpdateDOMWithTick((int)(ticks[ticks.Count - 1].TickPrice * specificDenom), ticks[ticks.Count - 1].TickQty, ticks[ticks.Count - 1].TickSimulate);
                    }
                }
            }

            previousDOMSymbol = domSymbol;
        }
        
        private void QuoteStatusLabel_DoubleClick(object sender, EventArgs e)
        {
            //if (QuoteStatusLabel.ForeColor == Color.Red || QuoteStatusLabel.ForeColor == Color.DarkRed)
            //{
            //    int code = skQuote.SKQuoteLib_EnterMonitor();
            //    WriteMessage("Quote", "EnterMonitor", code);
            //    if (code != 0)
            //        MessageBox.Show("報價主機無法連線", "Warning Message");
            //}
        }

        private void UpdatePositionButton_Click(object sender, EventArgs e)
        {
            UpdateFuturePosition();
        }

        private void UpdateEquityButton_Click(object sender, EventArgs e)
        {
            UpdateFutureEquity();
        }               
        
        private void OrderButton_Click(object sender, EventArgs e)        {
           
            bool validToSend = true;
            if (OrderSymbolComboBox.SelectedIndex < 0)
            {
                errorProvider1.SetError(OrderSymbolComboBox, "請輸入商品代碼");
                validToSend = false;
            }

            if (!(double.TryParse(OrderPriceTextBox.Text.Trim(), out double price)))
            {
                errorProvider2.SetError(OrderPriceTextBox, "委託價格請輸入數字");
                validToSend = false;
            }

            if (validToSend)
            {
                string orderType = limitOrderRadioButton.Checked ? "限價單" : "停損單";
                int qty = (int)qtyUpDown.Value;
                string buySell = (BuyRadioButton.Checked ? "買進" : "賣出");
                int dayTrade = (dayTradeCheckBox.Checked ? 1 : 0);

                Symbol symbol = (Symbol)OrderSymbolComboBox.SelectedItem;
                PlaceOrder(symbol, orderType, buySell, price, qty, dayTrade);               
            }
        }

        private void OrderBookGrid_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == OrderBookGrid.Columns["Delete"].Index)
            {                
                string orderNo = OrderBookGrid.Rows[e.RowIndex].Cells["OrderNo"].Value.ToString();
                Order orderToCancel = orderBook.FirstOrDefault(o => o.OrderNo == orderNo);                
                if (orderToCancel != null)
                    CancelOrder(orderToCancel);  
            }
        }
        
        private void DOMGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {           
            if (e.RowIndex >= 0 && (e.ColumnIndex == DOMGrid.Columns["BidQty"].Index || e.ColumnIndex == DOMGrid.Columns["AskQty"].Index))
            {
                double price = double.Parse(DOMGrid.Rows[e.RowIndex].Cells["UnformatPrice"].Value.ToString());
                price = price/domDenom;
                string buySell = e.ColumnIndex == DOMGrid.Columns["BidQty"].Index ? "買進" : "賣出";               
                if ((buySell == "買進" && price > domBestAsk) || (buySell == "賣出" && price < domBestBid))   // Stop Order
                    PlaceOrder(domSymbol, "停損單", buySell, price, 1, 0);  
                else
                    PlaceOrder(domSymbol, "限價單", buySell, price, 1, 0);                              
            }
        }
        
        private void DOMGrid_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && DOMGrid[e.ColumnIndex, e.RowIndex].Value.ToString() == "刪")
            {             
                int uPrice = (int)DOMGrid.Rows[e.RowIndex].Cells["UnFormatPrice"].Value;
                string buySell = e.ColumnIndex == 0 ? "買進" : "賣出";
               
                var ordersToCancel = orderBook.Where(o => o.Symbol == domSymbol.Code && o.BuySell == buySell && o.RemainQty != 0 &&
                       ((o.OrderType == "限價單" && Convert.ToInt32(o.OrderPrice * domDenom) == uPrice) ||
                       (o.OrderType == "停損單" && Convert.ToInt32(o.TriggerPrice * domDenom) == uPrice)));

                foreach (Order order in ordersToCancel)
                {
                    CancelOrder(order);
                }  
            }
        }
        
        private void PortfolioQuoteGrid_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && (e.ColumnIndex == PortfolioQuoteGrid.Columns["SymbolID"].Index ||
                e.ColumnIndex == PortfolioQuoteGrid.Columns["SymbolName"].Index))
            {                
                SpecificComboBox.SelectedValue = PortfolioQuoteGrid.Rows[e.RowIndex].Cells["SymbolID"].Value;
            }
            int t = 0;
        }
        
        private void Best5Grid_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 &&
               (e.ColumnIndex == Best5Grid.Columns["BidPrice"].Index || e.ColumnIndex == Best5Grid.Columns["AskPrice"].Index))
            {
                OrderSymbolComboBox.SelectedItem = specificSymbol;
                if (OrderSymbolComboBox.SelectedIndex != -1)
                {
                    OrderPriceTextBox.Text = Best5Grid[e.ColumnIndex, e.RowIndex].Value.ToString();
                    if (e.ColumnIndex == Best5Grid.Columns["BidPrice"].Index)
                        BuyRadioButton.Checked = true;
                    else
                        SellRadioButton.Checked = true;
                }
            }
        }
        
        private void LimitOrderRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (limitOrderRadioButton.Checked)
            {
                orderPriceLabel.Text = "委託價格";
                tradeTypeCombo.SelectedIndex = 0;
                StopOrderLabel.Text = "限價委託";
            }
            else
            {
                orderPriceLabel.Text = "觸發價格";
                tradeTypeCombo.SelectedIndex = 1;
                StopOrderLabel.Text = "停損委託";
            }
        }
        
        private void OrderSymbolCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!comboBoxPopulated || OrderSymbolComboBox.SelectedIndex == -1) return;
            Symbol symbol = (Symbol)OrderSymbolComboBox.SelectedItem;
            orderSymbolNameLabel.Text = symbol.Name;
            if (symbol.MarketType == "TS")
            {
                limitOrderRadioButton.Checked = true;
                stopLossRadioButton.Enabled = false;
            }
            else
            { 
                stopLossRadioButton.Enabled = true;
            }
        }

        private void BuyRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            OrderButton.BackColor = BuyRadioButton.Checked ? Color.PaleVioletRed : Color.CadetBlue;
            orderPanel.BackColor = BuyRadioButton.Checked ? Color.Pink : Color.LightBlue;
        }

        private void TBarsOnlyCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!loginSuccessful) return;

            kBars.Clear();
            ConvertTicksToBars(ticks);                        
        }

        private void AllOrderButton_Click(object sender, EventArgs e)
        {
            OrderBookGrid.DataSource = orderBook;
            AllOrderButton.BackColor = Color.Beige;
            DealedOrderButton.BackColor = Color.Gray;
            CanceledOrderButton.BackColor = Color.Gray;
            CancelableOrderButton.BackColor = Color.Gray;
        }

        private void DealedOrderButton_Click(object sender, EventArgs e)
        {
            OrderBookGrid.DataSource= new BindingList<Order>(orderBook.Where(o=>o.OrderStatus == "全部成交").ToList<Order>());
            AllOrderButton.BackColor = Color.DarkGray;
            DealedOrderButton.BackColor = Color.Beige;
            CanceledOrderButton.BackColor = Color.DarkGray;
            CancelableOrderButton.BackColor = Color.DarkGray;
        }

        private void CanceledOrderButton_Click(object sender, EventArgs e)
        {
            OrderBookGrid.DataSource = new BindingList<Order>(orderBook.Where(o => o.OrderStatus == "全部取消").ToList<Order>());
            AllOrderButton.BackColor = Color.DarkGray;
            DealedOrderButton.BackColor = Color.DarkGray;
            CanceledOrderButton.BackColor = Color.Beige;
            CancelableOrderButton.BackColor = Color.DarkGray;
        }

        private void CancelableOrderButton_Click(object sender, EventArgs e)
        {
            OrderBookGrid.DataSource = new BindingList<Order>(orderBook.Where(o => o.RemainQty != 0).ToList<Order>());
            AllOrderButton.BackColor = Color.DarkGray;
            DealedOrderButton.BackColor = Color.DarkGray;
            CanceledOrderButton.BackColor = Color.DarkGray;
            CancelableOrderButton.BackColor = Color.Beige;
        }
        #endregion

        #region API Command Wrappers, mostly User Command Event Handlers' Assistant          
        
        private void ConnectReplyServer()
        {
            int code = skReply.SKReplyLib_ConnectByID(User.ID);
            WriteMessage("Reply", "ConnectReplyServer", code);
            if (code != 0)
                MessageBox.Show("回報主機無法連線, 請確定網路狀態", "Warning Message");
        }
        
        private void ConnectQuoteServer()
        {
            int code = skQuote.SKQuoteLib_EnterMonitor();
            WriteMessage("Quote", "ConnectQuoteServer", code);
            if (code != 0)
                MessageBox.Show("報價主機無法連線", "Warning Message");
        }
        
        private void ReplenishStopOrders()
        {
            // 非停損委託的回報的起迄時間為7:31至次日的7:30, 停損回補的期間配合一般回報, 所以可能會跨兩個日曆日
            stopOrderReplyCompleted = false;
            DateTime loginTime = DateTime.Now;
           
            // 若登入日為假日, 則將登入時間設為 最近一個交易日的7:31
            if (loginTime.DayOfWeek == DayOfWeek.Sunday || loginTime.DayOfWeek == DayOfWeek.Saturday)
            {
                while (loginTime.DayOfWeek == DayOfWeek.Sunday || loginTime.DayOfWeek == DayOfWeek.Saturday)
                {
                    loginTime = loginTime.AddDays(-1.0);
                }                       
                loginTime = loginTime.Date.Add(new TimeSpan(7, 31, 0));         
            }
            
            if (loginTime.TimeOfDay < new TimeSpan(7, 31, 0))
                loginTime = loginTime.AddDays(-1.0);
            //第一日的查詢應可以篩選在T盤開後的停損單(在OnReplyStopLossReport),但太麻煩了, 不篩選沒有影響 
            stopReplyPass = 1;
            int code = skOrder.GetStopLossReport(User.ID, User.FutureAccount, 0, "STP", loginTime.ToString("yyyyMMdd"));
            WriteMessage("Order", $"Get StopLossReport for Date = {loginTime.ToString("yyyyMMdd")}", code);
           
            
            // 若登入時間在7:30前, 尚須取得次日0時至5時的停損單資訊  
            if (loginTime.TimeOfDay < new TimeSpan(7, 31, 0))
            {
                // 先等上一次查詢的資料全部收到後, 再執行第二日的查詢
                while (!stopOrderReplyCompleted)
                    Application.DoEvents();
                stopOrderReplyCompleted = false;
                stopReplyPass = 2;
                // GetStopLossReport 在很短時間內連續執行會回復錯誤訊息, 無法得到停損單資訊, 先hold個5秒
                Thread.Sleep(5000);  
                code = skOrder.GetStopLossReport(User.ID, User.FutureAccount, 0, "STP", loginTime.AddDays(1.0).ToString("yyyyMMdd"));
                WriteMessage("Order", $"Get StopLossReport for Date = {loginTime.AddDays(1.0).ToString("yyyyMMdd")}", code);     
            }
        }
        
        public void UpdateFuturePosition()
        {
            int code = skOrder.GetOpenInterest(User.ID, User.FutureAccount);
            WriteMessage("Order", "GetFuturePosition", code);
            if (code == 0)
            {
                openPositions.Clear();
                PositionLastUpdateLabel.Text = DateTime.Now.ToString("HH:mm:ss");
            }
        }

        public void UpdateFutureEquity()
        {
            int code = skOrder.GetFutureRights(User.ID, User.FutureAccount, 1);
            WriteMessage("Order", "GetFutureEquity", code);
            if (code == 0)
                FutureEquityGrid.Rows.Clear();            
        }
        
        private void PlaceOrder(Symbol symbol, string orderType, string buySell, double price, int qty, int dayTrade)
        {
            int code = -1;
            string message;           
            if (orderType == "限價單")
            {
                if (symbol.MarketType == "TS")
                {
                    STOCKORDER stockOrder = new STOCKORDER()
                    {
                        bstrFullAccount = User.StockAccount,
                        bstrStockNo = symbol.Code,
                        sPrime = 0,
                        sPeriod = 0,
                        sFlag = 0,
                        sBuySell = buySell == "買進" ? (short)0 : (short)1,
                        bstrPrice = price.ToString(),
                        nQty = qty
                    };
                    code = skOrder.SendStockOrder(User.ID, false, stockOrder, out message);
                    WriteMessage("Order", $"SendStockOrder : {message}", code);
                }
                else if (symbol.MarketType == "TF" || symbol.MarketType == "TO")
                {
                    FUTUREORDER limitOrder = new FUTUREORDER()
                    {
                        bstrFullAccount = User.FutureAccount,
                        bstrStockNo = symbol.Code,
                        bstrPrice = price.ToString(),
                        nQty = qty,
                        sBuySell = buySell == "買進" ? (short)0 : (short)1,
                        sTradeType = 0,
                        sDayTrade = (short)dayTrade,
                        sNewClose = 2
                    };

                    if (symbol.MarketType == "TF")
                    {
                        code = skOrder.SendFutureOrder(User.ID, false, limitOrder, out message);
                        WriteMessage("Order", $"SendFutureLimitOrder : {message}", code);
                    }
                    else
                    {
                        code = skOrder.SendOptionOrder(User.ID, false, limitOrder, out message);
                        WriteMessage("Order", $"SendOptionLimitOrder : {message}", code);
                    }
                }
            }
            else if (orderType == "停損單")
            {
                if (symbol.MarketType != "TF" && symbol.MarketType != "TO")
                    return;
                
                FUTUREORDER stopOrder = new FUTUREORDER()
                {
                    bstrFullAccount = User.FutureAccount,
                    bstrStockNo = symbol.Code,
                    bstrTrigger = price.ToString(),
                    bstrPrice = "M",
                    sTradeType = 1,                  
                    nQty = qty,
                    sBuySell = buySell == "買進" ? (short)0 : (short)1,                   
                    sDayTrade = (short)dayTrade,
                    sNewClose = 2
                };
               
                if (symbol.MarketType == "TF")
                {
                    code = skOrder.SendFutureStopLossOrder(User.ID, false, stopOrder, out message);
                    WriteMessage("Order", $"Send Future Stop Order : {message}", code);
                }
                else 
                {
                    code = skOrder.SendOptionStopLossOrder(User.ID, false, stopOrder, out message);                
                    WriteMessage("Order", $"Send Option Stop Order : {message}", code);
                }

                VocalReminder("停損單", buySell, "N", code == 0);
                if (code == 0)
                {                   
                    Order order = new Order()
                    {
                        OrderType = "停損單",
                        AccountNo = User.FutureAccount,
                        OrderNo = message.Split(':')[1].Substring(0, 6).Remove(2, 1),
                        KeyNo = message.Trim().Split('：')[1].Substring(0, 6),
                        MarketType = symbol.MarketType,
                        Symbol = symbol.Code,
                        BuySell = buySell,
                        OrderPrice = 0,
                        OrderQty = qty,
                        RemainQty = qty,
                        OrderDate = DateTime.Today.ToString("yyyyMMdd"),
                        OrderTime = DateTime.Now.ToString("HH:mm:ss"),
                        TriggerPrice = price,
                        OrderStatus = "尚未觸發"
                    };
                    orderBook.Add(order);
                    UpdateOrderBookView();
                    if (order.Symbol == domSymbol.Code)
                        UpdateDOMWithOrderTransaction("N", order.OrderType, order.BuySell, order.TriggerPrice, order.OrderQty);                    
                }                                
            }           
        }        
              
        private void CancelOrder(Order order)
        {
            int code;
            if (order.OrderType == "限價單")
            { 
                if (order.MarketType == "TS")
                {
                    code = skOrder.CancelOrderBySeqNo(User.ID, false, User.StockAccount, order.KeyNo, out string message);
                    WriteMessage("Order", "Cancel Stock Order", code);
                }
                else if (order.MarketType == "TF" || order.MarketType == "TO")
                {
                    code = skOrder.CancelOrderBySeqNo(User.ID, false, User.FutureAccount, order.KeyNo, out string message);
                    WriteMessage("Order", $"Cancel  Future Limit Order : {message}", code);
                }
            }
            else if (order.OrderType == "停損單")
            { 
                if (order.MarketType == "TF" || order.MarketType == "TO")
                {
                    code = skOrder.CancelFutureStopLoss(User.ID, false, User.FutureAccount, order.KeyNo, "STP", out string message);
                    WriteMessage("Order", $"Cancel  Future Stop Order : {message}", code);

                    VocalReminder("停損單", "", "C", code == 0);
                    if (code == 0)
                    {
                        order.RemainQty = 0;
                        order.OrderStatus = "全部取消";
                        UpdateOrderBookView();
                        if (order.Symbol == domSymbol.Code)                        
                            UpdateDOMWithOrderTransaction("C", order.OrderType, order.BuySell, order.TriggerPrice, order.OrderQty);                        
                    }
                    else
                    {
                        order.OrderStatus = "取消失敗";                        
                    }
                    order.StatusChangeTime = DateTime.Now.ToString("HH:mm:ss");                   
                }               
            }           
        }

        private void UpdateOrderBookView()
        {
            if (DealedOrderButton.BackColor == Color.Beige)
                OrderBookGrid.DataSource = new BindingList<Order>(orderBook.Where(o => o.OrderStatus == "全部成交").ToList<Order>());
            else if (CanceledOrderButton.BackColor == Color.Beige)            
                OrderBookGrid.DataSource = new BindingList<Order>(orderBook.Where(o => o.OrderStatus == "全部取消").ToList<Order>());
            else if (CancelableOrderButton.BackColor == Color.Beige)
                OrderBookGrid.DataSource = new BindingList<Order>(orderBook.Where(o => o.RemainQty != 0).ToList<Order>());
            else
                OrderBookGrid.DataSource = orderBook;          
        }

        private void VocalReminder(string orderType, string buySell, string transType, bool successful)
        {
            if (orderType == "停損單")
            {
                if (transType == "D")
                {
                    if (successful)
                    {
                        if (buySell == "買進")
                            using (var soundPlayer = new SoundPlayer(@"Assets\Wav\StopBuyDeal.wav")) soundPlayer.Play();
                        else
                            using (var soundPlayer = new SoundPlayer(@"Assets\Wav\StopSellDeal.wav")) soundPlayer.Play();
                    }
                    else
                        using (var soundPlayer = new SoundPlayer(@"Assets\Wav\Alarm.wav")) soundPlayer.Play();
                }
                else if (transType == "N")
                {
                    if (successful)
                    {
                        if (buySell == "買進")
                            using (var soundPlayer = new SoundPlayer(@"Assets\Wav\StopBuySuccess.wav")) soundPlayer.Play();
                        else
                            using (var soundPlayer = new SoundPlayer(@"Assets\Wav\StopSellSuccess.wav")) soundPlayer.Play();
                    }
                    else
                    {
                        if (buySell == "買進")
                            using (var soundPlayer = new SoundPlayer(@"Assets\Wav\StopBuyFail.wav")) soundPlayer.Play();
                        else
                            using (var soundPlayer = new SoundPlayer(@"Assets\Wav\StopSellFail.wav")) soundPlayer.Play();
                    }
                }
                else if (transType == "C")
                {
                    if (successful)
                        using (var soundPlayer = new SoundPlayer(@"Assets\Wav\StopDeleteSuccess.wav")) soundPlayer.Play();
                    else
                        using (var soundPlayer = new SoundPlayer(@"Assets\Wav\StopDeleteFail.wav")) soundPlayer.Play();
                }
            }
            else if ( orderType == "限價單")
            {
                if (transType == "D")
                {
                    if (successful)
                    {
                        if (buySell == "買進")
                            using (var soundPlayer = new SoundPlayer(@"Assets\Wav\BuyDeal.wav")) soundPlayer.Play();
                        else
                            using (var soundPlayer = new SoundPlayer(@"Assets\Wav\SellDeal.wav")) soundPlayer.Play();
                    }
                    else
                        using (var soundPlayer = new SoundPlayer(@"Assets\Wav\Alarm.wav")) soundPlayer.Play();
                }
                else if (transType == "N")
                {
                    if (successful)
                    {
                        if (buySell == "買進")
                            using (var soundPlayer = new SoundPlayer(@"Assets\Wav\Buy.wav")) soundPlayer.Play();
                        else
                            using (var soundPlayer = new SoundPlayer(@"Assets\Wav\Sell.wav")) soundPlayer.Play();
                    }
                    else
                    {
                        using (var soundPlayer = new SoundPlayer(@"Assets\Wav\Alarm.wav")) soundPlayer.Play();
                    }
                }
                else if (transType == "C")
                {
                    if (successful)
                        using (var soundPlayer = new SoundPlayer(@"Assets\Wav\Cancel.wav")) soundPlayer.Play();
                    else
                        using (var soundPlayer = new SoundPlayer(@"Assets\Wav\Alarm.wav")) soundPlayer.Play();
                }
            }          
        }
        #endregion

        #region API Event Handlers
        private void OnReceiveAccount(string bstrLogInID, string bstrAccountData)
        {
            string[] accountElements = bstrAccountData.Split(',');
            string accountNo = accountElements[1] + accountElements[3];

            if (accountElements[0] == "TS")
                User.StockAccount = accountNo;
            else if (accountElements[0] == "TF")
                User.FutureAccount = accountNo;
        }
       
        private void OnReceiveStopLossReport(string bstrData)
        {
            WriteMessage("OnReceiveStopeLossReport", bstrData);
            string[] items = bstrData.Split(',');
            if (items.Length <= 3)
            {
                if (bstrData == "##" || items[0] == "@@")
                {
                    WriteMessage("Info", "Stop Order Reply Completed");
                    stopOrderReplyCompleted = true;
                }
                return;
            }

            string status = items[3];
            int i = status == "7" ? 6 : 0;
            int j = status == "7" ? 8 : 0;
            string dateString = items[12 + i].Split(':')[1].Substring(0, 10);
            int qty = int.Parse(items[15 + i].Split(':')[1]);

            if (status == "5" || status == "8" || status == "7")
            {
                Order order = new Order()
                {
                    OrderType = "停損單",
                    AccountNo = items[0],
                    KeyNo = items[1],
                    OrderNo = items[22 + j].Split(':')[1].Trim(),
                    Symbol = items[8 + i],
                    MarketType = items[6 + i],
                    OrderDate = dateString.Substring(0,4)+dateString.Substring(5,2) + dateString.Substring(8,2),
                    OrderTime = items[12 + i].Split(' ')[1].Substring(0, 8),                        
                    BuySell = items[16 + i].Split(':')[1] == "買" ? "買進" : "賣出",
                    OrderPrice = items[20 + i].Split(':')[1] == "市價" ? 0 : double.Parse(items[20].Split(':')[1]),
                    OrderQty = qty,
                    TriggerPrice = double.Parse(items[21 + i].Split('=')[1]),
                    //已觸發委託(status=7)的剩餘數量與委託狀態更改, 在OnReplyData執行, 此處僅考慮 Order Initiation
                    RemainQty = status == "8" ? 0: qty,                   
                    OrderStatus = status == "8" ? "全部取消" : "尚未觸發"              
                };
                if (stopReplyPass != 1 || string.Compare(order.OrderTime, "07:31:00") >= 0)
                {
                    orderBook.Add(order);
                }                
            }      
        }

        private void OnReceiveFutureEquity(string bstrData)
        {
            WriteMessage("OnReceiveFutureEquity", bstrData.Substring(0, 10));
            string[] elements = bstrData.Split(',');
            if (elements[0] != "##" && elements.Length >= 3)
            {
                double yBalance = double.Parse(elements[22]) / 100.0;
                double realized = double.Parse(elements[11]) / 100.0;
                double expenses = (double.Parse(elements[2]) + double.Parse(elements[3])) / 100.0;
                double balance = double.Parse(elements[0]) / 100.0;
                double unRealized = double.Parse(elements[1]) / 100.0;
                double equity = double.Parse(elements[6]) / 100.0;
                double excessMargin = double.Parse(elements[7]) / 100.0;
                double totalRights = double.Parse(elements[19]) / 100.0;
                FutureEquityGrid.Rows.Add(yBalance, realized, expenses, balance, unRealized, equity, excessMargin, totalRights, DateTime.Now.ToString("HH:mm:ss"));
            }
        }
        
        private void OnReceiveFuturePosition(string bstrData)
        {
            WriteMessage("OnRecieveFuturePosition", bstrData);
            string[] elements = bstrData.Split(',');
            if (elements[0] != "##" && elements.Length >= 3)
            {
                int sign = elements[3] == "S" ? -1 : 1;
                Position position = new Position()
                {
                    MarketType = elements[0] == "TF" ? "期貨" : elements[0] == "TO" ? "選擇權" : elements[0],
                    Account = elements[1],
                    ContractID = elements[2],
                    Size = int.Parse(elements[4]) * sign,
                    AvgCost = int.Parse(elements[6]) / 1000.0,
                    TickValue = int.Parse(elements[7])
                };
                openPositions.Add(position);
            }
        }
        
        private void OnReceiveReplyConnect(string bstrUserID, int nErrorCode)
        {
            //WriteMessage("OnReceiveReplyServerConnected", $"ErrorCode = {nErrorCode}");
            WriteMessage("Info", "Reply Server Connected, Start to Replenish Historical Order Information");
            replyStatusLabel.ForeColor = Color.Yellow;
        }

        private void OnReceiveReplyData(string userid, string replyData)
        {
            WriteMessage("OnReceiveReplyData", replyData);
            string[] items = replyData.Split(',');

            string transType = items[2];
            string orderSymbol = items[8];
            string orderNo = items[10];    // 委託書號
            string buySell = items[6].Substring(0, 1) == "B" ? "買進" : "賣出";
            string orderErr = items[3];
            double price = double.Parse(items[11]);
            double domPrice = price;
            int qty = int.Parse(items[20]);
            Order order = new Order();

            if (transType == "N")  // 新委託
            {
                order = orderBook.FirstOrDefault(o => o.OrderNo == orderNo);
                if (order == null || orderNo == "00000")  // 失敗委託會有相同委託書號 "00000", 但也是新委託 
                {
                    string orderType ;
                    string orderStatus;
                    if (items[0][0] == '0' )
                    {
                        orderType =  "限價單";
                        orderStatus = orderErr == "N" ? "尚未成交" : "委託失敗";
                    }
                    else
                    {
                        // 可能會有由別的系統（MultiCharts)進入的停損單
                        orderType = "停損單";
                        orderStatus = orderErr == "N" ? "停損觸發" : "委託失敗";
                    }

                    order = new Order()
                    {
                        OrderType = orderType,
                        OrderNo = orderNo,
                        KeyNo = items[0],
                        AccountNo = items[5],
                        MarketType = items[1],
                        Symbol = orderSymbol,
                        OrderDate = items[23],
                        OrderTime = items[24],
                        BuySell = buySell,
                        OrderQty = qty,
                        OrderPrice = price,
                        OrderStatus = orderStatus,
                        RemainQty = orderErr == "N" ? qty : 0,
                    };
                    orderBook.Add(order);                 
                }
                else        // 委託簿中已有該委託書號, 檢查是否是由停損單觸發的
                {
                    if (order.OrderStatus == "尚未觸發")    // 由停損單觸發而來的Order
                    {
                        order.OrderStatus = orderErr == "N" ? "停損觸發" : "委託失敗";
                        order.StatusChangeTime = items[24];
                        order.KeyNo = items[0];            // 此時會有個正式序號
                        domPrice = order.TriggerPrice;
                    }
                    else
                        MessageBox.Show("Something abnormal");
                }
            }
            else if (transType == "C")  // 委託取消
            {
                // 此處只會出現限價委託的刪單(限價單一經觸發, 就會變成市價單, 立刻成交, 無機會取消)
                order = orderBook.FirstOrDefault(t => t.OrderNo == orderNo);
                if (order == null)
                {
                    WriteMessage("Warning", "Canceled Order without Order Initiation Information");
                    return;
                }

                if (orderErr == "N")
                {
                    order.RemainQty = 0;
                    order.OrderStatus = "全部取消";                   
                }
                else
                {
                    order.OrderStatus = "取消失敗";                    
                }
                order.StatusChangeTime = items[24];               
            }
            else if (transType == "D")   // 委託成交
            {
                order = orderBook.FirstOrDefault(t => t.OrderNo == orderNo); ;
                if (order == null)
                {
                    WriteMessage("Info", "單腳回報, 有成交資料卻無委託資料");
                    // T+1 盤的成交只有單腳, 無委託部分Leg, 因此無法得知Order Time, 
                    // Creae an Artificial 'new' Leg for this single-leged order
                    order = new Order()
                    {
                        OrderType = items[6].Substring(3, 1) == "1" ? "停損單" : "限價單",
                        // OrderType = items[0]="0000000000000" ? "停損單" : "限價單";
                        OrderNo = orderNo,
                        KeyNo = items[0],
                        AccountNo = items[5],
                        MarketType = items[1],
                        Symbol = orderSymbol,
                        BuySell = buySell,
                        OrderQty = qty,
                        OrderPrice = items[6].Substring(3, 1) == "1" ? 0 : price,
                        TriggerPrice = items[6].Substring(3, 1) == "1" ? price : 0,
                        OrderDate = items[23],
                        OrderStatus = orderErr == "N" ? "全部成交" : "委託失敗",
                        RemainQty = qty,
                    };
                    orderBook.Add(order);
                }
                order.DealQty += qty;
                order.RemainQty -= qty;
                order.DealPrice = price;
                if (order.RemainQty == 0)
                    order.OrderStatus = "全部成交";
                else
                    order.OrderStatus = "部分成交";
                order.StatusChangeTime = items[24];
                domPrice = order.OrderType == "停損單" ? domPrice = order.TriggerPrice : order.OrderPrice;

                if (replyCompleted &&  orderErr == "N")
                {                        
                    UpdateFuturePosition();
                    UpdateFutureEquity();
                }  
            }

            if (replyCompleted)
            {
                VocalReminder(order.OrderType, buySell, transType, orderErr == "N");
                if (orderErr == "N")
                {                
                    if (domSymbol != null && orderSymbol == domSymbol.Code)
                        UpdateDOMWithOrderTransaction(transType,order.OrderType, buySell, domPrice, qty);
                    UpdateOrderBookView();
                }
            }
        }

        private void OnReceiveReplyComplete(string bstrUserID)
        {
            WriteMessage("Info" , "Reply Completed");
           
            if (!replyCompleted)
                orderBook = new BindingList<Order>(orderBook.OrderBy(o => o.OrderDate).ThenBy(o=>o.OrderTime).ToList());
            OrderBookGrid.DataSource = orderBook;

            replyCompleted = true; 
            replyStatusLabel.ForeColor = Color.Green;
            //連線主機每隔一段期間就會發出OnComplete訊息, 回補完成後即不再訂閱    
            skReply.OnComplete -= OnReceiveReplyComplete;                      
        }

        private void OnReceiveReplyClear(string bstrMarket)
        {
            WriteMessage("OnReceiveReplyClear", bstrMarket);
        }

        //private void OnReceiveQuoteConnect(int nKind, int nCode)
        //{
        //    //WriteMessage("OnReceiveQuoteConnect", $" nKind = {nKind}, nCode = {nCode}");
        //    if (nKind == 3001 && nCode == 0)
        //    {
        //        QuoteStatusLabel.ForeColor = Color.Yellow;
        //        WriteMessage("Info", "Quote Server Connecting");
        //    }
        //    else if (nKind == 3003)
        //    {
        //        QuoteStatusLabel.ForeColor = Color.Green;
        //        WriteMessage("Info", "Quote Server Connected, Start Quoting");
        //        StartQuoting();
        //    }
        //    else if (nKind == 3002)
        //    {
        //        WriteMessage("Info", "Unable to Connect Quote Server");
        //        QuoteStatusLabel.ForeColor = Color.Red;
        //        MessageBox.Show("無法連線", "Warning Message");
        //    }
        //    else if (nKind == 3021)
        //    {
        //        WriteMessage("Info", "Quote Server Connection Lost, Disconnect Quote Server");
        //        QuoteStatusLabel.ForeColor = Color.DarkRed;
        //        int code = skQuote.SKQuoteLib_LeaveMonitor();
        //        WriteMessage("Quote", "DisConnectQuoteServer", code);
        //        MessageBox.Show("網路斷線", "Warning Message");
        //    }
        //    else
        //    {
        //        WriteMessage("OnReceiveQuoteConnect", $" nKind = {nKind}, nCode = {nCode}");
        //    }
        //}

        
               
        

        
        
        private void OnReceiveReplyDisconnect(string bstrUserID, int nErrorCode)
        {
            WriteMessage("OnReceiveReplyDisconnect", nErrorCode.ToString());           
            replyStatusLabel.ForeColor = Color.Red;
            skReply.OnComplete += OnReceiveReplyComplete;
            replyCompleted = false;            
            MessageBox.Show("與回報主機連線中斷", "Waring Message");
         }
        #endregion

        #region Subview Callback Event Handler
        private void OnSetUser(object sender, EventArgs e)
        {
            SetUserButton.Enabled = true;           
            if (!loginSuccessful)
                LogIntoSystem();            
        }

        private void OnSetPortfolio(object sender, EventArgs e)
        {
            SetPortfolioButton.Enabled = true;
            portfolioList = GetPortfolioList();
            comboBoxPopulated = false;
            PortfolioComboBox.DataSource = portfolioList;
            comboBoxPopulated = true;
            PortfolioComboBox.SelectedIndex = portfolioIndex;
        }
        #endregion

        #region Cutstom Methods, Mostly SKYCOM Event Handlers' Helper     
        private void PutSymbolToPortfolioQuoteTable(Symbol symbol, SKSTOCK stock)
        {
            double denom = Math.Pow(10, stock.sDecimal);
            double upDown = stock.nClose - stock.nRef;

            DataRow rowFind = portfolioQuoteTable.Rows.Find(symbol.Code);
            if (rowFind == null)
            {
                DataRow newRow = portfolioQuoteTable.NewRow();
                newRow["SymbolID"] = symbol.ID;
                newRow["SymbolCode"] = symbol.Code;
                newRow["SymbolName"] = symbol.Name;
                newRow["PriceFormat"] = symbol.PriceFormat;
                newRow["Stockidx"] = stock.sStockIdx;
                newRow["Open"] = stock.nOpen / denom;
                newRow["High"] = stock.nHigh / denom;
                newRow["Low"] = stock.nLow / denom;
                newRow["Deal"] = stock.nClose / denom;
                newRow["UpDown"] = stock.nClose == 0 ? 0 : upDown / denom;
                newRow["UpDownPercent"] = stock.nClose == 0 ? 0 : upDown / stock.nRef;
                newRow["TickQty"] = stock.nTickQty;
                newRow["RefPrice"] = stock.nRef / denom;
                newRow["Bid"] = stock.nBid / denom;
                newRow["BidQty"] = stock.nBc;
                newRow["Ask"] = stock.nAsk / denom;
                newRow["AskQty"] = stock.nAc;
                newRow["TotalQty"] = stock.nTQty;
                newRow["Simulate"] = stock.nSimulate;
                newRow["LastDeal"] = newRow["Deal"];
                newRow["LastBid"] = newRow["Bid"];
                newRow["LastAsk"] = newRow["Ask"];

                portfolioQuoteTable.Rows.Add(newRow);
            }
        }

        private void UpdateQuotes(SKSTOCKLONG stock)
        {
            //WriteMessage("Update Quote", $"{stock.sStockIdx}, TickQty = {stock.nTickQty}, TotalQty = {stock.nTQty}");
            string symbolCode = stock.bstrStockNo;
            double denom = Math.Pow(10, stock.sDecimal);
            double upDown = stock.nClose == 0 ? 0 : (stock.nClose - stock.nRef) / denom;

            // 1. Update QuoteTable 中該股票的資料
            DataRow rowFind = portfolioQuoteTable.Rows.Find(symbolCode);
            if (rowFind != null)
            {
                rowFind["LastDeal"] = rowFind["Deal"];
                rowFind["LastBid"] = rowFind["Bid"];
                rowFind["LastAsk"] =  rowFind["Ask"];
                rowFind["Open"] = stock.nOpen / denom;
                rowFind["RefPrice"] = stock.nRef / denom;
                rowFind["High"] = stock.nHigh / denom;
                rowFind["Low"] = stock.nLow / denom;                    
                rowFind["Deal"] = stock.nClose / denom;                    
                rowFind["UpDown"] = upDown;
                rowFind["UpDownPercent"] = upDown / (stock.nRef / denom);                
                rowFind["Bid"] = stock.nBid / denom;                
                rowFind["BidQty"] = stock.nBc;
                rowFind["Ask"] = stock.nAsk / denom;                            
                rowFind["AskQty"] = stock.nAc;
                rowFind["TickQty"] = stock.nTickQty;
                rowFind["TotalQty"] = stock.nTQty;  
                rowFind["Simulate"] = stock.nSimulate;
            }

            // 2.Update SpecificQuoteGrid 的資料
            if (symbolCode == specificSymbol.Code)
            {
                SpecificQuoteGrid.Rows[0].Cells["ClosePrice"].Value = (stock.nClose / denom);
                SpecificQuoteGrid.Rows[0].Cells["UpDown"].Value = upDown;                    
                SpecificQuoteGrid.Rows[0].Cells["UpDownPercent"].Value = upDown / (stock.nRef / denom);  
                SpecificQuoteGrid.Rows[0].Cells["TQty"].Value = stock.nTQty;
            }

            // 3. Update 現貨指數
            if (symbolCode == "TSEA")
            {
                SpotPriceLabel.Text = $"{(stock.nClose / denom):N2}";
                SpotUpDownLabel.Text = $"{upDown:▲ #,##0.00;▼ #,##0.00}";
                if (upDown >= 0)
                {
                    SpotPriceLabel.ForeColor = Color.Red;
                    SpotUpDownLabel.ForeColor = Color.Red;
                }
                else
                {
                    SpotPriceLabel.ForeColor = Color.Green;
                    SpotUpDownLabel.ForeColor = Color.Green;
                }
            }
            else if (symbolCode == "OTCA")
            {
                OTCPriceLabel.Text = $"{(stock.nClose / denom):N2}";
                OTCUpDownLabel.Text = $"{upDown:▲ #,##0.00;▼ #,##0.00}";
                if (upDown >= 0)
                {
                    OTCPriceLabel.ForeColor = Color.Red;
                    OTCUpDownLabel.ForeColor = Color.Red;
                }
                else
                {
                    OTCPriceLabel.ForeColor = Color.Green;
                    OTCUpDownLabel.ForeColor = Color.Green;
                }
            }
        }
               
        private void ResetDOM(int bestBid)
        {
            double price = bestBid / domDenom;
            int unFormatPrice;
            for (int i = 20; i < 40; i++)
            {
                unFormatPrice = Convert.ToInt32(price * domDenom);
                DOMGrid.Rows[i].Cells["BuyOrderQty"].Value = 0;
                DOMGrid.Rows[i].Cells["BuyOrderType"].Value = string.Empty;
                DOMGrid.Rows[i].Cells["BidQty"].Value = 0;
                DOMGrid.Rows[i].Cells["Price"].Value = price;
                DOMGrid.Rows[i].Cells["UnFormatPrice"].Value = unFormatPrice;
                DOMGrid.Rows[i].Cells["AskQty"].Value = 0;
                DOMGrid.Rows[i].Cells["SellOrderQty"].Value = 0;
                DOMGrid.Rows[i].Cells["SellOrderType"].Value = string.Empty;
                domLadder[i] = unFormatPrice;

                price = NextDownPrice(price);
            }

            price = NextUpPrice(bestBid / domDenom);
            for (int i = 19; i >= 0; i--)
            {
                unFormatPrice = Convert.ToInt32(price * domDenom);
                DOMGrid.Rows[i].Cells["BuyOrderQty"].Value = 0;
                DOMGrid.Rows[i].Cells["BidQty"].Value = 0;
                DOMGrid.Rows[i].Cells["Price"].Value = price;
                DOMGrid.Rows[i].Cells["UnFormatPrice"].Value = unFormatPrice;
                DOMGrid.Rows[i].Cells["AskQty"].Value = 0;
                DOMGrid.Rows[i].Cells["SellOrderQty"].Value = 0;
                domLadder[i] = unFormatPrice;

                price = NextUpPrice(price);
            }

            // Attach Orders
            var pendingOrders = orderBook.Where(o => (o.Symbol == domSymbol.Code && o.RemainQty != 0));
            for (int i = 0; i < 40; i++)
            {
                price = domLadder[i];
                var ordersAtThisPrice = pendingOrders.Where(o => (o.OrderType == "限價單" && Convert.ToInt32(o.OrderPrice * domDenom) == price)
                    || (o.OrderType == "停損單" && Convert.ToInt32(o.TriggerPrice * domDenom) == price));
                int buyQty = ordersAtThisPrice.Where(o => o.BuySell == "買進").Sum(o => o.OrderQty);
                int sellQty = ordersAtThisPrice.Where(o => o.BuySell == "賣出").Sum(o => o.OrderQty);
                if (buyQty != 0)
                {
                    DOMGrid.Rows[i].Cells["BuyOrderQty"].Value = buyQty;
                    DOMGrid.Rows[i].Cells["BuyOrderType"].Value = ordersAtThisPrice.First().OrderType;
                }
                if (sellQty != 0)
                {
                    DOMGrid.Rows[i].Cells["SellOrderQty"].Value = sellQty;
                    DOMGrid.Rows[i].Cells["SellOrderType"].Value = ordersAtThisPrice.First().OrderType;
                }
            }
        }
        
        private double NextDownPrice(double price)
        {
            if (domSymbol.MarketType == "TF")
            {
                return price - domSymbol.PIP;
            }
            else if (domSymbol.MarketType =="TO")
            {
                if (price <= 10)
                    return price - 0.1;
                else if (price <= 50)
                    return price - 0.5;
                else if (price <= 500)
                    return price - 1;
                else if (price <= 1000)
                    return price - 5;
                else
                    return price - 10;                
            }
            else
            {
                if (price <= 10)
                    return price - 0.01;
                else if (price <= 50)
                    return price - 0.05;
                else if (price <= 100)
                    return price - 0.10;
                else if (price <= 500)
                    return price - 0.5;
                else if (price <= 1000)
                    return price - 1;
                else
                    return price - 5;                
            }
        }
        
        private double NextUpPrice(double price)
        {
            if (domSymbol.MarketType == "TF" )
            {
                return price + domSymbol.PIP;
            }
            else if (domSymbol.MarketType == "TO")
            {
                if (price >= 1000)
                    return price + 10;
                else if (price >= 500)
                    return price + 5;
                else if (price >= 50)
                    return price + 1;
                else if (price >= 10)
                    return price + 0.5;
                else
                    return price + 0.1;   
            }
            else
            {
                if (price >= 1000)
                {
                    return price + 5;
                }
                else if (price >= 500)
                {
                    return price + 1;
                }
                else if (price >= 100)
                {
                    return price + 0.50;
                }
                else if (price >= 50)
                {
                    return price + 0.10;
                }
                else if (price >= 10)
                {
                    return price + 0.05;
                }
                else
                {
                    return price + 0.01;
                }
            }
        }

        private void UpdateDOMWithBest5(SKBEST5 best5)
        {
            if (best5.nBid1 >= domLadder[2] || best5.nBid1 <= domLadder[38])
                ResetDOM(best5.nBid1);

            for (int i = 0; i < 40; i++)
            {
                DOMGrid.Rows[i].Cells["BidQty"].Value = 0;
                DOMGrid.Rows[i].Cells["AskQty"].Value = 0;
            }

            int row = Array.IndexOf(domLadder, best5.nAsk5);
            if (row != -1)
                DOMGrid.Rows[row].Cells["AskQty"].Value = best5.nAskQty5;

            row = Array.IndexOf(domLadder, best5.nAsk4);
            if (row != -1)
                DOMGrid.Rows[row].Cells["AskQty"].Value = best5.nAskQty4;

            row = Array.IndexOf(domLadder, best5.nAsk3);
            if (row != -1)
                DOMGrid.Rows[row].Cells["AskQty"].Value = best5.nAskQty3;

            row = Array.IndexOf(domLadder, best5.nAsk2);
            if (row != -1)
                DOMGrid.Rows[row].Cells["AskQty"].Value = best5.nAskQty2;

            row = Array.IndexOf(domLadder, best5.nAsk1);
            if (row != -1)
                DOMGrid.Rows[row].Cells["AskQty"].Value = best5.nAskQty1;

            row = Array.IndexOf(domLadder, best5.nBid1);
            if (row != -1)
                DOMGrid.Rows[row].Cells["BidQty"].Value = best5.nBidQty1;

            row = Array.IndexOf(domLadder, best5.nBid2);
            if (row != -1)
                DOMGrid.Rows[row].Cells["BidQty"].Value = best5.nBidQty2;

            row = Array.IndexOf(domLadder, best5.nBid3);
            if (row != -1)
                DOMGrid.Rows[row].Cells["BidQty"].Value = best5.nBidQty3;

            row = Array.IndexOf(domLadder, best5.nBid4);
            if (row != -1)
                DOMGrid.Rows[row].Cells["BidQty"].Value = best5.nBidQty4;

            row = Array.IndexOf(domLadder, (best5.nBid5));
            if (row != -1)
                DOMGrid.Rows[row].Cells["BidQty"].Value = best5.nBidQty5;

            domBestAsk = best5.nAsk1 / domDenom;
            domBestBid = best5.nBid1 / domDenom;
        }

        private void UpdateDOMWithTick(int nClose, int nQty, int nSimulate)
        {
            int row = Array.IndexOf(domLadder, nClose);
            if (row != -1)
            {
                if (row != domLastTickRow)
                {                  
                    if (domLastTickRow != -1)
                    {
                        DOMGrid["Price", domLastTickRow].Style.BackColor = SystemColors.Control;
                        DOMGrid["Price", domLastTickRow].Value = (double)domLadder[domLastTickRow] / domDenom;
                    }
                }
                DOMGrid["Price", row].Value = nQty.ToString("(#) ") + (nClose / domDenom).ToString(domSymbol.PriceFormat);                   
                DOMGrid["Price", row].Style.BackColor = Color.Yellow;
                domLastTickRow = row;
            }
        }
        
        private void UpdateDOMWithOrderTransaction(string transType, string orderType, string buySell, double price, int qty)
        {
            int uPrice = Convert.ToInt32(price * domDenom);
            int rowIndex = Array.IndexOf(domLadder, uPrice);
            string columnIndex;
            string columnIndex2;
            if (rowIndex >= 0)
            {
                columnIndex = buySell == "買進" ? "BuyOrderQty" : "SellOrderQty";
                columnIndex2 = buySell == "買進" ? "BuyOrderType" : "SellOrderType";
                if (transType == "N")
                {
                    DOMGrid[columnIndex, rowIndex].Value = int.Parse(DOMGrid[columnIndex, rowIndex].Value.ToString()) + qty;
                    DOMGrid[columnIndex2, rowIndex].Value = orderType;
                }
                else
                {
                    DOMGrid[columnIndex, rowIndex].Value = int.Parse(DOMGrid[columnIndex, rowIndex].Value.ToString()) - qty;
                    DOMGrid[columnIndex2, rowIndex].Value = orderType;
                }
            }
        }

        private void ChartReset()
        {            
            kBars.Clear();
            chart.DataBind();
        }

        private void UpdateChart(int nPtr, int nTimehms, int nClose, int nQty, int nSimulate)
        {
            if (kBars.Count == 0 )    //  首次採批次合成, 之後則由個別Tick加入
            {
                ConvertTicksToBars(ticks);
                //kBars.Add(new KBar(nTimehms, nClose / specificDenom, nClose / specificDenom, nClose / specificDenom, nClose / specificDenom, nQty));
                //chart.DataBind();
            }
            else 
            {                
                double price = nClose / specificDenom;
                int timeStamp = GetBarTime(nTimehms);
                KBar currentBar = kBars[kBars.Count - 1];
                          
                if (timeStamp == currentBar.TimeStamp)        // Tick 在目前Bar的時間範圍內, 更新高低收量
                {                     
                    if (price > currentBar.High)
                        currentBar.High = price;
                    if (price < currentBar.Low)
                        currentBar.Low = price;
                    currentBar.Close = price;
                    currentBar.Volume += nQty;

                    if (DateTime.Now > chartLastUpdatedTime + TimeSpan.FromSeconds(1))
                    {                    
                        chart.Series["Prices"].Points[kBars.Count - 1].SetValueY(currentBar.High, currentBar.Low, currentBar.Open, currentBar.Close);
                        chart.Series["Volume"].Points[kBars.Count - 1].SetValueY(currentBar.Volume);
                        chart.Refresh();
                        chartLastUpdatedTime = DateTime.Now;
                    }
                }
                else  //新Bar開始
                {
                    // 補缺分
                    int lackBarTime = NextBarTime(currentBar.TimeStamp);
                    while (lackBarTime != timeStamp)
                    {
                        KBar lackBar = new KBar(lackBarTime, currentBar.Close, currentBar.Close, currentBar.Close, currentBar.Close, 0);
                        ChartAddBar(lackBar);
                        lackBarTime = NextBarTime(lackBar.TimeStamp);
                    }
                    currentBar = new KBar(timeStamp, price, price, price, price, nQty);
                    ChartAddBar(currentBar);                
                }
            }
        }
        
        private void ConvertTicksToBars(List<Tick> ticks)
        {
            List<KBar> tempBars = new List<KBar>();
            var bars = from tick in ticks
                       where (tick.TickSimulate == 0)
                       orderby tick.TickPtr
                       let stamp = GetBarTime(tick.TickTime)
                       group tick by stamp into tickGroup
                       let barPrices = tickGroup.Select(t => t.TickPrice)
                       select new KBar()
                       {
                           TimeStamp = tickGroup.Key,
                           Open = barPrices.First(),
                           Close = barPrices.Last(),
                           High = barPrices.Max(),
                           Low = barPrices.Min(),
                           Volume = tickGroup.Sum(t => t.TickQty)
                       };
          
            if (TBarsOnlyCheckBox.Checked)
                bars = bars.Where(b => b.TimeStamp >= session2BeginTime && b.TimeStamp <= session2EndTime);

            int nextBarTime = TBarsOnlyCheckBox.Checked ? session2BeginTime : session1BeginTime;
            double latestClose = 0;
            foreach (var bar in bars)
            {                
                //補缺分
                while (nextBarTime != bar.TimeStamp)
                {
                    KBar lackBar = new KBar(nextBarTime, latestClose, latestClose, latestClose, latestClose, 0);
                    kBars.Add(lackBar);
                    nextBarTime = NextBarTime(nextBarTime);
                }
                kBars.Add(bar);
                nextBarTime=NextBarTime(bar.TimeStamp);
                latestClose = bar.Close;
            }
         
            chart.DataBind();
            
            chart.Refresh();
            
            //if (chart.ChartAreas[1].AxisX.Maximum > chart.ChartAreas[1].AxisX.ScaleView.Size)
            //    chart.ChartAreas[1].AxisX.ScaleView.Scroll(chart.ChartAreas[1].AxisX.Maximum);
            chartLastUpdatedTime = DateTime.Now;
        }

        private void ChartAddBar(KBar bar)
        {
            kBars.Add(bar);
            chart.Series["Prices"].Points.AddXY(bar.TimeStamp, bar.High, bar.Low, bar.Open, bar.Close);
            chart.Series["Volume"].Points.AddXY(bar.TimeStamp, bar.Volume);
            //if (chart.ChartAreas[1].AxisX.Maximum > chart.ChartAreas[1].AxisX.ScaleView.Size)
            //    chart.ChartAreas[1].AxisX.ScaleView.Scroll(chart.ChartAreas[1].AxisX.Maximum);
            chartLastUpdatedTime = DateTime.Now;
        }
      
        private int GetBarTime(int tickTime)
        {
            int barTime = tickTime / 100 + 1;
            if (barTime % 100 == 60)
            {
                barTime = (barTime / 100 + 1) * 100;
                if (barTime == 2400)
                    barTime = 0;
            }
            if (barTime > session2EndTime && (barTime < session1BeginTime || session1BeginTime < session2EndTime))
                return session2EndTime;
            else if (barTime > session1EndTime && barTime < session2BeginTime)
                return session1EndTime;
            else
                return barTime;
        }
                
        private int NextBarTime(int barTime)
        {
            if (barTime == session2EndTime)
                return session1BeginTime;
            else if (barTime == session1EndTime)
                return session2BeginTime;
            else
            {
                barTime++;
                if (barTime % 100 == 60)
                {
                    barTime = (barTime / 100 + 1) * 100;
                    if (barTime == 2400)
                        barTime = 0;
                }
                return barTime;
            }
        }
       
        private void TicksGrid_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
       
            switch (e.ColumnIndex)
            {
                case 0:
                    e.Value = ticks[e.RowIndex].TickPtr;
                    break;
                case 1:
                    e.Value = ticks[e.RowIndex].TickTime;
                    break;
                case 2:
                    e.Value = ticks[e.RowIndex].TickPrice;
                    break;
                case 3:
                    e.Value = ticks[e.RowIndex].TickQty;
                    break;
                case 4:
                    e.Value = ticks[e.RowIndex].TickSimulate;
                    break;
            }
        }

        private void WriteMessage(string s)
        {
            MessageLog.Items.Add(s + "【" + DateTime.Now.ToString("HH:mm:ss fff") + "】");
            MessageLog.SelectedIndex = MessageLog.Items.Count - 1;
        }
        
        private void WriteMessage(string source, string msg)
        {
            WriteMessage($"【{source}】【{msg}】");
        }
        
        private void WriteMessage(string ALT, string custom, int code)
        {
            if (code == 0)
                WriteMessage($"【{ALT}】【{custom}】【{skCenter.SKCenterLib_GetReturnCodeMessage(code)}】");
            else
                WriteMessage($"【{ALT}】【{custom}】【{skCenter.SKCenterLib_GetReturnCodeMessage(code)}】【{skCenter.SKCenterLib_GetLastLogInfo()}】");
        }

        #endregion

        #region DataGridViews Formatting Routines 
        //OK
        private void PortfolioQuoteGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            int rowNo = e.RowIndex;
            if (rowNo < 0) return;

            string title = PortfolioQuoteGrid.Columns[e.ColumnIndex].Name;
            double preClose = (double)PortfolioQuoteGrid.Rows[rowNo].Cells["RefPrice"].Value;
            string priceFormat = PortfolioQuoteGrid.Rows[rowNo].Cells["PriceFormat"].Value.ToString();

            if (title == "Ask" || title == "Bid" || title == "Deal" || title == "High" || title == "Low")
            {
                double price = (double)e.Value;
                if (price > preClose)
                {
                    e.CellStyle.ForeColor = Color.Red;
                    e.CellStyle.SelectionForeColor = Color.Red;
                }
                else if (price < preClose)
                {
                    e.CellStyle.ForeColor = Color.LimeGreen;
                    e.CellStyle.SelectionForeColor = Color.LimeGreen;
                }

                e.CellStyle.Format = priceFormat;
            }
            else if (title == "TickQty")
            {
                double deal = (double)PortfolioQuoteGrid.Rows[rowNo].Cells["Deal"].Value; 
                double lastDeal = (double)PortfolioQuoteGrid.Rows[rowNo].Cells["LastDeal"].Value;
                double lastBid = (double)PortfolioQuoteGrid.Rows[rowNo].Cells["LastBid"].Value;
                double lastAsk = (double)PortfolioQuoteGrid.Rows[rowNo].Cells["LastAsk"].Value;
                
                if (deal > lastDeal)
                    e.CellStyle.ForeColor = Color.Red;
                else if (deal < lastDeal)
                    e.CellStyle.ForeColor = Color.LimeGreen;
                else
                {
                    if (deal >= lastAsk)
                        e.CellStyle.ForeColor = Color.Red;
                    else if (deal <= lastBid)
                        e.CellStyle.ForeColor = Color.LimeGreen;
                    else
                        e.CellStyle.ForeColor = Color.White;                    
                }

                if (PortfolioQuoteGrid.Rows[rowNo].Cells["Simulate"].Value.ToString() == "1")
                {
                    PortfolioQuoteGrid.Rows[rowNo].DefaultCellStyle.BackColor = Color.Gray;
                    PortfolioQuoteGrid.Rows[rowNo].DefaultCellStyle.SelectionBackColor = Color.Gray;
                }
                else
                    PortfolioQuoteGrid.Rows[rowNo].DefaultCellStyle = null;
            }
            else if (title == "UpOrDown" || title == "UpOrDownPercent")
            {
                if (title == "UpOrDown")
                {
                    if (priceFormat == "N0")
                        e.CellStyle.Format = "▲     0;▼     0";
                    else
                        e.CellStyle.Format = "▲  0.00;▼  0.00";
                }

                if (double.Parse(e.Value.ToString()) > 0)
                {
                    e.CellStyle.ForeColor = Color.Red;
                    e.CellStyle.SelectionForeColor = Color.Red;
                }
                else if (double.Parse(e.Value.ToString()) < 0)
                {
                    e.CellStyle.ForeColor = Color.LimeGreen;
                    e.CellStyle.SelectionForeColor = Color.LimeGreen;
                }
            }
        }

        private void SpecificQuoteGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex == 0 && e.Value != null)
            {
                string title = SpecificQuoteGrid.Columns[e.ColumnIndex].Name;
                double upDown = (double)SpecificQuoteGrid.Rows[0].Cells["UpDown"].Value;
                if (title == "ClosePrice" || title == "UpDown" || title == "UpDownPercent")
                {
                    if (upDown > 0)
                        e.CellStyle.ForeColor = Color.Red;
                    else if (upDown < 0)
                        e.CellStyle.ForeColor = Color.LimeGreen;                       
                    else
                        e.CellStyle.ForeColor = Color.White;                   
                }
            }
        }

        private void Best5Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == Best5Grid.Columns["BidPrice"].Index || e.ColumnIndex == Best5Grid.Columns["AskPrice"].Index)
            {
                double price = double.Parse(e.Value.ToString());
                if (price > specificRef)
                {
                    e.CellStyle.ForeColor = Color.Red;
                    e.CellStyle.SelectionForeColor = Color.Red;
                }
                else if (price < specificRef)
                {
                    e.CellStyle.ForeColor = Color.Lime;
                    e.CellStyle.SelectionForeColor = Color.Lime;
                }
            }
        }

        private void TicksGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == TicksGrid.Columns["TickPrice"].Index)
            {
                if ((int)TicksGrid.Rows[e.RowIndex].Cells["TickSimulate"].Value == 1)
                {
                    TicksGrid.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.Gray;
                    TicksGrid.Rows[e.RowIndex].DefaultCellStyle.SelectionBackColor = Color.Gray;
                }
                else
                    TicksGrid.Rows[e.RowIndex].DefaultCellStyle = null;

                double tickPrice = (double)e.Value;
                if (tickPrice > specificRef)
                {
                    e.CellStyle.ForeColor = Color.Red;
                    e.CellStyle.SelectionForeColor = Color.Red;
                }
                else if (tickPrice < specificRef)
                {
                    e.CellStyle.ForeColor = Color.Lime;
                    e.CellStyle.SelectionForeColor = Color.Lime;
                }

            }
            else if (e.ColumnIndex == TicksGrid.Columns["TickQuantity"].Index)  //對應到TicksGrid 中 TickQuantity 欄位，進行判斷
            {
                //if (ticks[e.RowIndex].TickPtr == 375)
                //{
                //    int testc = 0;
                //}
                if (ticks[e.RowIndex].TickPrice <= ticks[e.RowIndex].TickBid) //因為TickGrid資料與ticks資料是相呼應的，所以取ticks列資料來判斷，若成交價小於等於買價，即為賣張(綠)
                {
                    e.CellStyle.ForeColor = Color.Lime;
                    e.CellStyle.SelectionForeColor = Color.Lime;
                }
                else if (ticks[e.RowIndex].TickPrice >= ticks[e.RowIndex].TickAsk) //反之成交價大於等於買價，即為買張(紅)
                {
                    e.CellStyle.ForeColor = Color.Red;
                    e.CellStyle.SelectionForeColor = Color.Red;
                }
            }

        }

        private void TradeInfoGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if ((e.ColumnIndex == 1 || e.ColumnIndex == 2) && (e.RowIndex >= 2 ))
            {
                double val = double.Parse(e.Value.ToString());
                if (val > 0)
                {
                    e.CellStyle.ForeColor = Color.Red;
                    e.CellStyle.SelectionForeColor = Color.Red;
                }
                else if (val < 0)
                {
                    e.CellStyle.ForeColor = Color.LimeGreen;
                    e.CellStyle.SelectionForeColor = Color.LimeGreen;
                }
            }
        }

        private void OrderBookGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == OrderBookGrid.Columns["OrderStatus"].Index)
            {
                if (e.Value.ToString() == "尚未成交" || e.Value.ToString() == "尚未觸發")
                    OrderBookGrid.Rows[e.RowIndex].Cells["Delete"].Value = "刪";
                else
                    OrderBookGrid.Rows[e.RowIndex].Cells["Delete"].Value = "";

                if (e.Value.ToString() == "全部成交")
                    e.CellStyle.BackColor = Color.MistyRose;
                else if (e.Value.ToString() == "全部取消")
                    e.CellStyle.BackColor = Color.GreenYellow;
            }
            else if (e.ColumnIndex == OrderBookGrid.Columns["OrderType"].Index)
            {
                if (e.Value.ToString() == "停損單")
                    e.CellStyle.BackColor = Color.Orange;               
            }
            else if (e.ColumnIndex == OrderBookGrid.Columns["BuySell"].Index)
            {
                if (e.Value.ToString() == "買進")
                    e.CellStyle.BackColor = Color.Pink;
                else
                    e.CellStyle.BackColor = Color.PowderBlue;
            }
            else if (e.ColumnIndex == OrderBookGrid.Columns["OrderPrice"].Index)
            {
                if ((double)e.Value == 0.0)
                    e.Value ="市價";
            }
        }

        private void DOMGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == DOMGrid.Columns["BuyOrderQty"].Index)
            {
                int qty = (int)e.Value;
                if (qty != 0)
                {
                    DOMGrid.Rows[e.RowIndex].Cells["BuyDelete"].Value = "刪";
                    DOMGrid["BidQty", e.RowIndex].Style.BackColor = Color.PaleVioletRed;
                    DOMGrid["BidQty", e.RowIndex].Style.SelectionBackColor = Color.PaleVioletRed;
                    DOMGrid["BidQty", e.RowIndex].Style.Format =
                        (string)DOMGrid["BuyOrderType", e.RowIndex].Value == "限價單" ? "(限價單) #,###" : "(停止單) #,###";
                }
                else
                {
                    DOMGrid.Rows[e.RowIndex].Cells["BuyDelete"].Value = "";
                    DOMGrid["BidQty", e.RowIndex].Style = null;
                }
            }
            else if (e.ColumnIndex == DOMGrid.Columns["SellOrderQty"].Index)
            {
                int qty = (int)e.Value;
                if (qty != 0)
                {
                    DOMGrid.Rows[e.RowIndex].Cells["SellDelete"].Value = "刪";
                    DOMGrid["AskQty", e.RowIndex].Style.BackColor = Color.LightBlue;
                    DOMGrid["AskQty", e.RowIndex].Style.SelectionBackColor = Color.LightBlue;
                    DOMGrid["AskQty", e.RowIndex].Style.Format =
                        (string)DOMGrid["SellOrderType", e.RowIndex].Value == "限價單" ? "(限價單) #,###" : "(停止單) #,###";
                }
                else
                {
                    DOMGrid.Rows[e.RowIndex].Cells["SellDelete"].Value = "";
                    DOMGrid["AskQty", e.RowIndex].Style = null;
                }
            }
        }

        private void DOMGrid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.ColumnIndex == 0 || e.ColumnIndex == 1 || e.ColumnIndex == 7 || e.ColumnIndex == 9)
            {
                if (e.RowIndex < 0)
                {
                    e.Graphics.FillRectangle(new SolidBrush(Color.Gray), e.CellBounds);
                }
                else
                {
                    e.PaintBackground(e.ClipBounds, false);
                    e.PaintContent(e.ClipBounds);

                    if (e.FormattedValue.ToString() != "")
                    {
                        if (e.RowIndex >= 1 && DOMGrid[e.ColumnIndex, e.RowIndex - 1].Value.ToString() != "")
                        {
                            if (e.ColumnIndex == 0 || e.ColumnIndex == 7)
                                e.Graphics.DrawLine(Pens.Gray, e.CellBounds.X, e.CellBounds.Y - 1, e.CellBounds.X + Width, e.CellBounds.Y - 1);
                            else
                                e.Graphics.DrawLine(Pens.Gray, e.CellBounds.X, e.CellBounds.Y - 1, e.CellBounds.X + Width - 1, e.CellBounds.Y - 1);
                        }
                        Rectangle rect;
                        if (e.ColumnIndex == 0 || e.ColumnIndex == 7)
                            rect = new Rectangle(e.CellBounds.X, e.CellBounds.Y, e.CellBounds.Width, e.CellBounds.Height - 1);
                        else
                            rect = new Rectangle(e.CellBounds.X, e.CellBounds.Y, e.CellBounds.Width - 1, e.CellBounds.Height - 1);
                        e.Graphics.DrawRectangle(Pens.Black, rect);
                    }

                }
                e.Handled = true;
            }
        }

        #endregion

        private void button1_Click(object sender, EventArgs e)
        {
            int m_nCode = skQuote.SKQuoteLib_EnterMonitorLONG();
            skQuote.OnConnection += new _ISKQuoteLibEvents_OnConnectionEventHandler(m_SKQuoteLib_OnConnection);
            skQuote.OnNotifyQuoteLONG += new _ISKQuoteLibEvents_OnNotifyQuoteLONGEventHandler(m_SKQuoteLib_OnNotifyQuote);
            skQuote.OnNotifyBest5LONG += new _ISKQuoteLibEvents_OnNotifyBest5LONGEventHandler(m_SKQuoteLib_OnNotifyBest5);
            skQuote.OnNotifyHistoryTicksLONG += new _ISKQuoteLibEvents_OnNotifyHistoryTicksLONGEventHandler(m_SKQuoteLib_OnNotifyHistoryTicks);  //歷史成交明細
            skQuote.OnNotifyTicksLONG += new _ISKQuoteLibEvents_OnNotifyTicksLONGEventHandler(m_SKQuoteLib_OnNotifyTicks); //異動成交明細
            skQuote.OnNotifyFutureTradeInfoLONG += new _ISKQuoteLibEvents_OnNotifyFutureTradeInfoLONGEventHandler(m_SKQuoteLib_OnNotifyFutureTradeInfo);

        }
        void m_SKQuoteLib_OnConnection(int nKind, int nCode)
        {
            if (nKind == 3001)
            {

                if (nCode == 0)
                {
                    QuoteStatusLabel.ForeColor = Color.Yellow;
                }
            }
            else if (nKind == 3002)
            {
                QuoteStatusLabel.ForeColor = Color.Red;
            }
            else if (nKind == 3003 || nKind == 3036)
            {
                QuoteStatusLabel.ForeColor = Color.Green;
            }
            else if (nKind == 3021)//網路斷線
            {
                QuoteStatusLabel.ForeColor = Color.DarkRed;
            }
            else if (nKind == 3033)//異常
            {
                QuoteStatusLabel.ForeColor = Color.DimGray;
            }
        }

        void m_SKQuoteLib_OnNotifyBest5(short sMarketNo, int nStockIdx, int nBestBid1, int nBestBidQty1, int nBestBid2, int nBestBidQty2, int nBestBid3, int nBestBidQty3, int nBestBid4, int nBestBidQty4, int nBestBid5, int nBestBidQty5, int nExtendBid, int nExtendBidQty, int nBestAsk1, int nBestAskQty1, int nBestAsk2, int nBestAskQty2, int nBestAsk3, int nBestAskQty3, int nBestAsk4, int nBestAskQty4, int nBestAsk5, int nBestAskQty5, int nExtendAsk, int nExtendAskQty, int nSimulate)
        { 
            if (nStockIdx == specificStockIdx)
            {
                //WriteMessage("OnReceiveBest5", $"Receive Best5 of Specific Symbol = {specificSymbol.ID}");
                if (nSimulate == 0)
                    Best5Grid.BackColor = Color.Black;
                else
                    Best5Grid.BackColor = Color.Gray;

                // Update Best5Grid
                Best5Grid["BidQuantity", 0].Value = nBestBidQty1;
                Best5Grid["BidPrice", 0].Value = nBestBid1 / specificDenom;
                Best5Grid["AskPrice", 0].Value = nBestAsk1 / specificDenom;
                Best5Grid["AskQuantity", 0].Value = nBestAskQty1;
                Best5Grid["BidQuantity", 1].Value = nBestBidQty2;
                Best5Grid["BidPrice", 1].Value = nBestBid2 / specificDenom;
                Best5Grid["AskPrice", 1].Value = nBestAsk2 / specificDenom;
                Best5Grid["AskQuantity", 1].Value = nBestAskQty2;
                Best5Grid["BidQuantity", 2].Value = nBestBidQty3;
                Best5Grid["BidPrice", 2].Value = nBestBid3 / specificDenom;
                Best5Grid["AskPrice", 2].Value = nBestAsk3 / specificDenom;
                Best5Grid["AskQuantity", 2].Value = nBestAskQty3;
                Best5Grid["BidQuantity", 3].Value = nBestBidQty4;
                Best5Grid["BidPrice", 3].Value = nBestBid4 / specificDenom;
                Best5Grid["AskPrice", 3].Value = nBestAsk4 / specificDenom;
                Best5Grid["AskQuantity", 3].Value = nBestAskQty4;
                Best5Grid["BidQuantity", 4].Value = nBestBidQty5;
                Best5Grid["BidPrice", 4].Value = nBestBid5 / specificDenom;
                Best5Grid["AskPrice", 4].Value = nBestAsk5 / specificDenom;
                Best5Grid["AskQuantity", 4].Value = nBestAskQty5;
            }

            if (domStockIdx != -1 && nStockIdx == domStockIdx)
            {
                // Update DOM with Best5
                if (nBestBid1 >= domLadder[2] || nBestBid1 <= domLadder[38])
                    ResetDOM(nBestBid1);

                for (int i = 0; i < 40; i++)
                {
                    DOMGrid.Rows[i].Cells["BidQty"].Value = 0;
                    DOMGrid.Rows[i].Cells["AskQty"].Value = 0;
                }

                int row = Array.IndexOf(domLadder, nBestAsk5);
                if (row != -1)
                    DOMGrid.Rows[row].Cells["AskQty"].Value = nBestAskQty5;

                row = Array.IndexOf(domLadder, nBestAsk4);
                if (row != -1)
                    DOMGrid.Rows[row].Cells["AskQty"].Value = nBestAskQty4;

                row = Array.IndexOf(domLadder, nBestAsk3);
                if (row != -1)
                    DOMGrid.Rows[row].Cells["AskQty"].Value = nBestAskQty3;

                row = Array.IndexOf(domLadder, nBestAsk2);
                if (row != -1)
                    DOMGrid.Rows[row].Cells["AskQty"].Value = nBestAskQty2;

                row = Array.IndexOf(domLadder, nBestAsk1);
                if (row != -1)
                    DOMGrid.Rows[row].Cells["AskQty"].Value = nBestAskQty1;

                row = Array.IndexOf(domLadder, nBestBid1);
                if (row != -1)
                    DOMGrid.Rows[row].Cells["BidQty"].Value = nBestBidQty1;

                row = Array.IndexOf(domLadder, nBestBid2);
                if (row != -1)
                    DOMGrid.Rows[row].Cells["BidQty"].Value = nBestBidQty2;

                row = Array.IndexOf(domLadder, nBestBid3);
                if (row != -1)
                    DOMGrid.Rows[row].Cells["BidQty"].Value = nBestBidQty3;

                row = Array.IndexOf(domLadder, nBestBid4);
                if (row != -1)
                    DOMGrid.Rows[row].Cells["BidQty"].Value = nBestBidQty4;

                row = Array.IndexOf(domLadder, (nBestBid5));
                if (row != -1)
                    DOMGrid.Rows[row].Cells["BidQty"].Value = nBestBidQty5;

                domBestAsk = nBestAsk1 / domDenom;
                domBestBid = nBestBid1 / domDenom;
            }
        }

        void m_SKQuoteLib_OnNotifyHistoryTicks(short sMarketNo, int nStockIdx, int nPtr, int nDate, int nTimehms, int lTimemillismicros, int nBid, int nAsk, int nClose, int nQty, int nSimulate)
        {
            
            //string path = "E:\\Stock Program\\GOODINFO\\DataBase\\05_Tickdata\\" + TDate + "\\" + stockID + ".txt";
            //StreamWriter file = new StreamWriter(@path, true);
            

            if (nStockIdx == specificStockIdx)
            {
                if (nPtr == 0)
                {
                    WriteMessage("OnReceiveHistoryTicks", $"Specific Symbol {specificSymbol.ID} Receive First HisTick : Ptr = {nPtr}");
                    // 有可能已經收到了first live tick, 為了避免干擾tick的次序, 乾脆刪掉, 反正還會再收到
                    ticks.Clear();
                }
                ticks.Add(new Tick(nPtr, nTimehms, nClose, nQty, nSimulate, nBid, nAsk));

                //string txt = ticks[ticks.Count - 1].TickTime.ToString() + ", " + ticks[ticks.Count - 1].TickBid.ToString() + ", " + ticks[ticks.Count - 1].TickAsk.ToString() + ", " + ticks[ticks.Count - 1].TickPrice.ToString() + ", " + ticks[ticks.Count - 1].TickQty.ToString();


                //file.WriteLine(txt);



            }
            else if (nStockIdx != domStockIdx)
            {
                WriteMessage("OnReceiveHistoryTicks", "Receive Ticks of an Symbol other than Specific and DOM");
            }

            //file.Close();




        }

        void m_SKQuoteLib_OnNotifyTicks(short sMarketNo, int nStockIdx, int nPtr, int nDate, int lTimehms, int lTimemillismicros, int nBid, int nAsk, int nClose, int nQty, int nSimulate)
        {
            //WriteMessage($"SymBol Idx = {sStockIdx} Receive LiveTick : Ptr = {nPtr}");
            if (nStockIdx == specificStockIdx)
            {
                if (nPtr == 1 && firstSpecificLiveTick != -1 && lTimehms / 100 > session2EndTime)
                {
                    //系統持續開著, 從上午盤到下午盤,下午盤的第一根Tick時, ticks和kBars必須重設,反映新 Trading Day 的開始
                    WriteMessage("Info", "T+1 Session Start, Clear T Session Ticks and Chart");
                    TBarsOnlyCheckBox.Checked = false;
                    firstSpecificLiveTick = 1;
                    firstDOMLiveTick = 1;
                    TicksGrid.Rows.Clear();
                    ticks.Clear();
                    ChartReset();
                    SKSTOCKLONG stock = new SKSTOCKLONG();
                    //int code = skQuote.SKQuoteLib_GetStockByNo(specificSymbol.Code, ref stock);
                    int code = skQuote.SKQuoteLib_GetStockByIndexLONG(sMarketNo, nStockIdx, ref stock);
                    if (code == 0)
                        specificRef = stock.nRef / specificDenom;
                }

                ticks.Add(new Tick(nPtr, lTimehms, nClose, nQty, nSimulate, nBid, nAsk));
                if (firstSpecificLiveTick == -1)
                {
                    firstSpecificLiveTick = nPtr;
                    WriteMessage("OnReceiveTicks", $"Specific Symbol {specificSymbol.ID} Receive First LiveTick : Ptr = {nPtr}");
                }

                if (nPtr >= firstSpecificLiveTick)
                {
                    // TicksGrid Virtual Mode Management  
                    TicksGrid.RowCount = ticks.Count;
                    TicksGrid.FirstDisplayedScrollingRowIndex = TicksGrid.RowCount - 1;

                    // Update KBars only after all the historical(past) tick received in order to avoid unorder ticks
                    if (ticks.Count > 1)
                    {
                        int barTime = GetBarTime(lTimehms);
                        if (nSimulate == 0 && (!TBarsOnlyCheckBox.Checked || (barTime >= session2BeginTime && barTime <= session2EndTime)))
                        {
                            UpdateChart(nPtr, lTimehms, nClose, nQty, nSimulate);
                        }
                    }
                }
            }

            if (nStockIdx == domStockIdx)
            {
                if (firstDOMLiveTick == -1)
                {
                    firstDOMLiveTick = nPtr;
                    WriteMessage("OnReceiveTicks", $"DOM Symbol {domSymbol.ID} Receive First Live Tick : Ptr = {nPtr}");
                }

                //當Specific Symbol 變更時, 會重新RequestTicks, 可能會傳到DOM Symbol 的舊Ticks,故增加此一check
                if (nPtr >= firstDOMLiveTick)
                    UpdateDOMWithTick(nClose, nQty, nSimulate);
            }
        }

        void m_SKQuoteLib_OnNotifyQuote(short sMarketNo, int nStockIdx)
        {
            //WriteMessage("OnReceiveQuote", sStockIdx.ToString());
            SKSTOCKLONG stock = new SKSTOCKLONG();
            skQuote.SKQuoteLib_GetStockByIndexLONG(sMarketNo, nStockIdx, ref stock);
            UpdateQuotes(stock);
        }

        void m_SKQuoteLib_OnNotifyFutureTradeInfo(string bstrStockNo, short sMarketNo, int nStockIdx, int nBuyTotalCount, int nSellTotalCount, int nBuyTotalQty, int nSellTotalQty, int nBuyDealTotalCount, int nSellDealTotalCount)
        {
            //WriteMessage("OnReceiveFurtureTradeInfo", bstrStockNo);
            double avgBuy = (double)nBuyTotalQty / nBuyTotalCount;
            double avgSell = (double)nSellTotalQty / nSellTotalCount;
            int i = bstrStockNo == "TX00" ? 1 : 2;

            TradeInfoGrid[i, 0].Value = avgBuy;
            TradeInfoGrid[i, 1].Value = avgSell;
            TradeInfoGrid[i, 2].Value = avgBuy - avgSell;
            TradeInfoGrid[i, 3].Value = nBuyTotalCount - nSellTotalCount; ;
            TradeInfoGrid[i, 4].Value = nBuyTotalQty - nSellTotalQty; ;
            TradeInfoGrid[i, 5].Value = nSellDealTotalCount - nBuyDealTotalCount;
        }
    }
}