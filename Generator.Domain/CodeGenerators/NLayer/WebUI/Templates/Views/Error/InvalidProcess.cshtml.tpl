@model string
@{
    ViewData["Title"] = "Invalid Process";
    Layout = "_LayoutBase";
}

<div class="container-xxl container-p-y">
    <div class="misc-wrapper">
        <h1 class="mb-2 mx-2" style="line-height: 6rem;font-size: 6rem;">500</h1>
        <h4 class="mb-2 mx-2">Invalid Process Error ⚠️</h4>
        <p class="mb-6 mx-2">
            @(string.IsNullOrWhiteSpace(Model) ? "You are not authorized!" : Model)
        </p>
        <a href="/" class="btn btn-primary">Back to home</a>
        <div class="mt-6">
            <img src="~/assets/img/illustrations/girl-doing-yoga-light.png" alt="page-misc-error-light" width="500" class="img-fluid">
        </div>
    </div>
</div>
