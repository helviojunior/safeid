/*
* 
* FlowChart 1.0 - Client-side flow chart builder!
* Version 1.0.4b
* @requires jQuery http://jquery.com/‎
* 
* Copyright (c) 2014 Helvio Junior
* Examples and docs at: http://www.helviojunior.com.br
* 
*/
/**
* 
* @description Create a flowchart
* 
* @example $('#chart').flowchart();
* @desc Create a simple flowchart.
* 
* @example $('#chart').flowchart({ load_uri:'http://site.com.br/data.json' });
* @desc Create a flowchart with data source uri.
* 
* @example $('#chart').flowchart({
*       load_uri: 'http://site.com.br/data.json',
*       zoom: true,
*       error: function (errorText) { console.log(errorText); },
*       success: function (ctx) { console.log('OK'); },
*       nodeClick: function (node) { console.log(node) },
*       nodeMouseOver: function (node) { console.log(node) },
*       nodeMouseOut: function (node) { console.log(node) },
*       connectionClick: function (conn) { console.log(conn) },
*       connectionMouseOver: function (conn) { console.log(conn) },
*       connectionMouseOut: function (conn) { console.log(conn) }
*   });
* @desc Create a flowchart with all options.
* 
* @param Object
*            settings An object literal containing key/value pairs to provide
*            optional settings.
* 
* 
* @option String load_uri (optional) A uri of the json data source
*         Data return sample: 
*                   {
*                       "nodes": [
*                   		{
*                   			"nodeID": 0,
*                   			"name": "Node 1 name",
*                   			"column": 0,
*                   			"value": 1,
*                   			"cssClass": "class-to-add"
*                   		},
*                   		{
*                   			"nodeID": 1,
*                   			"name": "Node2",
*                   			"column": 1,
*                   			"value": 5
*                   		}
*                   	],
*                       "connections": [
*                   		{
*                               "source": {
*                                   "nodeID": 0
*                               },
*                               "dest": {
*                                   "nodeID": 1
*                               },
*                   			"title": "Text to show on mouse hover"
*                           }
*                       ]
*                   }
*
* @option String chart_data (optional) A chart data to be used
*         If this option is provided a option load_uri is ignored.
*         The data struct need be same of data return sample above
* 
* @option Boolean zoom (optional) Default value = true 
*         A boolean value to control zoom component
*         If this option is true the zoom control to be activated
* 
* @option Function error (optional) A function that will be raised in 'error' 
*         of build chart or load jSon data
* 
* @option Function success (optional) A function that will be raised in 'success' 
*         chart build
* 
* @option Function nodeClick (optional) A function that will be raised in 'click'
*         event od the node element
* 
* @option Function nodeMouseOver (optional) A function that will be raised in 'moudeOver'
*         event od the node element
* 
* @option Function nodeMouseOut (optional) A function that will be raised in 'mouseOut'
*         event od the node element
* 
* @option Function connectionClick (optional) A function that will be raised in 'click'
*         event od the connection element
* 
* @option Function connectionMouseOver (optional) A function that will be raised in 'moudeOver'
*         event od the connection element
* 
* @option Function connectionMouseOut (optional) A function that will be raised in 'mouseOut'
*         event od the connection element
* 
* 
* @type jQuery
* 
* @name flowchart
* 
* @cat Plugins/flowchart
* 
* @author Helvio Junior/helvio_junior@hotmail.com
*/

