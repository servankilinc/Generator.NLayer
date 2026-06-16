@model string?
@{
    ViewData["Title"] = "Forbiden Error";
    Layout = "_LayoutBase";
}

<div class="misc-wrapper">
    <h1 class="mb-2 mx-2" style="line-height: 6rem;font-size: 6rem;">401</h1>
    <h4 class="mb-2 mx-2">
        You don’t have permission to access!
    </h4>
    <p class="mb-6 mx-2">
        @(string.IsNullOrWhiteSpace(Model) ? "You are not authorized!" : Model)
    </p>
    <a href="index.html" class="btn btn-primary">Back to home</a>
    <div class="mt-6">
        <img src="~/assets/img/illustrations/girl-with-laptop-light.png" alt="page-misc-not-authorized-light" width="500" class="img-fluid" >
    </div>
</div>