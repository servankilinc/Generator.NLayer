@model {{ webui_project_name }}.Models.Auth.SignUpRequest
@{
    Layout = "_LayoutBase";
    ViewData["Title"] = "SignUp";
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

            <div class="d-flex flex-center flex-column flex-column-fluid px-lg-10 pb-0">
                <form class="form w-100" asp-controller="Account" asp-action="SignUp" method="post">
                    <div class="text-center mb-11">
                        <h1 class="text-gray-900 fw-bolder mb-3">Sign Up</h1>
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
                        <label asp-for="FirstName" class="form-label">First Name</label>
                        <input asp-for="FirstName" class="form-control bg-transparent" placeholder="First Name" type="text" autofocus />
                        <span asp-validation-for="FirstName" class="form_validation_feedback"></span>
                    </div>

                    <div class="fv-row mb-8">
                        <label asp-for="LastName" class="form-label">Last Name</label>
                        <input asp-for="LastName" class="form-control bg-transparent" placeholder="Last Name" autofocus />
                        <span asp-validation-for="LastName" class="form_validation_feedback"></span>
                    </div>

                    <div class="fv-row mb-8">
                        <label asp-for="Email" class="form-label">Email</label>
                        <input asp-for="Email" class="form-control bg-transparent" placeholder="Email" autofocus />
                        <span asp-validation-for="Email" class="form_validation_feedback"></span>
                    </div>

                    <div class="fv-row mb-8">
                        <label asp-for="UserName" class="form-label">User Name</label>
                        <input asp-for="UserName" class="form-control bg-transparent" placeholder="User Name" autofocus />
                        <span asp-validation-for="UserName" class="form_validation_feedback"></span>
                    </div>

                    <div class="fv-row mb-8" data-kt-password-meter="true">
                        <div class="mb-1">
                            <div class="position-relative mb-3">
                                <label asp-for="Password" class="form-label">Password</label>
                                <input asp-for="Password" type="password" class="form-control bg-transparent" placeholder="Password" autocomplete="off" />
                                <span asp-validation-for="Password" class="form_validation_feedback"></span>

                                <span class="btn btn-sm btn-icon position-absolute translate-middle top-50 end-0 me-n2" data-kt-password-meter-control="visibility">
                                    <i class="ki-duotone ki-eye-slash fs-2"></i>
                                    <i class="ki-duotone ki-eye fs-2 d-none"></i>
                                </span>
                            </div>

                            <div class="d-flex align-items-center mb-3" data-kt-password-meter-control="highlight">
                                <div class="flex-grow-1 bg-secondary bg-active-success rounded h-5px me-2"></div>
                                <div class="flex-grow-1 bg-secondary bg-active-success rounded h-5px me-2"></div>
                                <div class="flex-grow-1 bg-secondary bg-active-success rounded h-5px me-2"></div>
                                <div class="flex-grow-1 bg-secondary bg-active-success rounded h-5px"></div>
                            </div>
                        </div>

                        <!--begin::Hint-->
                        <div class="text-muted">Use 8 or more characters with a mix of letters, numbers & symbols.</div>
                        <!--end::Hint-->
                    </div>

                    @*         <div class="fv-row mb-8">
                        <input placeholder="Repeat Password" type="password" autocomplete="off" class="form-control bg-transparent" />
                    </div> *@


                    @* <div class="fv-row mb-8">
                        <label class="form-check form-check-inline">
                            <input class="form-check-input" type="checkbox" name="toc" value="1" />
                            <span class="form-check-label fw-semibold text-gray-700 fs-base ms-1">
                                I Accept the
                                <a href="#" class="ms-1 link-primary">Terms</a>
                            </span>
                        </label>
                    </div> *@
                    
                    <div asp-validation-summary="All" class="text-danger"></div>

                    <div class="d-grid mb-10">
                        <button type="submit" class="btn btn-primary">
                            Sign up
                        </button>
                    </div>

                    <div class="text-gray-500 text-center fw-semibold fs-6">
                        Already have an Account?
                        <a asp-controller="Account" asp-action="Login" class="link-primary fw-semibold">Login instead</a>
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
