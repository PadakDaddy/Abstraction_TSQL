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

        // Helper method to check if a table exists in a given server/database.
        private bool TableExists(string server, string database, string tableName, out string errorMessage)
        {
            errorMessage = string.Empty;

            string connString =
                $"Server={server};Database={database};Integrated Security=true;";

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();

                    string checkTableSql = @"
                        SELECT 1
                        FROM INFORMATION_SCHEMA.TABLES
                        WHERE TABLE_NAME = @TableName;
                    ";

                    using (SqlCommand cmd = new SqlCommand(checkTableSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@TableName", tableName);

                        object result = cmd.ExecuteScalar();

                        return result != null;
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        // Check both source and destination tables.
        private void btnCheckSource_Click(object sender, RoutedEventArgs e)
        {
            // Read source values
            string srcServer = txtSourceServer.Text.Trim();
            string srcDatabase = txtSourceDatabase.Text.Trim();
            string srcTable = txtSourceTable.Text.Trim();

            // Read destination values
            string destServer = txtDestServer.Text.Trim();
            string destDatabase = txtDestDatabase.Text.Trim();
            string destTable = txtDestTable.Text.Trim();

            // Basic validation
            if (string.IsNullOrEmpty(srcServer) ||
                string.IsNullOrEmpty(srcDatabase) ||
                string.IsNullOrEmpty(srcTable) ||
                string.IsNullOrEmpty(destServer) ||
                string.IsNullOrEmpty(destDatabase) ||
                string.IsNullOrEmpty(destTable))
            {
                txtResult.Text = "Please enter all source and destination fields.";
                return;
            }

            // Check source table
            string srcError;
            bool srcExists = TableExists(srcServer, srcDatabase, srcTable, out srcError);

            // Check destination table
            string destError;
            bool destExists = TableExists(destServer, destDatabase, destTable, out destError);

            // Build status message
            string message = "";

            if (!string.IsNullOrEmpty(srcError))
                message += "Source error: " + srcError + Environment.NewLine;
            else
                message += srcExists
                    ? "Source table exists." + Environment.NewLine
                    : "Source table does NOT exist." + Environment.NewLine;

            if (!string.IsNullOrEmpty(destError))
                message += "Destination error: " + destError;
            else
                message += destExists
                    ? "Destination table exists."
                    : "Destination table does NOT exist.";

            txtResult.Text = message;
        }
    }
}