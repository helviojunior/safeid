
var iamadmin = {};
var iamfnc = {}; //Object used to runtime functions

//Extends to use :block selector
$.extend($.expr[':'], {
    "block": function(a, i, m) {
        return $(a).css("display") == "block";
    }
});

jQuery(document).ready(function ($) {
    $.IAMAdminUi(
        {},
        {
            scroller: '#content',
            end_scroll_trigger: '#scroll-trigger',
            side_bar: '#content aside',
            header: 'header',
            title_bar: '#titlebar',
            footer: '#footer-wrap',
            search_box: '#searchbox',
            mobile_button_bar: '#titlebar #mobilebar',
            btn_box: '#btnbox'
        }
    );
    iamadmin = $(document).data('IAMAdminUi');
});

var modalBox = {
    base: {},
    show: function (opts) {

        base = this;

        var options = {
            class: 'confirm',
            title: 'Confirmação',
            text: '',
            ok_label: 'OK',
            cancel_label: 'Cncelar',
            show_cancel: true,
            success: null,
            cancel: null
        };

        options = $.extend({}, options, opts);

        base.$modal = $('#modal-box');

        if (options.uri)
            options.text = '<span class="content-loading"></span>'

        var html = '';
        html += '<div id="modal-box" class="' + options.class + '">';
        html += '<form><div id="modal-box-inside"><div class="alert-box">';
        html += '<div class="alert-box-title">';
        html += '        ' + options.title;
        html += '    </div>';
        html += '    <div class="alert-box-content">';
        if (options.uri) {
            html += '                ' + options.text;
        } else {
            html += '            <div class="alert-box-text">';
            html += '                ' + options.text;
            html += '            </div>';
        }
        html += '        <div class="clear-block"></div>';
        html += '    </div>';
        html += '    <div class="alert-box-footer">';
        html += '        <a class="button secondary modal-action">' + options.ok_label + '</a>';
        if (options.show_cancel)
            html += '        <a class="button link modal-close">' + options.cancel_label + '</a>';
        html += '        <div class="clear-block"></div>';
        html += '    </div>';
        html += '</div></div><div class="modal-box-close modal-close"><i class="icon-close remove"></i></div></div>';
        html += '<div id="modal-box-overlay"></form></div>';

        $('body').append(html);

        base.onWindowResize();

        $('#modal-box .modal-close').click(function (event) {
            event.preventDefault();
            base.safeRemove('#modal-box');
            base.safeRemove('#modal-box-overlay');

            if (options.cancel)
                options.cancel();
        });

        $(document).keyup(function (e) {
            if (e.keyCode == 27) {    // esc
                base.safeRemove('#modal-box');
                base.safeRemove('#modal-box-overlay');


                if (options.cancel)
                    options.cancel();
            }
        });

        var onClose = function (event) {
            if (event)
                event.preventDefault();

            $('#modal-box').css('display', 'none');
            $('#modal-box-overlay').css('display', 'none');

            if (options.success)
                options.success(base.$modal);

            base.safeRemove('#modal-box');
            base.safeRemove('#modal-box-overlay');
        }

        if (options.check_uri)
            $('#modal-box .modal-action').click(function () {
                $.ajax({
                    type: "POST",
                    url: options.check_uri,
                    dataType: "text",
                    data: $('#modal-box form').serialize(),
                    error: function (xhr, textStatus, errorThrown) {
                        iamadmin.doJSON(xhr.responseText);
                    },
                    success: function (data) { iamadmin.doJSON(data); onClose(); }
                });
            });
        else
            $('#modal-box .modal-action').click(onClose);

        base.$modal.resize(function () {
            base.onWindowResize();
        });

        $('alert-box-content', base.$modal).resize(function () {
            base.onWindowResize();
        });

        $(window).resize(function () {
            base.onWindowResize();
        });

        if (options.uri)
            $.ajax({
                type: "POST",
                url: options.uri,
                dataType: "text",
                data: options.data,
                error: function (xhr, textStatus, errorThrown) {
                    $('.alert-box-content', base.$modal).html('<div class="alert-box-text">Error loading content</div>');
                },
                success: base.contentLoad
            });

    },
    close: function () {

    },
    contentLoad: function (data) {
        base.$modal = $('#modal-box');
        iamadmin.doJSON(data);
        base.$modal.removeAttr('data-width');
        base.onWindowResize();

        $('select', base.$modal).change(function () {
            base.onWindowResize();
        });

    },
    onWindowResize: function () {

        var n = this;

        n.$modal = $('#modal-box');

        if (!n.$modal.attr('data-width'))
            n.$modal.attr('data-width', n.$modal.outerWidth(!0));

        var i = 0,
            s = $(window).height() / 2,
            a = $(window).width() / 2,
            w = n.$modal.attr('data-width');

        if (w > ($(window).width() * 0.92))
            w = ($(window).width() * 0.92);

        var r = s - n.$modal.outerHeight(!0) / 2;
        var o = a - w / 2;

        i = r, n.$modal.css({
            maxHeight: $(window).height() - 2,
            maxWidth: $(window).width() - 2,
            top: i,
            left: o,
            width: w
        });

    },
    safeRemove: function (id) {
        if ($(id)) {
            $(id).remove();
            //Element.remove(id);
        }
    }
};

