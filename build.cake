#addin nuget:?package=SimpleExec&version=6.2.0
#addin nuget:?package=Cake.Powershell&version=0.4.8

var target = Argument("target", "Default");

Task("Default")
    .Does(() =>
    {
        Information($"The script was executed!");
    });
    
RunTarget(target);
