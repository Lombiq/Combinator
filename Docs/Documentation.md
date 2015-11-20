# [Combinator Orchard module](https://github.com/Lombiq/Combinator) Documentation



## Installation

**The module is dependent on the [Helpful Libraries
module](https://gallery.orchardproject.net/List/Modules/Orchard.Module.Piedone.HelpfulLibraries), so make sure to install it first!**  
**Combinator needs at least Orchard 1.8!**

The module is also available for [DotNest](http://dotnest.com/) sites.


## The module's admin settings

After installation the module adds a settings page to the Settings menu.
You can also empty the cache there.  
**Note:** Currently the module preserves a combined resource forever,
without updating it if the resources have changed. If you have modified
a stylesheet or javascript file (or its resource's settings, like
conditions) and want to see the updates you should empty the cache. If
the list of resource for a specific page changes, however (e.g. from
Script1.js, Script2.js to include Script3.js too), Combinator will
produce a new resource.  
Note that Combinator also has a command line argument for emptying the
cache: "combinator empty".


## Exclusion filters and why they matter

You can set exclusion filters (regular expressions) on combination,
minification and image embedding.

-   Sometimes a resource does not play well with combination: e.g.
    WYSIWYG editors (like TinyMCE) tend to load scripts on the fly,
    based on relative paths to the script's location. Combinator can do
    nothing with such mechanisms, so it's better not to touch them:
    therefore, excluding from combination is the way to go.
-   Javascript minification is not bullet-proof, there are rare cases
    (mostly with already minified scripts) when the produced script is
    syntactically incorrect. To overcome this, you can exclude such
    scripts from minification.
-   Excluding stylesheets from embedding their images is useful for
    conditional stylesheets specifically targeted at browsers that don't
    support data urls.

 

## Development best practices

-   Don't have Combinator always enabled in your development
    (and testing) environment, as combined resources make it harder to
    find bugs in resources. Only enable Combinator steadily in the
    live environment.
-   **You should, however, always test the impact of the module on
    your site.** Try all pages where distinct sets of resources
    are used.
-   When testing in the development environment, don't have Shape
    Tracing enabled.
-   Always empty the cache in the live environment when you push
    resource changes to it.
-   Unless you adjust the urls to be the same, you can't push combined
    resources to the live environment (since Combinator adjusts relative
    paths, these will include the url of your development environment;
    however if you don't have AppPath set you should be able to publish
    to live, but if you have one, like the default "OrchardLocal",
    resources should be recompiled in live). Therefore, it's best to
    publish changes with the cache emptied and let combination happen on
    the live site (which will only cause a performance impact on the
    first views of pages with unique set of resources).
-   If you're using resource sharing check whether tenants can have
    different resources with the same paths because that would
    cause inconsistencies. E.g. if you have a module that provides a
    dynamic (e.g. user-configurable) stylesheet under the fixed path
    /dynamicstylesheet, where the path does not contain anything unique
    to the tenant (what would be e.g. /tenantname/dynamicstylesheet)
    then this stylesheet will be shared among all the tenants. Resource
    sharing only works if the resources with the same path also contain
    the same data.
-   Until [this issue](http://combinator.codeplex.com/workitem/68) is
    fixed, place font-face CSS declarations into separate CSS files and
    exclude them from combination.


## Important notes on sprite generation

Image sprite generation features a partially automatic detection whether
an image is suitable for sprite generation or not: backgrounds with a
size, position other than top-left, and repetition other than no-repeat
are excluded. Only images with the these criteria are used for sprite
generation.  
  
However, Combinator can do nothing when these properties are set
somewhere else than in the block of the background declaration.
Therefore if an image

-   has those properties set somewhere,
-   is already a sprite or
-   its container's size exceeds its size

then exclude it manually by using "no-sprite" in their file path or by
adding the ".no-sprite" class to the selectors of the css block they're
included in.  
  
Warning: sprite generation is experimental not only itself but it also
needs the [ExCSS library](https://github.com/TylerBrinks/ExCSS) to work.
The latter one is undergoing heave refactoring but the current release
can fail on some CSS files and produce incorrect results, thus rendering
the CSS produced by Combinator incorrect too.  


## Notes on using with Azure Blob storage

If your site runs on Azure Cloud Services or Azure Web Sites (probably
this also affects Azure VMs) and uses Blob storage to store media files
then you have to take the below actions to ensure that resources
processed by Combinator are properly served.  
  
When Orchard stores files in Blob storage it also sets their mime types;
this is then also used when serving the files. For JavaScript files the
mime type should be "application/javascript". However on Azure
webservers are missing the correct mime map for JavaScript files, thus
also the mime type of processed scripts is set wrong (or not set at
all). Orchard, by default, reads mime types first from the Web.config,
then from the registry. So to fix this the easiest way is to set up the
correct mime map in your site's Web.config as following:  
  
```
    <system.webServer>
        <staticContent>
            <remove fileExtension=".js" />
            <mimeMap fileExtension=".js" mimeType="application/javascript" />
        </staticContent>
    </system.webServer>
```
 
## Troubleshooting

If something fails, Combinator returns the original set of resources for
a specific resource type (like head scripts) or even for all resources,
depending on the type of failure. So if looking at the html source you
see that nothing has changed then something has gone wrong. Possible
causes are that a local or remote resource was not found or an exclusion
regex was erroneous. Take a look at the log file to see what happened.
If you're in doubt what have caused the failure, don't be afraid to ask.


## Who's using it?

Among others Combinator speeds up the [Orchard
Gallery](http://gallery.orchardproject.net/), all sites on
[DotNest](https://dotnest.com/), the [Hungarian Orchard Community
site](http://english.orchardproject.hu/), all the Lombiq sites like
[Lombiq.com](http://lombiq.com/) and [Orchard
Dojo](http://orcharddojo.net/), and the [Associativy home
page](http://associativy.com/). If you use and like Combinator, let me
know and your site will be listed here.

## Many thanks

To randompete, Sebastien Ros (for the incredible overridden stylesheet
discovery) and Znowman for their valuable contributions!

## See the [Version history](https://github.com/Lombiq/Combinator/releases)