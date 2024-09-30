using System;
using System.Windows.Forms;
using MyOrderMaster.Models;

namespace MyOrderMaster
{
    public partial class SetUserForm : Form
    {
        public event EventHandler OnAccountReset;

        public SetUserForm()
        {
            InitializeComponent();
        }

        private void FormSetAccount_Load(object sender, EventArgs e)
        {
            User.GetUserInfo();
            IDTextBox.Text = User.ID;
            PasswordTextBox.Text = User.Password;
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            User.ID = IDTextBox.Text;
            User.Password = PasswordTextBox.Text;
            this.Close();
        }
            

        private void SaveButton_Click(object sender, EventArgs e)
        {
            User.ID = IDTextBox.Text;
            User.Password = PasswordTextBox.Text;
            User.SaveUserInfo();
            this.Close();
        }

        private void FormSetAccount_FormClosed(object sender, FormClosedEventArgs e)
        {
            OnAccountReset(sender, e);
        }
    }
}
