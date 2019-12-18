# Details

This is a utility VS extension that can be used to test the invocation threading patterns for VS components. Specifically the NuGet components.

To use it, simply restore/build, and either launch the experimental instance or install the extension in your VisualStudio installation.
To open the window go to, View -> Other Windows - IVS Testing Extension.
It allows you to select a project and pass arguments in a kvp form. See an example in src\Tests\TestMethodProvider.cs

Then you can invoke the method by clicking Run/RunAsync depending on the threading pattern. (Run for sync, RunAsync for async obviously :) )
The possible threading patterns are defined in: src\ThreadAffinity.cs.
What they do is straightforward, you can play with it more by looking at ProjectCommandTestingModel.

```cs
namespace IVsTestingExtension
{
    public enum ThreadAffinity
    {
        ASYNC_FROM_UI,
        ASYNC_FROM_BACKGROUND,
        ASYNC_FREETHREADED_CHECK,
        SYNC_JTF_RUN,
        SYNC_JTF_RUNASYNC_FIRE_FORGET,
        SYNC_TASKRUN_UNAWAITED,
        SYNC_TASKRUN_BLOCKING,
    }
}
```

If you want add you own method instead, you only need to change 1 file, and that's src\Tests\TestMethodProvider.
The infrastructure autowires the Provider and calls the GetMethod call to get the method that you want to test.