(function ($) {

    $.IAMAdminUi = function (options, variables) {


        /* Public Variables
        =================================*/
        var root = this;
        root.admui = {};
        root.admui.base = {};
        root.admui.opt = null;
        root.admui.vars = null;

        /* root.admui.private Variables
        =================================*/
        root.admui.priv = {};
        root.admui.event = {};


        /* 
        ######################################
        ##### public functions
        */

         /* Build Line Chart
        =================================*/
        root.admui.base.buildLineChart = function (content, data, options) {
        
            
            var opts = {
                showText: false,
                color: 'rgba(255,2555,255,.9)'
            };
            opts = $.extend({}, opts, options);

            

            var opt = { 
                animation : false, 
                onAnimationComplete : function(){
                    var theCanvas = $(content + ' canvas').get(0);

                }
            };
            
            $(document).unbind('iam_resize.' + content);
            function draw(){
                
                var $content = $(content);

                if ($content.length > 0){
                    $content.html('<canvas width="'+ ($content.innerWidth() * 0.98) +'" height="'+ ($content.innerHeight() * 0.98) +'"></canvas>');
                    var theCanvas = $(content + ' canvas').get(0);
                    var ctx = theCanvas.getContext("2d");
                    var myNewChart = new Chart(ctx).Line(data, opt);
                }else{
                    $(document).unbind('iam_resize.' + content);
                }
            }
            $(document).on('iam_resize.' + content, draw);
            draw();
            
        }

        /* Build Flow Chart
        =================================*/
        root.admui.base.buildFlowChart = function (content, dataUri) {

            $(document).unbind('iam_resize.' + content);
            
            var chartData = {};
            var chartDataSuccess = function (data) { 
                chartData = JSON.parse(data); 

                function draw(){

                    var $content = $(content);
                    
                    var energy = chartData;
                    
                    var margin = {top: 1, right: 1, bottom: 6, left: 1},
                        width = ($content.innerWidth() * 0.98) - margin.left - margin.right,
                        height = energy.height - margin.top - margin.bottom;

                    var formatNumber = d3.format(",.0f"),
                        format = function(d) { return formatNumber(d); },
                        color = d3.scale.category20();

                    var svg = $(content + ' svg');
                    if (svg.length > 0)
                        svg.remove();

                    svg = d3.select(content).append("svg");
    
                    svg.attr("width", width + margin.left + margin.right)
                    .attr("height", height + margin.top + margin.bottom)
                    .append("g")
                    .attr("transform", "translate(" + margin.left + "," + margin.top + ")");

                    var sankey = d3.sankey()
                        .nodeWidth(15)
                        .nodePadding(10)
                        .size([width, height])
                        .nodes(energy.nodes)
                        .links(energy.links)
                        .layout(32);

                        
                    var path = sankey.link();

                    var link = svg.append("g").selectAll(".link")
                      .data(energy.links)
                      .enter().append("path")
                      .attr("class", "link")
                      .attr("d", path)
                      .style("stroke-width", function (d) { return Math.max(1, d.dy); });
                      //.sort(function (a, b) { return b.dy - a.dy; });

                    link.append("title")
                      .text(function (d) { return d.source.name + " → " + d.target.name + (d.text != undefined ? "\n" + d.text : ""); });//format(d.value)

                    var node = svg.append("g").selectAll(".node")
                      .data(energy.nodes)
                    .enter().append("g")
                      .attr("class", "node")
                      .attr("transform", function (d) { return "translate(" + d.x + "," + d.y + ")"; })
                    .call(d3.behavior.drag()
                      .origin(function (d) { return d; })
                      .on("dragstart", function () { this.parentNode.appendChild(this); })
                      .on("drag", dragmove));

                    node.append("rect")
                      .attr("height", function (d) { return d.dy; })
                      .attr("width", sankey.nodeWidth())
                      .style("fill", function (d) { return d.color = color(d.name.replace(/ .*/, "")); })
                      .style("stroke", function (d) { return d3.rgb(d.color).darker(2); })
                    .append("title")
                      .text(function (d) { return d.name });

                    node.append("text")
                      .attr("x", -6)
                      .attr("y", function (d) { return d.dy / 2; })
                      .attr("dy", ".35em")
                      .attr("text-anchor", "end")
                      .attr("transform", null)
                      .text(function (d) { return d.name; })
                    .filter(function (d) { return d.x < width / 2; })
                      .attr("x", 6 + sankey.nodeWidth())
                      .attr("text-anchor", "start");

                    function dragmove(d) {
                        d3.select(this).attr("transform", "translate(" + d.x + "," + (d.y = Math.max(0, Math.min(height - d.dy, d3.event.y))) + ")");
                        sankey.relayout();
                        link.attr("d", path);
                    }
                
                }
                $(document).on('iam_resize.' + content, draw);
                draw();
            }


            $.ajax({
                type: "POST",
                url: dataUri,
                dataType: "text",
                data: '',
                error: root.admui.event.submitOnError,
                success: chartDataSuccess
            });

        }

        /* Get URI
        =================================*/
        root.admui.base.buildPercentChart = function (contentCanvas, value, options) {
        
            
            var opts = {
                showText: false,
                color: 'rgba(255,2555,255,.9)',
                strokeColor: 'rgba(255,2555,255,.25)',
                textColor: "rgba(255,2555,255,.9)"
            };
            opts = $.extend({}, opts, options);

            var theCanvas = $(contentCanvas).get(0);
            var ctx = theCanvas.getContext("2d");

            var opt = { 
                segmentShowStroke : false, 
                segmentStrokeColor : opts.strokeColor, 
                segmentStrokeWidth : 0, 
                percentageInnerCutout : 80, 
                animation : false, 
                animateRotate : false,
                onAnimationComplete : function(){
                    if(opts.showText){
                        var fontSize = (theCanvas.width < theCanvas.height ? theCanvas.width : theCanvas.height) * 0.20;
                        ctx.fillStyle = opts.textColor;
                        ctx.font = fontSize + "px Arial";
                        ctx.textBaseline="top";

                        var message = value + '%';
                        var metrics = ctx.measureText(message);
                        var xPosition = (theCanvas.width/2) - (metrics.width/2);
                        var yPosition = (theCanvas.height/2) - (fontSize/2) - 2;

                        ctx.fillText(message, xPosition, yPosition);
                    }
                }
            };
            
            var data = [ { value: value, color:opts.color}, { value: 100-value, color:opts.strokeColor} ]; 
            var myNewChart = new Chart(ctx).Doughnut(data, opt);
            
        }

        /* ChangeHash
        =================================*/
        root.admui.base.changeHash = function (newHash) {

            var uri = $.trim(window.location);

            if ((newHash == null) || (newHash == undefined))
                newHash = "";

            var onde = uri.indexOf('#');
            if (onde > 0)
            {
                uri = uri.substr(0, onde);
            }

            if (uri.substr(uri.length - 1, 1) != '/')
                uri += '/';

            if ((newHash != "") && (newHash.substr(1, 1) != '#'))
                newHash = '#' + newHash;

            window.location = uri + newHash;
            
            if (newHash.length > 0)
                root.admui.priv.getPageContent();

        }
        
        /* Get URI
        =================================*/
        root.admui.base.getUri = function (sufix) {

            var uri = $.trim(window.location);
            var hashTag = '';
            var queryString = '';

            var onde = uri.indexOf('#');
            if (onde > 0)
            {
                hashTag = uri.substr(onde, uri.length - onde);
                uri = uri.substr(0, onde);
            }

            if (uri.substr(uri.length - 1, 1) != '/')
                uri += '/';

            if (sufix.substr(1, 1) != '/')
                sufix = sufix.substr(1, sufix.length - 1);

            if (sufix.substr(sufix.length - 1, 1) != '/')
                sufix += '/';

            return uri + sufix + '?' + root.admui.priv.newID();
        }

        /* Get HashTag
        =================================*/
        root.admui.base.getHash = function () {

            var uri = $.trim(window.location);
            var hashTag = '';

            var onde = uri.indexOf('#');
            if (onde > 0)
            {
                hashTag = uri.substr(onde + 1, uri.length - onde - 1);
            }

            return hashTag;
        }


        root.admui.base.passwordStrength = function (fieldId) {
            if ($(fieldId)) {

                clearTimeout(root.admui.base.vars.pwd_strength_timer);

                root.admui.base.vars.pwd_strength_timer = setTimeout(function () {

                    $.ajax({
                        type: "POST",
                        url: ApplicationVirtualPath + "consoleapi/passwordstrength/",
                        dataType: "text",
                        data: 'password=' + $(fieldId).val(),
                        error: root.admui.event.submitOnError,
                        success: root.admui.event.submitOnSuccess
                    });

                }, 300);


            }
        };

        
        /* Get page content
        =================================*/
        root.admui.base.getPageContent2 = function (options, callback) {

            var cOpts = {
                id: 'content-' + root.admui.priv.newID(),
                error: null,
                success: function () {
                    
                    if (typeof(callback) == "function")
                        callback();

                    $(root.admui.base.vars.scroller + ' table tbody tr').each(function (index, element) {
                        if ($(this).attr('data-href')) {
                            $(this).unbind('click');
                            $(this).click(function (event) {
                                event.preventDefault();
                                window.location = $(this).attr("data-href");
                            });
                        }
                    });
                                       


                    //Editor de nome
                    $('.field-editor').each(function (index, element) {
                        if ($(this).attr('data-function')) {
                            $(this).unbind('click');
                            $(this).click(function (event) {
                                event.preventDefault();
                                Function($(this).attr('data-function'))()
                            });
                        }
                    });

                }
            };

            root.admui.event.callBackOpts = $.extend({}, cOpts);

            var args = {
                cid: cOpts.id
            }

            var getOpts = {}
            getOpts = $.extend({}, args, options, { hashtag: root.admui.base.getHash() });

            $.ajax({
                type: "POST",
                url: (getOpts.search ? root.admui.base.getUri("/search/" + encodeURI($.trim(getOpts.search)) + "/") : root.admui.base.getUri("/content/")),
                dataType: "text",
                data: getOpts,
                error: root.admui.event.submitOnError,
                success: root.admui.event.submitOnSuccess
            });

        }

        /* Get page content
        =================================*/
        root.admui.base.editTextField = function (thisId, options, onEditCallback){
            $oThis = $(thisId);
            $oThis.css('visibility','hidden');

            //Cria campo de texto
            var offset = $oThis.offset();
            
            root.admui.priv.safeRemove('#text-editor');
            $('body').append('<div id="text-editor" style="opacity: 0;"><input type="text" value="" /></div>');
            $oTxt = $('#text-editor');

            $oTxt.css('position','absolute');
            $oTxt.css('left',offset.left + 'px');
            $oTxt.css('top',offset.top + 'px');
            $oTxt.css('opacity','1');
            $('#text-editor input').css('min-width','200px');
            $('#text-editor input').css('width',$oThis.width() + 'px');
            $('#text-editor input').css('height',($oThis.innerHeight() - 2) + 'px');
            $('#text-editor input').focus();
            $('#text-editor input').val($oThis.text());

            $oTxt.focusout(function() {
                root.admui.priv.safeRemove('#text-editor');
                $oThis.css('visibility','visible');
            });

            $('#text-editor input').keypress(function( event ) {
                if ( event.which == 13 ) {
                    event.preventDefault();
                    //$oThis.html($(this).val());
                    $oTxt.focusout();

                    if ((onEditCallback != null) && (jQuery.isFunction(onEditCallback))) onEditCallback(thisId, $(this).val());

                }
            });
        }


        root.admui.base.changeName = function (thisId, newName){
            $oThis = $(thisId);
            if ($oThis.attr('data-id')) {
                
                root.admui.priv.showLoading();
                $.ajax({
                    type: "POST",
                    url: root.admui.base.getUri("/" +$oThis.attr('data-id') + "/action/change_name/"),
                    dataType: "text",
                    data: { name: newName, hashtag: root.admui.base.getHash() },
                    error: root.admui.event.submitOnError,
                    success: root.admui.event.submitOnSuccess
                });

                //$oThis.html(newName);
            }else{
                root.admui.priv.showMessage("Erro", "Error on change role name", {
                    className: "error",
                    timer: 3000
                });
            }
        }
        
        root.admui.base.autoCompleteText = function (thisId, uri, send_data, onSelectCallback){
            $oThis = $(thisId);

            

            $oThis.autocomplete({
                source: function( request, response ) {

                    var sd = $.extend(
                        {}, 
                        send_data,
                        {
                            text: request.term,
                            hashtag: root.admui.base.getHash()
                        });

                    $.ajax({
                        type: "POST",
                        url: uri,
                        dataType: "text",
                        data: sd,
                        error: root.admui.event.submitOnError,
                        success: function(data){
                            response(JSON.parse(data));
                        }
                    });
                },
                minLength: 2,
                select: function( event, ui ) { 
                    event.preventDefault(); 

                    if ((onSelectCallback != null) && (jQuery.isFunction(onSelectCallback))) onSelectCallback(thisId, ui.item);

                    //$(this).val(''); $("#tst").append(ui.item.id + ' ==> ' + ui.item.value);
                }
            });
        }

        root.admui.base.buildPwdRule = function (objThis){
            $oThis = $(objThis);
            
            modalBox.show({
                class: 'pwd-rule',
                title: ($oThis.attr('confirm-title') ? $oThis.attr('confirm-title') : 'Password rule'), 
                text: ($oThis.attr('confirm-text') ? $oThis.attr('confirm-text') : 'Confirma a operação?'),
                ok_label: ($oThis.attr('ok') ? $oThis.attr('ok') : 'OK'),
                cancel_label: ($oThis.attr('cancel') ? $oThis.attr('cancel') : 'Cancel'),
                uri: $oThis.attr('data-uri'),
                success: function (modal) {
                    var rule = $('#pwd_rule', modal).val() + '[' + $('#pwd_pass', modal).val() + ']';
                    $('span', $oThis).text(rule);
                    $('input[type=hidden]', $oThis).attr('value',rule);
                },
                cancel: function(){}
             });

        }

        
        root.admui.base.buildRoleActRule = function (objThis){
            $oThis = $(objThis);
            
            modalBox.show({
                class: 'role-act-rule',
                title: ($oThis.attr('confirm-title') ? $oThis.attr('confirm-title') : 'Action rule'), 
                text: ($oThis.attr('confirm-text') ? $oThis.attr('confirm-text') : 'Confirma a operação?'),
                ok_label: ($oThis.attr('ok') ? $oThis.attr('ok') : 'OK'),
                cancel_label: ($oThis.attr('cancel') ? $oThis.attr('cancel') : 'Cancel'),
                uri: $oThis.attr('data-uri'),
                data: $('input', $oThis).serialize(),
                success: function (modal) {
                    var key = $('.key', modal).val();
                    $('.key', $oThis).attr('value', key);
                    $('.add_value', $oThis).attr('value', $('.' + key +' .add_value', modal).val());
                    $('.del_value', $oThis).attr('value', $('.' + key +' .del_value', modal).val());
                    $('span', $oThis).text($('.key :selected', modal).text());
                },
                cancel: function(){}
             });

        }
        
        root.admui.base.buildRoleTimeAcl = function (objThis){
            $oThis = $(objThis);
            
            modalBox.show({
                class: 'role-timeacl-rule',
                title: ($oThis.attr('confirm-title') ? $oThis.attr('confirm-title') : 'Time ACL rule'), 
                text: ($oThis.attr('confirm-text') ? $oThis.attr('confirm-text') : 'Confirma a operação?'),
                ok_label: ($oThis.attr('ok') ? $oThis.attr('ok') : 'OK'),
                cancel_label: ($oThis.attr('cancel') ? $oThis.attr('cancel') : 'Cancel'),
                uri: $oThis.attr('data-uri'),
                check_uri: $oThis.attr('check-uri'),
                data: $('input', $oThis).serialize(),
                success: function (modal) {
                    $('.type', $oThis).attr('value', $('.type', modal).val());
                    $('.start_time', $oThis).attr('value', $('.start_time', modal).val());
                    $('.end_time', $oThis).attr('value', $('.end_time', modal).val());
                    $('.end_time', $oThis).attr('value', $('.end_time', modal).val());

                    var wd = '';
                    $('.week_day:checked', modal).each(function() { 
                        wd += $(this).val() + ",";
                    });

                    $('.week_day', $oThis).attr('value', wd);

                    $('span', $oThis).text($('input.title', modal).val());
                },
                cancel: function(){}
             });

        }

        
        root.admui.base.buildWorkflowAct = function (objThis){
            $oThis = $(objThis);
            
            modalBox.show({
                class: 'role-act-rule',
                title: ($oThis.attr('confirm-title') ? $oThis.attr('confirm-title') : 'Action rule'), 
                text: ($oThis.attr('confirm-text') ? $oThis.attr('confirm-text') : 'Confirma a operação?'),
                ok_label: ($oThis.attr('ok') ? $oThis.attr('ok') : 'OK'),
                cancel_label: ($oThis.attr('cancel') ? $oThis.attr('cancel') : 'Cancel'),
                uri: $oThis.attr('data-uri'),
                data: $('input', $oThis).serialize(),
                success: function (modal) {
                    $('.key', $oThis).attr('value', $('.key', modal).val());
                    $('.add_value', $oThis).attr('value', $('.add_value', modal).val());
                    $('.del_value', $oThis).attr('value', $('.del_value', modal).val());
                    $('span', $oThis).text($('.key :selected', modal).text());
                },
                cancel: function(){}
             });

        }
        
        root.admui.base.openModal = function (objThis, options, callBackFunction){
            $oThis = $(objThis);
            
            var opt = $.extend({}, {
                class: 'modal-box',
                title: ($oThis.attr('modal-title') ? $oThis.attr('modal-title') : 'Modal box'), 
                text: ($oThis.attr('modal-text') ? $oThis.attr('modal-text') : 'Confirma a operação?'),
                ok_label: ($oThis.attr('ok') ? $oThis.attr('ok') : 'OK'),
                cancel_label: ($oThis.attr('cancel') ? $oThis.attr('cancel') : 'Cancel'),
                uri: $oThis.attr('data-uri'),
                data: $('input', $oThis).serialize(),
                success: function (modal) {
                    if (typeof (callBackFunction) == "function")
                        callBackFunction(modal);
                },
                cancel: function(){}
             }, options);

            modalBox.show(opt);

        }

        
        
        /* Open Modal box of log view
        =================================*/
        root.admui.base.openLog = function (objThis) {
            $oThis = $(objThis);

            modalBox.show({
                class: 'pwd-rule',
                title: ($oThis.attr('data-title') ? $oThis.attr('data-title') : 'Log'), 
                text: '',
                ok_label: 'OK',
                cancel_label: '',
                show_cancel: false,
                uri: $oThis.attr('data-uri'),
                success: function (modal) { },
                cancel: function(){}
             });

        }

        root.admui.base.licenseUploader = function (objThis, submitUri, itemTemplate) {
            $oThis = $(objThis);

            $(document).bind('drop dragover', function (e) {
                e.preventDefault();
            });

            $(document).bind('drop', function (e) {
                e.preventDefault();
                $oThis.removeClass('drag hover');
            });
            
            $('.drag-content', $oThis).bind('click', function (e) {
                e.preventDefault();
                $('input[type=file]', $oThis).click();
            });

            $(document).bind('dragover', function (e) {
                var dropZone = $oThis,
                    timeout = window.dropZoneTimeout;
                if (!timeout) {
                    dropZone.addClass('drag');
                } else {
                    clearTimeout(timeout);
                }
                var found = false,
                    node = e.target;
                do {
                    if (node === dropZone[0]) {
                        found = true;
                        break;
                    }
                    node = node.parentNode;
                } while (node != null);
                if (found) {
                    dropZone.addClass('hover');
                } else {
                    dropZone.removeClass('hover');
                }
                window.dropZoneTimeout = setTimeout(function () {
                    window.dropZoneTimeout = null;
                    dropZone.removeClass('drag hover');
                }, 100);
            });


            $('input', $oThis).fileupload({
                url: submitUri,
                dataType: 'json',
                dropZone: $oThis,
                add: function (e, data) {
                    $oThis.removeClass('drag hover');
                    $('#files').html('');
                    data.context = $('<div/>').appendTo('#files');

                    $.each(data.files, function (index, file) {

                        file.id = root.admui.priv.newID();

                        var node = $('<div/>', { id: file.id});
                        node.appendTo(data.context);
                        
                        $.ajax({
                            type: "POST",
                            url: itemTemplate,
                            dataType: "text",
                            async: false,
                            data: {
                                id: file.id,
                                file: file.name,
                                size: file.size
                            },
                            success: function(t){            
                                $('#files .none').remove();
                                root.admui.event.submitOnSuccess(t);
                            }
                        });

                    });
                    data.submit();

                },
                done: function (e, data) {
                    //data.context.text('Upload finished.');
                }
            }).on('fileuploadfail', function (e, data) {
                $.each(data.files, function (index, file) {
                    $('#' + file.id + ' .status').html('File upload failed.');
                    
                    /*$(data.context.children()[index])
                        .append('<br>')
                        .append(error);*/
                });
            }).on('fileuploaddone', function (e, data) {
                $.each(data.result.files, function (index, file) {
                    console.log(data.files);

                    //Localiza o arquivo enviado
                    $.each(data.files, function (i, fs) {
                        if (fs.name == file.name){
                            if (file.error) {
                                $('#' + fs.id + ' .status').html(file.error);
                            }else if(file.html){
                                $('#' + fs.id + ' .description').html(file.html);
                            }else{
                                $('#' + fs.id + ' .status').html('Upload ok');
                            }
                        }
                    });

                    //$('.file-item[data-name='+ file.name.replace('.','') +'] .status',  $oThis).html(file.error);
                    /*
                    if (file.url) {
                        var link = $('<a>')
                            .attr('target', '_blank')
                            .prop('href', file.url);
                        $(data.context.children()[index])
                            .wrap(link);
                    } else if (file.error) {
                        var error = $('<span class="text-danger"/>').text(file.error);
                        $(data.context.children()[index])
                            .append('<br>')
                            .append(error);
                    }*/
                });
            });

        }

        root.admui.base.pluginUploader = function (objThis, submitUri, itemTemplate) {
            $oThis = $(objThis);

            $(document).bind('drop dragover', function (e) {
                e.preventDefault();
            });

            $(document).bind('drop', function (e) {
                e.preventDefault();
                $oThis.removeClass('drag hover');
            });
            
            $('.drag-content', $oThis).bind('click', function (e) {
                e.preventDefault();
                $('input[type=file]', $oThis).click();
            });

            $(document).bind('dragover', function (e) {
                var dropZone = $oThis,
                    timeout = window.dropZoneTimeout;
                if (!timeout) {
                    dropZone.addClass('drag');
                } else {
                    clearTimeout(timeout);
                }
                var found = false,
                    node = e.target;
                do {
                    if (node === dropZone[0]) {
                        found = true;
                        break;
                    }
                    node = node.parentNode;
                } while (node != null);
                if (found) {
                    dropZone.addClass('hover');
                } else {
                    dropZone.removeClass('hover');
                }
                window.dropZoneTimeout = setTimeout(function () {
                    window.dropZoneTimeout = null;
                    dropZone.removeClass('drag hover');
                }, 100);
            });


            $('input', $oThis).fileupload({
                url: submitUri,
                dataType: 'json',
                dropZone: $oThis,
                add: function (e, data) {
                    $oThis.removeClass('drag hover');
                    data.context = $('<div/>').appendTo('#files');

                    $.each(data.files, function (index, file) {

                        file.id = root.admui.priv.newID();

                        var node = $('<div/>', { id: file.id});
                        node.appendTo(data.context);
                        
                        $.ajax({
                            type: "POST",
                            url: itemTemplate,
                            dataType: "text",
                            async: false,
                            data: {
                                id: file.id,
                                file: file.name,
                                size: file.size
                            },
                            success: function(t){            
                                $('#files .none').remove();
                                root.admui.event.submitOnSuccess(t);
                            }
                        });

                    });
                    data.submit();

                },
                done: function (e, data) {
                    //data.context.text('Upload finished.');
                }
            }).on('fileuploadfail', function (e, data) {
                $.each(data.files, function (index, file) {
                    $('#' + file.id + ' .status').html('File upload failed.');
                    
                    /*$(data.context.children()[index])
                        .append('<br>')
                        .append(error);*/
                });
            }).on('fileuploaddone', function (e, data) {
                $.each(data.result.files, function (index, file) {
                    console.log(data.files);

                    //Localiza o arquivo enviado
                    $.each(data.files, function (i, fs) {
                        if (fs.name == file.name){
                            if (file.error) {
                                $('#' + fs.id + ' .status').html(file.error);
                            }else if(file.html){
                                $('#' + fs.id + ' .description').html(file.html);
                            }else{
                                $('#' + fs.id + ' .status').html('Upload ok');
                            }
                        }
                    });

                    //$('.file-item[data-name='+ file.name.replace('.','') +'] .status',  $oThis).html(file.error);
                    /*
                    if (file.url) {
                        var link = $('<a>')
                            .attr('target', '_blank')
                            .prop('href', file.url);
                        $(data.context.children()[index])
                            .wrap(link);
                    } else if (file.error) {
                        var error = $('<span class="text-danger"/>').text(file.error);
                        $(data.context.children()[index])
                            .append('<br>')
                            .append(error);
                    }*/
                });
            });

        }

        /* Reload page after timeout
        =================================*/
        root.admui.base.doReload = function (timeout) {
            clearTimeout(root.admui.base.vars.reload_timer);

            root.admui.base.vars.reload_timer = setTimeout(function () { 
                
                root.admui.priv.getPageContent(true);    
                clearTimeout(root.admui.base.vars.reload_timer);

            }, timeout);
            
        }

        /* 
        ##### End of public functions
        ######################################
        */


        /* 
        ######################################
        ##### root.is.private functions
        */


        /* Init 
        =================================*/
        root.admui.priv.init = function () {

            root.admui.base.opt = $.extend({}, root.admui.priv.defaultOptions, options);
            root.admui.base.vars = $.extend({}, root.admui.priv.defaultVars, variables, root.admui.priv.defaultStaticVars);

            // Add a reference to the DOM object
            $(document).data("IAMAdminUi", root.admui.base);
            root.admui.base.api = $(document).data('IAMAdminUi');

            root.admui.priv.startTriggers();

            root.admui.priv.startComponents();

            root.admui.priv.getPageContent();

            root.admui.priv.startPing();
        };




        /* Start components 
        =================================*/
        root.admui.priv.startComponents = function () {

            $('table.sorter').tablesorter();

        }


        /* Start triggers 
        =================================*/
        root.admui.priv.startFormTriggers = function () {

            $("form").unbind('submit'); //Limpa 
            $("form").each(function (index, element) {
                $(element).submit(function (event) {
                    event.preventDefault();
                    root.admui.priv.genericSubmit('#' + $(element).attr('id'));
                });
            });

        }

        /* Start triggers 
        =================================*/
        root.admui.priv.startTriggers = function () {


            //Load page content
            /*
            if (LoadingText)
            $('#content').html(LoadingText);*/

            $(window).bind('beforeunload', function(){
                root.admui.base.vars.unloaded = true;
            });

            //iamadmin.OpenUrl(window.location, '', 'POST');
            root.admui.priv.startFormTriggers();

            $("#content .home a").unbind('click');
            $("#content .home a").each(function (index, element) {
                $(this).click(function (event) {
                    event.preventDefault();
                    window.location = $(this).attr("href");
                });
            });

            $(".section-nav-header a").unbind('click');
            $(".section-nav-header a").each(function (index, element) {
                $(this).click(function (event) {
                    event.preventDefault();
                    window.location = $(this).attr("href");
                });
            });


            root.admui.event.onWindowResize();
            $(window).unbind('resize');
            $(window).resize(function () {
                root.admui.event.onWindowResize();
            });

            $('#menu-user-dropdown').unbind('click');
            $('#menu-user-dropdown').click(function () {
                if ($(this).hasClass('hover'))
                    $(this).removeClass('hover');
                else
                    $(this).addClass('hover');
            });


            //Start Scroll calc
            var on_check = function () {
                if (root.admui.base.vars.check_lock) {
                    return;
                }
                root.admui.base.vars.check_lock = true;

                root.admui.event.onScroll();
                //setTimeout(root.admui.event.onScroll, 50);
            };
            
            if (!root.admui.base.vars.check_binded)
            {
                //$(root.admui.base.vars.scroller).unbind('scroll');
                $(root.admui.base.vars.scroller).scroll(on_check).resize(on_check);

                $(root.admui.base.vars.end_scroll_trigger).appear({ context: root.admui.base.vars.scroller });
                $(root.admui.base.vars.scroller).on('appear', root.admui.base.vars.end_scroll_trigger, function (e, $affected) {
                    root.admui.event.endOfScroll();
                });
                
                root.admui.base.vars.check_binded = true;
            }

            //Campos de busca

            $(root.admui.base.vars.search_box + ' input[type=text]').unbind('keyup');
            $(root.admui.base.vars.search_box + ' input[type=text]').each(function (index, element) {
                $(this).keyup(function (event) {
                    root.admui.event.onSearchType($(this).val());
                }).keydown(function (event) {
                    if (event.which == 13) {
                        event.preventDefault();
                    }
                });
            });


            //Ações específicas de usuário
            $('#btnbox a.user-lock, #btnbox a.user-resetpwd, #btnbox a.user-deploy, .confirm-action').unbind('click');
            $('#btnbox a.user-lock, #btnbox a.user-resetpwd, #btnbox a.user-deploy, .confirm-action').click(function (event) {
                event.preventDefault();
                event.stopPropagation();
                root.admui.priv.confirmAction($(this), $(this).attr("href"));
            });

            $('.data-action').unbind('click');
            $('.data-action').click(function (event) {
                event.preventDefault();
                event.stopPropagation();
                root.admui.priv.hrefAction($(this), $(this).attr("data-action"));
            });

            //Ação dos botões
            $('.a-btn').mouseenter( function(){
                $(this).addClass('a-btn-hover');
            } ).mouseleave( function(){
                $(this).removeClass('a-btn-hover');
            } );

            $('.menu-btn.menu .menu-item').unbind('click');
            $('.menu-btn.menu .menu-item').click( function(event){
                event.preventDefault();
                event.stopPropagation();
                $(document).unbind('click.menu-btn');
                $base = $(this).closest('.menu-btn');
                if ($base.hasClass('active')){
                    $base.removeClass('active');    
                }
                else
                {
                    $(document).bind('click.menu-btn', function(event){
                            $('.menu-btn.active').removeClass('active');
                    });
                    $base.addClass('active');
                    $pannel = $('.pannel', $base);
                    var os = $(this).offset();
                    var maxWidth = $(document).width() - os.left - 30;
                    if (parseInt($pannel.css('max-width')) < maxWidth)
                        maxWidth = parseInt($pannel.css('max-width'));

                    $pannel.css( {
                        left: os.left, 
                        top: os.top + $(this).outerHeight(), 
                        maxWidth: maxWidth });

                    //Calcula o maior box para definir o tamanho dos outros
                    var minHeight = 0;
                    $.each($('li', $pannel), function (i, item) {
                        console.log($(item).innerHeight());
                        if ($(item).innerHeight() > minHeight)
                            minHeight = $(item).innerHeight();
                    });

                    $('li', $pannel).css('min-height', minHeight);
                }
            });
            

        };
        
        root.admui.priv.hrefAction = function ($obj, action) {
            
            root.admui.priv.lockScreen();
                    
            var cOpts = {
                error: null,
                success: function(){
                    if ($obj.hasClass('no-reload'))
                        return;
                    
                    root.admui.priv.getPageContent();
                }
            };

            root.admui.event.callBackOpts = $.extend({}, cOpts);

            $.ajax({
                type: "POST",
                url: action,
                dataType: "text",
                data: { hashtag: root.admui.base.getHash() },
                error: root.admui.event.submitOnError,
                success: root.admui.event.submitOnSuccess
            });

        }

        root.admui.priv.confirmAction = function ($obj, action) {
        
            modalBox.show({
                title: ($obj.attr('confirm-title') ? $obj.attr('confirm-title') : 'Confirmação'), 
                text: ($obj.attr('confirm-text') ? $obj.attr('confirm-text') : 'Confirma a operação?'),
                ok_label: ($obj.attr('ok') ? $obj.attr('ok') : 'OK'),
                cancel_label: ($obj.attr('cancel') ? $obj.attr('cancel') : 'Cancelar'),
                success: function () {
                    
                    root.admui.priv.lockScreen();

                    //window.location = action;
                    
                    var cOpts = {
                        error: null,
                        success: function(){
                            if ($obj.hasClass('no-reload'))
                            return;
                    
                            root.admui.priv.getPageContent();
                        }
                    };

                    root.admui.event.callBackOpts = $.extend({}, cOpts);

                    $.ajax({
                        type: "POST",
                        url: action,
                        dataType: "text",
                        data: { hashtag: root.admui.base.getHash() },
                        error: root.admui.event.submitOnError,
                        success: root.admui.event.submitOnSuccess
                    });

                },
                cancel: function(){}
             });
        }

        root.admui.priv.isMobile = function () {
            return (function (a) {
                if (/(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows (ce|phone)|xda|xiino/i.test(a) || /1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-/i.test(a.substr(0, 4))) { return true; } else { return false; }
            })(navigator.userAgent || navigator.vendor || window.opera);
        };

        root.admui.priv.addWait = function (container) {
            try {
                $(container).addClass("wait-full");
            } catch (err) { }
        };


        root.admui.priv.removeWait = function (container) {
            try {
                $(container).removeClass("wait-full");
            } catch (err) { }
        };


        root.admui.priv.showLoading = function () {
            /*if (!message) message = "Loading..."*/

            if ($('#request-loading').length > 0)
                return;

            $('body').append('<div id="request-loading" style="opacity: 0;"></div>');

            $('#request-loading').animate({
                opacity: 1
            }, 300);

        };

        root.admui.priv.hideLoading = function () {

            $('#request-loading').animate({
                opacity: 0
            }, 300, function () {
                root.admui.priv.safeRemove('#request-loading');
            });


        };

        root.admui.priv.lockScreen = function () {
            /*if (!message) message = "Loading..."*/

            $.blockUI({
                css: {
                    opacity: 0
                }
            });

            root.admui.priv.showLoading();
        };

        root.admui.priv.unlockScreen = function () {
            try {
                $.unblockUI();
            } catch (err) { }

            root.admui.priv.hideLoading();
        };


        root.admui.priv.newID = function () {
            return new Date().getTime();
        };


        root.admui.priv.openUrl = function (uri, args, method, opts) {

            if (typeof (method) == "undefined") {
                method = 'POST';
            }

            root.admui.priv.lockScreen();

            var cOpts = {
                error: null,
                success: null
            };

            root.admui.event.callBackOpts = $.extend({}, cOpts, opts);

            $.ajax({
                type: "POST",
                url: uri,
                dataType: "text",
                data: args,
                error: root.admui.event.submitOnError,
                success: root.admui.event.submitOnSuccess
            });

        };


        root.admui.priv.genericSubmit = function (formId, argAction, opts) {
            if ($(formId) == null) {
                alert("Form " + formId + " not found");
                return false;
            }

            try {

                if (typeof (argAction) == "undefined") {
                    argAction = $(formId).attr("action");
                }

                if ((typeof (argAction) == "object") || (typeof (argAction) == "undefined")) {
                    argAction = "default.aspx";
                }

                //Adiciona uma tag apenas para não utilizar cache do navegador e servidor
                argAction += (argAction.indexOf('?') == -1 ? '?' : '&') + 'ts=' + root.admui.priv.newID();

                root.admui.priv.lockScreen();

                var cOpts = {
                    error: null,
                    success: null
                };

                root.admui.event.callBackOpts = $.extend({}, cOpts, opts);

                $.ajax({
                    type: "POST",
                    url: argAction,
                    dataType: "text",
                    data: $(formId).serialize() + '&hashtag=' + root.admui.base.getHash(),
                    error: root.admui.event.submitOnError,
                    success: root.admui.event.submitOnSuccess
                });

            }
            catch (err) {
                alert(err);
            }

            return false;
        };

        root.admui.priv.showMessage = function (messageTitle, message, opt) {
            var v37 = {
                id: "message",
                timer: 1500,
                bShowClose: false,
                bFastClose: false,
                bShowTitle: false,
                redirectURL: undefined
            };

            v37 = $.extend({}, v37, opt);

            if (root.admui.base.vars.timers["msgTimer" + v37.id] != null) clearTimeout(root.admui.base.vars.timers["msgTimer" + v37.id]);
            var html = '<div id="' + v37.id + '"';
            if (v37.className != null) html += ' class="' + v37.className + '"';
            html += ' onclick="this.style.display=\'none\'" style="z-index:1000001; top:-100px;"><div id="msg-wrap">';
            if (v37.bShowClose) html += '<a class=\"hide\" style="float:right;margin-right:50px;"><img src="images/close.gif" width="15" height="15" alt="Close" border="0"></a>';
            if (messageTitle) html += '<div class=msgTitle>' + messageTitle + '</div>';
            html += message;
            html += "</div></div>";
            root.admui.priv.safeRemove('#' + v37.id);

            $('body').append(html);

            $('#' + v37.id + ' a.hide').click(function () {
                root.admui.priv.hideMessage();
            });

            if ((v37.bShowTitle != undefined) && (!v37.bShowTitle))
                root.admui.priv.safeRemove('.msgTitle');

            $('#' + v37.id).animate({
                top: -50
            }, 400, "easeOutElastic",
            function () {
                if (v37.timer) {
                    root.admui.base.vars.timers["msgTimer" + v37.id] = setTimeout(function () {
                        root.admui.priv.hideMessage((v37.bFastClose ? 1 : 0), v37.id);
                    }, v37.timer);
                }

                if ((v37.redirectURL) && (v37.redirectURL != ''))
                    document.location = v37.redirectURL;
            });

        };


        root.admui.priv.hideMessage = function (bFast, id) {
            if (id == null) id = "message";
            if (bFast) root.admui.priv.safeRemove(id);
            else if ($('#' + id) != null) {
                $('#' + id).animate({
                    opacity: 0
                }, 800, "linear",
                function () {
                    $('#' + id).css("top", -($('#' + id).outerHeight() + 5))
                }
                );
            }
            if (root.admui.base.vars.timers["msgTimer" + id] != null) clearTimeout(root.admui.base.vars.timers["msgTimer" + id]);
        };


        root.admui.priv.hideMessageInAFewSecs = function () {
            tw.msgTimer = setTimeout(root.admui.priv.hideMessage, 1500);
        };

        root.admui.priv.safeRemove = function (id) {
            if ($(id)) {
                $(id).remove();
                //Element.remove(id);
            }
        };


        

        root.admui.base.doJSON = function (json, callBackFunction) {

            if ($.trim(json) == '')
                return;

            var o = {};
            try {
                if (json.length == 1) o = {};
                else o = jQuery.parseJSON(json);
            } catch (e) {
                //o = eval('(' + json + ')');
                root.admui.priv.showMessage("Erro", "Error parsing JSON data", {
                    className: "error",
                    timer: 3000
                });

                if ((root.admui.event.callBackOpts != null) && (typeof (root.admui.event.callBackOpts.error) == "function")) root.admui.event.callBackOpts.error();
                return;
            }

            if (o instanceof Array) {
                $.each(o, function (i, item) {
                    root.admui.base.doJSONElement(item, callBackFunction);    
                });
            } else {
                root.admui.base.doJSONElement(o, callBackFunction);
            }
        };

        root.admui.base.doJSONElement = function (o, callBackFunction) {

            if (o.containerId && o.html) {
                if ($(o.containerId).length > 0) {
                    if (o.append)
                        $(o.containerId).append(o.html);
                    else
                        $(o.containerId).html(o.html);

                    if (o.width && o.height) {
                        $(o.containerId).animate({ width: o.width, height: o.height }, 500);
                    }

                    root.admui.priv.startTriggers();
                    root.admui.priv.startComponents();
                }
            }

            if ((root.admui.event.callBackOpts != null) && (typeof (root.admui.event.callBackOpts.success) == "function") && ((!root.admui.event.callBackOpts.id) || (root.admui.event.callBackOpts.id && root.admui.event.callBackOpts.id == o.callId))) root.admui.event.callBackOpts.success();

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
                root.admui.priv.showMessage(o.ERRMSGTITLE, o.ERRMSG, {
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
                root.admui.priv.showMessage(o.msgTitle, o.msg, {
                    className: o.msgClass,
                    timer: o.msgTimer,
                    id: o.id,
                    redirectURL: o.redirectURL
                });
            }

            if (o.redirectURL) {
                root.admui.priv.lockScreen();
                document.location = o.redirectURL;
                return;
            }

            if (callBackFunction) callBackFunction();
            return o;
        };

        
        /* Get page content
        =================================*/
        root.admui.priv.getPageContent = function (pageOnly) {
            root.admui.priv.lockScreen();

            var cOpts = {
                id: 'content-' + root.admui.priv.newID(),
                error: null,
                success: function () {
                    $(root.admui.base.vars.scroller + ' table tbody tr').each(function (index, element) {
                        if ($(this).attr('data-href')) {
                            $(this).unbind('click');
                            $(this).click(function (event) {
                                event.preventDefault();
                                window.location = $(this).attr("data-href");
                            });
                        }
                    });

                    
                    //Editor de nome
                    $('.field-editor').each(function (index, element) {
                        if ($(this).attr('data-function')) {
                            $(this).unbind('click');
                            $(this).click(function (event) {
                                event.preventDefault();
                                Function($(this).attr('data-function'))()
                            });
                        }
                    });

                }
            };

            root.admui.event.callBackOpts = $.extend({}, cOpts);

            $.ajax({
                type: "POST",
                url: root.admui.base.getUri("/content/"),
                dataType: "text",
                data: {cid: cOpts.id, hashtag: root.admui.base.getHash() },
                error: root.admui.event.submitOnError,
                success: root.admui.event.submitOnSuccess
            });

            if (pageOnly == true)
                return;

            if ($(root.admui.base.vars.side_bar).length > 0) {
                $.ajax({
                    type: "POST",
                    url: root.admui.base.getUri("/content/sidebar/"),
                    dataType: "text",
                    data: { hashtag: root.admui.base.getHash() },
                    error: root.admui.event.submitOnError,
                    success: root.admui.event.submitOnSuccess
                });
            }

            if ($(root.admui.base.vars.mobile_button_bar).length > 0) {
                $.ajax({
                    type: "POST",
                    url: root.admui.base.getUri("/content/mobilebar/"),
                    dataType: "text",
                    data: { hashtag: root.admui.base.getHash() },
                    error: root.admui.event.submitOnError,
                    success: root.admui.event.submitOnSuccess
                });
            }

            
            if ($(root.admui.base.vars.btn_box).length > 0) {
                $.ajax({
                    type: "POST",
                    url: root.admui.base.getUri("/content/buttonbox/"),
                    dataType: "text",
                    data: { hashtag: root.admui.base.getHash() },
                    error: root.admui.event.submitOnError,
                    success: root.admui.event.submitOnSuccess
                });
            }

        }

        
        /* Start ping keep alive process
        =================================*/
        root.admui.priv.startPing = function () {
            clearTimeout(root.admui.base.vars.ping_timer);

            root.admui.base.vars.ping_timer = setTimeout(function () {

                $.ajax({
                    type: "GET",
                    url: ApplicationVirtualPath + "ping/",
                    dataType: "text",
                    data: '',
                    error: function(){ root.admui.priv.startPing() },
                    success: function(){ root.admui.priv.startPing() }
                });

            }, 60 * 1000);

        }



        /* 
        ##### End of private functions
        ######################################
        */



        /* 
        ######################################
        ##### root.admui.event functions
        */

        

        /* On Scrool Events
        =================================*/
        root.admui.event.onSearchType = function (text) {
            clearTimeout(root.admui.base.vars.search_type_timer);

            if (root.admui.base.vars.last_search == $.trim(text))
                return;

            root.admui.base.vars.search_type_timer = setTimeout(function () {

                //root.admui.priv.lockScreen();
                root.admui.priv.showLoading();

                var cOpts = {
                    id: 'search-' + root.admui.priv.newID(),
                    error: null,
                    success: function () {
                        $(root.admui.base.vars.scroller + ' table tbody tr').each(function (index, element) {
                            if ($(this).attr('data-href')) {
                                $(this).unbind('click');
                                $(this).click(function (event) {
                                    event.preventDefault();
                                    window.location = $(this).attr("data-href");
                                });
                            }
                        });

                        
                        //Editor de nome
                        $('.field-editor').each(function (index, element) {
                            if ($(this).attr('data-function')) {
                                $(this).unbind('click');
                                $(this).click(function (event) {
                                    event.preventDefault();
                                    Function($(this).attr('data-function'))()
                                });
                            }
                        });

                    }
                };

                root.admui.event.callBackOpts = $.extend({}, cOpts);

                if ($.trim(text).length > 0) {

                    root.admui.base.vars.last_search = $.trim(text);

                    $.ajax({
                        type: "POST",
                        url: root.admui.base.getUri("/search/" + encodeURI($.trim(text)) + "/"),
                        dataType: "text",
                        data: { cid: cOpts.id, hashtag: root.admui.base.getHash() },
                        error: root.admui.event.submitOnError,
                        success: root.admui.event.submitOnSuccess
                    });
                } else {
                    root.admui.priv.getPageContent(true);
                    root.admui.base.vars.last_search = '';
                }

            }, 500);

        }

        /* On Scrool Events
        =================================*/
        root.admui.event.onScroll = function () {

            var direction = 'down';
            if (root.admui.base.vars.last_scrool > $(root.admui.base.vars.scroller).scrollTop())
                direction = 'up';

            root.admui.base.vars.last_scrool = $(root.admui.base.vars.scroller).scrollTop();

            var $element = $(root.admui.base.vars.side_bar);
            if (!$element.attr('data-top'))
                $element.attr('data-top', $element.offset().top);

            if ($(root.admui.base.vars.scroller).scrollTop() == 0) {
                $element.css('position', 'static');
            } else {

                var top = parseInt($element.attr('data-top')) + root.admui.base.vars.contentTop;

                var elemTop = $element.offset().top;
                var elemBottom = elemTop + $element.height();

                if (direction == 'up') {

                } else {
                    if (elemBottom > root.admui.base.vars.contentH) {

                    } else {
                        //$element.css('position', 'absolute');
                        //$element.css('top', (top + 11) + 'px');
                    }
                }
            }
            root.admui.base.vars.check_lock = false;
        };


        /* On Scrool Events
        =================================*/
        root.admui.event.endOfScroll = function () {
            //$('#content-loader').html('Loading');
            //Carrega o restante do conteúdo da página

            //Raise event
            $( document ).trigger( 'end_of_scroll' );
        };


        /* On windows resizes
        =================================*/
        root.admui.event.onWindowResize = function () {
            root.admui.base.vars.windowWidth = $(window).width();
            root.admui.base.vars.windowHeight = $(window).height();
            root.admui.base.vars.contentTop = 0;

            //Content
            root.admui.base.vars.contentH = root.admui.base.vars.windowHeight;
            if (($(root.admui.base.vars.header).length > 0) && ($(root.admui.base.vars.header).is(':visible'))) {
                root.admui.base.vars.contentH -= $(root.admui.base.vars.header).outerHeight();
                root.admui.base.vars.contentTop += $(root.admui.base.vars.header).outerHeight();
            }

            if (($(root.admui.base.vars.title_bar).length > 0) && ($(root.admui.base.vars.title_bar).is(':visible'))) {
                root.admui.base.vars.contentH -= $(root.admui.base.vars.title_bar).outerHeight();
                root.admui.base.vars.contentTop += $(root.admui.base.vars.title_bar).outerHeight();
            }

            if (($(root.admui.base.vars.footer).length > 0) && ($(root.admui.base.vars.footer).is(':visible')))
                root.admui.base.vars.contentH -= $(root.admui.base.vars.footer).outerHeight();

            $(root.admui.base.vars.scroller).height(root.admui.base.vars.contentH);

            //List menu icons
            if ($('#content .home').length > 0) {
                var totalW = 0;
                var calcW = '100%';
                var eW = 0;
                var qtd = 0;

                $("#content .home li").each(function (index, element) {
                    eW = $(element).outerWidth();
                    qtd++;
                });

                if (eW > 0) {
                    var c = Math.floor(root.admui.base.vars.windowWidth / eW);
                    if (qtd < c)
                        c = qtd;

                    if ((c * eW) < root.admui.base.vars.windowWidth)
                        calcW = (c * eW) + 'px';
                }

                $('#content .home').css('width', calcW);

            }

            $( document ).trigger( 'iam_resize' );

        };

        root.admui.event.submitOnError = function (xhr, textStatus, errorThrown) {
            root.admui.priv.unlockScreen();

            if (root.admui.base.vars.unloaded)
                return;

            try {
                root.admui.priv.showMessage("Teste", "Erro ao realizar a operação: " + errorThrown, { className: "error", timer: 3000 });
            } catch (e) {
                alert("Erro ao realizar a operação: " + errorThrown);
            }

        };


        root.admui.event.submitOnSuccess = function (t) {
            root.admui.priv.unlockScreen();
            root.admui.base.doJSON(t);
        };

        /* 
        ##### End of root.is.event functions
        ######################################
        */


        /* 
        ######################################
        ##### root.is.private default variables and options
        */

        /* Default Options
        ----------------------------*/
        root.admui.priv.defaultOptions = {
            scroller: document,
            end_scroll_trigger: '',
            side_bar: '',
            header: 'header',
            title_bar: '',
            search_box: '',
            mobile_button_bar: '',
            btn_box: '',
            check_lock: false
        };

        /* Default Variables
        ----------------------------*/
        root.admui.priv.defaultVars = {
            timers: [],
            check_binded: false
        };

        /* Default static Variables
        ----------------------------*/
        root.admui.priv.defaultStaticVars = {
            last_scrool: 0,
            unloaded:false
        };


        /* 
        ######################################
        ##### Start all process
        */
        root.admui.priv.init();

    }


})(jQuery);

