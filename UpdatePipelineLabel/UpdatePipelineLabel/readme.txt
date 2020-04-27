Update pipeline label for all issues in the specified repository:
Command: UpdatePipelineLabel.exe NuGet/Home <githubToken> <zenhubToken>

Update pipeline label for a range of issues in the specified repository: 
(rarely used, only used if huge number of issues need to process, and rate limiting changed for Github, for now, 5000/hr and Patch 1/s)
Command: UpdatePipelineLabel.exe NuGet/Home <githubToken> <zenhubToken> --from 0 --to 9000

Note: 
1.It's safe to run the command repeatedly.
  It will only update the issues that need updating from the current state. 
  Issues with the right pipeline label and no wrong pipeline label will be skipped.

2.You have to wait about 30 seconds until you start to see the outputs.(wait for getting all the 60+ pages issues from github!)

Error handling:
1.Exception : Issue https://github.com/NuGet/Home/issues/8199 could not update label on GitHub, exception is : Validation Failed
=> Reason: When updating this issue, Github throws an exceptioin. 
=> Solution: Need to mannually add pipeline label to this issue.

[update, https://github.com/AlexGhiondea/ZenHub.NET/pull/15 is merged, no more following exception]
2.System.Text.Json throws an exception:�The JSON value could not be converted to System.Int32. Path: $.pipelines[0].issues[1160].estimate.value | LineNumber: 0 | BytePositionInLine: 62341.
=> Reason: There is a parsing error(usually there is a float estimation of an issue https://github.com/AlexGhiondea/ZenHub.NET/pull/15 )
=> Solution: Need to find the issue and manually change the estimation to an int.
   step1: Add zenhub token in the end of this URL: https://github.com/ZenHubIO/API#authentication
https://api.zenhub.io/p2/workspaces/55aec9a240305cf007585881/repositories/29996513/board?access_token=
   step2: Paste the result in http://jsonviewer.stack.hu/   and locate the issue number
   step3: Change the estimation of this issue.