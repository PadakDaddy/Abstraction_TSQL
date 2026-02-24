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
using System.Data;
using System.Data.SqlClient;


namespace A03_abstraction
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // Check if the source table exists in the specified database.
        private void btnCheckSource_Click(object sender, RoutedEventArgs e)
        {
            // 1. Read values from text boxes
            string server = txtSourceServer.Text.Trim();
            string database = txtSourceDatabase.Text.Trim();
            string table = txtSourceTable.Text.Trim();

            // Basic input validation
            if (string.IsNullOrEmpty(server) ||
                string.IsNullOrEmpty(database) ||
                string.IsNullOrEmpty(table))
            {
                txtResult.Text = "Please enter server, database, and table names.";
                return;
            }

            // 2. Build connection string (Windows Authentication)
            string connString =
                $"Server={server};Database={database};Integrated Security=true;";

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open(); // If this fails, server or database is likely wrong.

                    // 3. Check if the source table exists
                    string checkTableSql = @"
                        SELECT 1
                        FROM INFORMATION_SCHEMA.TABLES
                        WHERE TABLE_NAME = @TableName;
                    ";

                    using (SqlCommand cmd = new SqlCommand(checkTableSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@TableName", table);

                        object result = cmd.ExecuteScalar();

                        if (result != null)
                        {
                            txtResult.Text = "Source table exists.";
                        }
                        else
                        {
                            txtResult.Text = "Source table does NOT exist.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Show connection or query error
                txtResult.Text = "Error: " + ex.Message;
            }
        }
    }
}