@using {{ webui_project_name }}.Models.UI
@model Breadcrum

@if (Model != null){
	<div id="kt_app_toolbar" class="app-toolbar py-3 py-lg-6">
		<div id="kt_app_toolbar_container" class="app-container container-fluid d-flex flex-stack">
			<div class="page-title d-flex flex-column justify-content-center flex-wrap me-3">
				@if(!string.IsNullOrWhiteSpace(Model.Title))
				{
					<h1 class="page-heading d-flex text-gray-900 fw-bold fs-3 flex-column justify-content-center my-0">@Model.Title</h1>
				}
				<ul class="breadcrumb breadcrumb-separatorless fw-semibold fs-7 my-0 pt-1">
					@if(Model.BreadcrumbItems != null)
					{
						@foreach (var item in Model.BreadcrumbItems)
						{
							<li class="breadcrumb-item text-muted">
								<a href="@item.Path" class="text-muted text-hover-primary">@item.Title</a>
							</li>
							<li class="breadcrumb-item">
								<span class="bullet bg-gray-500 w-5px h-2px"></span>
							</li>
						}
					}
					<li class="breadcrumb-item text-muted">@Model.PageName</li>
				</ul>
			</div>
			@* <div class="d-flex align-items-center gap-2 gap-lg-3">
				<a href="#" class="btn btn-sm fw-bold btn-secondary" data-bs-toggle="modal" data-bs-target="#kt_modal_create_app">Rollover</a>
				<a href="#" class="btn btn-sm fw-bold btn-primary" data-bs-toggle="modal" data-bs-target="#kt_modal_new_target">Add Target</a>
			</div> *@
		</div>
	</div>
}