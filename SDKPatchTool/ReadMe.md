1.Go to the SDKPatchTool folder
2.Change the following path
  $patchSDKFolder 
  $nupkgsPath 
  in patchOnWindows.ps1, if you're going to patch on Windows;
  in patchOnUnix.ps1, if you're going to patch on Linux/Mac
3.Run ./patchOnWindows.ps1  (or patchOnUnix.ps1)
4.If you see "Finish patching", you may start testing the patched dotnet now, with abusolute path
  If you see "Patching failed", you have to check why the patching is failed.