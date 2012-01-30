
var searchHash;
var useSearchHash = false;

$(function () {
    updateJobsetList(jobHash);

    $("#uploadRunbookDialog").dialog({
        autoOpen: false,
        minWidth: 600,
        modal: true
    });

    $("#fileTransferDialog").dialog({
        autoOpen: false,
        minWidth: 1500,
        modal: true
    });

    $("#runbookButton").click(function () {
        showRunbook();
        return false;
    });

    $("#searchRunbooksButton").click(function () {
        if ($("input:text[name=searchRunbooksBox]").val().length > 2) {
            searchRunbooks();
        }
        else {
            alert("Please enter at least 3 characters");
        }
        return false;
    });

    $("#uploadButton").click(function () {
        $('#uploadFrame').contents().find('html').html("");
        $('#uploadRunbookDialog').dialog('open');
        return false;
    });
});

function showRunbook() {
    if ($('select[name=jobset]').val() == "select") {
        alert("Please choose a jobset");
        return false;
    }
    $("#viewingArea").html("Please Wait...");
    var params = {
        environment: $('input:radio[name=environment]:checked').val(),
        system: $('input:radio[name=system]:checked').val(),
        jobType: $('input:radio[name=jobType]:checked').val(),
        jobset: $('select[name=jobset]').val(),
        jobname: $('select[name=jobname]').val(),
        showempty: $('input[name=showempty]:checked').val(),
        search: "false"
    }

    var listurl = "/ShowRunbook.aspx";
    $.ajax({
        url: listurl,
        data: params,
        timeout: 30000,
        success: function (result) {
            $("#viewingArea").html(result);
        },
        error: function (xhr, status, error) {
            $("#viewingArea").html("<p>Error : " + status + " " + error + "</p>");
        }
    });
}

function showFileTransfer(rtemplate) {
    $("#fileTransferDialog").html("Please Wait...");
    $('#fileTransferDialog').dialog('open');
    var params = {
        template: rtemplate,
        showempty: $('input[name=showempty]:checked').val()
    }

    var listurl = "/ShowFileTransfer.aspx";
    $.ajax({
        url: listurl,
        data: params,
        timeout: 30000,
        success: function (result) {
            $("#fileTransferDialog").html(result);
        },
        error: function (xhr, status, error) {
            $("#fileTransferDialog").html("<p>Error : " + status + " " + error + "</p>");
        }
    });

    return false;
}

function searchRunbooks() {
    var params = {
        searchbox: $('input:text[name=searchRunbooksBox]').val(),
        column: $('select[name=column]').val(),
        showempty: $('input[name=showempty]:checked').val(),
        search: "true"
    }

    var listurl = "/ShowRunbook.aspx";
    $.ajax({
        url: listurl,
        data: params,
        timeout: 30000,
        success: function (result) {
            $("#viewingArea").html(result);
        },
        error: function (xhr, status, error) {
            $("#viewingArea").html("<p>Error : " + status + " " + error + "</p>");
        }
    });

    return false;
}

function updateSuccessors() {
    $("#UpdateSuccessorsSpan").html("Please wait...");
    var listurl = "/UpdateSuccessors.aspx";
    $.ajax({
        url: listurl,
        timeout: 30000,
        success: function (result) {
            $("#UpdateSuccessorsSpan").html(result);
        },
        error: function (xhr, status, error) {
            $("#UpdateSuccessorsSpan").html("<p>Error : " + status + " " + error + "</p>");
        }
    });

    return false;
}

function updateJobsetList(jobs) {
    useSearchHash = false;
    $("#jobsetSelector").html("<option value='select'>Select</option>");
    var jobsetList = jobs[$("input[name=environment]:checked").val()][$("select[name=system]").val()][$("input[name=jobType]:checked").val()];
    for (var js in jobsetList) {
        $('#jobsetSelector').append("<option value='" + js + "'>" + js + "</option>");
    }
    updateJobnameList(jobs);
}

function updateJobnameList(jobs) {
    var jobset = $("#jobsetSelector").val();
    $("#jobnameSelector").html("<option value='all'>All</option>");
    var jobList;
    if (useSearchHash) { jobList = searchHash[jobset]; }
    else { jobList = jobs[$("input[name=environment]:checked").val()][$("select[name=system]").val()][$("input[name=jobType]:checked").val()][jobset]; }

    for (var i in jobList) {
        $('#jobnameSelector').append("<option value='" + jobList[i] + "'>" + jobList[i] + "</option>");
    }
}

function searchJobs(jobs, searchString) {
    searchHash = new Array();
    useSearchHash = true;
    $("#jobsetSelector").html("<option value='all'>All</option>");
    for (var env in jobs) {
        for (var sys in jobs[env]) {
            for (var jobType in jobs[env][sys]) {
                for (var jobset in jobs[env][sys][jobType]) {
                    if (jobset.toUpperCase().indexOf(searchString.toUpperCase()) != -1) {
                        $('#jobsetSelector').append(new Option(jobset, jobset));
                        searchHash[jobset] = jobs[env][sys][jobType][jobset];
                    }
                    else {
                        for (var i in jobs[env][sys][jobType][jobset]) {
                            if (jobs[env][sys][jobType][jobset][i].toUpperCase().indexOf(searchString.toUpperCase()) != -1) {
                                $('#jobsetSelector').append(new Option(jobset, jobset));
                                searchHash[jobset] = jobs[env][sys][jobType][jobset];
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
    updateJobnameList(jobs);
    return false;
}

function searchKeyPress(e, jobs, searchString) {
    var key;

    if (window.event) key = window.event.keyCode;     //IE
    else key = e.which;     //firefox

    if (key == 13) {
        searchJobs(jobs, searchString);
        return false;
    }
    else return true;
}
