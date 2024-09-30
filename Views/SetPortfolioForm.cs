using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.IO;
using System.Windows.Forms;
using MyOrderMaster.Models;

namespace MyOrderMaster
{

    public partial class SetPortfolioForm : Form
    {
        public event EventHandler OnPortfolioSetting;
        public List<Symbol> Symbols { get; set; }
        public int CurrentIndex {get;set;}

        List<Portfolio> portfolios = new List<Portfolio>();
        bool justRenamed = false;
    
        public SetPortfolioForm()
        {
            InitializeComponent();
        }

        private void SetPortfolioForm_Load(object sender, EventArgs e)
        {
            SetupPortfolioGrid();
            PortfolioGrid.Rows[CurrentIndex].Selected = true;
            PopulateInputCheckedListBox();
            PopulateOutputCheckedListBox();
        }
        

        private void SetupPortfolioGrid()
        {
            string[] lines = File.ReadAllLines(@"Assets\PortfolioList.txt");

            foreach (string line in lines)
            {
                if (line != "" && line.Contains("|"))
                {
                    string[] separated = line.Split('|');
                    Portfolio port = new Portfolio()
                    {
                        ID = separated[0].Trim(),
                        Name = separated[1].Trim(),
                        Items = separated[2]
                    };
                    portfolios.Add(port);
                    PortfolioGrid.Rows.Add(port.ID, port.Name);                   
                }
            }
        }

        private void PopulateInputCheckedListBox()
        {
            foreach (var symbol in Symbols)
            {
                InputCheckedListBox.Items.Add(symbol.ID.PadRight(6) + "\t" + symbol.Name, false);                    
            }                      
        }

        private void PopulateOutputCheckedListBox()
        {
            OutputCheckedListBox.Items.Add("全選", false);
            OpenPortfolio(CurrentIndex);
        }
                 
        private void AddButton_Click(object sender, EventArgs e)
        {
            MoveCheckedItemsToOutputBox();
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            MoveCheckedItemsBackToInputBox();
        }

