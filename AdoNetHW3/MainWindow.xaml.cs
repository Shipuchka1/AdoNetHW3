using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Configuration;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Reflection;

namespace AdoNetHW3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //фильтр по дате отображается только для таблицы TrackMeter

        private string conStr = "";
        public SqlConnection con;
        public List<TextBox> boxes;
        public MainWindow()
        {
            InitializeComponent();
            conStr = "Data Source = DESKTOP-BL837NF; Initial Catalog = Auto; User Id = Natalya; Password = 12345";


            con = new SqlConnection(conStr);
            con.Open();
            SqlCommand cmdTables = new SqlCommand("select * from information_schema.tables", con);

            var readTables = cmdTables.ExecuteReader();
            while (readTables.Read())
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = readTables[2].ToString();
                TablesComboBox.Items.Add(item);
            }



            con.Close();
        }


        public void refui(string tname, string selectStr)
        {
            try
            {

            con.Open();

            if (tname == "TrackMeter")
                MeterWrapPanel.Visibility = Visibility.Visible;
            else
                MeterWrapPanel.Visibility = Visibility.Hidden;
            if (string.IsNullOrEmpty(selectStr))
            ColumnsComboBox.Items.Clear();
            boxes = new List<TextBox>();
            boxes.Clear();

            SqlCommand cmdColumns = new SqlCommand("EXEC sp_columns " + tname, con);
            var readColumns = cmdColumns.ExecuteReader();
            DataWrapPanel.Children.Clear();
            DataGridView.Columns.Clear();
            int count = 0;
            while (readColumns.Read())
            {
                
                if (count == 10) break;
                GridViewColumn col = new GridViewColumn();
                col.Header = readColumns[3].ToString();
                if(string.IsNullOrEmpty(selectStr))
                {
                    ComboBoxItem cbi = new ComboBoxItem();
                    cbi.Content = readColumns[3].ToString();
                    ColumnsComboBox.Items.Add(cbi);
                }
                
                col.DisplayMemberBinding = new Binding("field" + DataGridView.Columns.Count.ToString());
                DataGridView.Columns.Add(col);
                StackPanel stack = new StackPanel();
                Label lab = new Label();
                lab.Content = readColumns[3].ToString();
                TextBox txt = new TextBox();
                txt.Name = readColumns[3].ToString();
                if ((readColumns[5].ToString()).Contains("identity"))
                {
                    txt.IsReadOnly = true;
                }
                lab.Margin = new Thickness(10);
                txt.Margin = new Thickness(10);
                stack.Children.Add(lab);
                stack.Children.Add(txt);
                DataWrapPanel.Children.Add(stack);
                boxes.Add(txt);
                count++;
            }

            con.Close();
            con.Open();
            SqlCommand cmdData = new SqlCommand("select*from " + tname+selectStr, con);
            var readData = cmdData.ExecuteReader();
            List<Temp> eqs = new List<Temp>();
            while (readData.Read())
            {

                Temp temp = new Temp();

                for (int i = 0; i < readData.FieldCount && i < 10; i++)
                {

                    Type type = temp.GetType();
                    PropertyInfo prop = type.GetProperty("field" + i);

                    prop.SetValue(temp, readData[i].ToString(), null);

                }

                eqs.Add(temp);
            }
            DataListView.ItemsSource = eqs;

            con.Close();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        } 

        private void TablesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            ComboBoxItem t = (ComboBoxItem)((ComboBox)sender).SelectedItem;
            refui(t.Content.ToString(),"");
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            con.Open();
            string str = "";
            string str2 = "";
            foreach (TextBox item in boxes)
            {
                if (string.IsNullOrEmpty(item.Text))
                    continue;
               if (!item.IsReadOnly)
                    str += " " + item.Name + " = '" + item.Text + "',";
                else
                    str2 += item.Name + " = '" + item.Text+"',";
            }
            str = str.Trim(',');
            str2 = str2.Trim(',');
            string end = "Update " + ((ComboBoxItem)TablesComboBox.SelectedItem).Content.ToString() + " set" + str + " where " + str2;
            MessageBox.Show(end);
            SqlCommand com = new SqlCommand(end, con);
            com.ExecuteNonQuery();
            MessageBox.Show("Строка изменена");
            con.Close();
            refui(((ComboBoxItem)TablesComboBox.SelectedItem).Content.ToString(),"");
        }

    private void Insert_Click(object sender, RoutedEventArgs e)
        {
            con.Open();
            string names = "";
            string values = "";
            foreach (TextBox item in boxes)
            {
                if (string.IsNullOrEmpty(item.Text))
                    continue;
                if (!item.IsReadOnly)
                {
                    names += " " + item.Name + ",";
                    values += "'" + item.Text + "',";
                }
                    
               
            }
            names = names.Trim(',');
            values = values.Trim(',');
            string end = "Insert into " + ((ComboBoxItem)TablesComboBox.SelectedItem).Content.ToString() + " (" + names + ") values( " + values+")";
            MessageBox.Show(end);
            SqlCommand com = new SqlCommand(end, con);
            com.ExecuteNonQuery();
            MessageBox.Show("Строка добавлена");
            con.Close();
            refui(((ComboBoxItem)TablesComboBox.SelectedItem).Content.ToString(),"");
        }

       
        private void DataListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
           for(int i = 0; i<DataGridView.Columns.Count;i++)
            {
                Type type = typeof(Temp);
                PropertyInfo prop = type.GetProperty("field" + i);

              
                if(DataListView.SelectedIndex!=-1)
                boxes[i].Text =prop.GetValue(((Temp)(DataListView.SelectedItem))).ToString();
               
            }
        }

        private void SElectFilter_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)FilterCheckBox.IsChecked&&ColumnsComboBox.SelectedIndex!=-1)
            {
                string selStr = " where " + ((ComboBoxItem)ColumnsComboBox.SelectedItem).Content.ToString() + " = '" + ValueFilterTextBox.Text + "'";
                refui(((ComboBoxItem)TablesComboBox.SelectedItem).Content.ToString(), selStr);
            }

            else
                refui(((ComboBoxItem)TablesComboBox.SelectedItem).Content.ToString(), "");
        }

        private void SelectDateButton_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)SelectDateCheckBox.IsChecked&&BeginDatePicker.SelectedDate!=null&&EndDatePicker.SelectedDate!=null )
            {
                string selStr = " where dMeterDate between '" + BeginDatePicker.SelectedDate  + "' and '" + EndDatePicker.SelectedDate+"'";
                MessageBox.Show(selStr);
                refui(((ComboBoxItem)TablesComboBox.SelectedItem).Content.ToString(), selStr);
            }

            else
                refui(((ComboBoxItem)TablesComboBox.SelectedItem).Content.ToString(), "");
        }
    }

    public class Temp
    {
        public string field0 { get; set; }
        public string field1 { get; set; }
        public string field2 { get; set; }
        public string field3 { get; set; }
        public string field4 { get; set; }
        public string field5 { get; set; }
        public string field6 { get; set; }
        public string field7 { get; set; }
        public string field8 { get; set; }
        public string field9 { get; set; }
    }
}
