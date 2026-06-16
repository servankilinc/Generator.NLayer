@{
    // var currentUrl = Context.GetUrl();
    // var lightMode = Context.GetLightMode();
}
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - {{ project_name }}</title>

    <partial name="./Sections/_LayoutHeader.cshtml" />
</head>
<body id="kt_app_body" data-kt-app-layout="dark-sidebar" data-kt-app-header-fixed="true" data-kt-app-sidebar-enabled="true" data-kt-app-sidebar-fixed="true" data-kt-app-sidebar-hoverable="true" data-kt-app-sidebar-push-header="true" data-kt-app-sidebar-push-toolbar="true" data-kt-app-sidebar-push-footer="true" data-kt-app-toolbar-enabled="true" class="app-default">

    <div class="d-flex flex-column flex-root app-root" id="kt_app_root">
        <div class="app-page flex-column flex-column-fluid" id="kt_app_page">
            <div class="app-wrapper flex-column flex-row-fluid mt-0" id="kt_app_wrapper">
                <main role="main" class="pb-3">
                    @RenderBody()
                </main>
            </div>

        </div>
    </div>


    <partial name="./Sections/_LayoutScripts.cshtml" />

    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>