const ModalManager = {

    //#region BUTTON PROPS
    buttonKinds: {
        save: 'save',
        update: 'update',
        delete: 'delete',
        confirmation: 'confirmation',
        cancel: 'cancel',
    },
    defaultButtonsProps: {
        save: {
            text: 'Kaydet',
            name: 'btn-primary',
            icon: 'fa-regular fa-circle-check'
        },
        update: {
            text: 'Güncelle',
            name: 'btn-warning',
            icon: 'fa-regular fa-pen-to-square'
        },
        delete: {
            text: 'Sil',
            name: 'btn-danger',
            icon: 'fa-solid fa-trash-can'
        },
        confirmation: {
            text: 'Onay',
            name: 'btn-success',
            icon: 'fa-solid fa-check-circle'
        },
        cancel: {
            text: 'İptal',
            name: 'btn-secondary',
            icon: 'fa-solid fa-arrow-left'
        }
    },
    //#endregion

    CreateModal({
        id = null,
        title = null,
        showHeader = true,
        innerHtml = '',
        buttons = [],
        modalSize = 'xl',
        btnCancelEnable = true,
        btnCancelSize = 'md',
        btnCancelText = 'Kapat',
        backdrop = true,
        tabindex = -1,
        onBeforeShow = null,
        onAfterShow = null,
        onBeforeClose = null,
        onAfterClose = null,
    }) {
        id ??= GenerateId();

        $(`#${id}`).remove();

        const modalElement = this._generateModalDOM(
            id, tabindex, backdrop, modalSize, showHeader,
            title, innerHtml, btnCancelEnable, btnCancelSize,
            btnCancelText, buttons
        );

        document.body.appendChild(modalElement);

        const modal = $(`#${id}`);

        modal.on('show.bs.modal', e => onBeforeShow?.(e));
        modal.on('shown.bs.modal', e => onAfterShow?.(e));
        modal.on('hide.bs.modal', e => onBeforeClose?.(e));
        modal.on('hidden.bs.modal', e => {
            onAfterClose?.(e);
            modal.remove();
        });

        // ****** FlatPicker Congifrations If Exist ******
        AutoInitFlatPicker(modal);
        // ****** select2 Congifrations If Exist ******
        AutoInitSelect2(modal);
        //// ****** FlatPicker Congifrations If Exist ******
        //AutoInitDatePicker(modal);

        return {
            element: modal,
            show: () => modal.modal("show"),
            close: () => modal.modal("hide"),
            remove: () => modal.remove()
        };
    },

    DeleteModal({ id = null, onClick = null }) {
        return this.CreateModal({
            id,
            innerHtml: `
                <div class="d-flex flex-column justify-content-center">
                    <i class="fa-solid fa-triangle-exclamation text-warning opacity-50" style="font-size: 2.5rem;"></i>
                    <h4 class="text-center fw-normal">Silmek İstediğinize Emin misiniz?</h4>
                </div>`,
            buttons: [
                this.Button({
                    kind: this.buttonKinds.delete,
                    text: 'Sil',
                    onClick,
                    size: 'sm'
                })
            ],
            modalSize: 'sm',
            btnCancelSize: 'sm',
            btnCancelText: 'İptal',
            showHeader: false
        });
    },

    Button({
        kind = null,
        id = null,
        className = '',
        type = 'button',
        attributes = {},
        disable = false,
        size = 'md',
        onClick = null,
        text = null,
        icon = null,
    }) {
        const defaults = this.defaultButtonsProps[kind] || {};

        return {
            id: id || GenerateId(),
            className,
            type,
            attributes,
            disable,
            size,
            onClick,
            text: text ?? defaults.text ?? '',
            name: defaults.name ?? 'btn-secondary',
            icon: icon ?? defaults.icon ?? null,
        };
    },

    //#region HELPERS
    _generateModalDOM(id, tabindex, backdrop, modalSize, showHeader, title, innerHtml, btnCancelEnable, btnCancelSize, btnCancelText, buttons) {
        const modal = document.createElement('div');
        modal.className = 'modal fade';
        modal.id = id;
        modal.tabIndex = tabindex;
        modal.dataset.bsBackdrop = backdrop ? 'static' : 'true';

        const dialog = document.createElement('div');
        dialog.className = `modal-dialog modal-dialog-scrollable modal-${modalSize}`;

        const content = document.createElement('div');
        content.className = 'modal-content';

        if (showHeader) {
            const header = document.createElement('div');
            header.className = 'modal-header';

            header.innerHTML = `
                <h3 class="modal-title">${title ?? ''}</h3>
                <div class="btn btn-icon btn-sm btn-active-light-primary ms-2" data-bs-dismiss="modal" aria-label="Close">
                    <i class="ki-duotone ki-cross fs-1"><span class="path1"></span><span class="path2"></span></i>
                </div>
            `;
            content.appendChild(header);
        }

        const body = document.createElement('div');
        body.className = 'modal-body';

        typeof innerHtml === 'string'
            ? body.innerHTML = innerHtml
            : body.append(innerHtml);

        const footer = document.createElement('div');
        footer.className = 'modal-footer';

        if (btnCancelEnable) {
            footer.innerHTML += `
                <button type="button" class="btn btn-light btn-${btnCancelSize}" data-bs-dismiss="modal">
                    <i class="fa-solid fa-xmark me-2"></i>${btnCancelText}
                </button>`;
        }

        (Array.isArray(buttons) ? buttons : [buttons]).forEach(btn => {
            if (btn) footer.appendChild(this._generateModalButtonDOM(btn, modal));
        });

        content.append(body, footer);
        dialog.appendChild(content);
        modal.appendChild(dialog);

        return modal;
    },


    _generateModalButtonDOM(btnObject, modalRef) {
        const btn = document.createElement('button');
        btn.id = btnObject.id;
        btn.type = btnObject.type;
        btn.className = `btn ${btnObject.name} btn-${btnObject.size} ${btnObject.className}`;
        btn.disabled = btnObject.disable;

        Object.entries(btnObject.attributes || {}).forEach(([k, v]) => btn.setAttribute(k, v));

        btn.onclick = async e => {
            if (!btnObject.onClick) return;

            const dynamic = btn.querySelector('.dynamic-content');
            const original = dynamic?.innerHTML;

            btn.disabled = true;
            if (dynamic) dynamic.innerHTML = '<i class="fa-solid fa-spinner me-2"></i> Bekleyiniz';

            try {
                await btnObject.onClick(e, modalRef, $(modalRef).find("form"));
            } finally {
                btn.disabled = false;
                if (dynamic) dynamic.innerHTML = original;
            }
        };

        const span = document.createElement('span');
        span.className = 'dynamic-content';

        if (btnObject.icon) {
            span.innerHTML = `<i class="${btnObject.icon} me-2"></i>`;
        }

        span.append(document.createTextNode(btnObject.text));
        btn.appendChild(span);

        return btn;
    }
    //#endregion
};
