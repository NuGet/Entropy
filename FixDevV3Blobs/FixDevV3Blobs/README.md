A tool to do a search and replace for every blob in blob container.

Blob storage account connection string, container name, search and replace text are all defined on top of the `Main` method.

Has the ability to do a "dry run": the blobs are downloaded, changes are applied and original and updated blobs are saved to
local disk under 'input' and 'output' subdirectories. Useful to verify that the applied changes are the ones that are indeed
expected. Controlled by the `Program.dryRun` variable, set to `false` to do the actual processing.

If not doing dry run, the tool tracks processed blobs in 'processed.txt' file, so if it crashes, it would be able to pick up
where it stopped without reprocessing already processed files. If you are processing multiple containers with lots of files,
it is advised to delete or rename that file between containers, to save time and memory on loading unrelated URLs.

The tool supports gzip compressed blobs if they have 'Content-Encoding' header properly set. Blobs are compressed back to gz
when uploaded.

Network failures are retried and after several consecutive failures are ignored. Please verify the number of processed blobs
after the tool finishes, so it matches the expected number.

The tool run conists of single threaded blob list building, followed by multi-threaded blob processing. The `MaxTasks` const
specifies the number of parallel tasks used to do the processing.