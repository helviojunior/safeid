# Single Sign On (SafeID)

An complete solution to make an Identity Manager and Single Sign On

## Before Start

Before continue this Read-me, please visit the website [Single-Sign-On](http://single-sign-on.com.br/en/), there i'm explanning everything that we need to know before use this application.

** Trainning Videos (only in Portuguese)**
* [YouTube Channel](https://www.youtube.com/user/safetrendsec/videos?sort=dd&view=0&shelf_id=0)
* [Vídeo aula 01](https://www.youtube.com/watch?v=EXeOxjccfHc)
* [Vídeo aula 02](https://www.youtube.com/watch?v=NPqp1yyuobc)
* [Vídeo aula 03](https://www.youtube.com/watch?v=g_OFp5t19WU)
* [Vídeo aula 04](https://www.youtube.com/watch?v=mGSEQwt62gs)
* [Vídeo aula 05](https://www.youtube.com/watch?v=8t4yaiwAkCw)


## Getting Started

These instructions will get you how can you compile and build the install package to deploy the project on a live system.

### Prerequisites

Before complile this application you need to install this packages:

* [Microsoft Visual Studio 2010](https://msdn.microsoft.com/en-us/library/dd831853(v=vs.100).aspx) - To build the projects
* [Inno Setup](http://www.jrsoftware.org/isinfo.php) - To build the Setup package
* [Pre-requisits](http://single-sign-on.com.br/wp-content/uploads/2017/12/SetupPreReqs.zip) - Setup pre requisits

### Compiling

A step by step that tell you have to get a complete package. You need to follow this sequence to be successful

Compiling Plugin Base Structure

```
Open IAMPluginsManager\IAMPluginsManager.sln and run the Rebuild Solution
```

Compiling Available Plugins

```
Open IAMPlugins\IAMPlugins.sln and run the Rebuild Solution
```

Compiling Proxy Modules

```
Open IAMProxy\IAMProxy.sln and run the Rebuild Solution
```

Compiling Server Modules

```
Open IAMServer\IAMServer.sln and run the Rebuild Solution. I don't know why but sometimes i need to run rebuild process twice to complete the process.
```

Compiling CAS`s confirmation codes plugins

```
Open IAMCodePlugins\IAMCodePlugins.sln and run the Rebuild Solution
```

Compiling CAS Web Server

```
Open IAMWebCas\IAMWebCas.sln make sure that your are compiling at "Release mode" and run the Rebuild Solution.
Right-click at IAMWebServer project and click on “Build Deployment Package”
```

Compiling Identity Manager Web Console

```
Open IAMWebServer\ IAMWebServer.sln make sure that your are compiling at "Release mode" and run the Rebuild Solution.
Right-click at IAMWebServer project and click on “Build Deployment Package”
```

Run final script to organize all components

```
Execute _BuildPackage.cmd file
```


### Building Setup File

* Install the Inno Setup
* Download [Pre-requisits](http://single-sign-on.com.br/wp-content/uploads/2017/12/SetupPreReqs.zip) File and extract inside of .\Setup\SetupPreReqs
* Right click at Setup.iss File and click on **Compile**


## Authors

* **Helvio Junior** - *Initial work* - [HelvioJunior](http://helviojunior.com.br/)

See also the list of [contributors](https://github.com/helviojunior/safeid/graphs/contributors) who participated in this project.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

## Acknowledgments

* Let-me know when you use this application and yout exppirience
* I need to make documentation better 
* I hope that it will helps the community

