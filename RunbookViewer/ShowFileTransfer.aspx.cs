using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace RunbookViewer
{
    public partial class ShowFileTransfer : System.Web.UI.Page
    {
        public string fileTransferTable = "";

        protected void Page_Load(object sender, EventArgs e)
        {

            
            SqlConnection sqlCon = new SqlConnection("Data Source=TIGDEV01;Initial Catalog=ENTSCH;Integrated Security=True");

            string template = Request.QueryString["template"];
            bool showEmptyFields = Request.QueryString["showempty"] != null;
            

            sqlCon.Open();
            string query = "SELECT * FROM dbo.ViewFileTransfer WHERE RoutingTemplateName='" + template + "'";
            SqlCommand cmd = new SqlCommand(query, sqlCon);
            SqlDataReader rdr = cmd.ExecuteReader();

            if (rdr.Read())
            {
                fileTransferTable += "<table class='fileTransferTable'>";
                fileTransferTable += "<tr><td class='fileTransferTable'>" + createTableWithFields(showEmptyFields, rdr, 5, 13, "Source") + "</td>";
                fileTransferTable += "<td class='fileTransferTable'>" + createTableWithFields(showEmptyFields, rdr, 13, 21, "Destination") + "</td></tr>";
                fileTransferTable += "<tr><td class='fileTransferTable'>" + createTableWithFields(showEmptyFields, rdr, 0, 5, "Description") + "</td>";
                fileTransferTable += "<td class='fileTransferTable'>" + createTableWithFields(showEmptyFields, rdr, 21, rdr.FieldCount, "Misc") + "</td></tr>";
                
                fileTransferTable += "</table>";
            }
            else {fileTransferTable = "<p>File Transfer Template " + template + " not found.</p>";}

            if (sqlCon != null) sqlCon.Close();
        }

        private string createTableWithFields(bool showEmptyFields, SqlDataReader rdr, int startIndex, int endIndex, string title)
        {
            List<string[]> fields = new List<string[]>();
            string result = "<table class='runbookTable'><tr><th class='tableHeader tableHeaderLeft'>&nbsp &nbsp " + title + "</th><th class='tableHeader tableHeaderRight'></th>";
            for (int i = startIndex; i < endIndex; i++)
            {
                string value;
                try { value = rdr.GetString(i); }
                catch (Exception) { value = ""; }
                if (showEmptyFields || !value.Equals(""))
                {
                    fields.Add(new string[] { rdr.GetName(i), value });
                }
            }
            for (int i = 0; i < fields.Count; i++)
            {
                result += "<tr><th class='tableCell'>" + fields[i][0] + ":</th><td class='tableCell'>" + fields[i][1] + "</td></tr>";
            }
            result += "</table>";
            return result;
        }
    }
}
