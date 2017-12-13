/*
* 
* FileDragDrop 1.0 - Drag & Drop file to a hidden field!
* Version 1.0.1b
* @requires jQuery http://jquery.com/‎
* 
* Copyright (c) 2014 Helvio Junior
* Examples and docs at: http://www.helviojunior.com.br
* 
*/


(function ($) {
    /* Extension to drag & drop file to Text field
    =================================*/
    $.fn.fileDragDrop = function (options) {
        return this.each(function () {
            var $this = $(this);
            var opt;

            opt = $.extend({}, {
                name: 'file_upload',
                click_text: 'Selecione arquivos para enviar',
                drag_text: 'Arraste o arquivo até aqui',
                lease_text: 'Solte o arquivo',
                size_exceeded: 'Arquivo maior que o permitido',
                pre_load_value: null,
                max_size: 100000
            }, options);

            $this.html('<div class="drag-content">' + opt.click_text + '</div><div class="dragDrop-content"><span class="label l1">' + opt.drag_text + '</span><span class="label l2">' + opt.lease_text + '</span></div><input type="file" name="files[]" /><input type="hidden" name="' + opt.name + '" id="' + opt.name + '" />');

            if (opt.pre_load_value != null)
                $('input[type=hidden]', $this).attr('value', opt.pre_load_value);

            $(document).bind('drop dragover', function (e) {
                e.preventDefault();
            });

            $(document).bind('drop', function (e) {
                e.preventDefault();
                $this.removeClass('drag hover');
            });

            $(document).bind('drag', function (e) {
                e.preventDefault();
                $this.addClass('drag');
            });

            $('.drag-content', $this).bind('click', function (e) {
                e.preventDefault();
                $('input[type=file]', $this).click();
            });

            $(document).bind('dragover', function (e) {
                var dropZone = $this,
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

            $this.fileSelected = function (file) {

                $('.drag-content', $this).removeClass('error ok');
                $('input[type=hidden]', $this).attr('value', '');

                if (file.size > opt.max_size) {
                    $('.drag-content', $this).addClass('error').html(opt.size_exceeded);
                } else {

                    var reader = new FileReader();
                    reader.onload = function (event) {
                        $('.drag-content', $this).addClass('ok').html(file.name);
                        $('input[type=hidden]', $this).attr('value', btoa(event.target.result));
                    };

                    reader.onerror = function (event) {
                        $('.drag-content', $this).addClass('error').html(event.target.error);
                    };

                    reader.readAsBinaryString(file);
                }
            }

            $('input[type=file]', $this).change(function (e) {
                $this._getFileInputFiles(this).always(function (files) {
                    $this.fileSelected(files[0]);
                });
            });


            $this.bind('drop', function (e) {
                e.dataTransfer = e.originalEvent && e.originalEvent.dataTransfer;
                var that = this,
                dataTransfer = e.dataTransfer;
                if (dataTransfer && dataTransfer.files && dataTransfer.files.length) {
                    e.preventDefault();
                    $this._getDroppedFiles(dataTransfer).always(function (files) {
                        $this.fileSelected(files[0]);
                    });
                }
            });


            $this._handleFileTreeEntry = function (entry, path) {
                var that = this,
                dfd = $.Deferred(),
                errorHandler = function (e) {
                    if (e && !e.entry) {
                        e.entry = entry;
                    }
                    // Since $.when returns immediately if one
                    // Deferred is rejected, we use resolve instead.
                    // This allows valid files and invalid items
                    // to be returned together in one set:
                    dfd.resolve([e]);
                },
                dirReader;
                path = path || '';
                if (entry.isFile) {
                    if (entry._file) {
                        // Workaround for Chrome bug #149735
                        entry._file.relativePath = path;
                        dfd.resolve(entry._file);
                    } else {
                        entry.file(function (file) {
                            file.relativePath = path;
                            dfd.resolve(file);
                        }, errorHandler);
                    }
                } else if (entry.isDirectory) {
                    dirReader = entry.createReader();
                    dirReader.readEntries(function (entries) {
                        that._handleFileTreeEntries(
                        entries,
                        path + entry.name + '/'
                    ).done(function (files) {
                        dfd.resolve(files);
                    }).fail(errorHandler);
                    }, errorHandler);
                } else {
                    // Return an empy list for file system items
                    // other than files or directories:
                    dfd.resolve([]);
                }
                return dfd.promise();
            }

            $this._handleFileTreeEntries = function (entries, path) {
                var that = this;
                return $.when.apply(
                    $,
                    $.map(entries, function (entry) {
                        return $this._handleFileTreeEntry(entry, path);
                    })
                ).pipe(function () {
                    return Array.prototype.concat.apply(
                        [],
                        arguments
                    );
                });
            }

            $this._getDroppedFiles = function (dataTransfer) {
                dataTransfer = dataTransfer || {};
                var items = dataTransfer.items;
                if (items && items.length && (items[0].webkitGetAsEntry ||
                    items[0].getAsEntry)) {
                    return $this._handleFileTreeEntries(
                    $.map(items, function (item) {
                        var entry;
                        if (item.webkitGetAsEntry) {
                            entry = item.webkitGetAsEntry();
                            if (entry) {
                                // Workaround for Chrome bug #149735:
                                entry._file = item.getAsFile();
                            }
                            return entry;
                        }
                        return item.getAsEntry();
                    })
                );
                }
                return $.Deferred().resolve(
                        $.makeArray(dataTransfer.files)
                    ).promise();
            }

            $this._getFileInputFiles = function (fileInput) {
                if (!(fileInput instanceof $) || fileInput.length === 1) {
                    return $this._getSingleFileInputFiles(fileInput);
                }
                return $.when.apply(
                    $,
                    $.map(fileInput, $this._getSingleFileInputFiles)
                ).pipe(function () {
                    return Array.prototype.concat.apply(
                        [],
                        arguments
                    );
                });
            }


            $this._getSingleFileInputFiles = function (fileInput) {
                fileInput = $(fileInput);
                var entries = fileInput.prop('webkitEntries') ||
                        fileInput.prop('entries'),
                    files,
                    value;
                if (entries && entries.length) {
                    return $this._handleFileTreeEntries(entries);
                }
                files = $.makeArray(fileInput.prop('files'));
                if (!files.length) {
                    value = fileInput.prop('value');
                    if (!value) {
                        return $.Deferred().resolve([]).promise();
                    }
                    // If the files property is not available, the browser does not
                    // support the File API and we add a pseudo File object with
                    // the input value as name with path information removed:
                    files = [{ name: value.replace(/^.*\\/, '')}];
                } else if (files[0].name === undefined && files[0].fileName) {
                    // File normalization for Safari 4 and Firefox 3:
                    $.each(files, function (index, file) {
                        file.name = file.fileName;
                        file.size = file.fileSize;
                    });
                }
                return $.Deferred().resolve(files).promise();
            }

        });
    };

})(jQuery);
