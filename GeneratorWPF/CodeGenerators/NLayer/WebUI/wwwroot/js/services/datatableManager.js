const DatatableManager =
{
    baseUrl: '',

    //#region DOM TEMPLATE
    defaultTableDom: `
        <"card shadow-none border"
            <"card-header bg-light"
                <"card-toolbar d-flex justify-content-between align-items-center"
                    <"d-flex align-items-center gap-5"
                        <"my-2 d-flex align-items-center justify-conten-start dt-toolbar"l>
                        <"my-2 dt-action-buttons text-end"B>
                    >
                    <"d-flex align-items-center justify-content-end dt-toolbar"f>
                >
            >
            <"card-body"
                <"table-responsive"tr>
                <"d-flex justify-content-between row px-3 pb-3"
                    <"col-sm-12 col-md-6 d-flex align-items-center justify-content-center justify-content-md-start"i>
                    <"col-sm-12 col-md-6 d-flex align-items-center justify-content-center justify-content-md-end"p>
                >
            >
        >`,
    defaultTableDomWithButtons: `
        <"card shadow-none border"
            <"card-header bg-light"
                <"card-toolbar w-100"
                    <"w-100"B>
                >
            >
            <"card-body"
                <"d-flex justify-content-between align-items-center"
                    <"my-2 d-flex align-items-center justify-conten-start dt-toolbar"l>
                    <"my-2 d-flex align-items-center justify-content-end dt-toolbar"f>
                >
                <"table-responsive"tr>
                <"d-flex justify-content-between row px-3 pb-3"
                    <"col-sm-12 col-md-6 d-flex align-items-center justify-content-center justify-content-md-start"i>
                    <"col-sm-12 col-md-6 d-flex align-items-center justify-content-center justify-content-md-end"p>
                >
            >
        >`,
    defaultButtonsDom: {
        container: {
            className: 'w-100 d-flex justify-content-start align-itmes-start'
        },
        button: {
            className: 'btn-sm'
        }
    },
    //#endregion

    //#region LENGTH
    defaultLengthMenu: [
        [10, 25, 50, 100, - 1],
        [10, 25, 50, 100, 'All']
    ],
    //#endregion

    //#region EXPORT BUTTON
    exportButton: {
        extend: 'collection',
        className: 'btn btn-light-success shadow-sm dropdown-toggle ms-auto',
        text: '<i class="ki-duotone ki-exit-down fs-3"><span class="path1"></span><span class="path2"></span></i> <span class="d-none d-lg-inline-block">Export</span>',
        buttons: ['print', 'excel', 'pdf', 'copy'].map(type => ({
            extend: type,
            className: 'dropdown-item',
            text: {
                print: '<i class="bx bx-printer me-1"></i>Yazdır',
                excel: '<i class="bx bx-file me-1"></i>Excel',
                pdf: '<i class="bx bxs-file-pdf me-1"></i>Pdf',
                copy: '<i class="bx bx-copy me-1"></i>Kopyala'
            }[type],
            exportOptions: {} // columns: [3, 4, 5, 6, 7]
        }))
    },
    //#endregion

    //#region CUSTOM BUTTONS
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
            name: 'btn btn-outline btn-outline-primary',
            icon: 'fa-solid fa-file-lines', // 'fa-regular fa-file-lines',
        },
        update: {
            text: 'Güncelle',
            name: 'btn btn-outline btn-outline-success',
            icon: 'fa-solid fa-pen-to-square',
        },
        delete: {
            text: 'Sil',
            name: 'btn btn-outline btn-outline-danger',
            icon: 'fa-solid fa-trash',
        },
        confirmation: {
            text: 'Onay',
            name: 'btn btn-outline btn-outline-info',
            icon: 'fa-solid fa-circle-check',
        },
        cancel: {
            text: 'İptal',
            name: 'btn btn-outline btn-outline-secondary',
            icon: 'fa-solid fa-ban',
        },
        undo: {
            text: 'Geri Al',
            name: 'btn btn-outline btn-outline-warning',
            icon: 'fa-solid fa-trash-can-arrow-up',
        }
    },
    //#endregion

    Create: function ({
        tableId = '',
        serverSide = false,
        path = '',
        method = 'GET',
        requestData = null,
        formId = null,
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
        responsive = false,
        hiddenColumnsInModal = [],
        onBefore = null,
        onSuccess = null,
        onAfter = null,
        onError = null,
        extendedProps = {}
    }) {
        const $btn = buttonId ? $(`#${buttonId}`) : (buttonElement ? $(buttonElement) : null);

        let headerButtons = this._getTableHeaderButtons(exportEnable, customButtons, exportColumns);

        if ($.fn.DataTable.isDataTable(`#${tableId}`)) {
            $(`#${tableId}`).DataTable().clear().destroy();
        }

        const dt = $(`#${tableId}`).DataTable({
            ajax: function (data, callback, settings) {

                let payload = serverSide ? data : {};
                if (formId) {
                    $(`#${formId}`).serializeArray().forEach(x => payload[x.name] = x.value);
                }
                if (requestData != null) {
                    if (typeof requestData === 'object') {
                        Object.assign(payload, requestData);
                    }
                    else if (Array.isArray(requestData)) {
                        requestData.forEach(item => { payload[item.name] = item.value; });
                    }
                }

                if (path && !path.startsWith('/'))
                    path = '/' + path.trim();

                $.ajax({
                    url: `${DatatableManager.baseUrl}${path}`,
                    type: method,
                    dataType: 'json',
                    data: payload,
                    beforeSend: function () {
                        if ($btn != null) DatatableManager._toggleButtonLoading($btn, true);
                        if (typeof onBefore === 'function') onBefore();
                    },
                    success: function (response) {
                        callback(response);
                        if (onSuccess != null && typeof onSuccess === 'function') onSuccess();
                    },
                    error: function (xhr, status, error) {
                        AlertManager.Error("Bilgiler Alınırken Bir Sorun Oluştu!").then(() => {
                            if (onError != null && typeof onError === 'function') onError(error)
                        });
                        callback({ data: [] });
                    },
                    complete: function () {
                        if ($btn != null) DatatableManager._toggleButtonLoading($btn, false);
                        if (typeof onAfter === 'function') onAfter();
                    }
                });
            },
            serverSide: serverSide,
            searchDelay: 500,
            retrieve: true,
            destroy: true,
            processing: true,
            responsive: responsive,
            dom: dom || ((customButtons != null && customButtons.length > 0) ? this.defaultTableDomWithButtons : this.defaultTableDom),
            columns: columns,
            columnDefs: columnDefs,
            order: order,
            ordering: ordering,
            searching: searching,
            stateSave: stateSave,
            scrollCollapse: !responsive,
            scrollY: scrollY,
            buttons: {
                dom: this.defaultButtonsDom,
                buttons: headerButtons
            },
            pageLength: pageLength,
            lengthMenu: lengthMenu || this.defaultLengthMenu,
            stateSave: true,
            select: {
                style: 'multi',
                selector: 'td:first-child input[type="checkbox"]',
                className: 'row-selected'
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
        tableId = '',
        data = [],
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
        responsive = false,
        hiddenColumnsInModal = [],
        extendedProps = {},
    }) {
        let headerButtons = this._getTableHeaderButtons(exportEnable, customButtons, exportColumns);

        //if ($(`#${tableId}`).length > 0) $(`#${tableId}`).destroy();

        const dt = $(`#${tableId}`).DataTable({
            data: data,
            retrieve: true,
            destroy: true,
            processing: true,
            responsive: responsive,
            dom: dom || this.defaultTableDom,
            columns: columns,
            columnDefs: columnDefs,
            order: order,
            ordering: ordering,
            searching: searching,
            stateSave: stateSave,
            scrollCollapse: !responsive,
            scrollY: scrollY,
            buttons: {
                dom: this.defaultButtonsDom,
                buttons: headerButtons
            },
            pageLength: pageLength,
            lengthMenu: lengthMenu || this.defaultLengthMenu,
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
        responsive = false,
        hiddenColumnsInModal = [],
        extendedProps = {},
    }) {

        let headerButtons = this._getTableHeaderButtons(exportEnable, customButtons, exportColumns);

        //if ($(`#${tableId}`).length > 0) $(`#${tableId}`).destroy();

        const dt = $(`#${tableId}`).DataTable({
            retrieve: true,
            destroy: true,
            processing: true,
            responsive: responsive,
            dom: dom == null ? this.defaultTableDom : dom,
            columns: columns,
            columnDefs: columnDefs,
            order: order,
            ordering: ordering,
            searching: searching,
            stateSave: stateSave,
            scrollCollapse: !responsive,
            scrollY: scrollY,
            buttons: {
                dom: this.defaultButtonsDom,
                buttons: headerButtons
            },
            pageLength: pageLength,
            lengthMenu: lengthMenu == null ? this.defaultLengthMenu : lengthMenu,
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
        kind = kind || this.buttonKinds.detail;
        let defaultProps = this.defaultButtonsProps[kind] || {};

        btnObject.id = btnObject.id || GenerateId();
        btnObject.text = btnObject.text || '';
        btnObject.name = btnObject.name || defaultProps.name;
        btnObject.icon = btnObject.icon || defaultProps.icon;

        // btn-init
        const btnElement = document.createElement('button');
        btnElement.id = btnObject.id;
        btnElement.className = `btn ${btnObject.name} btn-${btnObject.size} mx-1 ${btnObject.className || ``} px-2 py-1`;
        Object.entries(btnObject.attributes).forEach(([key, val]) => btnElement.setAttribute(key, val));
        btnElement.disabled = btnObject.disable;

        // btn-onclick
        if (typeof btnObject.onClick === 'function') {
            btnElement.onclick = async (e) => {
                //const $btn = $(btnElement); // (zaten requestManager buutonu işliyor)
                //if ($btn != null) this._toggleButtonLoading($btn, true);
                try {
                    await onClick(e);
                }
                finally {
                    //if ($btn != null) this._toggleButtonLoading($btn, false);
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

    //#region HELPERS
    _getTableHeaderButtons: function (exportEnable, customButtons, exportColumns) {
        let btns = [...(customButtons || [])];
        if (exportEnable) {
            let exp = structuredClone(this.exportButton);
            if (exportColumns?.length > 0)
                exp.buttons.forEach(b => b.exportOptions.columns = exportColumns);
            btns.push(exp);
        }
        return btns;
    },
    _toggleButtonLoading: function ($btn, isLoading) {
        if (!$btn || !$btn.length) return;

        const $content = $btn.find(".dynamic-content");

        if (isLoading) {
            $btn.data("prev-content", $content.clone(true, true));
            $btn.prop("disabled", true);
            $content.html('<i class="fa-solid fa-spinner fa-spin me-2"></i> Bekleyiniz...');
        }
        else {
            $btn.prop("disabled", false);
            const prev = $btn.data("prev-content");
            if (prev) {
                $content.replaceWith(prev);
                $btn.removeData("prev-content");
            }
        }
    }
    //#endregion
}