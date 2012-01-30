using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Web;
using System.Text.RegularExpressions;

namespace RunbookViewer
{
    public partial class UploadRunbook : System.Web.UI.Page
    {
        private const string DOCTYPE_RUNBOOK_NORMAL = "runbookNormal";
        private const string DOCTYPE_RUNBOOK_STATEMENT = "runbookStatement";
        private const string DOCTYPE_RUNBOOK_LETTER = "runbookLetter";
        private const string DOCTYPE_RUNBOOK_CDM = "runbookCDM";
        private const string DOCTYPE_RUNBOOK_SMSSU = "runbookSMSSU";
        private const string DOCTYPE_FILE_TRANSFER = "fileTransfer";
        private const string DOCTYPE_RUNBOOK_RMS = "runbookRMS";
        
        public string message { get; set; }
        public int count { get; set; }
        IDictionary<string, string[,]> runbookColumns;

        private string tempJobset = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            initRunbookColumns();

            string runbookInfo = Request.Form["uploadsystem"].ToUpper() + " " + Request.Form["uploadenv"].ToUpper() + " " + 
                Request.Form["uploadversion"] + " " + Request.Form["uploaddate"];

            HttpPostedFile file = Request.Files["runbookFile"];
            if (file == null || file.ContentLength == 0)
            {
                message = "Null file";
                return;
            }
            string fileName = file.FileName;
            string tempPath = System.IO.Path.GetTempPath();
            fileName = System.IO.Path.GetFileName(fileName);
            string currFileExtension = System.IO.Path.GetExtension(fileName);
            string currFilePath = tempPath + fileName;
            if (!currFileExtension.Equals(".xls"))
            {
                message = "File is not an excel file.  It's a " + currFileExtension + " file.";
                return;
            }
            file.SaveAs(currFilePath);
            
            OleDbConnection excelCon = new OleDbConnection(@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + currFilePath + ";Extended Properties=Excel 8.0;Mode=Read");
            excelCon.Open();
            DataTable schemaTable = excelCon.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);

            if (schemaTable == null)
            {
                message = "Error reading Excel file";
                return;
            }
            SqlConnection sqlCon = new SqlConnection("Data Source=TIGDEV01;Initial Catalog=ENTSCH;Integrated Security=True");

            string sheetName = null;
            string docType = null;
            string tableName = null;
            if (fileName.ToUpper().Contains("FILE TRANSFER")) 
            {
                sheetName = "File Transfer";
                docType = DOCTYPE_FILE_TRANSFER;
                tableName = "dbo.ViewFileTransfer";
            }
            else 
            {
                sheetName = "Jobs Summary";
                tableName = "dbo.ViewRunbook";
                if ((fileName.Contains("SU") && fileName.ToUpper().Contains("R-FUNDS")) || fileName.ToUpper().Contains("DW_RUNBOOK") ||
                    fileName.ToUpper().Contains("RESEARCH REPORT")) docType = DOCTYPE_RUNBOOK_SMSSU;
                else if (fileName.ToUpper().Contains("STATEMENT")) docType = DOCTYPE_RUNBOOK_STATEMENT;
                else if (fileName.ToUpper().Contains("LETTER")) docType = DOCTYPE_RUNBOOK_LETTER;
                else if (fileName.ToUpper().Contains("CDM") | fileName.ToUpper().Contains("CMAH CMA RUNBOOK")) docType = DOCTYPE_RUNBOOK_CDM;
                else if (fileName.ToUpper().Contains("RMS")) docType = DOCTYPE_RUNBOOK_RMS;
                else docType = DOCTYPE_RUNBOOK_NORMAL;
            }

            
  
            bool sheetExists = false;
            foreach (DataRow row in schemaTable.Rows)
            {
                if (row["TABLE_NAME"].ToString().Contains(sheetName))
                {
                    sheetName = row["TABLE_NAME"].ToString();
                    sheetExists = true;
                    break;
                }
            }
            if (!sheetExists)
            {
                message = "File does not have a " + sheetName + " Sheet";
                return;
            }

            OleDbDataAdapter da = new OleDbDataAdapter("SELECT * FROM [" + sheetName + "]", excelCon);
            DataTable resultDataTable = new DataTable();
            da.Fill(resultDataTable);

