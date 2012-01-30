using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace RunbookViewer
{
    public partial class ShowRunbook : System.Web.UI.Page
    {
        public string jobsTable="";

        protected void Page_Load(object sender, EventArgs e)
        {
            

            SqlConnection sqlCon = new SqlConnection("Data Source=TIGDEV01;Initial Catalog=ENTSCH;Integrated Security=True");

            string search = Request.QueryString["search"];
            string searchBox = Request.QueryString["searchBox"];
            string column = Request.QueryString["column"];
            string jobset = Request.QueryString["jobset"];
            string jobname = Request.QueryString["jobname"];
            bool showEmptyFields = Request.QueryString["showempty"] != null;

            sqlCon.Open();
            string query;
            if (search.Equals("true"))
            {
                query = "SELECT * FROM dbo.ViewRunbook WHERE " + column + " LIKE '%" + searchBox + "%'";
            }
            else
            {
                query = "SELECT * FROM dbo.ViewRunbook WHERE jobset='" + jobset + "'";
                if (!jobname.ToUpper().Equals("ALL")) query += " AND jobname='" + jobname + "'";
            }
            
            
            SqlCommand cmd = new SqlCommand(query, sqlCon);
            SqlDataReader rdr = cmd.ExecuteReader();
            bool found = false;
            while (rdr.Read())
            {
                found = true;
                jobsTable += "<table class='runbookTable'>";
                List<string[]> fields = new List<string[]>();
                for (int i = 2; i < rdr.FieldCount; i++)
                {
                    string value;
                    try { value = rdr.GetString(i); }
                    catch (Exception) { value = ""; }
                    if (showEmptyFields || !value.Equals(""))
                    {
                        if (rdr.GetName(i).ToUpper().Equals("COMMANDLINE") || rdr.GetName(i).ToUpper().Equals("STATICPARAM"))
                        {
                            Regex jobRegex = new Regex("Template:(?<template>CMAH_.+)");
                            Match m = jobRegex.Match(value);
                            if (m.Success)
                            {
                                string template = m.Groups["template"].Value;
                                string replacement = "<a href='#' onclick='return showFileTransfer(\"" + template + "\");'>" + template + "</a>";
                                value = jobRegex.Replace(value, replacement);
                            }
                        }
                        fields.Add(new string[] { rdr.GetName(i), value });
                    }
                } 
                int numCols = 3;
                int numrows = (fields.Count + numCols - 1)/numCols;
                jobsTable += "<tr><th colspan='6' class='tableHeader tableHeaderLeft tableHeaderRight'>&nbsp &nbsp Jobset:" + rdr.GetString(0);
                if (!rdr.GetString(1).Equals("")) jobsTable += "&nbsp &nbsp Jobname:" + rdr.GetString(1) + "</th>";
                for (int i = 2; i < numCols; i++) { jobsTable += "<th colspan='2' class='tableHeader " + ((i==(numCols-1))?"tableHeaderRight":"") + "'></th>"; }
                for (int i = 0; i < numrows; i++)
                {
                    jobsTable += "<tr>";
                    for (int j = 0; j < numCols; j++)
                    {
                        if (fields.Count > (j * numrows + i))
                        {
                            string style = "";
                            if (fields[j * numrows + i][0].Equals("Description")) style = "style='min-width:300px'";
                            jobsTable += "<th class='tableCell'>" + fields[j * numrows + i][0] + ":</th><td class='tableCell' " + style + ">" + fields[j * numrows + i][1] + "</td>";
                        }
                        else
                            jobsTable += "<th class='tableCell'></th><td class='tableCell'></td>";
                    }
                    jobsTable += "</tr>";
                }
                jobsTable += "</table>";
            }

            if (sqlCon != null) sqlCon.Close();
            if (!found) jobsTable += "<p>Runbook data not found</p>";
        }

        private string createSubTable(SqlDataReader rdr, int start, int end, bool showEmpty)
        {
            string result = "<td style='vertical-align:text-top;'><table >";
            
            {
                
            }
            result += "</table></td>";
            return result;
        }
    }
}
