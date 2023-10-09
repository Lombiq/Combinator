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
- The Combinator cache can be emptied when the Activated shell event fires if:
    - A marker file with the name *_ClearCache.txt* is present in the *Orchard.Web/App_Data/Sites/<tenant_name>/_PiedoneModules/Combinator* folder. This file will then be deleted to have the cache cleared only on the first shell start, e.g. after a new deployment.
    - The `CombinatorCacheClearingShellEventHandler` class's `IsDisabled` property is set to `false` (default is `true`) by adding the following section to *Orchard.Web/Config/HostComponents.config*:
        ```
        <Component Type="Piedone.Combinator.EventHandlers.CombinatorCacheClearingShellEventHandler">
          <Properties>
            <Property Name="IsDisabled" Value="false"/>
          </Properties>
        </Component>
        ```
        Alternatively, the same can be achieved through a file transformation ([see this for details](https://learn.microsoft.com/en-us/aspnet/web-forms/overview/deployment/visual-studio-web-deployment/web-config-transformations)) to only activate this feature for a given build configuration, e.g., by adding the following to *HostComponents.Release.config* for `Release` mode.
        ```
        <Component Type="Piedone.Combinator.EventHandlers.CombinatorCacheClearingShellEventHandler" xdt:Transform="Insert">
          <Properties>
            <Property Name="IsDisabled" Value="false" xdt:Transform="Insert" />
          </Properties>
        </Component>
        ```

The module is also available for [DotNest](https://dotnest.com) sites.  

You can download an install the module from the [Orchard Gallery](http://orchardproject.net/gallery/List/Modules/Orchard.Module.Piedone.Combinator).  
For known issues and future plans please see the [Issue Tracker](https://github.com/Lombiq/Combinator/issues).

**Please make sure to read the [Documentation](Docs/Documentation.md)!**


## Contributing and support

Bug reports, feature requests, comments, questions, code contributions, and love letters are warmly welcome, please do so via GitHub issues and pull requests. Please adhere to our [open-source guidelines](https://lombiq.com/open-source-guidelines) while doing so.

This project is developed by [Lombiq Technologies](https://lombiq.com/). Commercial-grade support is available through Lombiq.