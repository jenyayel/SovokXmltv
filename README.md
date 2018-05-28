# SovokXmltv

[![Build status](https://ci.appveyor.com/api/projects/status/chsydou48dpci70r?svg=true)](https://ci.appveyor.com/project/jenyayel/sovokxmltv)

Simple proxy for [sovok.tv API](http://forum.sovok.tv/viewtopic.php?f=6&t=240), which expose channels information and EPG in [xmltv format](http://wiki.xmltv.org/index.php/XMLTVFormat). The application implemented in [dnx451](https://github.com/aspnet/home) framework.

## Usage

The endpoint is root of the web-application and accepts requests with the following parameters: 

```//[HOST NAME]/?user=[USER]&password=[PASSWORD]&period=[NUMBER OF HOURS]```

The parameters are:
* `user` is the user name of account on sovok.tv
* `password` the password of the account above
* `period` is optional parameter, specifies the period for which the EPG will be retreived.

The channels and EPG retreived based on subscription level of the given account in sovok.tv.

## Demo

CI configured to [azure website](https://sovokxmltv.azurewebsites.net/?user=1111&password=1111) with demo account.

## Why credentials in query string?

Although this is indeed a bad idea, I didn't find a better way to configure EPG in Kodi's [IPTV Simple Client](http://kodi.wiki/view/Add-on:IPTV_Simple_Client). The client is limited to the URL (or local path), which enforces to pass any parameter only via GET.