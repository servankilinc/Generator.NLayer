@using {{ webui_project_name }}.Models.Auth
@model LoginRequest
@{
    Layout = "_LayoutBase";
    ViewData["Title"] = "Login";
}

<style>
    body {
        background-image: url('/media/backgrounds/bg4.jpg');
    }

    [data-bs-theme="dark"] body {
        background-image: url('/media/backgrounds/bg5-dark.jpg');
    }
</style>


<div class="d-flex flex-column flex-column-fluid flex-lg-row">
    <div class="d-flex flex-center w-lg-50 pt-15 pt-lg-0 px-10">
        <div class="d-flex flex-center flex-lg-start flex-column">
            <a href="index.html" class="mb-7">
                <img alt="Logo" src="~/media/logos/default-dark.svg" />
            </a>
            <h2 class="text-white fw-normal m-0">Branding tools designed for your business</h2>
        </div>
    </div>
    <div class="d-flex flex-column-fluid flex-lg-row-auto justify-content-center justify-content-lg-end p-12 p-lg-20">
        <div class="bg-body d-flex flex-column align-items-stretch flex-center rounded-4 w-md-600px p-20">
            <div class="d-flex flex-center flex-column flex-column-fluid px-lg-10 pb-15 pb-lg-20">
                <form class="form w-100" asp-controller="Account" asp-action="Login" method="post">
                    <div class="text-center mb-11">
                        <h1 class="text-gray-900 fw-bolder mb-3">Login</h1>
                        <div class="text-gray-500 fw-semibold fs-6">Your Social Campaigns</div>
                    </div>

                    @* <div class="row g-3 mb-9">
						<div class="col-md-6">
							<a href="#" class="btn btn-flex btn-outline btn-text-gray-700 btn-active-color-primary bg-state-light flex-center text-nowrap w-100">
								<img alt="Logo" src="~/media/svg/brands/google-icon.svg" class="h-15px me-3" />Sign in with Google
							</a>
						</div>
						<div class="col-md-6">
							<a href="#" class="btn btn-flex btn-outline btn-text-gray-700 btn-active-color-primary bg-state-light flex-center text-nowrap w-100">
								<img alt="Logo" src="~/media/svg/brands/apple-black.svg" class="theme-light-show h-15px me-3" />
								<img alt="Logo" src="~/media/svg/brands/apple-black-dark.svg" class="theme-dark-show h-15px me-3" />Sign in with Apple
							</a>
						</div>
					</div> 
					<div class="separator separator-content my-14">
						<span class="w-125px text-gray-500 fw-semibold fs-7">Or with email</span>
					</div> *@


                    <div class="fv-row mb-8">
                        <ul class="nav nav-tabs nav-line-tabs mb-5 fs-6">
                            <li class="nav-item">
                                <a class="nav-link active" data-bs-toggle="tab" href="#login_email_tab">Email</a>
                            </li>
                            <li class="nav-item">
                                <a class="nav-link" data-bs-toggle="tab" href="#login_username_tab">User Name</a>
                            </li>
                        </ul>
                        <div class="tab-content">
                            <div class="tab-pane fade show active" id="login_email_tab">
                                <label asp-for="Email" class="form-label">Email</label>
                                <input asp-for="Email" class="form-control bg-transparent" placeholder="Email" autofocus />
                                <span asp-validation-for="Email" class="form_validation_feedback"></span>
                            </div>
                            <div class="tab-pane fade" id="login_username_tab">
                                <label asp-for="UserName" class="form-label">User Name</label>
                                <input asp-for="UserName" class="form-control bg-transparent" placeholder="User Name" />
                                <span asp-validation-for="UserName" class="form_validation_feedback"></span>
                            </div>
                        </div>
                    </div>

                    <div class="fv-row mb-3">
                        <label asp-for="Password" class="form-label">Password</label>
                        <input asp-for="Password" type="password" class="form-control bg-transparent" placeholder="Password" autocomplete="off" />
                        <span asp-validation-for="Password" class="form_validation_feedback"></span>
                    </div>

                    <div class="fv-row mb-8">
                        <div class="form-check mb-0">
                            <input asp-for="RememberMe" class="form-check-input" type="checkbox" />
                            <label asp-for="RememberMe" class="form-check-label">Remember me?</label>
                        </div>
                    </div>

                    @* <div class="d-flex flex-stack flex-wrap gap-3 fs-base fw-semibold mb-8">
						<div></div>
						<a href="authentication/layouts/creative/reset-password.html" class="link-primary">Forgot Password ?</a>
					 </div> *@

					<div asp-validation-summary="All" class="text-danger"></div>

                    <div class="d-grid mb-10">
                        <button type="submit" id="kt_sign_in_submit" class="btn btn-primary">
                            <span class="indicator-label">Login</span>
                        </button>
                    </div>

                    <div class="text-gray-500 text-center fw-semibold fs-6">
						Not a Member yet?
						<a asp-controller="Account" asp-action="SignUp" class="link-primary">Sign up</a>
					</div>
                </form>
            </div>

			@* <div class="d-flex flex-stack px-lg-10">
				<div class="me-0">
					<button class="btn btn-flex btn-link btn-color-gray-700 btn-active-color-primary rotate fs-base" data-kt-menu-trigger="click" data-kt-menu-placement="bottom-start" data-kt-menu-offset="0px, 0px">
						<img data-kt-element="current-lang-flag" class="w-20px h-20px rounded me-3" src="assets/media/flags/united-states.svg" alt="" />
						<span data-kt-element="current-lang-name" class="me-1">English</span>
						<i class="ki-duotone ki-down fs-5 text-muted rotate-180 m-0"></i>
					</button>
					<div class="menu menu-sub menu-sub-dropdown menu-column menu-rounded menu-gray-800 menu-state-bg-light-primary fw-semibold w-200px py-4 fs-7" data-kt-menu="true" id="kt_auth_lang_menu">
						<div class="menu-item px-3">
							<a href="#" class="menu-link d-flex px-5" data-kt-lang="English">
								<span class="symbol symbol-20px me-4">
									<img data-kt-element="lang-flag" class="rounded-1" src="assets/media/flags/united-states.svg" alt="" />
								</span>
								<span data-kt-element="lang-name">English</span>
							</a>
						</div> 
						<div class="menu-item px-3">
							<a href="#" class="menu-link d-flex px-5" data-kt-lang="Spanish">
								<span class="symbol symbol-20px me-4">
									<img data-kt-element="lang-flag" class="rounded-1" src="assets/media/flags/spain.svg" alt="" />
								</span>
								<span data-kt-element="lang-name">Spanish</span>
							</a>
						</div> 
						<div class="menu-item px-3">
							<a href="#" class="menu-link d-flex px-5" data-kt-lang="German">
								<span class="symbol symbol-20px me-4">
									<img data-kt-element="lang-flag" class="rounded-1" src="assets/media/flags/germany.svg" alt="" />
								</span>
								<span data-kt-element="lang-name">German</span>
							</a>
						</div> 
						<div class="menu-item px-3">
							<a href="#" class="menu-link d-flex px-5" data-kt-lang="Japanese">
								<span class="symbol symbol-20px me-4">
									<img data-kt-element="lang-flag" class="rounded-1" src="assets/media/flags/japan.svg" alt="" />
								</span>
								<span data-kt-element="lang-name">Japanese</span>
							</a>
						</div> 
						<div class="menu-item px-3">
							<a href="#" class="menu-link d-flex px-5" data-kt-lang="French">
								<span class="symbol symbol-20px me-4">
									<img data-kt-element="lang-flag" class="rounded-1" src="assets/media/flags/france.svg" alt="" />
								</span>
								<span data-kt-element="lang-name">French</span>
							</a>
						</div> 
					</div> 
				</div> 
				<div class="d-flex fw-semibold text-primary fs-base gap-5">
					<a href="pages/team.html" target="_blank">Terms</a>
					<a href="pages/pricing/column.html" target="_blank">Plans</a>
					<a href="pages/contact.html" target="_blank">Contact Us</a>
				</div> 
			</div> *@
        </div>
    </div>
</div>

<script>
    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-bs-toggle="tab"]').forEach(function (tab) {
            tab.addEventListener('shown.bs.tab', function (e) {
                var target = e.target.getAttribute('href');
                if (target === '#login_email_tab') {
                    document.querySelector('#login_username_tab input')?.setAttribute('value', '');
                    document.querySelector('#login_username_tab input').value = '';
                } else if (target === '#login_username_tab') {
                    document.querySelector('#login_email_tab input')?.setAttribute('value', '');
                    document.querySelector('#login_email_tab input').value = '';
                }
            });
        });
    });
</script>