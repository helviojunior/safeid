
var cas = {};
var ApplicationVirtualPath = '/';

jQuery(document).ready(function ($) {
    
    $.CASUi();
    cas = $(document).data('CASUi');
});

(function ($) {

    $.CASUi = function (options, variables) {


        /* Public Variables
        =================================*/
        var root = this;
        root.ui = {};
        root.ui.base = {};
        root.ui.opt = null;
        root.ui.vars = null;

        /* root.ui.private Variables
        =================================*/
        root.ui.priv = {};
        root.ui.event = {};


        /* 
        ######################################
        ##### public functions
        */


        /* Get URI
        =================================*/
        root.ui.base.getUri = function (sufix) {

            var uri = $.trim(window.location);

            if (uri.substr(uri.length - 1, 1) != '/')
                uri += '/';

            if (sufix.substr(1, 1) != '/')
                sufix = sufix.substr(1, sufix.length - 1);

            if (sufix.substr(sufix.length - 1, 1) != '/')
                sufix += '/';

            return uri + sufix + '?' + root.ui.priv.newID();
        }


        root.ui.base.passwordStrength = function (fieldId) {
            if ($(fieldId)) {

                clearTimeout(root.ui.base.vars.pwd_strength_timer);

                root.ui.base.vars.pwd_strength_timer = setTimeout(function () {

                    $.ajax({
                        type: "POST",
                        url: ApplicationVirtualPath + "cas/passwordstrength/",
                        dataType: "text",
                        data: 'password=' + $(fieldId).val(),
                        error: root.ui.event.submitOnError,
                        success: root.ui.event.submitOnSuccess
                    });

                }, 300);


            }
        };


        /* 
        ##### End of public functions
        ######################################
        */



        /* 
        ######################################
        ##### root.is.private functions
        */

        
        root.ui.event.submitOnSuccess = function (t) {
            root.ui.priv.doJSON(t);
        };

        
        root.ui.priv.doJSON = function (json, callBackFunction) {

            if ($.trim(json) == '')
                return;

            var o = {};
            try {
                if (json.length == 1) o = {};
                else o = jQuery.parseJSON(json);
            } catch (e) {
                //o = eval('(' + json + ')');
                root.ui.priv.showMessage("Erro", "Error parsing JSON data", {
                    className: "error",
                    timer: 3000
                });

                if ((root.ui.event.callBackOpts != null) && (typeof (root.ui.event.callBackOpts.error) == "function")) root.ui.event.callBackOpts.error();
                return;
            }


            if (o.containerId && o.html) {
                if ($(o.containerId).length > 0) {
                    if (o.append)
                        $(o.containerId).append(o.html);
                    else
                        $(o.containerId).html(o.html);

                    if (o.width && o.height) {
                        $(o.containerId).animate({ width: o.width, height: o.height }, 500);
                    }

                    root.ui.priv.startTriggers();
                    root.ui.priv.startComponents();
                }
            }

            if ((root.ui.event.callBackOpts != null) && (typeof (root.ui.event.callBackOpts.success) == "function") && ((!root.ui.event.callBackOpts.id) || (root.ui.event.callBackOpts.id && root.ui.event.callBackOpts.id == o.callId))) root.ui.event.callBackOpts.success();

            if (o.js) {
                //try{
                    window.o = o;
                    Function(o.js)();
                //}catch (err) { }
            }

            if (o.errMsg || o.ERRMSG) {
                if (o.errMsgTitle == null) o.errMsgTitle = "";
                if (o.errMsgTimer == null) o.errMsgTimer = 3000;
                if (o.ERRMSGTITLE == null) o.ERRMSGTITLE = o.errMsgTitle;
                if (o.ERRMSG == null) o.ERRMSG = o.errMsg;
                if (o.ERRMSGTIMER == null) o.ERRMSGTIMER = o.errMsgTimer;
                root.ui.priv.showMessage(o.ERRMSGTITLE, o.ERRMSG, {
                    className: "error",
                    timer: o.ERRMSGTIMER,
                    redirectURL: o.redirectURL
                });

                return o;
            }

            if (o.msgTitle || o.msg) {
                if (o.msgTimer == null) o.msgTimer = 2000;
                if (o.id == null) {
                    o.id = "message";
                }
                root.ui.priv.showMessage(o.msgTitle, o.msg, {
                    className: o.msgClass,
                    timer: o.msgTimer,
                    id: o.id,
                    redirectURL: o.redirectURL
                });
            }

            if (o.redirectURL) {
                root.ui.priv.lockScreen();
                document.location = o.redirectURL;
                return;
            }

            if (callBackFunction) callBackFunction();
            return o;
        };



        /* Init 
        =================================*/
        root.ui.priv.init = function () {

            root.ui.base.opt = $.extend({}, root.ui.priv.defaultOptions, options);
            root.ui.base.vars = $.extend({}, root.ui.priv.defaultVars, variables, root.ui.priv.defaultStaticVars);

            // Add a reference to the DOM object
            $(document).data("CASUi", root.ui.base);
            root.ui.base.api = $(document).data('CASUi');

            root.ui.priv.startTriggers();

            root.ui.priv.startComponents();
        };




        /* Start components 
        =================================*/
        root.ui.priv.startComponents = function () {

            

        }


        /* Start triggers 
        =================================*/
        root.ui.priv.startFormTriggers = function () {

            $("form").unbind('submit'); //Limpa 
            $("form").each(function (index, element) {
                $(element).submit(function (event) {
                    root.ui.priv.lockScreen();
                });
            });

        }

        /* Start triggers 
        =================================*/
        root.ui.priv.startTriggers = function () {

            root.ui.priv.startFormTriggers();

        };

        root.ui.priv.showLoading = function () {
            /*if (!message) message = "Loading..."*/

            if ($('#request-loading').length > 0)
                return;

            $('body').append('<div id="request-loading" style="opacity: 0;"></div>');

            $('#request-loading').animate({
                opacity: 1
            }, 300);

        };

        root.ui.priv.hideLoading = function () {

            $('#request-loading').animate({
                opacity: 0
            }, 300, function () {
                root.ui.priv.safeRemove('#request-loading');
            });


        };

        root.ui.priv.lockScreen = function () {
            /*if (!message) message = "Loading..."*/

            $.blockUI({
                css: {
                    opacity: 0
                }
            });

            root.ui.priv.showLoading();
        };

        root.ui.priv.unlockScreen = function () {
            try {
                $.unblockUI();
            } catch (err) { }

            root.ui.priv.hideLoading();
        };


        root.ui.priv.newID = function () {
            return new Date().getTime();
        };



        root.ui.priv.safeRemove = function (id) {
            if ($(id)) {
                $(id).remove();
                //Element.remove(id);
            }
        };



        /* 
        ##### End of private functions
        ######################################
        */


        /* 
        ######################################
        ##### root.is.private default variables and options
        */

        /* Default Options
        ----------------------------*/
        root.ui.priv.defaultOptions = { 
        };

        /* Default Variables
        ----------------------------*/
        root.ui.priv.defaultVars = {
        };

        /* Default static Variables
        ----------------------------*/
        root.ui.priv.defaultStaticVars = {
        };


        /* 
        ######################################
        ##### Start all process
        */
        root.ui.priv.init();

    }


})(jQuery);

