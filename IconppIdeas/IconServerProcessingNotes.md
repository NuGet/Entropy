### A note regarding Server Image Processing

It's suggested that NuGet Server serve images just like the Icon Cache.
It is expected that there will be differences as follows:

**On Uploading**

* Rely on Server to validate Images for suspicious (malware) content.
* Execute the same validations as IconProvider in clients

**On Serving**

* Use a CDN-based cache mechanism to serve the images.
* Rely on Server for processing icons from packages that have
  `<iconUrl/>` property
