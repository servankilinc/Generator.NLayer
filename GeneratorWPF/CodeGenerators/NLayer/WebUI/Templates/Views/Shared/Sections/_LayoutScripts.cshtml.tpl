
<!-- Global Javascript Bundle (used by all pages) -->
<script src="~/metronic/plugins/global/plugins.bundle.js"></script>
<script src="~/metronic/js/scripts.bundle.js"></script>
<!-- Global Javascript Bundle -->

<!-- LIBS -->
<script src="~/lib/jquery-validation/dist/jquery.validate.min.js"></script>
<script src="~/lib/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js"></script>
<script src="~/lib/moment/moment.js"></script>
<script src="~/lib/simple-notify/simple-notify.min.js"></script>
<script src="~/lib/pace/pace.min.js"></script>
<!-- LIBS -->

<script src="~/js/services/requestManager.js"></script>
<script src="~/js/services/alertManager.js"></script>
<script src="~/js/services/datatableManager.js"></script>
<script src="~/js/services/modalManager.js"></script>

<script src="~/js/helper.js"></script>
<script src="~/js/main.js"></script>

<script>
    window.paceOptions = {
        ajax: {
            trackMethods: ['GET', 'POST', 'DELETE', 'PACH'],
            ignoreURLs: [
                "/heartbeat",
                "/keepalive",
                "signalr",
                "__browserLink",
                "browserLinkSignalR"
            ],
            trackWebSockets: false,
        },
        document: true,
        eventLag: false,
        restartOnRequestAfter: false
    };
    // jquery pace.js handle
    $(document).ajaxStart(function () { Pace.restart(); });

    // fetch pace.js handle
    const _fetch = window.fetch;
    window.fetch = function (...args) {
        Pace.restart();
        return _fetch.apply(this, args);
    };
</script>


<!-- Theme mode setup on page load-->
<script>
    var defaultThemeMode = "light"; 
    var themeMode; 
    if (document.documentElement) { 
        if (document.documentElement.hasAttribute("data-bs-theme-mode")) { 
            themeMode = document.documentElement.getAttribute("data-bs-theme-mode"); 
        } 
        else { 
            if (localStorage.getItem("data-bs-theme") !== null) { 
                themeMode = localStorage.getItem("data-bs-theme"); 
            } 
            else { 
                themeMode = defaultThemeMode; 
            } 
        } 
        if (themeMode === "system") { 
            themeMode = window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light"; 
        } 
        document.documentElement.setAttribute("data-bs-theme", themeMode);
    }

    // const syncSidebarTheme = (mode) => {
    //     document.body.setAttribute("data-kt-app-layout", mode === "light" ? "light-sidebar" : "dark-sidebar");
    //     document.body.setAttribute("data-kt-theme-mode", mode === "light" ? "light" : "dark");
    // };

    // syncSidebarTheme(document.documentElement.getAttribute("data-bs-theme"));

    // window.addEventListener("kt.thememode.change", function (event) {
    //     syncSidebarTheme(event.detail.mode);
    // });
</script>
<!-- Theme mode setup on page load-->