(function ($) {
    $.extend({
        flowchart: new 
        function () {

            /* Public Variables
            =================================*/
            var base = this;
            base.fc = {};
            base.fc.vars = {};
            base.fc.fn = {};

            /* Carrega os dados do gráfico através de AJAX 
            =================================*/
            base.fc.fn.loadData = function (ctx) {

                if ((typeof (ctx.priv.settings.chart_data) == "object") && (ctx.priv.settings.chart_data != undefined) && (ctx.priv.settings.chart_data != null)) {
                    ctx.priv.chart_data = ctx.priv.settings.chart_data;
                    base.fc.fn.buildChart(ctx);
                } else {
                    $.ajax({
                        type: "GET",
                        url: ctx.priv.settings.load_uri,
                        dataType: "json",
                        data: {},
                        crossDomain: true,
                        error: function (xhr, textStatus, errorThrown) {
                            if (base.fc.vars.unloaded)
                                return;

                            base.fc.fn.log("Erro ao carregar os dados");
                            ctx.trigger('error', 'Error loading data: ' + textStatus);
                        },
                        success: function (jData) {
                            ctx.priv.chart_data = jData;
                            base.fc.fn.buildChart(ctx);
                        }
                    });
                }

            };


            /* Build Chart
            =================================*/
            base.fc.fn.buildChart = function (ctx) {
                if ((ctx.priv.chart_data == undefined) || (ctx.priv.chart_data.nodes == undefined) || (ctx.priv.chart_data.connections == undefined)) {
                    base.fc.fn.log('Invalid data struct');
                    ctx.trigger('error', 'Invalid data struct');
                    return;
                }

                try {

                    var columns = [];
                    var maxValue = 0;
                    $.each(ctx.priv.chart_data.nodes, function (key, node) {
                        if ($.inArray(node.column, columns) == -1)
                            columns.push(node.column);

                        if (node.value > maxValue)
                            maxValue = node.value;
                    });

                    columns.sort();

                    var valueFactor = 233 / maxValue;

                    if (valueFactor > 2)
                        valueFactor = 2;

                    var maxWidth = 0;
                    var startX = ctx.priv.vars.padding;

                    if (ctx.priv.settings.zoom)
                        startX += ctx.priv.vars.padding + ctx.priv.vars.zoom_width;

                    //Calcula posicionamento (indice) horizontal e tamanho dos itens
                    var columIndex = 0;
                    var columnsItems = [];
                    $.each(columns, function (cIndex, c) {

                        columnsItems[columIndex] = [];

                        var x = startX + (columIndex * (ctx.priv.vars.item_width + ctx.priv.vars.h_space));
                        var y = ctx.priv.vars.padding;

                        $.each(ctx.priv.chart_data.nodes, function (key, node) {
                            if (node.column == c) {
                                ctx.priv.chart_data.nodes[key] = $.extend({}, node, { type: 'node', path: '', chields: [], parent: null, include: true, columIndex: columIndex, basePadding: ctx.priv.vars.v_space, rowIndex: -1, x: x, y: 0, width: ctx.priv.vars.item_width, height: 7 + (node.value > 0 ? (node.value * valueFactor) : 0) });
                                columnsItems[columIndex].push(ctx.priv.chart_data.nodes[key]);

                                if ((x + ctx.priv.chart_data.nodes[key].width + ctx.priv.vars.h_space) > maxWidth)
                                    maxWidth = x + ctx.priv.chart_data.nodes[key].width + ctx.priv.vars.h_space;
                            }
                        });

                        columIndex++;
                    });

                    //Remove conexões duplicadas, caso haja

                    var connections = []
                    $.each(ctx.priv.chart_data.connections, function (key, conn) {
                        var exists = false;
                        $.each(connections, function (i, c) {
                            if ((conn.source.nodeID == c.source.nodeID) && (conn.dest.nodeID == c.dest.nodeID))
                                exists = true;
                        });

                        if (!exists)
                            connections.push(conn);
                    });
                    ctx.priv.chart_data.connections = connections;

                    //Calcula os filhos
                    $.each(ctx.priv.chart_data.connections, function (key, conn) {

                        var source = null;
                        var destination = null;

                        $.each(ctx.priv.chart_data.nodes, function (k1, node) {
                            if (conn.source.nodeID == node.nodeID)
                                source = node;

                            if (conn.dest.nodeID == node.nodeID)
                                destination = node;
                        });

                        if ((source != null) && (destination != null)) {


                            source.chields.push(destination);
                            if (destination.parent == null)
                                destination.parent = source;

                            ctx.priv.chart_data.connections[key] = $.extend({}, conn, {
                                connID: source.nodeID + '-' + destination.nodeID,
                                steps: []
                            });

                        }

                    });

                    //Calcula padding para colunas com itens em branco, 
                    //faz coluna a coluna para poder inserir os itens intermediarios
                    $.each(columnsItems, function (columIndex, itemsArray) {
                        $.each(itemsArray, function (key, node) {
                            //ctx.priv.chart_data.nodes[key] = $.extend({}, node, { type: 'node', chields: [], parent: null, columIndex: columIndex, rowIndex: -1, x: x, y: 0, width: ctx.priv.vars.item_width, height: 7 + (node.value > 0 ? (node.value * valueFactor) : 0) });

                            $.each(node.chields, function (i, chield) {
                                if (chield.columIndex > (node.columIndex + 1)) {
                                    var newItem = { type: 'empty-node', node: (chield.type == 'node' ? chield : chield.node), connID: (chield.type == 'node' && node.type == 'node' ? node.nodeID + '-' + chield.nodeID : node.connID), nodeID: ctx.priv.chart_data.nodes.length, basePadding: ctx.priv.vars.v_space, chields: [], parent: node, columIndex: (node.columIndex + 1), rowIndex: -1, width: chield.width, height: chield.height, name: chield.name, x: ctx.priv.vars.padding + ((node.columIndex + 1) * (ctx.priv.vars.item_width + ctx.priv.vars.h_space)), include: true };

                                    $.each(ctx.priv.chart_data.connections, function (key, conn) {
                                        if (conn.connID == newItem.connID) {
                                            conn.steps.push(newItem);
                                        }
                                    });

                                    newItem.chields.push(chield);

                                    //Substitui o objeto atual no arrar de filhos
                                    node.chields[i] = newItem;

                                    //Adiciona no array principal
                                    ctx.priv.chart_data.nodes.push(newItem);

                                    //Adiciona na listagem dessa coluna
                                    columnsItems[(node.columIndex + 1)].push(newItem);

                                }
                            });

                            var maxH = base.fc.fn.maxChieldHeight(node);

                            if (node.height < maxH)
                                node.basePadding = maxH - node.height;

                        });
                    });

                    //Realiza ordenação, coluna a coluna
                    $.each(columnsItems, function (cIndex, itemsArray) {

                        //Reconstroi o path (string) para a ordenação
                        $.each(itemsArray, function (key, node) {
                            if ((node.parent == null) || (node.parent.path == null))
                                node.path = (node.name).toLowerCase();
                            else
                                node.path = (node.parent.path + '-' + node.name).toLowerCase();
                        });

                        /*
                        itemsArray.sort(function (a, b) {
                        return ((a.path < b.path) ? -1 : ((a.path > b.path) ? 1 : 0));
                        });*/

                        //Atualiza os indices nos objetos

                        $.each(itemsArray, function (i, node) {
                            node.rowIndex = i;
                        });

                    });

                    //Corrige a ordenação dos itens de preenchimento

                    $.each(ctx.priv.chart_data.nodes, function (key, node) {
                        if ((node.type == 'empty-node') && (node.rowIndex != node.node.rowIndex)) {
                            $.each(columnsItems[node.columIndex], function (i, row) {
                                if (row.rowIndex > node.rowIndex)
                                    row.rowIndex--;

                                if (row.rowIndex > node.node.rowIndex)
                                    row.rowIndex++;
                            });

                            base.fc.fn.log(node.rowIndex + ' => ' + node.node.rowIndex);
                            node.rowIndex = node.node.rowIndex;
                        }
                    });
                    $.each(columnsItems, function (cIndex, itemsArray) {
                        itemsArray.sort(function (a, b) {
                            return ((a.rowIndex < b.rowIndex) ? -1 : ((a.rowIndex > b.rowIndex) ? 1 : 0));
                        });
                    });

                    //base.fc.fn.log(columnsItems);

                    //Calcula o posicionamento (indice) vertical
                    /*
                    $.each(columnsItems, function (columIndex, itemsArray) {
                    var y = ctx.priv.vars.padding;

                    $.each(itemsArray, function (key, node) {
                    //if (node.parent != null)
                    //    y += node.parent.y;

                    itemsArray[key].y = y;
                    y += node.height + node.basePadding;

                    if (y > maxHeight)
                    maxHeight = y;
                    });

                    });*/
                    $.each(columnsItems, function (columIndex, itemsArray) {
                        var y = ctx.priv.vars.padding;

                        $.each(itemsArray, function (key, node) {

                            var minY = 0;

                            if (node.parent != null)
                                minY = node.parent.y;

                            $.each(ctx.priv.chart_data.nodes, function (i, fn) {
                                $.each(fn.chields, function (i2, chield) {
                                    if ((chield.nodeID == node.nodeID) && (minY > fn.y))
                                        minY = fn.y;
                                });
                            });

                            if (y < minY)
                                y = minY;

                            itemsArray[key].y = y;
                            y += node.height + node.basePadding;

                            if (y > maxHeight)
                                maxHeight = y;
                        });

                    });

                    var maxHeight = 0;
                    $.each(ctx.priv.chart_data.nodes, function (i, node) {
                        var h = node.y + node.height + ctx.priv.vars.v_space;
                        if (maxHeight < h)
                            maxHeight = h;
                    });

                    maxWidth += ctx.priv.vars.padding;
                    maxHeight += ctx.priv.vars.padding;

                    var srcConn = [];
                    var dstConn = [];

                    //Calcula os links
                    $.each(ctx.priv.chart_data.connections, function (key, conn) {

                        var source = null;
                        var destination = null;

                        $.each(ctx.priv.chart_data.nodes, function (k1, node) {
                            if (conn.source.nodeID == node.nodeID)
                                source = node;

                            if (conn.dest.nodeID == node.nodeID)
                                destination = node;
                        });

                        if ((source != null) && (destination != null)) {
                            ctx.priv.chart_data.connections[key] = $.extend({}, conn, {
                                sourceColumIndex: source.columIndex,
                                sourceRowIndex: source.rowIndex,
                                destinationColumIndex: destination.columIndex,
                                destinationRowIndex: destination.rowIndex,
                                sourceHeight: source.height,
                                destinationHeight: destination.height,
                                p1: { x: source.x + source.width, y: source.y },
                                p2: { x: destination.x, y: destination.y },
                                p3: { x: destination.x, y: destination.y + destination.height },
                                p4: { x: source.x + source.width, y: source.y + source.height }
                            });

                            $.each(ctx.priv.chart_data.connections[key].steps, function (i, s) {

                                ctx.priv.chart_data.connections[key].steps[i] = $.extend({}, s, {
                                    columIndex: s.columIndex,
                                    p1: { x: s.x, y: s.y },
                                    p2: { x: s.x + s.width, y: s.y },
                                    p3: { x: s.x + s.width, y: s.y + s.height },
                                    p4: { x: s.x, y: s.y + s.height }
                                });

                            });

                            //Ordena os passos por coluna
                            ctx.priv.chart_data.connections[key].steps.sort(function (a, b) {
                                return ((a.columIndex < b.columIndex) ? -1 : ((a.columIndex > b.columIndex) ? 1 : 0));
                            });

                            var srcFindedIndex = -1;
                            $.each(srcConn, function (index, item) {
                                if (item.position == source.columIndex + '-' + source.rowIndex)
                                    srcFindedIndex = index;
                            });

                            var dstFindedIndex = -1;
                            $.each(dstConn, function (index, item) {
                                if (item.position == destination.columIndex + '-' + destination.rowIndex)
                                    dstFindedIndex = index;
                            });


                            if (srcFindedIndex == -1) {
                                var newItem = { position: source.columIndex + '-' + source.rowIndex, values: [] };
                                newItem.values.push(ctx.priv.chart_data.connections[key]);
                                srcConn.push(newItem);
                            } else {
                                srcConn[srcFindedIndex].values.push(ctx.priv.chart_data.connections[key]);
                            }

                            if (dstFindedIndex == -1) {
                                var newItem = { position: destination.columIndex + '-' + destination.rowIndex, values: [] };
                                newItem.values.push(ctx.priv.chart_data.connections[key]);
                                dstConn.push(newItem);
                            } else {
                                dstConn[dstFindedIndex].values.push(ctx.priv.chart_data.connections[key]);
                            }

                        }

                    });

                    //Calcula colisão nos links
                    //Calculate sources
                    $.each(srcConn, function (index, item) {
                        if (item.values.length > 1) {
                            var y = 0;
                            var totalHeight = 0;
                            $.each(item.values, function (i, value) {
                                totalHeight += value.destinationHeight;
                            });


                            item.values.sort(function (a, b) {
                                return ((a.destinationRowIndex < b.destinationRowIndex) ? -1 : ((a.destinationRowIndex > b.destinationRowIndex) ? 1 : 0));
                            });

                            $.each(item.values, function (i, value) {
                                var p = value.destinationHeight / totalHeight;
                                var h = ((value.p4.y - value.p1.y) * p)
                                value.p1.y = value.p1.y + y;
                                value.p4.y = value.p1.y + h;
                                y += h;
                            });
                        }
                    });

                    //Calculate destinations
                    $.each(dstConn, function (index, item) {
                        if (item.values.length > 1) {
                            var y = 0;
                            var totalHeight = 0;
                            $.each(item.values, function (i, value) {
                                totalHeight += value.sourceHeight;
                            });

                            item.values.sort(function (a, b) {
                                return ((a.sourceRowIndex < b.sourceRowIndex) ? -1 : ((a.sourceRowIndex > b.sourceRowIndex) ? 1 : 0));
                            });

                            $.each(item.values, function (i, value) {
                                var p = value.sourceHeight / totalHeight;
                                var h = ((value.p3.y - value.p2.y) * p)
                                value.p2.y = value.p2.y + y;
                                value.p3.y = value.p2.y + h;
                                y += h;

                            });
                        }
                    });

                    //Cria o gráfico
                    var svg = $(document.createElementNS('http://www.w3.org/2000/svg', 'svg'))
                    .attr("width", maxWidth)
                    .attr("height", maxHeight);

                    var gPath = $(document.createElementNS('http://www.w3.org/2000/svg', 'g'))
                    .attr("transform", "translate(0,0)");


                    var gRect = $(document.createElementNS('http://www.w3.org/2000/svg', 'g'))
                    .attr("transform", "translate(0,0)");

                    ctx.html('');

                    if (ctx.priv.settings.zoom) {
                        ctx.append('<div class=\"fcZoom\"></div>');
                        $(".fcZoom", ctx)
                        .css('height', ($(".fcZoom", ctx).parent().innerHeight() * 0.8))
                        .fcZoom()
                        .bind('changed', function (e, zoom) {
                            $('.fcContent .scrollContainer', ctx).css('zoom', zoom);
                        });
                    }

                    ctx.append('<div class=\"fcContent\"><div class="scrollContainer" style="width:' + maxWidth + 'px; height:' + maxHeight + 'px;"><div class="svgGraph" style="width:' + maxWidth + 'px; height:' + maxHeight + 'px;"></div></div></div>');

                    $(".fcContent", ctx).fcDragScroll();

                    var svgGraph = $('.fcContent .scrollContainer .svgGraph', ctx);
                    svgGraph.html(svg);

                    $(svg).append(gPath);
                    $(svg).append(gRect);

                    $.each(ctx.priv.chart_data.connections, function (key, conn) {
                        var path = base.fc.fn.builPath(conn.p1, conn.p2, conn.p3, conn.p4, conn.title, conn.steps);
                        path.on('click', function () {
                            $(this).trigger('connectionClick', conn);
                        }).on('mouseover', function () {
                            $(this).trigger('connectionMouseOver', conn);
                        }).on('mouseout', function () {
                            $(this).trigger('connectionMouseOut', conn);
                        });

                        gPath.append(path);
                    });

                    $.each(ctx.priv.chart_data.nodes, function (key, node) {

                        if (node.type == 'node') {

                            var oNode = $('<div class="dNode ' + (node.cssClass != undefined ? node.cssClass : '') + '" style="position:absolute; left:' + node.x + 'px; top:' + node.y + 'px; width:' + node.width + 'px; height:' + node.height + 'px;"><div class="text" style="position:absolute; left:10px; top:10px;">' + node.name + '</div></div>');
                            //var oNode = base.fc.fn.buildRect(node.x, node.y, node.width, node.height, node.name, (node.cssClass != undefined ? node.cssClass : ''));

                            oNode.on('click', function () {
                                $(this).trigger('nodeClick', node);
                            }).on('mouseover', function () {
                                $(this).trigger('nodeMouseOver', node);
                            }).on('mouseout', function () {
                                $(this).trigger('nodeMouseOut', node);
                            });

                            //gRect.append(oNode);
                            svgGraph.append(oNode);
                        } else if (ctx.priv.debug) {


                            var oNode = $('<div class="dNode ' + (node.cssClass != undefined ? node.cssClass : '') + '" style="position:absolute; left:' + node.x + 'px; top:' + node.y + 'px; width:' + node.width + 'px; height:' + node.height + 'px;"><div class="text" style="position:absolute; left:10px; top:10px;">' + node.rowIndex + ' ' + (node.node != undefined ? node.node.name : '') + '</div></div>');
                            //var oNode = base.fc.fn.buildRect(node.x, node.y, node.width, node.height, node.name, (node.cssClass != undefined ? node.cssClass : ''));

                            //gRect.append(oNode);
                            svgGraph.append(oNode);
                        }
                    });


                    $(".fcZoom", ctx)
                    .css('height', ($(".fcZoom", ctx).parent().innerHeight() * 0.8))
                    .fcZoom()
                    .bind('changed', function (e, zoom) {
                        $('.fcContent .scrollContainer', ctx).css('zoom', zoom);
                    });

                    ctx.trigger('success', ctx);
                } catch (ex) {
                    ctx.trigger('error', 'Error on chart build: ' + ex.Message);
                }
            };


            /* Build Link path
            =================================*/
            base.fc.fn.maxChieldHeight = function (node) {


                var maxHeight = 0;

                if (node.include) {
                    maxHeight = node.height + node.basePadding;

                    if (node.chields.length > 0) {
                        var colTotalHeight = [];
                        $.each(node.chields, function (i, chield) {

                            if (!$.isNumeric(colTotalHeight[chield.columIndex]))
                                colTotalHeight[chield.columIndex] = 0;

                            if ((chield.parent != null) && (chield.parent.nodeID == node.nodeID))
                                colTotalHeight[chield.columIndex] += base.fc.fn.maxChieldHeight(chield);


                        });

                        colTotalHeight.forEach(function (c) { if (maxHeight < c) { maxHeight = c; } });
                        //node.include = false;
                    }

                }

                return maxHeight;

            };

            /* Build Link path
            =================================*/
            base.fc.fn.builPath = function (p1, p2, p3, p4, title, steps) {


                var path = 'M' + p1.x + ' ' + p1.y;

                var mid = p1.x + ((p2.x - p1.x) / 2);
                path += ' L ' + (p1.x + 16) + ' ' + p1.y;
                path += ' C ' + mid + ' ' + p1.y + ' ' + mid + ' ' + p2.y + ' ' + (p2.x - 16) + ' ' + p2.y;
                path += ' L ' + p2.x + ' ' + p2.y;

                path += ' L ' + p3.x + ' ' + p3.y + ' ' + (p3.x - 16) + ' ' + p3.y;
                path += ' C ' + mid + ' ' + p3.y + ' ' + mid + ' ' + p4.y + ' ' + (p4.x + 16) + ' ' + p4.y;
                path += ' L ' + p4.x + ' ' + p4.y;

                path += ' Z';

                //Refaz no novo modelo
                path = 'M' + p1.x + ' ' + p1.y;

                var path1 = '';
                var path2 = '';

                var lastPoint1 = p1;
                var lastPoint2 = p3;

                $.each(steps, function (i, step) {

                    nextPoint1 = step.p1;

                    var mid = lastPoint1.x + ((nextPoint1.x - lastPoint1.x) / 2);
                    path1 += ' L ' + (lastPoint1.x + 16) + ' ' + lastPoint1.y;
                    path1 += ' C ' + mid + ' ' + lastPoint1.y + ' ' + mid + ' ' + nextPoint1.y + ' ' + (nextPoint1.x - 16) + ' ' + nextPoint1.y;
                    path1 += ' L ' + nextPoint1.x + ' ' + nextPoint1.y;
                    nextPoint1 = step.p2;
                    path1 += ' L ' + nextPoint1.x + ' ' + nextPoint1.y;

                    lastPoint1 = nextPoint1;
                });


                $.each(steps.reverse(), function (i, step) {
                    nextPoint2 = step.p3;

                    var mid = lastPoint2.x + ((nextPoint2.x - lastPoint2.x) / 2);

                    path2 += ' L ' + lastPoint2.x + ' ' + lastPoint2.y + ' ' + (lastPoint2.x - 16) + ' ' + lastPoint2.y;
                    path2 += ' C ' + mid + ' ' + lastPoint2.y + ' ' + mid + ' ' + nextPoint2.y + ' ' + (nextPoint2.x + 16) + ' ' + nextPoint2.y;
                    path2 += ' L ' + nextPoint2.x + ' ' + nextPoint2.y;
                    nextPoint2 = step.p4;
                    path2 += ' L ' + nextPoint2.x + ' ' + nextPoint2.y;

                    lastPoint2 = nextPoint2;
                });

                var nextPoint1 = p2;
                var nextPoint2 = p4;

                var mid = lastPoint1.x + ((nextPoint1.x - lastPoint1.x) / 2);
                path1 += ' L ' + (lastPoint1.x + 16) + ' ' + lastPoint1.y;
                path1 += ' C ' + mid + ' ' + lastPoint1.y + ' ' + mid + ' ' + nextPoint1.y + ' ' + (nextPoint1.x - 16) + ' ' + nextPoint1.y;
                path1 += ' L ' + nextPoint1.x + ' ' + nextPoint1.y;

                var mid = lastPoint2.x + ((nextPoint2.x - lastPoint2.x) / 2);
                path2 += ' L ' + lastPoint2.x + ' ' + lastPoint2.y + ' ' + (lastPoint2.x - 16) + ' ' + lastPoint2.y;
                path2 += ' C ' + mid + ' ' + lastPoint2.y + ' ' + mid + ' ' + nextPoint2.y + ' ' + (nextPoint2.x + 16) + ' ' + nextPoint2.y;
                path2 += ' L ' + nextPoint2.x + ' ' + nextPoint2.y;

                //path2 = ' L 500 500'

                path += path1 + path2 + ' Z';


                var p = $(document.createElementNS('http://www.w3.org/2000/svg', 'path'))
                    .attr('class', 'link')
                    .attr('d', path);

                var t = $(document.createElementNS('http://www.w3.org/2000/svg', 'title'))
                    .append(title);

                p.append(t);

                return p;

            };

            /* Build Item Rect
            =================================*/
            base.fc.fn.buildRect = function (x, y, width, height, title, cssClass) {
                var g = $(document.createElementNS('http://www.w3.org/2000/svg', 'g'))
                    .attr('class', 'node')
                    .attr('transform', 'translate(' + x + ',' + y + ')');

                var r = $(document.createElementNS('http://www.w3.org/2000/svg', 'rect'))
                    .attr('height', height)
                    .attr('width', width)
                    .attr('class', cssClass)
                    .attr('style', 'fill: #1f77b4; stroke: transparent;');

                var t = $(document.createElementNS('http://www.w3.org/2000/svg', 'title'))
                    .append(title);

                var t1 = $(document.createElementNS('http://www.w3.org/2000/svg', 'text'))
                    .attr('x', 10)
                    .attr('y', 20)
                    .attr('dy', '.35em')
                    .attr('text-anchor', 'start')
                    .append(text);

                r.append(t);
                g.append(r);
                g.append(t1);

                return g;

            };

            /* Start triggers 
            =================================*/
            base.fc.fn.startTriggers = function (ctx) {

                $(window).bind('beforeunload', function () {
                    base.fc.vars.unloaded = true;
                });

                if (typeof ($ctx.priv.settings.connectionMouseOver) == 'function') {
                    ctx.unbind("connectionMouseOver").bind("connectionMouseOver", function (e, connection) {
                        $ctx.priv.settings.connectionMouseOver(connection);
                    });
                }

                if (typeof ($ctx.priv.settings.connectionMouseOut) == 'function') {
                    ctx.unbind("connectionMouseOut").bind("connectionMouseOut", function (e, connection) {
                        $ctx.priv.settings.connectionMouseOut(connection);
                    });
                }

                if (typeof ($ctx.priv.settings.connectionClick) == 'function') {
                    ctx.unbind("connectionClick").bind("connectionClick", function (e, connection) {
                        $ctx.priv.settings.connectionClick(connection);
                    });
                }

                if (typeof ($ctx.priv.settings.nodeMouseOver) == 'function') {
                    ctx.unbind("nodeMouseOver").bind("nodeMouseOver", function (e, node) {
                        $ctx.priv.settings.nodeMouseOver(node);
                    });
                }

                if (typeof ($ctx.priv.settings.nodeMouseOut) == 'function') {
                    ctx.unbind("nodeMouseOut").bind("nodeMouseOut", function (e, node) {
                        $ctx.priv.settings.nodeMouseOut(node);
                    });
                }

                if (typeof ($ctx.priv.settings.nodeClick) == 'function') {
                    ctx.unbind("nodeClick").bind("nodeClick", function (e, node) {
                        $ctx.priv.settings.nodeClick(node);
                    });
                }

                if (typeof ($ctx.priv.settings.error) == 'function') {
                    ctx.unbind("error").bind("error", function (e, description) {
                        $ctx.priv.settings.error(description);
                    });
                }

                if (typeof ($ctx.priv.settings.success) == 'function') {
                    ctx.unbind("success").bind("success", function (e) {
                        $ctx.priv.settings.success();
                    });
                }

            };


            /* Construtor
            =================================*/
            this.construct = function (settings) {
                return this.each(function () {
                    $ctx = $(this);

                    /* Local variables
                    =================================*/
                    $ctx.priv = {};

                    $ctx.priv.settings = {};
                    $ctx.priv.vars = {
                        width: 0,
                        height: 0,
                        v_space: 45,
                        h_space: 96,
                        item_width: 126,
                        padding: 10,
                        zoom_width: 59
                    };

                    $ctx.priv.settings = $.extend({}, base.fc.defaults, settings);

                    base.fc.fn.startTriggers($ctx);
                    base.fc.fn.loadData($ctx);
                });
            };


            /* Logs de debug
            =================================*/
            base.fc.fn.debugLog = function (s) {
                if (!root.fc.priv.vars.debug)
                    return;

                if (typeof console != "undefined" && typeof console.debug != "undefined") {
                    console.log(s);
                } else {
                    alert(s);
                }
            };

            /* Logs do sistema
            =================================*/
            base.fc.fn.log = function (s) {
                if (typeof console != "undefined" && typeof console.debug != "undefined") {
                    console.log(s);
                } else {
                    alert(s);
                }
            };


            /* Variaveis e atributos iniciais
            =================================*/
            base.fc.vars = {
                unloaded: false,
                debug: false
            };

            base.fc.defaults = {
                load_uri: 'data.json',
                zoom: true,
                chart_data: null,
                nodeClick: null,
                nodeMouseOver: null,
                nodeMouseOut: null,
                connectionClick: null,
                connectionMouseOver: null,
                connectionMouseOut: null
            };
        }
    });

    /* Extend plugin scope
    =================================*/
    $.fn.extend({
        flowchart: $.flowchart.construct
    });


    /* Extension to Zoom
    =================================*/
    $.fn.fcZoom = function () {
        return this.each(function () {
            var $this = $(this);

            var y, top, mouseDown, minY, maxY, mid, percent;

            $($this).attr("onselectstart", "return false;");   // Disable text selection in IE8

            $($this).html('<div class="zoomController"><div class="bg bar"></div><div class="bg plus"></div><div class="bg minus"></div><div class="bg slider"></div></div>');

            $this.calc = function () {
                minY = $('.plus', $this).offset().top + $('.plus', $this).outerHeight() - 4;
                maxY = $('.minus', $this).offset().top - $('.slider', $this).outerHeight() + 2;
                mid = minY + ((maxY - minY) / 2)
            }

            $this.moveTo = function (p) {
                if (p < 0.00000001)
                    p = 0.00000001;

                if (p > 2)
                    p = 2;

                percent = p;

                $this.calc();
                $('.slider', $this).offset({ top: maxY - ((mid - minY) * p) });
                $($this).trigger('changed', percent);
            }

            $this.moveTo(1);

            $('.minus', $this).click(function () {
                percent -= 0.1;
                $this.moveTo(percent);
            });

            $('.plus', $this).click(function () {
                percent += 0.1;
                $this.moveTo(percent);
            });

            $('.slider', $this).mousedown(function (e) {
                e.preventDefault();
                mouseDown = true;
                y = e.pageY;
                top = $(this).offset().top;
            });
            $(document).mousemove(function (e) {
                if (mouseDown) {
                    var newY = e.pageY;

                    var t = top + newY - y;
                    if (t < minY)
                        t = minY;
                    else if (t > maxY)
                        t = maxY;

                    $this.calc();
                    percent = 0.00000001 + (-(maxY - t) / (mid - maxY));
                    $('.slider', $this).offset({ top: t });
                    $($this).trigger('changed', percent);
                }
            });

            $(document).mouseup(function (e) {
                mouseDown = false;
                if (mouseDown) {
                    $this.calc();
                    percent = 0.00000001 + (-(maxY - $('.slider', $this).offset().top) / (mid - maxY));
                    $($this).trigger('changed', percent);
                }
            });
        });
    };

    /* Extension to drag & drop
    =================================*/
    $.fn.fcDragScroll = function (options) {
        return this.each(function () {
            var $this = $(this);
            var x, y, top, left, mouseDown, opt;

            opt = $.extend({}, options);

            $($this).attr("onselectstart", "return false;");   // Disable text selection in IE8

            $($this).mousedown(function (e) {
                e.preventDefault();
                mouseDown = true;
                x = e.pageX;
                y = e.pageY;
                top = $(this).scrollTop();
                left = $(this).scrollLeft();
            });
            $($this).mouseleave(function (e) {
                mouseDown = false;
            });
            $(document).mousemove(function (e) {
                if (mouseDown) {
                    var newX = e.pageX;
                    var newY = e.pageY;

                    if (!opt.lockY)
                        $($this).scrollTop(top - newY + y)

                    if (!opt.lockX)
                        $($this).scrollLeft(left - newX + x);
                }
            });
            $(document).mouseup(function (e) { mouseDown = false; });
        });
    };


})(jQuery);