        private void OutputCheckedListBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.Index == 0 && e.NewValue == CheckState.Checked)
            { 
                for (int i = 1; i < OutputCheckedListBox.Items.Count; i++)
                        OutputCheckedListBox.SetItemChecked(i, true);
            }
        }

        private void MoveUp()
        {
            for (int i = 2; i < OutputCheckedListBox.Items.Count; i++)
            {
                if (OutputCheckedListBox.GetItemChecked(i))  //identify the checked item
                {
                    //swap with the top item(move up)
                    if (i > 1 && !OutputCheckedListBox.GetItemChecked(i - 1))
                    {
                        var item = OutputCheckedListBox.Items[i];
                        OutputCheckedListBox.Items.RemoveAt(i);
                        OutputCheckedListBox.Items.Insert(i - 1, item);
                        OutputCheckedListBox.SetItemChecked(i - 1, true);
                    }
                }
            }
        }

        private void MoveDown()
        {
            int startindex = OutputCheckedListBox.Items.Count - 2;
            for (int i = startindex; i > 0; i--)
            {
                if (OutputCheckedListBox.GetItemChecked(i))//identify the selected item
                {
                    //swap with the lower item(move down)
                    if (i <= startindex && !OutputCheckedListBox.GetItemChecked(i + 1))
                    {
                        var item = OutputCheckedListBox.Items[i];
                        OutputCheckedListBox.Items.RemoveAt(i);
                        OutputCheckedListBox.Items.Insert(i + 1, item);
                        OutputCheckedListBox.SetItemChecked(i + 1, true);                        
                    }
                }
            }
        }
             

        private void PortfolioGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
                OpenPortfolio(e.RowIndex);
        }
        
        private void OpenPortfolioButton_Click(object sender, EventArgs e)
        {
            int portfolioNo = PortfolioGrid.SelectedRows[0].Index;
            if (portfolioNo>=0)
                OpenPortfolio(portfolioNo);           
        }
        
        private void OpenPortfolio(int idx)
        {
            PortfolioLabel.Text = "自選股 - [" + portfolios[idx].ID + "]  " + portfolios[idx].Name;

            // Clear the Old Portfolio
            for (int i = 1; i <= OutputCheckedListBox.Items.Count - 1; i++)
            {
                OutputCheckedListBox.SetItemChecked(i, true);
            }
            MoveCheckedItemsBackToInputBox();

            // Populate with new Portfolio
            string[] stocks = portfolios[idx].Items.Split(',');
            foreach (string stock in stocks)
            {
                string codeSearch = stock.PadRight(6);
                int foundIndex = InputCheckedListBox.FindString(codeSearch);
                if (foundIndex >= 0)
                    InputCheckedListBox.SetItemChecked(foundIndex, true);            
                MoveCheckedItemsToOutputBox();
            }       
        }

        private void RenamePortfolioButton_Click(object sender, EventArgs e)
        {
            int rowIndex = PortfolioGrid.CurrentCell.RowIndex;
            PortfolioGrid.Rows[rowIndex].DefaultCellStyle.BackColor = Color.Pink;
            PortfolioGrid.CurrentCell = PortfolioGrid.Rows[rowIndex].Cells["PortfolioName"];
            PortfolioGrid.BeginEdit(true);
        }

        private void PortfolioGrid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            Portfolio port = portfolios[e.RowIndex];
            PortfolioGrid.Rows[e.RowIndex].DefaultCellStyle.BackColor = PortfolioGrid.DefaultCellStyle.BackColor;
            port.Name = PortfolioGrid[e.ColumnIndex, e.RowIndex].Value.ToString();
            portfolios[e.RowIndex] = port;
            justRenamed = true;            
        }

        private void ResetPortfolioButton_Click(object sender, EventArgs e)
        {
            Portfolio port = portfolios[PortfolioGrid.CurrentRow.Index];
            port.Items = "";
            portfolios[PortfolioGrid.CurrentRow.Index] = port;
            OutputCheckedListBox.SetItemChecked(0, true);
            MoveCheckedItemsBackToInputBox();
        }
            
        private void MoveCheckedItemsToOutputBox()
        {
            foreach (var item in InputCheckedListBox.CheckedItems)
            {
                OutputCheckedListBox.Items.Add(item);
            }
            while (InputCheckedListBox.CheckedItems.Count > 0)
            {
                InputCheckedListBox.Items.Remove(InputCheckedListBox.CheckedItems[0]);
            }
        }

        private void MoveCheckedItemsBackToInputBox()
        {
            for (int i = OutputCheckedListBox.Items.Count - 1; i > 0; i--)
            {
                if (OutputCheckedListBox.GetItemChecked(i))
                {
                    InputCheckedListBox.Items.Add(OutputCheckedListBox.Items[i]);
                    OutputCheckedListBox.Items.Remove(OutputCheckedListBox.Items[i]);
                }
            }
            OutputCheckedListBox.SetItemCheckState(0, CheckState.Unchecked);
        }

        private void MoveUpButton_Click(object sender, EventArgs e)
        {
            MoveUp();
        }

        private void MoveDownButton_Click(object sender, EventArgs e)
        {
            MoveDown();
        }

        private void SaveSettingButton_Click(object sender, EventArgs e)
        {
            string items = "";
            for (int i = 1; i < OutputCheckedListBox.Items.Count; i++)
            {
                items = items + "," + OutputCheckedListBox.Items[i].ToString().Substring(0, 6).Trim();
            }
            if (items!="")
                items = items.Remove(0, 1);

            int idx = PortfolioGrid.SelectedRows[0].Index;
            Portfolio port = portfolios[idx];
            port.Items = items;
            portfolios[idx]=port;

            using (StreamWriter file = new StreamWriter(@"Assets\PortfolioList.txt"))
            {
                foreach (Portfolio p in portfolios)
                {
                    string line = p.ID + "|" + p.Name + "|" + p.Items;
                    file.WriteLine(line);
                }               
            }
            MessageBox.Show("自選股設定已儲存", "Information");           
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void PortfolioGrid_SelectionChanged(object sender, EventArgs e)
        {
            int idx = PortfolioGrid.CurrentRow.Index;
            if (justRenamed)
            {
                justRenamed = false;
                PortfolioGrid.Rows[idx-1].Selected = true;
            }
        }

        
        private void SetPortfolioForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            OnPortfolioSetting(sender, e);
        }

        
    }
}
