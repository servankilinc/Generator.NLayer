const DatatableManager =
{
    defaultDom: `
        <"card shadow-none border"
            <"card-header border-bottom bg-lightest d-flex justify-content-between align-items-center"
                <"d-flex align-items-center gap-5"
                    <"py-2 ps-1 pe-3"l>
                    <"dt-action-buttons text-end"B>
                >
                <"ms-5"f> 
            >
            <"card-body"
                rt
                <"d-flex justify-content-between row px-3 pb-3"
                    <"col-sm-12 col-md-6"i>
                    <"col-sm-12 col-md-6"p>
                >
            >
        >`,
    defaultDomWithButtons: `
        <"card shadow-none border"
            <"card-header border-bottom bg-lightest"
                <"dt-action-buttons text-end"B>
            >
            <"card-body"
                <"d-flex justify-content-between align-items-center"
                    <"py-2 ps-1 pe-3"l>
                    <"ms-5"f>
                >
                rt
                <"d-flex justify-content-between row px-3 pb-3"
                    <"col-sm-12 col-md-6"i>
                    <"col-sm-12 col-md-6"p>
                >
            >
        >`,

    defaultLengthMenu: [
        [10, 25, 50, 100, - 1],
        [10, 25, 50, 100, 'All']
    ],
    exportButton: {
        extend: 'collection',
        className: 'btn buttons-collection bg-label-success shadow-none border-success dropdown-toggle',
        text: '<i class="icon-base bx bx-export me-1"></i> <span class="d-none d-lg-inline-block">Dışa Aktar</span>',
        buttons: [
            {
                extend: 'print',
                text: '<i class="icon-base bx bx-printer me-1" ></i>Yazdır',
                className: 'dropdown-item',
                exportOptions: {} //columns: [3, 4, 5, 6, 7]
            },
            {
                extend: 'excel',
                text: '<i class="icon-base bx bx-file me-1" ></i>Excel',
                className: 'dropdown-item',
                exportOptions: {}
            },
            {
                extend: 'pdf',
                text: '<i class="icon-base bx bxs-file-pdf me-1"></i>Pdf',
                className: 'dropdown-item',
                exportOptions: {}
            },
            {
                extend: 'copy',
                text: '<i class="icon-base bx bx-copy me-1" ></i>Kopyala',
                className: 'dropdown-item',
                exportOptions: {}
            }
        ]
    },
    buttonKinds: {
        detail: 'detail',
        update: 'update',
        delete: 'delete',
        confirmation: 'confirmation',
        cancel: 'cancel',
        undo: 'undo'
    },
    defaultButtonsProps: {
        detail: {
            text: 'Detay',
            name: 'btn-primary',
            icon: 'fa-solid fa-file-lines', // 'fa-regular fa-file-lines',
        },
        update: {
            text: 'Güncelle',
            name: 'btn-success',
            icon: 'fa-solid fa-pen-to-square',
        },
        delete: {
            text: 'Sil',
            name: 'btn-danger',
            icon: 'fa-solid fa-trash',
        },
        confirmation: {
            text: 'Onay',
            name: 'btn-info',
            icon: 'fa-solid fa-circle-check',
        },
        cancel: {
            text: 'İptal',
            name: 'btn-secondary',
            icon: 'fa-solid fa-ban',
        },
        undo: {
            text: 'Geri Al',
            name: 'btn-warning',
            icon: 'fa-solid fa-trash-can-arrow-up',
        }
    },

    Create: function ({
        serverSide = false,
        path = "",
        method = "GET",
        requestData = null,
        formId = null,
        tableId = "",
        buttonId = null,
        buttonElement = null,
        dom = null,
        columns = [],
        columnDefs = [],
        order = [],
        ordering = true,
        searching = true,
        stateSave = false,
        scrollCollapse = true,
        scrollY = 700,
        exportEnable = true,
        exportColumns = [],
        customButtons = [],
        pageLength = 10,
        lengthMenu = null,
        responsive = false, // true çekince hata var
        hiddenColumnsInModal = [],
        extendedProps = {},
        onBefore = null,
        onSuccess = null,
        onAfter = null,
        onError = null
    }) {
        if (responsive == true) {
            const thList = $(`#${tableId} thead tr th`);
            if (thList != null && thList.length > 0 && !$(thList[0]).hasClass("control")) {
                let thOfResponsive = document.createElement("th");
                thOfResponsive.className = "control";
                $(`#${tableId} thead tr `).prepend(thOfResponsive);

                columns.unshift(
                    {
                        data: '',
                        className: 'control',
                        orderable: false,
                        searchable: false,
                        responsivePriority: 2,
                        targets: 0,
                        render: function (data, type, full, meta) {
                            return '';
                        }
                    })
            }
        }

        const _exportButton = structuredClone(this.exportButton);
        if (exportEnable == true && exportColumns != null && exportColumns.length > 0) {
            _exportButton.buttons.forEach(d => d.exportOptions.columns = exportColumns);
        }

        let _baseUrl = RequestManager.baseUrl;
        if ($.fn.DataTable.isDataTable(`#${tableId}`)) {
            $(`#${tableId}`).DataTable().clear().destroy();
        }
        const dt = $(`#${tableId}`).DataTable({
            ajax: function (data, callback, settings) {
                data = serverSide == true ? data : {};
                if (requestData != null && typeof requestData === 'object') Object.assign(data, requestData);

                if (formId != null) {
                    const formDataArray = $(`#${formId}`).serializeArray();
                    const formDataObj = {};
                    formDataArray.forEach(item => {
                        formDataObj[item.name] = item.value;
                    });
                    Object.assign(data, formDataObj);
                }

                let originalBtnContent = '...';
				if (path[0] != '/') path = '/' + path.trim();
				
				$.ajax({
                    url: `${_baseUrl}${path}`,
                    type: method,
                    dataType: 'json',
                    data: data,
                    beforeSend: function () {
                        if (buttonId != null) {
                            let btn = $(`#${buttonId}`);
                            btn.prop("disabled", true);
                            let dynamicContent = btn.find(".dynamic-content");
                            if (dynamicContent.length == 1) {
                                originalBtnContent = dynamicContent.html();
                                dynamicContent.html('<i class="fa-solid fa-spinner me-2"></i> Bekleyiniz');
                            }
                        }
                        else if (buttonElement != null) {
                            let btn = $(buttonElement);
                            btn.prop("disabled", true);
                            let dynamicContent = btn.find(".dynamic-content");
                            if (dynamicContent.length == 1) {
                                originalBtnContent = dynamicContent.html();
                                dynamicContent.html('<i class="fa-solid fa-spinner me-2"></i> Bekleyiniz');
                            }
                        }

                        if (onBefore != null && typeof onBefore === 'function') onBefore();
                    },
                    success: function (response) {
                        callback(response);
                        if (onSuccess != null && typeof onSuccess === 'function') onSuccess();
                    },
                    error: function (xhr, status, error) {
                        const isThereOnError = onError != null && typeof onError === 'function';
                        AlertManager.Error("Bilgiler Alınırken Bir Sorun Oluştu!").then(() => { if (isThereOnError) onError(error) });
                        callback({ data: [] });
                    },
                    complete: function () {
                        if (buttonId != null) {
                            let btn = $(`#${buttonId}`);
                            btn.prop("disabled", false);
                            let dynamicContent = btn.find(".dynamic-content");
                            if (dynamicContent.length == 1) {
                                dynamicContent.html(originalBtnContent);
                            }
                        }
                        else if (buttonElement != null) {
                            let btn = $(buttonElement);
                            btn.prop("disabled", false);
                            let dynamicContent = btn.find(".dynamic-content");
                            if (dynamicContent.length == 1) {
                                dynamicContent.html(originalBtnContent);
                            }
                        }

                        if (onAfter != null && typeof onAfter === 'function') onAfter();
                    }
                });
            },
            retrieve: true,
            destroy: true,
            processing: true,
            serverSide: serverSide,
            dom: dom == null ? ((customButtons != null && customButtons.length > 0) ? this.defaultDomWithButtons : this.defaultDom) : dom,
            columns: columns,
            columnDefs: columnDefs,
            order: order,
            ordering: ordering,
            searching: searching,
            stateSave: stateSave,
            scrollCollapse: scrollCollapse,
            scrollY: scrollY,
            buttons: exportEnable == true ? [...customButtons, _exportButton] : [...customButtons],
            pageLength: pageLength,
            lengthMenu: lengthMenu == null ? this.defaultLengthMenu : lengthMenu,
            responsive: responsive != true ? false :
                {
                    details: {
                        display: $.fn.dataTable.Responsive.display.modal({
                            header: function (row) {
                                //var data = row.data();  //+ data['full_name'];
                                return '<h4 class="pb-2">Detay</h4>';
                            }
                        }),
                        type: 'column',
                        renderer: function (api, rowIdx, columns) {
                            var data = $.map(columns, function (col, i) {
                                return (!hiddenColumnsInModal.includes(col.columnIndex) && col.title !== '') ? // col.hidden &&
                                    `<tr data-dt-row="${col.rowIndex}" data-dt-column="${col.columnIndex}">
                                    <td class="fw-bold">${col.title}</td>  
                                    <td>${col.data}</td>
                                </tr>` : '';
                            }).join('');

                            return data ? $('<table class="table"/><tbody />').append(data) : false;
                        }
                    }
                },
            ...extendedProps
        });

        return {
            table: dt,
            api: () => dt,
            reload: () => dt.ajax.reload(),
            clear: () => dt.clear().draw(),
            rowCount: () => dt.rows({ search: 'applied' }).count(),
            getSelectedRows: () => dt.rows().nodes().to$().find('input[type="checkbox"]:checked').closest('tr').map(() => dt.row(this).data()).get()
        };
    },

    CreateByExistData: function ({
        data = [],
        tableId = "",
        dom = null,
        columns = [],
        columnDefs = [],
        order = [],
        ordering = true,
        searching = true,
        stateSave = false,
        scrollCollapse = true,
        scrollY = 700,
        exportEnable = true,
        exportColumns = [],
        customButtons = [],
        pageLength = 10,
        lengthMenu = null,
        responsive = true,
        hiddenColumnsInModal = [],
        extendedProps = {},
    }) {
        if (responsive == true) {
            const thList = $(`#${tableId} thead tr th`);
            if (thList != null && thList.length > 0 && !$(thList[0]).hasClass("control")) {
                var thOfResponsive = document.createElement("th");
                thOfResponsive.className = "control";
                $(`#${tableId} thead tr `).prepend(thOfResponsive);

                columns.unshift(
                    {
                        data: '',
                        className: 'control',
                        orderable: false,
                        searchable: false,
                        responsivePriority: 2,
                        targets: 0,
                        render: function (data, type, full, meta) {
                            return '';
                        }
                    })
            }
        }

        const _exportButton = structuredClone(this.exportButton);
        if (exportEnable == true && exportColumns != null && exportColumns.length > 0) {
            _exportButton.buttons.forEach(d => d.exportOptions.columns = exportColumns);
        }

        //if ($(`#${tableId}`).length > 0) $(`#${tableId}`).destroy();

        const dt = $(`#${tableId}`).DataTable({
            data: data,
            retrieve: true,
            destroy: true,
            processing: true,
            dom: dom == null ? this.defaultDom : dom,
            columns: columns,
            columnDefs: columnDefs,
            order: order,
            ordering: ordering,
            searching: searching,
            stateSave: stateSave,
            scrollCollapse: scrollCollapse,
            scrollY: scrollY,
            buttons: exportEnable == true ? [...customButtons, _exportButton] : [...customButtons],
            pageLength: pageLength,
            lengthMenu: lengthMenu == null ? this.defaultLengthMenu : lengthMenu,
            responsive: responsive != true ? false :
                {
                    details: {
                        display: $.fn.dataTable.Responsive.display.modal({
                            header: function (row) {
                                //var data = row.data();  //+ data['full_name'];
                                return '<h4 class="pb-2">Detay</h4>';
                            }
                        }),
                        type: 'column',
                        renderer: function (api, rowIdx, columns) {
                            var data = $.map(columns, function (col, i) {
                                return (!hiddenColumnsInModal.includes(col.columnIndex) && col.title !== '') ? // col.hidden &&
                                    `<tr data-dt-row="${col.rowIndex}" data-dt-column="${col.columnIndex}">
                                    <td class="fw-bold">${col.title}</td>  
                                    <td>${col.data}</td>
                                </tr>` : '';
                            }).join('');

                            return data ? $('<table class="table"/><tbody />').append(data) : false;
                        }
                    }
                },
            ...extendedProps
        });

        return {
            table: dt,
            api: () => dt,
            reload: () => dt.ajax.reload(),
            clear: () => dt.clear().draw(),
            rowCount: () => dt.rows({ search: 'applied' }).count(),
            getSelectedRows: () => dt.rows().nodes().to$().find('input[type="checkbox"]:checked').closest('tr').map(() => dt.row(this).data()).get()
        };
    },

    CreateByExistTable: function ({
        tableId = "",
        dom = null,
        columns = [],
        columnDefs = [],
        order = [],
        ordering = true,
        searching = true,
        stateSave = false,
        scrollCollapse = true,
        scrollY = 700,
        exportEnable = true,
        exportColumns = [],
        customButtons = [],
        pageLength = 10,
        lengthMenu = null,
        responsive = true,
        hiddenColumnsInModal = [],
        extendedProps = {},
    }) {
        if (responsive == true) {
            const thList = $(`#${tableId} thead tr th`);
            if (thList != null && thList.length > 0 && !$(thList[0]).hasClass("control")) {
                var thOfResponsive = document.createElement("th");
                thOfResponsive.className = "control";
                $(`#${tableId} thead tr `).prepend(thOfResponsive);

                columns.unshift(
                    {
                        data: '',
                        className: 'control',
                        orderable: false,
                        searchable: false,
                        responsivePriority: 2,
                        targets: 0,
                        render: function (data, type, full, meta) {
                            return '';
                        }
                    })
            }
        }

        const _exportButton = structuredClone(this.exportButton);
        if (exportEnable == true && exportColumns != null && exportColumns.length > 0) {
            _exportButton.buttons.forEach(d => d.exportOptions.columns = exportColumns);
        }

        //if ($(`#${tableId}`).length > 0) $(`#${tableId}`).destroy();

        const dt = $(`#${tableId}`).DataTable({
            retrieve: true,
            destroy: true,
            processing: true,
            dom: dom == null ? this.defaultDom : dom,
            columns: columns,
            columnDefs: columnDefs,
            order: order,
            ordering: ordering,
            searching: searching,
            stateSave: stateSave,
            scrollCollapse: scrollCollapse,
            scrollY: scrollY,
            buttons: exportEnable == true ? [...customButtons, _exportButton] : [...customButtons],
            pageLength: pageLength,
            lengthMenu: lengthMenu == null ? this.defaultLengthMenu : lengthMenu,
            responsive: responsive != true ? false :
                {
                    details: {
                        display: $.fn.dataTable.Responsive.display.modal({
                            header: function (row) {
                                //var data = row.data();  //+ data['full_name'];
                                return '<h4 class="pb-2">Detay</h4>';
                            }
                        }),
                        type: 'column',
                        renderer: function (api, rowIdx, columns) {
                            var data = $.map(columns, function (col, i) {
                                return (!hiddenColumnsInModal.includes(col.columnIndex) && col.title !== '') ? // col.hidden &&
                                    `<tr data-dt-row="${col.rowIndex}" data-dt-column="${col.columnIndex}">
                                    <td class="fw-bold">${col.title}</td>  
                                    <td>${col.data}</td>
                                </tr>` : '';
                            }).join('');

                            return data ? $('<table class="table"/><tbody />').append(data) : false;
                        }
                    }
                },
            ...extendedProps
        });

        return {
            table: dt,
            api: () => dt,
            reload: () => dt.ajax.reload(),
            clear: () => dt.clear().draw(),
            rowCount: () => dt.rows({ search: 'applied' }).count(),
            getSelectedRows: () => dt.rows().nodes().to$().find('input[type="checkbox"]:checked').closest('tr').map(() => dt.row(this).data()).get()
        };
    },

    RowButton: function ({
        kind = null,
        id = null,
        className = '',
        attributes = {},
        disable = false,
        size = 'sm',
        onClick = null,
        text = null,
        icon = null,
    }) {
        let btnObject = {
            id: id,
            className: className,
            attributes: attributes,
            disable: disable,
            size: size,
            onClick: onClick,
            text: text,
            icon: icon,
        };

        let defaultProps = this.defaultButtonsProps[kind];

        btnObject.id = btnObject.id || GenerateId();
        btnObject.text = btnObject.text || '';
        btnObject.name = btnObject.name || defaultProps.name;
        btnObject.icon = btnObject.icon || defaultProps.icon;

        // btn-init
        const btnElement = document.createElement('button');
        btnElement.id = btnObject.id;
        btnElement.className = `btn ${btnObject.name} btn-${btnObject.size} mx-1 ${btnObject.className || ``} px-3`;
        Object.entries(btnObject.attributes).forEach(([key, val]) => btnElement.setAttribute(key, val));
        btnElement.disabled = btnObject.disable;

        // btn-onclick
        if (btnObject.onClick != null && typeof btnObject.onClick === 'function') {
            btnElement.onclick = async function (e) {
                const button = this;

                let original = '';

                let dynamicContent = $(button).find(".dynamic-content");
                if (dynamicContent.length == 1) {
                    original = dynamicContent.html();
                    dynamicContent.html('<i class="fa-solid fa-spinner me-2"></i> Bekleyiniz');
                }

                $(button).prop("disabled", true);

                try {
                    await btnObject.onClick(e);
                }
                finally {
                    $(button).prop("disabled", false);

                    if (dynamicContent.length == 1) {
                        dynamicContent.html(original);
                    }
                }
            };
        }

        // btn-DynamicContent
        const dynamicContentElement = document.createElement('span');
        dynamicContentElement.className = `dynamic-content`;

        // btn-icon
        if (btnObject.icon != null) {
            const iconElement = document.createElement('i');
            iconElement.className = `${btnObject.icon} ${btnObject.text.length > 0 ? 'me-2' : ''}`;
            dynamicContentElement.append(iconElement);
        }

        // btn-text
        const textNode = document.createTextNode(btnObject.text);
        dynamicContentElement.appendChild(textNode);

        btnElement.append(dynamicContentElement);

        return btnElement;
    },

    AppendRowButtons: function (td, buttons) {
        let btnGroup = document.createElement("div");
        btnGroup.className = "d-flex flex-nowrap align-items-center justify-content-center";

        if (buttons != null) {
            if (Array.isArray(buttons)) {
                buttons.forEach((btn) => {
                    if (btn instanceof HTMLElement) {
                        btnGroup.appendChild(btn);
                    }
                });
            }
            else if (buttons instanceof HTMLElement) {
                btnGroup.appendChild(b);
            }
        }

        td.appendChild(btnGroup);
    },
}