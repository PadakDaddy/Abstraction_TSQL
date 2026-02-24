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

        // Reads the schema (column name and data type) for a given table.
        private DataTable? GetTableSchema(string server, string database, string tableName, out string errorMessage)
        {
            errorMessage = string.Empty;

            string connString =
                $"Server={server};Database={database};Integrated Security=true;";

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();

                    string sql = $"SELECT * FROM {tableName}";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
                    {
                        DataTable schemaTable = reader.GetSchemaTable();
                        return schemaTable;
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return null;
            }
        }

        // Compares two schema tables. Returns true if columns and data types match.
        private bool SchemasMatch(DataTable sourceSchema, DataTable destSchema)
        {
            // Quick check: same number of columns?
            if (sourceSchema.Rows.Count != destSchema.Rows.Count)
                return false;

            for (int i = 0; i < sourceSchema.Rows.Count; i++)
            {
                var srcRow = sourceSchema.Rows[i];
                var destRow = destSchema.Rows[i];

                string srcName = srcRow["ColumnName"].ToString() ?? "";
                string destName = destRow["ColumnName"].ToString() ?? "";

                string srcType = srcRow["DataType"].ToString() ?? "";
                string destType = destRow["DataType"].ToString() ?? "";

                if (!srcName.Equals(destName, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!srcType.Equals(destType, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        // Check both source and destination tables, and compare schema if both exist.
        private void btnCheckSource_Click(object sender, RoutedEventArgs e)
        {
            // 1. Read fields
            string srcServer = txtSourceServer.Text.Trim();
            string srcDatabase = txtSourceDatabase.Text.Trim();
            string srcTable = txtSourceTable.Text.Trim();

            string destServer = txtDestServer.Text.Trim();
            string destDatabase = txtDestDatabase.Text.Trim();
            string destTable = txtDestTable.Text.Trim();

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

            // 2. Check existence
            string srcError;
            bool srcExists = TableExists(srcServer, srcDatabase, srcTable, out srcError);

            string destError;
            bool destExists = TableExists(destServer, destDatabase, destTable, out destError);

            if (!string.IsNullOrEmpty(srcError))
            {
                txtResult.Text = "Source error: " + srcError;
                return;
            }

            if (!srcExists)
            {
                txtResult.Text = "Source table does NOT exist.";
                return;
            }

            if (!string.IsNullOrEmpty(destError))
            {
                txtResult.Text = "Destination error: " + destError;
                return;
            }

            if (!destExists)
            {
                // Destination table does not exist → create it using source schema.
                string createSchemaError;
                DataTable? srcSchemaForCreate = GetTableSchema(srcServer, srcDatabase, srcTable, out createSchemaError);

                if (srcSchemaForCreate == null)
                {
                    txtResult.Text = "Could not read source schema to create destination: " + createSchemaError;
                    return;
                }

                string createError;
                bool created = CreateDestinationTable(destServer, destDatabase, destTable, srcSchemaForCreate, out createError);

                if (!created)
                {
                    txtResult.Text = "Failed to create destination table: " + createError;
                    return;
                }

                txtResult.Text = "Destination table did not exist. It has been created based on the source schema.";
                return;
            }

            // 3. Both tables exist → compare schemas
            string schemaError;

            DataTable? srcSchema = GetTableSchema(srcServer, srcDatabase, srcTable, out schemaError);
            if (srcSchema == null)
            {
                txtResult.Text = "Could not read source schema: " + schemaError;
                return;
            }

            DataTable? destSchema = GetTableSchema(destServer, destDatabase, destTable, out schemaError);
            if (destSchema == null)
            {
                txtResult.Text = "Could not read destination schema: " + schemaError;
                return;
            }

            bool match = SchemasMatch(srcSchema, destSchema);

            txtResult.Text = match
                ? "Source and destination schemas MATCH. Safe to copy data."
                : "Source and destination schemas do NOT match.";
        }

        // Show column names of the source table (e.g., Northwind.Products)
        private void btnShowSourceColumns_Click(object sender, RoutedEventArgs e)
        {
            // Read source values
            string srcServer = txtSourceServer.Text.Trim();
            string srcDatabase = txtSourceDatabase.Text.Trim();
            string srcTable = txtSourceTable.Text.Trim();

            if (string.IsNullOrEmpty(srcServer) ||
                string.IsNullOrEmpty(srcDatabase) ||
                string.IsNullOrEmpty(srcTable))
            {
                txtResult.Text = "Please enter source server, database, and table first.";
                return;
            }

            string connString =
                $"Server={srcServer};Database={srcDatabase};Integrated Security=true;";

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();

                    string sql = $"SELECT * FROM {srcTable}";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
                    {
                        DataTable schemaTable = reader.GetSchemaTable();

                        if (schemaTable == null)
                        {
                            txtResult.Text = "Could not read schema information.";
                            return;
                        }

                        string message = "Source columns:" + Environment.NewLine;

                        foreach (DataRow row in schemaTable.Rows)
                        {
                            string columnName = row["ColumnName"].ToString();
                            string dataType = row["DataType"].ToString();

                            message += $"- {columnName} ({dataType}){Environment.NewLine}";
                        }

                        txtResult.Text = message;
                    }
                }
            }
            catch (Exception ex)
            {
                txtResult.Text = "Error reading schema: " + ex.Message;
            }
        }


        // Builds a simple CREATE TABLE statement based on the source schema.
        private string BuildCreateTableSql(string destTableName, DataTable sourceSchema)
        {
            // Basic CREATE TABLE template
            // We will generate: CREATE TABLE [destTableName] ( [Col1] INT, [Col2] NVARCHAR(50), ... )
            var sb = new System.Text.StringBuilder();
            sb.Append($"CREATE TABLE [{destTableName}] (");

            for (int i = 0; i < sourceSchema.Rows.Count; i++)
            {
                DataRow row = sourceSchema.Rows[i];

                string columnName = row["ColumnName"].ToString() ?? "";
                Type dataType = (Type)row["DataType"];

                // Map .NET types to SQL Server types (simple mapping)
                string sqlType;

                if (dataType == typeof(int))
                    sqlType = "INT";
                else if (dataType == typeof(short))
                    sqlType = "SMALLINT";
                else if (dataType == typeof(long))
                    sqlType = "BIGINT";
                else if (dataType == typeof(decimal) || dataType == typeof(double) || dataType == typeof(float))
                    sqlType = "DECIMAL(18, 2)";
                else if (dataType == typeof(DateTime))
                    sqlType = "DATETIME";
                else if (dataType == typeof(bool))
                    sqlType = "BIT";
                else
                    // default to NVARCHAR for unknown/string types
                    sqlType = "NVARCHAR(255)";

                sb.Append($"[{columnName}] {sqlType}");

                if (i < sourceSchema.Rows.Count - 1)
                    sb.Append(", ");
            }

            sb.Append(");");
            return sb.ToString();
        }


        // Creates the destination table using the source schema.
        private bool CreateDestinationTable(
            string destServer,
            string destDatabase,
            string destTable,
            DataTable sourceSchema,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            string connString =
                $"Server={destServer};Database={destDatabase};Integrated Security=true;";

            try
            {
                string createSql = BuildCreateTableSql(destTable, sourceSchema);

                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand(createSql, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        // Copies all rows from source table to destination table using a single transaction.
        private bool CopyDataWithTransaction(
            string srcServer,
            string srcDatabase,
            string srcTable,
            string destServer,
            string destDatabase,
            string destTable,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            string srcConnString =
                $"Server={srcServer};Database={srcDatabase};Integrated Security=true;";
            string destConnString =
                $"Server={destServer};Database={destDatabase};Integrated Security=true;";

            try
            {
                using (SqlConnection srcConn = new SqlConnection(srcConnString))
                using (SqlConnection destConn = new SqlConnection(destConnString))
                {
                    srcConn.Open();
                    destConn.Open();

                    // Start a transaction on the destination connection
                    using (SqlTransaction tx = destConn.BeginTransaction())
                    {
                        try
                        {
                            // Read all rows from source
                            string selectSql = $"SELECT * FROM {srcTable}";
                            using (SqlCommand selectCmd = new SqlCommand(selectSql, srcConn))
                            using (SqlDataReader reader = selectCmd.ExecuteReader())
                            {
                                // For each row, build an INSERT command
                                // We assume schemas match (same columns in same order)
                                while (reader.Read())
                                {
                                    int fieldCount = reader.FieldCount;

                                    // Build column list and parameter list
                                    var columnNames = new string[fieldCount];
                                    var paramNames = new string[fieldCount];

                                    for (int i = 0; i < fieldCount; i++)
                                    {
                                        string colName = reader.GetName(i);
                                        columnNames[i] = $"[{colName}]";
                                        paramNames[i] = $"@p{i}";
                                    }

                                    string insertSql =
                                        $"INSERT INTO {destTable} ({string.Join(", ", columnNames)}) " +
                                        $"VALUES ({string.Join(", ", paramNames)})";

                                    using (SqlCommand insertCmd = new SqlCommand(insertSql, destConn, tx))
                                    {
                                        // Add parameters
                                        for (int i = 0; i < fieldCount; i++)
                                        {
                                            object value = reader.GetValue(i);
                                            insertCmd.Parameters.AddWithValue($"@p{i}", value ?? DBNull.Value);
                                        }

                                        insertCmd.ExecuteNonQuery();
                                    }
                                }
                            }

                            // If we reach here, all inserts succeeded
                            tx.Commit();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            // Rollback on any error
                            tx.Rollback();
                            errorMessage = ex.Message;
                            return false;
                        }
                    }
                }
            }
            catch (Exception exOuter)
            {
                errorMessage = exOuter.Message;
                return false;
            }
        }

        private void btnCopyData_Click(object sender, RoutedEventArgs e)
        {
            // Read source and destination values
            string srcServer = txtSourceServer.Text.Trim();
            string srcDatabase = txtSourceDatabase.Text.Trim();
            string srcTable = txtSourceTable.Text.Trim();

            string destServer = txtDestServer.Text.Trim();
            string destDatabase = txtDestDatabase.Text.Trim();
            string destTable = txtDestTable.Text.Trim();

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

            // 1. Check existence
            string srcError;
            bool srcExists = TableExists(srcServer, srcDatabase, srcTable, out srcError);

            string destError;
            bool destExists = TableExists(destServer, destDatabase, destTable, out destError);

            if (!string.IsNullOrEmpty(srcError))
            {
                txtResult.Text = "Source error: " + srcError;
                return;
            }

            if (!srcExists)
            {
                txtResult.Text = "Source table does NOT exist.";
                return;
            }

            if (!string.IsNullOrEmpty(destError))
            {
                txtResult.Text = "Destination error: " + destError;
                return;
            }

            if (!destExists)
            {
                txtResult.Text = "Destination table does NOT exist. Please create it first (using Check Source).";
                return;
            }

            // 2. Ensure schemas match
            string schemaError;

            DataTable? srcSchema = GetTableSchema(srcServer, srcDatabase, srcTable, out schemaError);
            if (srcSchema == null)
            {
                txtResult.Text = "Could not read source schema: " + schemaError;
                return;
            }

            DataTable? destSchema = GetTableSchema(destServer, destDatabase, destTable, out schemaError);
            if (destSchema == null)
            {
                txtResult.Text = "Could not read destination schema: " + schemaError;
                return;
            }

            bool match = SchemasMatch(srcSchema, destSchema);
            if (!match)
            {
                txtResult.Text = "Source and destination schemas do NOT match. Copy aborted.";
                return;
            }

            // 3. Copy data with transaction
            string copyError;
            bool success = CopyDataWithTransaction(
                srcServer, srcDatabase, srcTable,
                destServer, destDatabase, destTable,
                out copyError);

            txtResult.Text = success
                ? "Data copy completed successfully."
                : "Data copy failed: " + copyError;
        }

    }
}
