<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="RunbookViewer.Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title>Runbook Viewer</title>
    <link href="/Styles/Site.css" rel="stylesheet" type="text/css" />
    <link rel="stylesheet" type="text/css" href="/Styles/anytime.css" />
    <link rel="stylesheet" type="text/css" href="/Styles/Site.css" />
    <link rel="Stylesheet" type="text/css" href="/Styles/jquery-ui-1.8.11.custom.css"  />	
    <script src="/Scripts/jquery-1.5.1.min.js" type="text/javascript"></script>
    <script src="/Scripts/jquery-ui-1.8.11.custom.min.js" type="text/javascript"></script>
    <script src="/Scripts/Index.js" type="text/javascript"></script>
    <script type="text/javascript">
        var jobHash = <%=jobsJson %>;
    </script>
</head>
<body>
    <form action="/" method="post">    <div class="controls">
        <table><tr>
            <td><fieldset class="controls-fieldset" >
                <legend>Job Selector</legend>
                <div class="div-float-left">
                    <ul class="controls-radio-list">
                            <li>System:
                                <select onchange="updateJobsetList(jobHash)"id="systemSelector" name="system">
                                    <option value='rb'>R-Broker</option>
                                    <option value='rf'>R-Funds</option>
                                    <option value='crl'>Letter</option>
                                    <option value='crs'>Statement</option>
                                    <option value='crm'>CRM</option>
                                    <option value='cma'>CMA</option>
                                    <option value='cdm'>CDM</option>
                                    <option value='rms'>RMS</option>
                                    <option value='other'>Other</option>
                                </select>
                            </li>
                        <li class="middle">
                            <input checked="checked" name="environment" onclick="updateJobsetList(jobHash)" type="radio" value="pr" />Prod &nbsp
                            <input name="environment" onclick="updateJobsetList(jobHash)" type="radio" value="ig" />IG &nbsp
                            <input name="environment" onclick="updateJobsetList(jobHash)" type="radio" value="su" />SU
                        </li>
                        <li>
                            <input checked="checked" name="jobType" onclick="updateJobsetList(jobHash)" type="radio" value="batch" />Batch &nbsp
                            <input name="jobType" onclick="updateJobsetList(jobHash)" type="radio" value="report" />Report
                        </li>
                    </ul>
                </div>
                <div class="div-float-right">
                    <ul>
                        <li><input class="job-search-box" id="searchbox" name="search" onchange="searchJobs(jobHash, searchbox.value)" onkeypress="return searchKeyPress(event, jobHash, searchbox.value)" type="text" value="" />

                        <button type="button" onclick="searchJobs(jobHash, searchbox.value)">Search</button></li>
                        
                        <li>Jobset: 
                            <select class="jobSelectorDropdown" id="jobsetSelector" name="jobset" onchange="updateJobnameList(jobHash)">
                                <option selected="selected" value="select">Select</option>
                            </select>
                        </li>
                    
                        <li>Job: 
                            <select class="jobSelectorDropdown" id="jobnameSelector" name="jobname">
                                <option selected="selected" value="all">All</option>
                            </select>
                        </li>
                     </ul>
                </div>
            </fieldset></td>

            <td><fieldset class="controls-fieldset">

                <legend>Keyword Search</legend>
                <ul>
                    <li><input type="text" name="searchRunbooksBox" /></li>
                    <li>Column:
                        <select id="searchColumnsDropdown" name="column">
                            <option value='Jobset'>Jobset</option>
                            <option value='Jobname'>Jobname</option>
                            <option value='Station'>Station</option>
                            <option value='QuantResource'>QuantResource</option>
                            <option value='CmdUser'>CmdUser</option>
                            <option value='Commandline'>Commandline</option>
                            <option value='StaticParam'>StaticParam</option>
                            <option value='DynamicParam'>DynamicParam</option>
                            <option value='EarlyStart'>EarlyStart</option>
                            <option value='MustStart'>MustStart</option>
                            <option value='MustComplete'>MustComplete</option>
                            <option value='MaxTime'>MaxTime</option>
                            <option value='Dependencies'>Dependencies</option>
                            <option value='PerfTrigger'>PerfTrigger</option>
                            <option value='DynamicDependencies'>DynamicDependencies</option>
                            <option value='Successors'>Successors</option>
                            <option value='Calendar'>Calendar</option>
                            <option value='SuccessfulRC'>SuccessfulRC</option>
                            <option value='AssigneeCode'>AssigneeCode</option>
                            <option value='SeverityCode'>SeverityCode</option>
                            <option value='AlertNotification'>AlertNotification</option>
                            <option value='Description'>Description</option>
                            <option value='Notes'>Notes</option>
                            <option value='NotificationToUser'>NotificationToUser</option>
                        </select>
                    </li>
                    <li><input type="submit" class="data-button" id="searchRunbooksButton" name="searchRunbooksButton" value="Search Runbook" /></li>
                </ul>
            </fieldset></td>

            <td><fieldset class="controls-fieldset">

                <legend>Display Data</legend>
                <ul>
                    <li><input type="checkbox" name="showempty" />Show Empty Fields</li>
                    <li><input type="submit" class="data-button" id="runbookButton" name="submit" value="Show Runbook" /></li>
                    <li><button class="data-button" id="uploadButton" name="upload">Upload Runbook</button></li>
                </ul>
            </fieldset></td>
        </tr></table>
    </div>
