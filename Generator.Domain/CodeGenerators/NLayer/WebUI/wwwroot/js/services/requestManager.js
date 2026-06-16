const RequestManager = {
    baseUrl: '',

    Get: function (options) {
        return this.HandleRequest({ ...options, type: 'GET' });
    },
    Post: function (options) {
        return this.HandleRequest({ ...options, type: 'POST' });
    },
    Put: function (options) {
        return this.HandleRequest({ ...options, type: 'PUT' });
    },
    Patch: function (options) {
        return this.HandleRequest({ ...options, type: 'PATCH' });
    },
    Delete: function (options) {
        return this.HandleRequest({ ...options, type: 'DELETE' });
    },

    HandleRequest: function ({
        type = 'GET',
        path = '',
        dataType = null,
        requestData = null,
        formId = null,
        buttonId = null,
        buttonElement = null,
        onBefore = null,
        onSuccess = null,
        onAfter = null,
        onError = null,
        waitToastr = false,
        showToastrSuccess = true,
        showToastrError = true,
        successMessage = "İşlem Başarıyla Tamamlandı.",
        errorMessage = "İşlem Sırasında Bir Hata oluştu."
    }) {
        return new Promise((resolve, reject) => {
            let data = null;
            let $btn = buttonId ? $(`#${buttonId}`) : (buttonElement ? $(buttonElement) : null);

            if (formId) {
                data = new FormData(document.getElementById(formId));
            }
            else if (requestData) {
                if (Array.isArray(requestData)) {
                    data = {};
                    requestData.forEach(item => { data[item.name] = item.value; });
                }
                else {
                    data = requestData;
                }
            }

            if (path && !path.startsWith('/'))
                path = '/' + path.trim();
            const url = `${this.baseUrl}${path}`;

            const ajaxOptions = {
                url: url,
                type: type.toUpperCase(),
                data: data,
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                },
                beforeSend: function () {
                    if ($btn != null) RequestManager._toggleButtonLoading($btn, true);
                    if (typeof onBefore === 'function') onBefore();
                },
                success: function (response) {
                    const finalizeSuccess = () => {
                        if (typeof onSuccess === 'function') onSuccess(response);
                        resolve(response);
                    };

                    if (showToastrSuccess && typeof AlertManager !== 'undefined') {
                        if (waitToastr) {
                            AlertManager.Success(successMessage).then(() => finalizeSuccess())
                        }
                        else {
                            AlertManager.Success(successMessage);
                            finalizeSuccess();
                        }
                    }
                    else {
                        finalizeSuccess();
                    }
                },
                error: function (xhr) {
                    let finalErrorMsg = errorMessage;
                    const resJson = xhr.responseJSON;

                    // ProblemDetails uyumlu response işle
                    if (resJson) {
                        const pType = resJson.type || resJson.Type || "";
                        const pTitle = resJson.title || resJson.Title;

                        // 100: failure, 200: notfound, 300: validation, 400: forbiden
                        if (pType.includes("problems/Failure"))
                            finalErrorMsg = pTitle || "Sunucu hatası oluştu.";
                        else if (pType.includes("problems/NotFound"))
                            finalErrorMsg = pTitle || "Bilgi/Kayıt bulunamadığı için işleme devam edilemiyor.";
                        else if (pType.includes("problems/Validation")) {
                            finalErrorMsg = pTitle || "Doğrulama hatası.";
                            ShowValidationErrors(resJson);
                        }
                        else if (pType.includes("problems/Forbidden"))
                            finalErrorMsg = pTitle || "İşlem için yetkiniz bulunmamakta.";
                    }

                    const finalizeError = () => {
                        if (typeof onError === 'function') onError(xhr);
                        reject(xhr);
                    };

                    if (showToastrError && typeof AlertManager !== 'undefined') {
                        if (waitToastr) {
                            AlertManager.Error(finalErrorMsg).then(() => finalizeError())
                        }
                        else {
                            AlertManager.Error(finalErrorMsg);
                            finalizeError();
                        }
                    }
                    else {
                        finalizeError();
                    }
                },
                complete: function () {
                    if ($btn != null) RequestManager._toggleButtonLoading($btn, false);
                    if (typeof onAfter === 'function') onAfter();
                }
            };

            // FormData GET isteklerinde query string olarak gönderilecek
            if (ajaxOptions.data instanceof FormData) {
                if (ajaxOptions.type === 'GET') {
                    const params = new URLSearchParams(ajaxOptions.data);
                    ajaxOptions.url += (ajaxOptions.url.includes('?') ? '&' : '?') + params.toString();
                    delete ajaxOptions.data;
                } else {
                    ajaxOptions.processData = false;
                    ajaxOptions.contentType = false;
                }
            }

            $.ajax(ajaxOptions);
        });
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
};

function ShowValidationErrors(response) {
    $(`[data-valmsg-for]`).text("").removeClass("text-danger");

    const errors = response?.extensions?.errors || response?.errors;

    if (errors && typeof errors === 'object') {
        Object.keys(errors).forEach(key => {
            const messages = errors[key];
            const message = Array.isArray(messages) ? messages[0] : messages;

            let $target = $(`[data-valmsg-for='${key}'], [data-valmsg-for$='.${key}']`);

            if ($target.length) {
                $target.text(message).addClass("text-danger");
            }
        });
    }
}