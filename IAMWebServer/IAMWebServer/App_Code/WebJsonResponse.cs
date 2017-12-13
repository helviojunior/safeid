using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SafeTrend.Json;

[Serializable()]
public class WebJsonResponse
{
    public String callId;

    public String redirectURL;

    public String js;

    public String msgTitle;
    public String msg;
    public Int32 msgTimer;

    public String errMsg;
    public String errMsgTitle;
    public Int32 errMsgTimer;

    public String containerId;
    public String html;
    public Boolean append;
    public Int32 width;
    public Int32 height;

    public WebJsonResponse()
    {
    }

    public WebJsonResponse(String redirectURL)
    {
        this.redirectURL = redirectURL;
    }


    public WebJsonResponse(String containerId, String html, Int32 width, Int32 height) :
        this(containerId, html, false, width, height) { }

    public WebJsonResponse(String containerId, String html) :
        this(containerId, html, false) { }


    public WebJsonResponse(String containerId, String html, Boolean append, Int32 width, Int32 height) :
        this(containerId, html)
    {
        this.width = width;
        this.height = height;
        this.append = append;
    }

    public WebJsonResponse(String containerId, String html, Boolean append)
    {
        this.containerId = containerId;
        this.html = html;
        this.append = append;
    }

    public WebJsonResponse(String title, String message, Int32 timer, Boolean error) :
        this(title, message, timer, error, null) { }

    public WebJsonResponse(String title, String message, Int32 timer, Boolean error, String redirectURL)
    {
        if (error)
        {
            this.errMsgTitle = title;
            this.errMsg = message;
            this.errMsgTimer = timer;
            this.redirectURL = redirectURL;
        }
        else
        {
            this.msgTitle = title;
            this.msg = message;
            this.msgTimer = timer;
            this.redirectURL = redirectURL;
        }
    }

    public String ToJSON()
    {
        return JSON.Serialize<WebJsonResponse>(this);
    }

}
