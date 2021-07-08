Serving Icon Images: The IconProvider mechanism
-----------------------------------------------

The biggest change that bring icon storage within packages is figuring
out how to serve Icon images from our possible scenarios. The main goal
is to serve the Icon image that package authors provide in their
packages, taking into consideration the limitations each platform has.
To accomplish this, a new mechanism is suggested: The IconProvider. This
mechanism will have the following features:

Depending of the performance, the design/implementation is planned in two parts:

### IconProvider 1st Generation

* On/Off capable: especially for non-interactive environments, such as CI/CD builders

Source of truth for IconProvider will be:

  1. Global packages folder for packages with `<icon/>` element
  2. nuget.org Server `<iconUrl/>` eleement

Clients (PM UI, PM Console, dotnet.exe, nuget.exe) should search for
images in this order, using IconProvider mechanism.

A new Project is suggested: NuGet.Icon; it will be a Class Library for
all the code related to implement the following features:

* Icon Discovery
* Icon Validation
* Icon Serving
* Icon Link

Each feature is described in the next section.

### IconProvider 2nd generation

If we need more icon image sizes

* On-Demand based: serve images as requested, without caching

### IconProvider 3rd generation

If we see performance drawbacks in icon processing, we can add LRU caches on client
side

* Cached: Avoid re-processing of icon images

The IconProvider is an LRU-based (Least Recently Used) image cache. Every time an Icon
image is requested, the IconProvider will provide the appropriate Icon
Image. The IconProvider will have a limited size for storing requested
images. In case of cache overflow, the least recently used images
will be replaced with new served images.

The size of the IconProvider will be an option that can be configured
by the user. By default, the cache size will be 300 MB.

NOTE: We need to discuss if 1) addint client cache to iconProvider is worth it and 2) cache size limit.


### Icon Serving

Given a Discovered and Verified Icon and a target size, serve an Icon
Image and update Icon Cache with the following steps:

  1. Look for the Icon image in the Icon Cache
  2. If the Image is found and cache has not expired

      1. Update Cache Expiration
      2. Return Image

  3. Otherwise:

      1. Look for Icon Image
      2. If Icon Image found:

          1. Open the Icon File
          2. Rescale the Icon Image
          3. Store the resized icon in the desired location, according
             to LRU-policy
          4. Return Image

      3. Otherwise (involves `<iconUrl/>` present):

          1. Ask for the image required to Nuget.org
          2. Return image

  4. If a problem is encountered, return the default image
