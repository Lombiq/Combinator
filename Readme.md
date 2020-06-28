# Combinator Orchard module



## About

An Orchard CMS module that combines and minifies external stylesheets and javascript files to cut down on load times.  


## Features

- Combines and minifies css files
- Combines and minifies javascript files
- If local and remote resources are mixed (like a local js files with one from a CDN) preserves their original order
- Preserves conditional resources and minifies (if multiple with the same condition are after each other, also combines) them
- Can combine remote (CDN) resources
- Can embed images into stylesheets as data urls
- Experimental image sprite generation support
- Resource sets can be defined for better client-side caching: you can create sets of resources that are combined separately (e.g. all jQuery scripts can be in their individual file)
- Ability to share processed resources between tenants in a multi-tenant application so a set of resources is only processed once, not for every tenant (resource sharing)
- Busts browser cache when resources are updated (with a query string parameter containing a time stamp)
- Ability to set custom resource domain
- Exposing resource processing events
- LESS and SASS preprocessors, contribution of Onestop Internet, Inc.
- Command line command for emptying cache ("combinator empty")
- Info comment in bundled resources about which resources were combined
- Tuned to be fast
- With custom IStorageProvider can work in cloud hosting too (if there is no write access to the Media folder anyway)
- Import/export settings
- Administration page:
	- Adjust combination exclusion filter
	- Enable/disable combination of CDN resources
	- Set up resource domain
	- Enable/disable minification and adjust exclusion filter
	- Enable/disable image embedding and adjust exclusion filter
	- Enable/disable image sprite generation
	- Define resource sets
	- Enable/disable for admin site
	- Empty cache

The module is also available for [DotNest](http://dotnest.com/) sites.  

You can download an install the module from the [Orchard Gallery](http://orchardproject.net/gallery/List/Modules/Orchard.Module.Piedone.Combinator).  
For known issues and future plans please see the [Issue Tracker](https://github.com/Lombiq/Combinator/issues).

**Please make sure to read the [Documentation](Docs/Documentation.md)!**


## Contributing and support

Bug reports, feature requests, comments, questions, code contributions, and love letters are warmly welcome, please do so via GitHub issues and pull requests. Please adhere to our [open-source guidelines](https://lombiq.com/open-source-guidelines) while doing so.

This project is developed by [Lombiq Technologies](https://lombiq.com/). Commercial-grade support is available through Lombiq.