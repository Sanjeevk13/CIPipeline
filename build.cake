#addin "nuget:?package=Cake.Docker&version=0.9.9"
#tool "nuget:?package=OctopusTools&version=6.1.1"
//#tool "nuget:?package=GitVersion.CommandLine"
#addin "nuget:?package=Cake.Powershell&version=0.4.7"
#addin "nuget:?package=Cake.FileHelpers&version=3.1.0"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var buildNumber = Argument("buildNumber", "0");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////


// Define directories.
var artifacts ="./.artifacts";
var artifactsdir = Directory (artifacts);

// Define config variables
var appName ="DemoApp";
//var projectName = "GoRewards Web-Test";
//var projectPath = "BCS.GetGo.Booking.AppWeb";
var solutionFile = "DemoApp/DemoApp.sln";
//var registryUsername = EnvironmentVariable("REG_USER") ?? "GoRewardsACS";
//var registryPassword = EnvironmentVariable("REG_PASSWORD") ?? "AFZB=jKoJijGCKHI3k9KlLhZwpvBJwtF";
var buildVersion = EnvironmentVariable("BUILD_BUILDID") ??  EnvironmentVariable("BUILD_NUMBER") ?? buildNumber;
//var octoApiKey = EnvironmentVariable("OCTOPUS_API_KEY") ?? "API-JMONTCBFJP5GY8ETET85WSVOXZC";
//var octoAddress = EnvironmentVariable("OCTOPUS_URL") ?? "https://davideploy.apps.bcstechnology.com.au/";

var versionBuildString ="";
var buildTag ="";
var buildTagExt ="";

Information(logAction => logAction ("Target : {0}", target));    

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(_ =>
{
    Information("");
    Information(" ____   _____  _____ ");
    Information("|  _ \\ / ____|/ ____|");
    Information("| |_) | |    | (___  ");
    Information("|  _ <| |     \\___ \\ ");
    Information("| |_) | |____ ____) |");
    Information("|____/ \\_____|_____/ ");
    Information("");
});

Teardown(_ =>
{
    Information("Finished running tasks.");
});


//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectory(artifactsdir);
    CreateDirectory(artifactsdir);
    CleanDirectory("./drop");
    CreateDirectory("./drop");
    CreateDirectory("./drop/nuget");
    CreateDirectory("./drop/package");
    CleanDirectory("./BCS.GetGo.Booking.AppWeb/drop/");
    CreateDirectory("./BCS.GetGo.Booking.AppWeb/drop/");
});

Task("GetVersionInfo")
    .WithCriteria(target != "Jenkins" && target != "JenkinsBuild")
    .Does(() =>
    {
        var versioninfo = GitVersion(new GitVersionSettings {
            UpdateAssemblyInfo = false,
            NoFetch = true
        });

        var versionSuffixPadded = "";
        if(versioninfo.BuildMetaDataPadded != "") {
            Information(logAction => logAction ("MajorMinorPatch : {0}", versioninfo.MajorMinorPatch));    
            versionSuffixPadded = versioninfo.BuildMetaDataPadded;
        }
        
        versionBuildString = versioninfo.MajorMinorPatch + versionSuffixPadded;
        Information(logAction => logAction ("Build String : {0}", versionBuildString));    
        buildTag = versionBuildString + buildVersion;
        Information(logAction => logAction ("Version number : {0}", buildTag));    
    });

Task("GetJenkinsInfo")
    .WithCriteria(target == "Jenkins" || target == "JenkinsBuild")
    .Does(() =>
    {
        buildTag = buildVersion;
        buildTagExt = ".0.0";
        Information(logAction => logAction ("Version number : {0}", buildTag));
    });


Task("Restore-NuGet-Packages")
    .Does(() =>
    {
        NuGetRestore(solutionFile);
    });

// build
Task("Build")
    .IsDependentOn("GetVersionInfo")
    .IsDependentOn("GetJenkinsInfo")
    .IsDependentOn("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(()=>
    {
    if(IsRunningOnWindows())
        { 
            // Use MSBuild
            MSBuild(solutionFile, settings =>
                {
                    settings.SetConfiguration(configuration)
                      .WithTarget("Build")
                      .WithProperty("OutDir", "./drop");
                });
        }
        else
        {
            // Use XBuild
            XBuild(solutionFile, settings =>
                settings.SetConfiguration(configuration));
        }
    });


Task("JenkinsBuild")
    .IsDependentOn("Build")
    .Does(() => {
        Information("Done Deploying...");
    });

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("JenkinsBuild");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
