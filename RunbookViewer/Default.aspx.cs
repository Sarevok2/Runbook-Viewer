using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace RunbookViewer
{
    public partial class Default : System.Web.UI.Page
    {
        public string jobsJson;

        protected void Page_Load(object sender, EventArgs e)
        {
            Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>>> jobs;
            jobs = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>>>();
            jobs.Add("pr", new Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>>());
            jobs.Add("ig", new Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>>());
            jobs.Add("su", new Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>>());
            foreach (KeyValuePair<String, Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>>> env in jobs)
            {
                env.Value.Add("rb", new Dictionary<string, Dictionary<string, List<string>>>());
                env.Value.Add("rf", new Dictionary<string, Dictionary<string, List<string>>>());
                env.Value.Add("crl", new Dictionary<string, Dictionary<string, List<string>>>());
                env.Value.Add("crs", new Dictionary<string, Dictionary<string, List<string>>>());
                env.Value.Add("crm", new Dictionary<string, Dictionary<string, List<string>>>());
                env.Value.Add("cma", new Dictionary<string, Dictionary<string, List<string>>>());
                env.Value.Add("cdm", new Dictionary<string, Dictionary<string, List<string>>>());
                env.Value.Add("rms", new Dictionary<string, Dictionary<string, List<string>>>());
                env.Value.Add("other", new Dictionary<string, Dictionary<string, List<string>>>());
                foreach (KeyValuePair<String, Dictionary<string, Dictionary<string, List<string>>>> sys in env.Value)
                {
                    sys.Value.Add("batch", new Dictionary<string, List<string>>());
                    sys.Value.Add("report", new Dictionary<string, List<string>>());                
                }
            }

            SqlConnection conn = new SqlConnection("Data Source=TIGDEV01;Initial Catalog=ENTSCH;Integrated Security=True");
            SqlDataReader rdr = null;
            List<Job> dbJobs = new List<Job>();
            string query = "SELECT * FROM dbo.Jobs ORDER BY Jobset, Jobname";
            try
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(query, conn);
                rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    dbJobs.Add(new Job()
                    {
                        Jobname = rdr.GetString(1),
                        Jobset = rdr.GetString(0),
                    });
                }
            }
            finally
            {
                if (rdr != null) rdr.Close();
                if (conn != null) conn.Close();
            }

            foreach (Job job in dbJobs)
            {
                string environment = "", system = "", type = "";

                if (job.Jobset.Length >= 11)
                {

                    if (String.Compare(job.Jobset.Substring(5, 2), "PR") == 0) { environment = "pr"; }
                    else if (String.Compare(job.Jobset.Substring(5, 2), "IG") == 0) { environment = "ig"; }
                    else if (String.Compare(job.Jobset.Substring(5, 2), "SU") == 0) { environment = "su"; }

                    if (String.Compare(job.Jobset.Substring(8, 2), "RB") == 0) { system = "rb"; }
                    else if (String.Compare(job.Jobset.Substring(8, 2), "RF") == 0) { system = "rf"; }
                    else if (String.Compare(job.Jobset.Substring(8, 3), "CRL") == 0) { system = "crl"; }
                    else if (String.Compare(job.Jobset.Substring(8, 3), "CRS") == 0) { system = "crs"; }
                    else if (String.Compare(job.Jobset.Substring(8, 3), "CRM") == 0) { system = "crm"; }
                    else if (String.Compare(job.Jobset.Substring(8, 3), "CMA") == 0) { system = "cma"; }
                    else if (String.Compare(job.Jobset.Substring(8, 3), "CDM") == 0) { system = "cdm"; }
                    else if (String.Compare(job.Jobset.Substring(8, 3), "RMS") == 0) { system = "rms"; }
                    else system = "other";
                }
                else
                {
                    system = "other";
                    environment = "pr";
                }

                string pattern = @"R\d+$";
                if (Regex.IsMatch(job.Jobset, pattern)) { type = "report"; }
                else { type = "batch"; }

                if (environment.Length > 0 && system.Length > 0 && type.Length > 0)
                {
                    if (!jobs[environment][system][type].ContainsKey(job.Jobset))
                    {
                        jobs[environment][system][type].Add(job.Jobset, new List<string>());
                    }
                    jobs[environment][system][type][job.Jobset].Add(job.Jobname);
                }
            }

            string result = "{";
            foreach (KeyValuePair<string, Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>>> environment in jobs)
            {
                result += "\"" + environment.Key + "\":{";
                foreach (KeyValuePair<string, Dictionary<string, Dictionary<string, List<string>>>> system in environment.Value)
                {
                    result += "\"" + system.Key + "\":{";
                    foreach (KeyValuePair<string, Dictionary<string, List<string>>> type in system.Value)
                    {
                        result += "\"" + type.Key + "\":{";
                        foreach (KeyValuePair<string, List<string>> jobset in type.Value)
                        {
                            result += "\"" + jobset.Key + "\":[";
                            foreach (string jobName in jobset.Value)
                            {
                                result += "\"" + jobName + "\",";
                            }
                            if (jobset.Value.Count > 0) result = result.Substring(0, result.Length - 1);
                            result += "],";
                        }
                        if (type.Value.Count > 0) result = result.Substring(0, result.Length - 1);
                        result += "},";
                    }
                    if (system.Value.Count > 0) result = result.Substring(0, result.Length - 1);
                    result += "},";
                }
                if (environment.Value.Count > 0) result = result.Substring(0, result.Length - 1);
                result += "},";

            }
            if (jobs.Count > 0) result = result.Substring(0, result.Length - 1);
            result += "}";
            jobsJson = result;
        }


    }
}