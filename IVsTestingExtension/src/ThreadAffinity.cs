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