</form>
<div class="viewingArea" id="viewingArea"></div>


<div id="uploadRunbookDialog" title="Upload Runbook">
    <form action="/UploadRunbook.aspx" method="post" id="uploadForm" enctype="multipart/form-data" target="uploadFrame">
        <ul class="uploadList">
            <li>
                <p class="uploadNotes">All jobs in uploaded runbooks will overwrite existing jobs in the database.  Runbooks must be in excel format.  
                To upload runbooks in Word format, copy the runbook table portion of these documents into a new Excel file and rename the sheet to 
                'Jobs Summary'.  Save the file with exactly the same filename as the Word doc other than the extension.</p>
                <p class="uploadNotes">The 'Update Successors' button will calculate all the successor jobs (reverse dependencies) of the entire runbook table.  This
                should be done once you have uploaded all the runbooks you are going to upload presently. </p>
            </li>
            <li>
                <input type="file" id="myFile" name="runbookFile" />
            </li>
            <li>System:
                <select name="uploadsystem">
                    <option value='rbroker batch'>R-Broker Batch</option>
                    <option value='rfunds batch'>R-Funds Batch</option>
                    <option value='rbroker report'>R-Broker Report</option>
                    <option value='rfunds report'>R-Funds Report</option>
                    <option value='crl'>Letter</option>
                    <option value='crs'>Statement</option>
                    <option value='crm'>CRM</option>
                    <option value='cma'>CMA</option>
                    <option value='cdm'>CDM</option>
                    <option value='rms'>RMS</option>
                    <option value='dw'>DW</option>
                    <option value='bdsui'>BDSUI</option>
                    <option value='file transfer'>File Transfer</option>
                    <option value='other'>Other</option>
                </select>
            </li>
            <li>
                Environment:
                <select name="uploadenv">
                    <option value='pr'>Production</option>
                    <option value='ig'>Integration</option>
                    <option value='su'>Prod Support</option>
                    <option value='na'>N/A</option>
                </select>
            </li>
            <li>
                Version: <input type="text" name="uploadversion" /> &nbsp 
            </li>
            <li>
                Date: <input type="text" name="uploaddate" /> &nbsp 
            </li>
            <li>
                <input class="uploadDialogButton" type="submit" id="uploadToServerButton" value="Upload" />
                <button class="uploadDialogButton" onclick="return updateSuccessors();">Update Successors</button>
            </li>
            <li>
                <span id="UpdateSuccessorsSpan"></span>
            </li>
        </ul>
    </form>
    <iframe class="uploadFrame" name="uploadFrame"></iframe>
</div>
<div id="fileTransferDialog" title="File Transfer">
</div>
</body>
</html>
