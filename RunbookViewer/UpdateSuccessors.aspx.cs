using System;
using System.Collections.Generic;

using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace RunbookViewer
{
    public partial class UpdateSuccessors : System.Web.UI.Page
    {
        public string message;

        protected void Page_Load(object sender, EventArgs e)
        {
            SqlConnection sqlCon = new SqlConnection("Data Source=TIGDEV01;Initial Catalog=ENTSCH;Integrated Security=True");
            try
            {
                sqlCon.Open();
                string query = "SELECT jobset, jobname, dependencies FROM dbo.ViewRunbook WHERE dependencies != ''";
                SqlCommand cmd = new SqlCommand(query, sqlCon);
                SqlDataReader rdr = cmd.ExecuteReader();
                List<string[]> jobLinks = new List<string[]>();
                while (rdr.Read())
                {
                    string depend = rdr.GetString(2);
                    string successorJobset = rdr.GetString(0);
                    string successorJobname = rdr.GetString(1);
                    Regex jobRegex = new Regex("(?<job>CMAH_[^\\(]+)\\((?<jobset>CMAH_[^\\)]+)\\)");
                    MatchCollection matches = jobRegex.Matches(depend);
                    foreach (Match m in matches)
                    {
                        string jobset = m.Groups["jobset"].Value;
                        string jobname = m.Groups["job"].Value;
                        jobLinks.Add(new string[] { jobset, jobname, successorJobset, successorJobname });
                    }
                    if (matches.Count == 0)
                    {
                        Regex jobRegex2 = new Regex("(?<job>CMAH_.+)");
                        MatchCollection matches2 = jobRegex2.Matches(depend);
                        foreach (Match m in matches2)
                        {
                            string jobname = m.Groups["job"].Value;
                            jobLinks.Add(new string[] { successorJobset, jobname, successorJobset, successorJobname });
                        }
                    }
                }
                if (rdr != null) rdr.Close();
                query = "UPDATE dbo.ViewRunbook SET successors = ''";
                cmd = new SqlCommand(query, sqlCon);
                cmd.ExecuteNonQuery();
                foreach (string[] link in jobLinks)
                {
                    string prevSuccessors = "";
                    query = "SELECT successors FROM dbo.ViewRunbook WHERE (jobset='" + link[0] + "' AND jobname='" + link[1] + "') OR " +
                            "(jobset='" + link[1] + "' AND jobname='" + link[0] + "')";
                    cmd = new SqlCommand(query, sqlCon);
                    rdr = cmd.ExecuteReader();
                    if (rdr.Read())
                    {
                        prevSuccessors = rdr.GetString(0);
                    }
                    if (rdr != null) rdr.Close();
                    query = "UPDATE dbo.ViewRunbook SET successors = '" + prevSuccessors + " " + link[2] + "(" + link[3] +
                            ")' WHERE (jobset='" + link[0] + "' AND jobname='" + link[1] + "') OR " +
                            "(jobset='" + link[1] + "' AND jobname='" + link[0] + "')";
                    cmd = new SqlCommand(query, sqlCon);
                    cmd.ExecuteNonQuery();
                }
                message = "Successors Updated Successfully";
            }
            catch (Exception ex) { message = "Error: " + ex.Message; }
            finally
            {
                if (sqlCon != null) sqlCon.Close();
            }
        }
    }
}