            try
            {
                sqlCon.Open();
                foreach (DataRow row in resultDataTable.Rows)
                {
                    importRow(schemaTable, excelCon, sqlCon, tableName, docType, row, runbookInfo);
                }
            }
            finally
            {
                
                if (sqlCon != null) sqlCon.Close();
                if (excelCon != null) excelCon.Close();
            }
            
            message = "Upload complete";
        }

        private void importRow(DataTable schemaTable, OleDbConnection excelCon, SqlConnection sqlCon, string tableName, string docType, DataRow row, string runbookInfo)
        {
            SqlDataReader rdr = null;
            bool useRow = false;
            string jobset = "";
            string job = "";
            if (docType.Equals(DOCTYPE_FILE_TRANSFER))
            {
                if (getStringVal(row[1]).Length > 5 && getStringVal(row[1]).ToUpper().Substring(0, 5).Equals("CMAH_")) useRow = true;
            }
            else if (docType.Equals(DOCTYPE_RUNBOOK_STATEMENT) || docType.Equals(DOCTYPE_RUNBOOK_LETTER))
            {
                if ((getStringVal(row[0]).Length > 5 && getStringVal(row[0]).Substring(0, 5).ToUpper().Equals("CMAH_")) ||
                    (getStringVal(row[2]).Length > 5 && getStringVal(row[2]).Substring(0, 5).ToUpper().Equals("CMAH_")))
                {
                    useRow = true;
                    if (getStringVal(row[0]).Length > 5) tempJobset = getStringVal(row[0]);
                    else if (getStringVal(row[0]).Length == 0) row[0] = tempJobset;
                    jobset = getStringVal(row[0]);
                    job = getStringVal(row[2]);
                }
            }
            else
            {
                if ((getStringVal(row[0]).Length > 5 && getStringVal(row[0]).Substring(0, 5).ToUpper().Equals("CMAH_")) ) useRow = true;
                if (getStringVal(row[0]).Length == 0) row[0] = tempJobset;
                jobset = getStringVal(row[0]);
                job = getStringVal(row[1]);
            }
            job = job.Replace("\n", "");
            jobset = jobset.Replace("\n", "");
            job = job.Replace("\r", "");
            jobset = jobset.Replace("\r", "");
            if (useRow)
            {
                string query = "DELETE FROM dbo.ViewRunbook WHERE jobset='" + jobset + "' AND jobname='" + job + "'";
                SqlCommand cmd = new SqlCommand(query, sqlCon);
                cmd.ExecuteNonQuery();

                if (!jobset.Equals("") && !job.Equals(""))
                {
                    query = "INSERT INTO dbo.Jobs (Jobset, Jobname) VALUES ('" + jobset + "','" + job + "')";
                    try { 
                        cmd = new SqlCommand(query, sqlCon);
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception) { }
                }
                
                query = createSqlInsert(docType, tableName, row, runbookInfo);
                cmd = new SqlCommand(query, sqlCon);

                try {cmd.ExecuteNonQuery();}
                catch (Exception ex) { message = ex.Message + "<br/>" + query; }
                if (rdr != null) rdr.Close();
            } 
        }


        private string getStringVal(object obj)
        {
            if (obj.GetType() == typeof(System.DBNull)) return "";
            string result = obj.ToString();
            result = result.Replace("'", "''");
            return result;
        }

        private int getIntVal(object obj)
        {
            if (obj.GetType() == typeof(System.DBNull)) return 0;
            else if (obj.GetType() == typeof(System.String))
            {
                try
                {
                    return Int32.Parse((string)obj);
                }
                catch (FormatException e)
                {
                    return 0;
                }
            }
            else return 0;
        }

        private string createSqlInsert(string docType, string table, DataRow row, string runbookInfo)
        {
            string result = "INSERT INTO " + table + " (";
            for (int i = 0; i < runbookColumns[docType].GetLength(0); i++)
            {
                result += runbookColumns[docType][i, 0] + ", ";
            }
            result += "runbookInfo) VALUES (";
            for (int i = 0; i < runbookColumns[docType].GetLength(0); i++)
            {
                bool isString = runbookColumns[docType][i, 1].ToUpper().Contains("VARCHAR");
                if (isString) result += "'" + getStringVal(row[i]) + "'";
                else result += getIntVal(row[i]);
                result += ", ";
            }
            result += "'" + runbookInfo + "')";
            return result;
        }

        /*private string createSqlUpdate(string docType, string table, DataRow row)
        {
            string result = "UPDATE " + table + " SET ";
            for (int i = 0; i < runbookColumns[docType].GetLength(0); i++)
            {
                result += runbookColumns[docType][i, 0] + "=";
                bool isString = runbookColumns[docType][i, 1].ToUpper().Contains("VARCHAR");
                if (isString) result += "'" + getStringVal(row[i]) + "'";
                else result += getIntVal(row[i]);
                result += ", ";
            }
            result = result.Substring(0, result.Length - 2) + " WHERE ";
            if (docType.Equals(DOCTYPE_FILE_TRANSFER)) result += "RoutingTemplateName='" + getStringVal(row[1]) + "'";
            else result += "jobset='" + getStringVal(row[0]) + "' AND jobname='" + getStringVal(row[1]) + "'";
            return result;
        }*/

        private string createSqlSelect(string docType, DataRow row)
        {
            if (docType.Equals(DOCTYPE_FILE_TRANSFER)) {
                return "SELECT Stream FROM dbo.ViewFileTransfer WHERE RoutingTemplateName='" + getStringVal(row[1]) + "'";
            }
            else if (docType.Equals(DOCTYPE_RUNBOOK_STATEMENT) || docType.Equals(DOCTYPE_RUNBOOK_LETTER))
            {
                return "SELECT jobname FROM dbo.ViewRunbook WHERE jobset='" + getStringVal(row[0]) + "' AND jobname='" + getStringVal(row[2]) + "'";
            }
            else
            {
                return "SELECT jobname FROM dbo.ViewRunbook WHERE jobset='" + getStringVal(row[0]) + "' AND jobname='" + getStringVal(row[1]) + "'";
            }
        }

        private void initRunbookColumns() 
        {
            runbookColumns = new Dictionary<string, string[,]>();
            runbookColumns.Add(DOCTYPE_RUNBOOK_NORMAL, new string[,]{
                {"Jobset", "varchar(200)"},
                {"Jobname", "varchar(200)"},
                {"Station", "varchar(200)"},
                {"QuantResource", "varchar(200)"},
                {"CmdUser", "varchar(200)"},
                {"Commandline", "varchar(1000)"},
                {"EarlyStart", "varchar(200)"},
                {"Dependencies", "varchar(1000)"},
                {"MustStart", "varchar(200)"},
                {"MustComplete", "varchar(200)"},
                {"MaxTime", "varchar(200)"},
                {"SuccessfulRC", "int"},
                {"AssigneeCode", "varchar(200)"},
                {"SeverityCode", "int"},
                {"AlertNotification", "varchar(200)"}
            });
            runbookColumns.Add(DOCTYPE_RUNBOOK_SMSSU, new string[,]{
                {"Jobset", "varchar(200)"},
                {"Jobname", "varchar(200)"},
                {"Station", "varchar(200)"},
                {"CmdUser", "varchar(200)"},
                {"Commandline", "varchar(1000)"},
                {"EarlyStart", "varchar(200)"},
                {"Dependencies", "varchar(1000)"},
                {"MustStart", "varchar(200)"},
                {"MustComplete", "varchar(200)"},
                {"MaxTime", "varchar(200)"},
                {"SuccessfulRC", "int"},
                {"AssigneeCode", "varchar(200)"},
                {"SeverityCode", "int"},
                {"AlertNotification", "varchar(200)"}
            });
            runbookColumns.Add(DOCTYPE_RUNBOOK_LETTER, new string[,]{
                {"Jobset", "varchar(200)"},
                {"Description", "varchar(1000)"},
                {"Jobname", "varchar(200)"},
                {"Station", "varchar(200)"},
                {"CmdUser", "varchar(200)"},
                {"Commandline", "varchar(1000)"},
                {"Calendar", "varchar(200)"},
                {"SuccessfulRC", "int"},
                {"StaticParam", "varchar(1000)"},
                {"DynamicParam", "varchar(1000)"},
                {"Dependencies", "varchar(1000)"},
                {"DynamicDependencies", "varchar(1000)"},
                {"AssigneeCode", "varchar(200)"},
                {"SeverityCode", "int"},
                {"AlertNotification", "varchar(200)"}
            });
            runbookColumns.Add(DOCTYPE_RUNBOOK_STATEMENT, new string[,]{
                {"Jobset", "varchar(200)"},
                {"Description", "varchar(1000)"},
                {"Jobname", "varchar(200)"},
                {"Station", "varchar(200)"},
                {"CmdUser", "varchar(200)"},
                {"Commandline", "varchar(1000)"},
                {"Calendar", "varchar(200)"},
                {"SuccessfulRC", "int"},
                {"StaticParam", "varchar(1000)"},
                {"DynamicParam", "varchar(1000)"},
                {"Dependencies", "varchar(1000)"},
                {"PerfTrigger", "varchar(200)"},
                {"DynamicDependencies", "varchar(1000)"},
                {"AssigneeCode", "varchar(200)"},
                {"SeverityCode", "int"},
                {"AlertNotification", "varchar(200)"},
                {"NotificationToUser", "varchar(500)"},
                {"Notes", "varchar(500)"}
            });
            runbookColumns.Add(DOCTYPE_RUNBOOK_CDM, new string[,]{
                {"Jobset", "varchar(200)"},        
                {"Jobname", "varchar(200)"},
                {"QuantResource", "varchar(200)"},
                {"Commandline", "varchar(1000)"},
                {"EarlyStart", "varchar(200)"},
                {"Dependencies", "varchar(1000)"},
                {"CmdUser", "varchar(200)"},
                {"Station", "varchar(200)"},
                {"MustStart", "varchar(200)"},//TODO: Must Start, Must Complete, and Max Time are combined here
                {"SuccessfulRC", "int"},
                {"AssigneeCode", "varchar(200)"},
                {"SeverityCode", "int"},
                {"AlertNotification", "varchar(200)"}
            });	
            runbookColumns.Add(DOCTYPE_RUNBOOK_RMS, new string[,]{
                {"Jobset", "varchar(200)"},
                {"Jobname", "varchar(200)"},
                {"Station", "varchar(200)"},
                {"CmdUser", "varchar(200)"},
                {"Commandline", "varchar(1000)"},
                {"EarlyStart", "varchar(200)"},
                {"MustStart", "varchar(200)"},
                {"MustComplete", "varchar(200)"},
                {"MaxTime", "varchar(200)"},
                {"SuccessfulRC", "int"},
                {"StaticParam", "varchar(1000)"},
                {"PerfTrigger", "varchar(200)"},
                {"DynamicParam", "varchar(1000)"},
                {"Dependencies", "varchar(1000)"},
                {"DynamicDependencies", "varchar(1000)"},
                {"AssigneeCode", "varchar(200)"},
                {"SeverityCode", "int"},
                {"AlertNotification", "varchar(200)"}
               
            });

            runbookColumns.Add(DOCTYPE_FILE_TRANSFER, new string[,]{
                {"TransferCategory", "varchar(200)"},
                {"RoutingTemplateName", "varchar(200)"},
                {"Stream", "varchar(200)"},
                {"Description", "varchar(1000)"},
                {"CMASME", "varchar(200)"},
                {"SrcServerFunction", "varchar(200)"},
                {"SrcInternalExternal", "varchar(200)"},
                {"SrcDirectoryTest", "varchar(200)"},
                {"SrcDirectoryProd", "varchar(200)"},
                {"SrcFilenameTest", "varchar(200)"},
                {"SrcFilenameProd", "varchar(200)"},
                {"SrcUserAuthTest", "varchar(200)"},
                {"SrcUserAuthProd", "varchar(200)"},
                {"DstServerFunction", "varchar(200)"},
                {"DstInternalExternal", "varchar(200)"},
                {"DstDirectoryTest", "varchar(200)"},
                {"DstDirectoryProd", "varchar(200)"},
                {"DstFilenameTest", "varchar(200)"},
                {"DstFilenameProd", "varchar(200)"},
                {"DstUserAuthTest", "varchar(200)"},
                {"DstUserAuthProd", "varchar(200)"},
                {"Winzip", "varchar(200)"},
                {"Encryp", "tinyint"},
                {"Adapter", "varchar(200)"},
                {"FailOnNoFile", "tinyint"},
                {"SendRCWhenNoSourceFile", "tinyint"},
                {"DeleteFromSource", "tinyint"},
                {"BinaryToASCII", "tinyint"},
                {"TransferEmptyFiles", "tinyint"},
                {"Detailed", "varchar(1000)"},
                {"Network", "varchar(200)"},
                {"Decommission", "tinyint"}
            });
        }
    }
}