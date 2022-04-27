using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ExpressAnalyzer_2;

namespace TestExpressAnalyzer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //ExprAnalyzer ea = new ExprAnalyzer("1+2+a/4-a*b", "a=12.3,b=1.2e23,c=0.67,d=0.67,e=0.67,f=0.67,g=0.67,h=0.67,i=0.67,j=0.67,k=0.67,l=0.67,m=0.67,n=0.67,o=0.67,p=0.67,q=0.67,r=0.67,s=0.67,t=0.67");
            this.EA = new ExprAnalyzer();
            this.txtExpr.Text = @"E*2+3*e-2*sin(a+Pi/(10.02E2-b))/(2.23-c)";
            this.txtVars.Text = @"E = 2
e = -1
a=0
Pi=3.1415926535897932384626
b=0.996e+3
c=1.2";
            this.EA.SetVariableTable(this.txtVars.Text);
        }

        private ExprAnalyzer EA;

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                this.lblResult.Text = @"Result: " + EA.Value.ToString();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void txtVars_Leave(object sender, EventArgs e)
        {
            this.EA.SetVariableTable(this.txtVars.Text);
        }

        private void txtExpr_Leave(object sender, EventArgs e)
        {
            this.EA.Express = this.txtExpr.Text;
        }
    }
}