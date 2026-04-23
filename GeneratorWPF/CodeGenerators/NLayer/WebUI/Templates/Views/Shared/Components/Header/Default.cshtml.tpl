@model List<LocalizationItem>
@{
	LocalizationItem activeLocalization = Model.First(f => f.IsActive); 
}

<div id="kt_app_header" class="app-header" data-kt-sticky="true" data-kt-sticky-activate="{default: true, lg: true}" data-kt-sticky-name="app-header-minimize" data-kt-sticky-offset="{default: '200px', lg: '0'}" data-kt-sticky-animation="false">
	<!--begin::Header container-->
	<div class="app-container container-fluid d-flex align-items-stretch justify-content-between" id="kt_app_header_container">
		<!--begin::Sidebar mobile toggle-->
		<div class="d-flex align-items-center d-lg-none ms-n3 me-1 me-md-2" title="Show sidebar menu">
			<div class="btn btn-icon btn-active-color-primary w-35px h-35px" id="kt_app_sidebar_mobile_toggle">
				<i class="ki-duotone ki-abstract-14 fs-2 fs-md-1">
					<span class="path1"></span>
					<span class="path2"></span>
				</i>
			</div>
		</div>
		<!--end::Sidebar mobile toggle-->
		<!--begin::Mobile logo-->
		<div class="d-flex align-items-center flex-grow-1 flex-lg-grow-0">
			<a href="index.html" class="d-lg-none">
				<img alt="Logo" src="~/media/logos/default-small.svg" class="h-30px" />
			</a>
		</div>
		<!--end::Mobile logo-->
		<!--begin::Header wrapper-->
		<div class="d-flex align-items-stretch justify-content-between flex-lg-grow-1" id="kt_app_header_wrapper">
			<!--begin::Menu wrapper-->
			<div class="app-header-menu app-header-mobile-drawer align-items-stretch" data-kt-drawer="true" data-kt-drawer-name="app-header-menu" data-kt-drawer-activate="{default: true, lg: false}" data-kt-drawer-overlay="true" data-kt-drawer-width="250px" data-kt-drawer-direction="end" data-kt-drawer-toggle="#kt_app_header_menu_toggle" data-kt-swapper="true" data-kt-swapper-mode="{default: 'append', lg: 'prepend'}" data-kt-swapper-parent="{default: '#kt_app_body', lg: '#kt_app_header_wrapper'}">
				<!--begin::Menu-->
				<div class="menu menu-rounded menu-column menu-lg-row my-5 my-lg-0 align-items-stretch fw-semibold px-2 px-lg-0" id="kt_app_header_menu" data-kt-menu="true">
					<!--begin:Menu item-->
					<div data-kt-menu-trigger="{default: 'click', lg: 'hover'}" data-kt-menu-placement="bottom-start" class="menu-item here show menu-here-bg menu-lg-down-accordion me-0 me-lg-2">
						<!--begin:Menu link-->
						<span class="menu-link">
							<span class="menu-title">Dashboards</span>
							<span class="menu-arrow d-lg-none"></span>
						</span>
						<!--end:Menu link-->
						<!--begin:Menu sub-->
						<div class="menu-sub menu-sub-lg-down-accordion menu-sub-lg-dropdown p-0 w-100 w-lg-850px">
							<!--begin:Dashboards menu-->
							<div class="menu-state-bg menu-extended overflow-hidden overflow-lg-visible" data-kt-menu-dismiss="true">
								<!--begin:Row-->
								<div class="row">
									<!--begin:Col-->
									<div class="col-lg-8 mb-3 mb-lg-0 py-3 px-3 py-lg-6 px-lg-6">
										<!--begin:Row-->
										<div class="row">
											<!--begin:Col-->
											<div class="col-lg-6 mb-3">
												<!--begin:Menu item-->
												<div class="menu-item p-0 m-0">
													<!--begin:Menu link-->
													<a href="index.html" class="menu-link active">
														<span class="menu-custom-icon d-flex flex-center flex-shrink-0 rounded w-40px h-40px me-3">
															<i class="ki-duotone ki-element-11 text-primary fs-1">
																<span class="path1"></span>
																<span class="path2"></span>
																<span class="path3"></span>
																<span class="path4"></span>
															</i>
														</span>
														<span class="d-flex flex-column">
															<span class="fs-6 fw-bold text-gray-800">Default</span>
															<span class="fs-7 fw-semibold text-muted">Reports & statistics</span>
														</span>
													</a>
													<!--end:Menu link-->
												</div>
												<!--end:Menu item-->
											</div>
											<!--end:Col-->
											<!--begin:Col-->
											<div class="col-lg-6 mb-3">
												<!--begin:Menu item-->
												<div class="menu-item p-0 m-0">
													<!--begin:Menu link-->
													<a href="dashboards/ecommerce.html" class="menu-link">
														<span class="menu-custom-icon d-flex flex-center flex-shrink-0 rounded w-40px h-40px me-3">
															<i class="ki-duotone ki-basket text-danger fs-1">
																<span class="path1"></span>
																<span class="path2"></span>
																<span class="path3"></span>
																<span class="path4"></span>
															</i>
														</span>
														<span class="d-flex flex-column">
															<span class="fs-6 fw-bold text-gray-800">eCommerce</span>
															<span class="fs-7 fw-semibold text-muted">Sales reports</span>
														</span>
													</a>
													<!--end:Menu link-->
												</div>
												<!--end:Menu item-->
											</div>
											<!--end:Col-->
											<!--begin:Col-->
											<div class="col-lg-6 mb-3">
												<!--begin:Menu item-->
												<div class="menu-item p-0 m-0">
													<!--begin:Menu link-->
													<a href="dashboards/projects.html" class="menu-link">
														<span class="menu-custom-icon d-flex flex-center flex-shrink-0 rounded w-40px h-40px me-3">
															<i class="ki-duotone ki-abstract-44 text-info fs-1">
																<span class="path1"></span>
																<span class="path2"></span>
															</i>
														</span>
														<span class="d-flex flex-column">
															<span class="fs-6 fw-bold text-gray-800">Projects</span>
															<span class="fs-7 fw-semibold text-muted">Tasts, graphs & charts</span>
														</span>
													</a>
													<!--end:Menu link-->
												</div>
												<!--end:Menu item-->
											</div>
											<!--end:Col-->
											<!--begin:Col-->
											<div class="col-lg-6 mb-3">
												<!--begin:Menu item-->
												<div class="menu-item p-0 m-0">
													<!--begin:Menu link-->
													<a href="dashboards/online-courses.html" class="menu-link">
														<span class="menu-custom-icon d-flex flex-center flex-shrink-0 rounded w-40px h-40px me-3">
															<i class="ki-duotone ki-color-swatch text-success fs-1">
																<span class="path1"></span>
																<span class="path2"></span>
																<span class="path3"></span>
																<span class="path4"></span>
																<span class="path5"></span>
																<span class="path6"></span>
																<span class="path7"></span>
																<span class="path8"></span>
																<span class="path9"></span>
																<span class="path10"></span>
																<span class="path11"></span>
																<span class="path12"></span>
																<span class="path13"></span>
																<span class="path14"></span>
																<span class="path15"></span>
																<span class="path16"></span>
																<span class="path17"></span>
																<span class="path18"></span>
																<span class="path19"></span>
																<span class="path20"></span>
																<span class="path21"></span>
															</i>
														</span>
														<span class="d-flex flex-column">
															<span class="fs-6 fw-bold text-gray-800">Online Courses</span>
															<span class="fs-7 fw-semibold text-muted">Student progress</span>
														</span>
													</a>
													<!--end:Menu link-->
												</div>
												<!--end:Menu item-->
											</div>
											<!--end:Col-->
											<!--begin:Col-->
											<div class="col-lg-6 mb-3">
												<!--begin:Menu item-->
												<div class="menu-item p-0 m-0">
													<!--begin:Menu link-->
													<a href="dashboards/marketing.html" class="menu-link">
														<span class="menu-custom-icon d-flex flex-center flex-shrink-0 rounded w-40px h-40px me-3">
															<i class="ki-duotone ki-chart-simple text-gray-900 fs-1">
																<span class="path1"></span>
																<span class="path2"></span>
																<span class="path3"></span>
																<span class="path4"></span>
															</i>
														</span>
														<span class="d-flex flex-column">
															<span class="fs-6 fw-bold text-gray-800">Marketing</span>
															<span class="fs-7 fw-semibold text-muted">Campaings & conversions</span>
														</span>
													</a>
													<!--end:Menu link-->
												</div>
												<!--end:Menu item-->
											</div>
											<!--end:Col-->
											<!--begin:Col-->
											<div class="col-lg-6 mb-3">
												<!--begin:Menu item-->
												<div class="menu-item p-0 m-0">
													<!--begin:Menu link-->
													<a href="dashboards/bidding.html" class="menu-link">
														<span class="menu-custom-icon d-flex flex-center flex-shrink-0 rounded w-40px h-40px me-3">
															<i class="ki-duotone ki-switch text-warning fs-1">
																<span class="path1"></span>
																<span class="path2"></span>
															</i>
														</span>
														<span class="d-flex flex-column">
															<span class="fs-6 fw-bold text-gray-800">Bidding</span>
															<span class="fs-7 fw-semibold text-muted">Campaings & conversions</span>
														</span>
													</a>
													<!--end:Menu link-->
												</div>
												<!--end:Menu item-->
											</div>
											<!--end:Col-->
											<!--begin:Col-->
											<div class="col-lg-6 mb-3">
												<!--begin:Menu item-->
												<div class="menu-item p-0 m-0">
													<!--begin:Menu link-->
													<a href="dashboards/pos.html" class="menu-link">
														<span class="menu-custom-icon d-flex flex-center flex-shrink-0 rounded w-40px h-40px me-3">
															<i class="ki-duotone ki-abstract-42 text-danger fs-1">
																<span class="path1"></span>
																<span class="path2"></span>
															</i>
														</span>
														<span class="d-flex flex-column">
															<span class="fs-6 fw-bold text-gray-800">POS System</span>
															<span class="fs-7 fw-semibold text-muted">Campaings & conversions</span>
														</span>
													</a>
													<!--end:Menu link-->
												</div>
												<!--end:Menu item-->
											</div>
											<!--end:Col-->
											<!--begin:Col-->
											<div class="col-lg-6 mb-3">
												<!--begin:Menu item-->
												<div class="menu-item p-0 m-0">
													<!--begin:Menu link-->
													<a href="dashboards/call-center.html" class="menu-link">
														<span class="menu-custom-icon d-flex flex-center flex-shrink-0 rounded w-40px h-40px me-3">
															<i class="ki-duotone ki-call text-primary fs-1">
																<span class="path1"></span>
																<span class="path2"></span>
																<span class="path3"></span>
																<span class="path4"></span>
																<span class="path5"></span>
																<span class="path6"></span>
																<span class="path7"></span>
																<span class="path8"></span>
															</i>
														</span>
														<span class="d-flex flex-column">
															<span class="fs-6 fw-bold text-gray-800">Call Center</span>
															<span class="fs-7 fw-semibold text-muted">Campaings & conversions</span>
														</span>
													</a>
													<!--end:Menu link-->
												</div>
												<!--end:Menu item-->
											</div>
											<!--end:Col-->
										</div>
										<!--end:Row-->
										<div class="separator separator-dashed mx-5 my-5"></div>
										<!--begin:Landing-->
										<div class="d-flex flex-stack flex-wrap flex-lg-nowrap gap-2 mx-5">
											<div class="d-flex flex-column me-5">
												<div class="fs-6 fw-bold text-gray-800">Landing Page Template</div>
												<div class="fs-7 fw-semibold text-muted">Onpe page landing template with pricing & others</div>
											</div>
											<a href="landing.html" class="btn btn-sm btn-primary fw-bold">Explore</a>
										</div>
										<!--end:Landing-->
									</div>
									<!--end:Col-->
									<!--begin:Col-->
									<div class="menu-more bg-light col-lg-4 py-3 px-3 py-lg-6 px-lg-6 rounded-end">
										<!--begin:Heading-->
										<h4 class="fs-6 fs-lg-4 text-gray-800 fw-bold mt-3 mb-3 ms-4">More Dashboards</h4>
										<!--end:Heading-->
										<!--begin:Menu item-->
										<div class="menu-item p-0 m-0">
											<!--begin:Menu link-->
											<a href="dashboards/logistics.html" class="menu-link py-2">
												<span class="menu-title">Logistics</span>
											</a>
											<!--end:Menu link-->
										</div>
										<!--end:Menu item-->
										<!--begin:Menu item-->
										<div class="menu-item p-0 m-0">
											<!--begin:Menu link-->
											<a href="dashboards/website-analytics.html" class="menu-link py-2">
												<span class="menu-title">Website Analytics</span>
											</a>
											<!--end:Menu link-->
										</div>
										<!--end:Menu item-->
										<!--begin:Menu item-->
										<div class="menu-item p-0 m-0">
											<!--begin:Menu link-->
											<a href="dashboards/finance-performance.html" class="menu-link py-2">
												<span class="menu-title">Finance Performance</span>
											</a>
											<!--end:Menu link-->
										</div>
										<!--end:Menu item-->
										<!--begin:Menu item-->
										<div class="menu-item p-0 m-0">
											<!--begin:Menu link-->
											<a href="dashboards/store-analytics.html" class="menu-link py-2">
												<span class="menu-title">Store Analytics</span>
											</a>
											<!--end:Menu link-->
										</div>
										<!--end:Menu item-->
										<!--begin:Menu item-->
										<div class="menu-item p-0 m-0">
											<!--begin:Menu link-->
											<a href="dashboards/social.html" class="menu-link py-2">
												<span class="menu-title">Social</span>
											</a>
											<!--end:Menu link-->
										</div>
										<!--end:Menu item-->
										<!--begin:Menu item-->
										<div class="menu-item p-0 m-0">
											<!--begin:Menu link-->
											<a href="dashboards/delivery.html" class="menu-link py-2">
												<span class="menu-title">Delivery</span>
											</a>
											<!--end:Menu link-->
										</div>
										<!--end:Menu item-->
										<!--begin:Menu item-->
										<div class="menu-item p-0 m-0">
											<!--begin:Menu link-->
											<a href="dashboards/crypto.html" class="menu-link py-2">
												<span class="menu-title">Crypto</span>
											</a>
											<!--end:Menu link-->
										</div>
										<!--end:Menu item-->
										<!--begin:Menu item-->
										<div class="menu-item p-0 m-0">
											<!--begin:Menu link-->
											<a href="dashboards/school.html" class="menu-link py-2">
												<span class="menu-title">School</span>
											</a>
											<!--end:Menu link-->
										</div>
										<!--end:Menu item-->
										<!--begin:Menu item-->
										<div class="menu-item p-0 m-0">
											<!--begin:Menu link-->
											<a href="dashboards/podcast.html" class="menu-link py-2">
												<span class="menu-title">Podcast</span>
											</a>
											<!--end:Menu link-->
										</div>
										<!--end:Menu item-->
									</div>
									<!--end:Col-->
								</div>
								<!--end:Row-->
							</div>
							<!--end:Dashboards menu-->
						</div>
						<!--end:Menu sub-->
					</div>
					<!--end:Menu item-->
					<!--begin:Menu item-->
					<div data-kt-menu-trigger="{default: 'click', lg: 'hover'}" data-kt-menu-placement="bottom-start" class="menu-item menu-lg-down-accordion menu-sub-lg-down-indention me-0 me-lg-2">
						<!--begin:Menu link-->
						<span class="menu-link">
							<span class="menu-title">Apps</span>
							<span class="menu-arrow d-lg-none"></span>
						</span>
						<!--end:Menu link-->
						<!--begin:Menu sub-->
						<div class="menu-sub menu-sub-lg-down-accordion menu-sub-lg-dropdown px-lg-2 py-lg-4 w-lg-250px">

							<!--begin:Menu item-->
							<div data-kt-menu-trigger="{default:'click', lg: 'hover'}" data-kt-menu-placement="right-start" class="menu-item menu-lg-down-accordion">
								<!--begin:Menu link-->
								<span class="menu-link">
									<span class="menu-icon">
										<i class="ki-duotone ki-chart fs-2">
											<span class="path1"></span>
											<span class="path2"></span>
										</i>
									</span>
									<span class="menu-title">Support Center</span>
									<span class="menu-arrow"></span>
								</span>
								<!--end:Menu link-->
								<!--begin:Menu sub-->
								<div class="menu-sub menu-sub-lg-down-accordion menu-sub-lg-dropdown menu-active-bg px-lg-2 py-lg-4 w-lg-225px">
									<!--begin:Menu item-->
									<div class="menu-item">
										<!--begin:Menu link-->
										<a class="menu-link" href="apps/support-center/overview.html">
											<span class="menu-bullet">
												<span class="bullet bullet-dot"></span>
											</span>
											<span class="menu-title">Overview</span>
										</a>
										<!--end:Menu link-->
									</div>
									<!--end:Menu item-->
									<!--begin:Menu item-->
									<div data-kt-menu-trigger="{default:'click', lg: 'hover'}" data-kt-menu-placement="right-start" class="menu-item menu-lg-down-accordion">
										<!--begin:Menu link-->
										<span class="menu-link">
											<span class="menu-bullet">
												<span class="bullet bullet-dot"></span>
											</span>
											<span class="menu-title">Tickets</span>
											<span class="menu-arrow"></span>
										</span>
										<!--end:Menu link-->
										<!--begin:Menu sub-->
										<div class="menu-sub menu-sub-lg-down-accordion menu-sub-lg-dropdown menu-active-bg px-lg-2 py-lg-4 w-lg-225px">
											<!--begin:Menu item-->
											<div class="menu-item">
												<!--begin:Menu link-->
												<a class="menu-link" href="apps/support-center/tickets/list.html">
													<span class="menu-bullet">
														<span class="bullet bullet-dot"></span>
													</span>
													<span class="menu-title">Ticket List</span>
												</a>
												<!--end:Menu link-->
											</div>
											<!--end:Menu item-->
											<!--begin:Menu item-->
											<div class="menu-item">
												<!--begin:Menu link-->
												<a class="menu-link" href="apps/support-center/tickets/view.html">
													<span class="menu-bullet">
														<span class="bullet bullet-dot"></span>
													</span>
													<span class="menu-title">Ticket View</span>
												</a>
												<!--end:Menu link-->
											</div>
											<!--end:Menu item-->
										</div>
										<!--end:Menu sub-->
									</div>
									<!--end:Menu item-->
									<!--begin:Menu item-->
									<div data-kt-menu-trigger="{default:'click', lg: 'hover'}" data-kt-menu-placement="right-start" class="menu-item menu-lg-down-accordion">
										<!--begin:Menu link-->
										<span class="menu-link">
											<span class="menu-bullet">
												<span class="bullet bullet-dot"></span>
											</span>
											<span class="menu-title">Tutorials</span>
											<span class="menu-arrow"></span>
										</span>
										<!--end:Menu link-->
										<!--begin:Menu sub-->
										<div class="menu-sub menu-sub-lg-down-accordion menu-sub-lg-dropdown menu-active-bg px-lg-2 py-lg-4 w-lg-225px">
											<!--begin:Menu item-->
											<div class="menu-item">
												<!--begin:Menu link-->
												<a class="menu-link" href="apps/support-center/tutorials/list.html">
													<span class="menu-bullet">
														<span class="bullet bullet-dot"></span>
													</span>
													<span class="menu-title">Tutorials List</span>
												</a>
												<!--end:Menu link-->
											</div>
											<!--end:Menu item-->
											<!--begin:Menu item-->
											<div class="menu-item">
												<!--begin:Menu link-->
												<a class="menu-link" href="apps/support-center/tutorials/post.html">
													<span class="menu-bullet">
														<span class="bullet bullet-dot"></span>
													</span>
													<span class="menu-title">Tutorials Post</span>
												</a>
												<!--end:Menu link-->
											</div>
											<!--end:Menu item-->
										</div>
										<!--end:Menu sub-->
									</div>
									<!--end:Menu item-->
									<!--begin:Menu item-->
									<div class="menu-item">
										<!--begin:Menu link-->
										<a class="menu-link" href="apps/support-center/faq.html">
											<span class="menu-bullet">
												<span class="bullet bullet-dot"></span>
											</span>
											<span class="menu-title">FAQ</span>
										</a>
										<!--end:Menu link-->
									</div>
									<!--end:Menu item-->
									<!--begin:Menu item-->
									<div class="menu-item">
										<!--begin:Menu link-->
										<a class="menu-link" href="apps/support-center/licenses.html">
											<span class="menu-bullet">
												<span class="bullet bullet-dot"></span>
											</span>
											<span class="menu-title">Licenses</span>
										</a>
										<!--end:Menu link-->
									</div>
									<!--end:Menu item-->
									<!--begin:Menu item-->
									<div class="menu-item">
										<!--begin:Menu link-->
										<a class="menu-link" href="apps/support-center/contact.html">
											<span class="menu-bullet">
												<span class="bullet bullet-dot"></span>
											</span>
											<span class="menu-title">Contact Us</span>
										</a>
										<!--end:Menu link-->
									</div>
									<!--end:Menu item-->
								</div>
								<!--end:Menu sub-->
							</div>
							<!--end:Menu item-->
							<!--begin:Menu item-->
							<div data-kt-menu-trigger="{default:'click', lg: 'hover'}" data-kt-menu-placement="right-start" class="menu-item menu-lg-down-accordion">
								<!--begin:Menu link-->
								<span class="menu-link">
									<span class="menu-icon">
										<i class="ki-duotone ki-shield-tick fs-2">
											<span class="path1"></span>
											<span class="path2"></span>
										</i>
									</span>
									<span class="menu-title">User Management</span>
									<span class="menu-arrow"></span>
								</span>
								<!--end:Menu link-->
								<!--begin:Menu sub-->
								<div class="menu-sub menu-sub-lg-down-accordion menu-sub-lg-dropdown menu-active-bg px-lg-2 py-lg-4 w-lg-225px">
									<!--begin:Menu item-->
									<div data-kt-menu-trigger="{default:'click', lg: 'hover'}" data-kt-menu-placement="right-start" class="menu-item menu-lg-down-accordion">
										<!--begin:Menu link-->
										<span class="menu-link">
											<span class="menu-bullet">
												<span class="bullet bullet-dot"></span>
											</span>
											<span class="menu-title">Users</span>
											<span class="menu-arrow"></span>
										</span>
										<!--end:Menu link-->
										<!--begin:Menu sub-->
										<div class="menu-sub menu-sub-lg-down-accordion menu-sub-lg-dropdown menu-active-bg px-lg-2 py-lg-4 w-lg-225px">
											<!--begin:Menu item-->
											<div class="menu-item">
												<!--begin:Menu link-->
												<a class="menu-link" href="apps/user-management/users/list.html">
													<span class="menu-bullet">
														<span class="bullet bullet-dot"></span>
													</span>
													<span class="menu-title">Users List</span>
												</a>
												<!--end:Menu link-->
											</div>
											<!--end:Menu item-->
											<!--begin:Menu item-->
											<div class="menu-item">
												<!--begin:Menu link-->
												<a class="menu-link" href="apps/user-management/users/view.html">
													<span class="menu-bullet">
														<span class="bullet bullet-dot"></span>
													</span>
													<span class="menu-title">View User</span>
												</a>
												<!--end:Menu link-->
											</div>
											<!--end:Menu item-->
										</div>
										<!--end:Menu sub-->
									</div>
									<!--end:Menu item-->
									<!--begin:Menu item-->
									<div data-kt-menu-trigger="{default:'click', lg: 'hover'}" data-kt-menu-placement="right-start" class="menu-item menu-lg-down-accordion">
										<!--begin:Menu link-->
										<span class="menu-link">
											<span class="menu-bullet">
												<span class="bullet bullet-dot"></span>
											</span>
											<span class="menu-title">Roles</span>
											<span class="menu-arrow"></span>
										</span>
										<!--end:Menu link-->
										<!--begin:Menu sub-->
										<div class="menu-sub menu-sub-lg-down-accordion menu-sub-lg-dropdown menu-active-bg px-lg-2 py-lg-4 w-lg-225px">
											<!--begin:Menu item-->
											<div class="menu-item">
												<!--begin:Menu link-->
												<a class="menu-link" href="apps/user-management/roles/list.html">
													<span class="menu-bullet">
														<span class="bullet bullet-dot"></span>
													</span>
													<span class="menu-title">Roles List</span>
												</a>
												<!--end:Menu link-->
											</div>
											<!--end:Menu item-->
											<!--begin:Menu item-->
											<div class="menu-item">
												<!--begin:Menu link-->
												<a class="menu-link" href="apps/user-management/roles/view.html">
													<span class="menu-bullet">
														<span class="bullet bullet-dot"></span>
													</span>
													<span class="menu-title">View Roles</span>
												</a>
												<!--end:Menu link-->
											</div>
											<!--end:Menu item-->
										</div>
										<!--end:Menu sub-->
									</div>
									<!--end:Menu item-->
									<!--begin:Menu item-->
									<div class="menu-item">
										<!--begin:Menu link-->
										<a class="menu-link" href="apps/user-management/permissions.html">
											<span class="menu-bullet">
												<span class="bullet bullet-dot"></span>
											</span>
											<span class="menu-title">Permissions</span>
										</a>
										<!--end:Menu link-->
									</div>
									<!--end:Menu item-->
								</div>
								<!--end:Menu sub-->
							</div>
							<!--end:Menu item-->
							<!--begin:Menu item-->
							<div class="menu-item">
								<!--begin:Menu link-->
								<a class="menu-link" href="apps/calendar.html">
									<span class="menu-icon">
										<i class="ki-duotone ki-calendar-8 fs-2">
											<span class="path1"></span>
											<span class="path2"></span>
											<span class="path3"></span>
											<span class="path4"></span>
											<span class="path5"></span>
											<span class="path6"></span>
										</i>
									</span>
									<span class="menu-title">Calendar</span>
								</a>
								<!--end:Menu link-->
							</div>
							<!--end:Menu item-->
						</div>
						<!--end:Menu sub-->
					</div>
					<!--end:Menu item-->
				</div>
				<!--end::Menu-->
			</div>
			<!--end::Menu wrapper-->
			<!--begin::Navbar-->
			<div class="app-navbar flex-shrink-0">
				<!--begin::Search-->
				<div class="app-navbar-item align-items-stretch ms-1 ms-md-4">
					<!--begin::Search-->
					<div id="kt_header_search" class="header-search d-flex align-items-stretch" data-kt-search-keypress="true" data-kt-search-min-length="2" data-kt-search-enter="enter" data-kt-search-layout="menu" data-kt-menu-trigger="auto" data-kt-menu-overflow="false" data-kt-menu-permanent="true" data-kt-menu-placement="bottom-end">
						<!--begin::Search toggle-->
						<div class="d-flex align-items-center" data-kt-search-element="toggle" id="kt_header_search_toggle">
							<div class="btn btn-icon btn-custom btn-icon-muted btn-active-light btn-active-color-primary w-35px h-35px">
								<i class="ki-duotone ki-magnifier fs-2">
									<span class="path1"></span>
									<span class="path2"></span>
								</i>
							</div>
						</div>
						<!--end::Search toggle-->
						<!--begin::Menu-->
						<div data-kt-search-element="content" class="menu menu-sub menu-sub-dropdown p-7 w-325px w-md-375px">
							<!--begin::Wrapper-->
							<div data-kt-search-element="wrapper">
								<!--begin::Form-->
								<form data-kt-search-element="form" class="w-100 position-relative mb-3" autocomplete="off">
									<!--begin::Icon-->
									<i class="ki-duotone ki-magnifier fs-2 text-gray-500 position-absolute top-50 translate-middle-y ms-0">
										<span class="path1"></span>
										<span class="path2"></span>
									</i>
									<!--end::Icon-->
									<!--begin::Input-->
									<input type="text" class="search-input form-control form-control-flush ps-10" name="search" value="" placeholder="Search..." data-kt-search-element="input" />
									<!--end::Input-->
									<!--begin::Spinner-->
									<span class="search-spinner position-absolute top-50 end-0 translate-middle-y lh-0 d-none me-1" data-kt-search-element="spinner">
										<span class="spinner-border h-15px w-15px align-middle text-gray-500"></span>
									</span>
									<!--end::Spinner-->
									<!--begin::Reset-->
									<span class="search-reset btn btn-flush btn-active-color-primary position-absolute top-50 end-0 translate-middle-y lh-0 d-none" data-kt-search-element="clear">
										<i class="ki-duotone ki-cross fs-2 fs-lg-1 me-0">
											<span class="path1"></span>
											<span class="path2"></span>
										</i>
									</span>
									<!--end::Reset-->
									<!--begin::Toolbar-->
									<div class="position-absolute top-50 end-0 translate-middle-y" data-kt-search-element="toolbar">
										<!--begin::Preferences toggle-->
										<div data-kt-search-element="preferences-show" class="btn btn-icon w-20px btn-sm btn-active-color-primary me-1" data-bs-toggle="tooltip" title="Show search preferences">
											<i class="ki-duotone ki-setting-2 fs-2">
												<span class="path1"></span>
												<span class="path2"></span>
											</i>
										</div>
										<!--end::Preferences toggle-->
										<!--begin::Advanced search toggle-->
										<div data-kt-search-element="advanced-options-form-show" class="btn btn-icon w-20px btn-sm btn-active-color-primary" data-bs-toggle="tooltip" title="Show more search options">
											<i class="ki-duotone ki-down fs-2"></i>
										</div>
										<!--end::Advanced search toggle-->
									</div>
									<!--end::Toolbar-->
								</form>
								<!--end::Form-->
								<!--begin::Separator-->
								<div class="separator border-gray-200 mb-6"></div>
								<!--end::Separator-->
								<!--begin::Recently viewed-->
								<div data-kt-search-element="results" class="d-none">
									<!--begin::Items-->
									<div class="scroll-y mh-200px mh-lg-350px">
										<!--begin::Category title-->
										<h3 class="fs-5 text-muted m-0 pb-5" data-kt-search-element="category-title">Users</h3>
										<!--end::Category title-->
										<!--begin::Item-->
										<a href="#" class="d-flex text-gray-900 text-hover-primary align-items-center mb-5">
											<!--begin::Symbol-->
											<div class="symbol symbol-40px me-4">
												<img src="~/media/avatars/300-6.jpg" alt="" />
											</div>
											<!--end::Symbol-->
											<!--begin::Title-->
											<div class="d-flex flex-column justify-content-start fw-semibold">
												<span class="fs-6 fw-semibold">Karina Clark</span>
												<span class="fs-7 fw-semibold text-muted">Marketing Manager</span>
											</div>
											<!--end::Title-->
										</a>
										<!--end::Item-->
										<!--begin::Item-->
										<a href="#" class="d-flex text-gray-900 text-hover-primary align-items-center mb-5">
											<!--begin::Symbol-->
											<div class="symbol symbol-40px me-4">
												<img src="~/media/avatars/300-2.jpg" alt="" />
											</div>
											<!--end::Symbol-->
											<!--begin::Title-->
											<div class="d-flex flex-column justify-content-start fw-semibold">
												<span class="fs-6 fw-semibold">Olivia Bold</span>
												<span class="fs-7 fw-semibold text-muted">Software Engineer</span>
											</div>
											<!--end::Title-->
										</a>
										<!--end::Item-->
										<!--begin::Item-->
										<a href="#" class="d-flex text-gray-900 text-hover-primary align-items-center mb-5">
											<!--begin::Symbol-->
											<div class="symbol symbol-40px me-4">
												<img src="~/media/avatars/300-9.jpg" alt="" />
											</div>
											<!--end::Symbol-->
											<!--begin::Title-->
											<div class="d-flex flex-column justify-content-start fw-semibold">
												<span class="fs-6 fw-semibold">Ana Clark</span>
												<span class="fs-7 fw-semibold text-muted">UI/UX Designer</span>
											</div>
											<!--end::Title-->
										</a>
										<!--end::Item-->
									</div>
									<!--end::Items-->
								</div>
								<!--end::Recently viewed-->
								<!--begin::Recently viewed-->
								<div class="mb-5" data-kt-search-element="main">
									<!--begin::Heading-->
									<div class="d-flex flex-stack fw-semibold mb-4">
										<!--begin::Label-->
										<span class="text-muted fs-6 me-2">Recently Searched:</span>
										<!--end::Label-->
									</div>
									<!--end::Heading-->
									<!--begin::Items-->
									<div class="scroll-y mh-200px mh-lg-325px">
										<!--begin::Item-->
										<div class="d-flex align-items-center mb-5">
											<!--begin::Symbol-->
											<div class="symbol symbol-40px me-4">
												<span class="symbol-label bg-light">
													<i class="ki-duotone ki-laptop fs-2 text-primary">
														<span class="path1"></span>
														<span class="path2"></span>
													</i>
												</span>
											</div>
											<!--end::Symbol-->
											<!--begin::Title-->
											<div class="d-flex flex-column">
												<a href="#" class="fs-6 text-gray-800 text-hover-primary fw-semibold">BoomApp by Keenthemes</a>
												<span class="fs-7 text-muted fw-semibold">#45789</span>
											</div>
											<!--end::Title-->
										</div>
										<!--end::Item-->
										<!--begin::Item-->
										<div class="d-flex align-items-center mb-5">
											<!--begin::Symbol-->
											<div class="symbol symbol-40px me-4">
												<span class="symbol-label bg-light">
													<i class="ki-duotone ki-chart-simple fs-2 text-primary">
														<span class="path1"></span>
														<span class="path2"></span>
														<span class="path3"></span>
														<span class="path4"></span>
													</i>
												</span>
											</div>
											<!--end::Symbol-->
											<!--begin::Title-->
											<div class="d-flex flex-column">
												<a href="#" class="fs-6 text-gray-800 text-hover-primary fw-semibold">"Kept API Project Meeting</a>
												<span class="fs-7 text-muted fw-semibold">#84050</span>
											</div>
											<!--end::Title-->
										</div>
										<!--end::Item-->
										<!--begin::Item-->
										<div class="d-flex align-items-center mb-5">
											<!--begin::Symbol-->
											<div class="symbol symbol-40px me-4">
												<span class="symbol-label bg-light">
													<i class="ki-duotone ki-chart fs-2 text-primary">
														<span class="path1"></span>
														<span class="path2"></span>
													</i>
												</span>
											</div>
											<!--end::Symbol-->
											<!--begin::Title-->
											<div class="d-flex flex-column">
												<a href="#" class="fs-6 text-gray-800 text-hover-primary fw-semibold">"KPI Monitoring App Launch</a>
												<span class="fs-7 text-muted fw-semibold">#84250</span>
											</div>
											<!--end::Title-->
										</div>
										<!--end::Item-->
									</div>
									<!--end::Items-->
								</div>
								<!--end::Recently viewed-->
								<!--begin::Empty-->
								<div data-kt-search-element="empty" class="text-center d-none">
									<!--begin::Icon-->
									<div class="pt-10 pb-10">
										<i class="ki-duotone ki-search-list fs-4x opacity-50">
											<span class="path1"></span>
											<span class="path2"></span>
											<span class="path3"></span>
										</i>
									</div>
									<!--end::Icon-->
									<!--begin::Message-->
									<div class="pb-15 fw-semibold">
										<h3 class="text-gray-600 fs-5 mb-2">No result found</h3>
										<div class="text-muted fs-7">Please try again with a different query</div>
									</div>
									<!--end::Message-->
								</div>
								<!--end::Empty-->
							</div>
							<!--end::Wrapper-->
							<!--begin::Preferences-->
							<form data-kt-search-element="advanced-options-form" class="pt-1 d-none">
								<!--begin::Heading-->
								<h3 class="fw-semibold text-gray-900 mb-7">Advanced Search</h3>
								<!--end::Heading-->
								<!--begin::Input group-->
								<div class="mb-5">
									<input type="text" class="form-control form-control-sm form-control-solid" placeholder="Contains the word" name="query" />
								</div>
								<!--end::Input group-->
								<!--begin::Input group-->
								<div class="mb-5">
									<!--begin::Radio group-->
									<div class="nav-group nav-group-fluid">
										<!--begin::Option-->
										<label>
											<input type="radio" class="btn-check" name="type" value="has" checked="checked" />
											<span class="btn btn-sm btn-color-muted btn-active btn-active-primary">All</span>
										</label>
										<!--end::Option-->
										<!--begin::Option-->
										<label>
											<input type="radio" class="btn-check" name="type" value="users" />
											<span class="btn btn-sm btn-color-muted btn-active btn-active-primary px-4">Users</span>
										</label>
										<!--end::Option-->
										<!--begin::Option-->
										<label>
											<input type="radio" class="btn-check" name="type" value="orders" />
											<span class="btn btn-sm btn-color-muted btn-active btn-active-primary px-4">Orders</span>
										</label>
										<!--end::Option-->
										<!--begin::Option-->
										<label>
											<input type="radio" class="btn-check" name="type" value="projects" />
											<span class="btn btn-sm btn-color-muted btn-active btn-active-primary px-4">Projects</span>
										</label>
										<!--end::Option-->
									</div>
									<!--end::Radio group-->
								</div>
								<!--end::Input group-->
								<!--begin::Input group-->
								<div class="mb-5">
									<input type="text" name="assignedto" class="form-control form-control-sm form-control-solid" placeholder="Assigned to" value="" />
								</div>
								<!--end::Input group-->
								<!--begin::Input group-->
								<div class="mb-5">
									<input type="text" name="collaborators" class="form-control form-control-sm form-control-solid" placeholder="Collaborators" value="" />
								</div>
								<!--end::Input group-->
								<!--begin::Input group-->
								<div class="mb-5">
									<!--begin::Radio group-->
									<div class="nav-group nav-group-fluid">
										<!--begin::Option-->
										<label>
											<input type="radio" class="btn-check" name="attachment" value="has" checked="checked" />
											<span class="btn btn-sm btn-color-muted btn-active btn-active-primary">Has attachment</span>
										</label>
										<!--end::Option-->
										<!--begin::Option-->
										<label>
											<input type="radio" class="btn-check" name="attachment" value="any" />
											<span class="btn btn-sm btn-color-muted btn-active btn-active-primary px-4">Any</span>
										</label>
										<!--end::Option-->
									</div>
									<!--end::Radio group-->
								</div>
								<!--end::Input group-->
								<!--begin::Input group-->
								<div class="mb-5">
									<select name="timezone" aria-label="Select a Timezone" data-control="select2" data-dropdown-parent="#kt_header_search" data-placeholder="date_period" class="form-select form-select-sm form-select-solid">
										<option value="next">Within the next</option>
										<option value="last">Within the last</option>
										<option value="between">Between</option>
										<option value="on">On</option>
									</select>
								</div>
								<!--end::Input group-->
								<!--begin::Input group-->
								<div class="row mb-8">
									<!--begin::Col-->
									<div class="col-6">
										<input type="number" name="date_number" class="form-control form-control-sm form-control-solid" placeholder="Lenght" value="" />
									</div>
									<!--end::Col-->
									<!--begin::Col-->
									<div class="col-6">
										<select name="date_typer" aria-label="Select a Timezone" data-control="select2" data-dropdown-parent="#kt_header_search" data-placeholder="Period" class="form-select form-select-sm form-select-solid">
											<option value="days">Days</option>
											<option value="weeks">Weeks</option>
											<option value="months">Months</option>
											<option value="years">Years</option>
										</select>
									</div>
									<!--end::Col-->
								</div>
								<!--end::Input group-->
								<!--begin::Actions-->
								<div class="d-flex justify-content-end">
									<button type="reset" class="btn btn-sm btn-light fw-bold btn-active-light-primary me-2" data-kt-search-element="advanced-options-form-cancel">Cancel</button>
									<a href="utilities/search/horizontal.html" class="btn btn-sm fw-bold btn-primary" data-kt-search-element="advanced-options-form-search">Search</a>
								</div>
								<!--end::Actions-->
							</form>
							<!--end::Preferences-->
							<!--begin::Preferences-->
							<form data-kt-search-element="preferences" class="pt-1 d-none">
								<!--begin::Heading-->
								<h3 class="fw-semibold text-gray-900 mb-7">Search Preferences</h3>
								<!--end::Heading-->
								<!--begin::Input group-->
								<div class="pb-4 border-bottom">
									<label class="form-check form-switch form-switch-sm form-check-custom form-check-solid flex-stack">
										<span class="form-check-label text-gray-700 fs-6 fw-semibold ms-0 me-2">Projects</span>
										<input class="form-check-input" type="checkbox" value="1" checked="checked" />
									</label>
								</div>
								<!--end::Input group-->
								<!--begin::Input group-->
								<div class="py-4 border-bottom">
									<label class="form-check form-switch form-switch-sm form-check-custom form-check-solid flex-stack">
										<span class="form-check-label text-gray-700 fs-6 fw-semibold ms-0 me-2">Targets</span>
										<input class="form-check-input" type="checkbox" value="1" checked="checked" />
									</label>
								</div>
								<!--end::Input group-->
								<!--begin::Input group-->
								<div class="py-4 border-bottom">
									<label class="form-check form-switch form-switch-sm form-check-custom form-check-solid flex-stack">
										<span class="form-check-label text-gray-700 fs-6 fw-semibold ms-0 me-2">Affiliate Programs</span>
										<input class="form-check-input" type="checkbox" value="1" />
									</label>
								</div>
								<!--end::Input group-->
								<!--begin::Input group-->
								<div class="py-4 border-bottom">
									<label class="form-check form-switch form-switch-sm form-check-custom form-check-solid flex-stack">
										<span class="form-check-label text-gray-700 fs-6 fw-semibold ms-0 me-2">Referrals</span>
										<input class="form-check-input" type="checkbox" value="1" checked="checked" />
									</label>
								</div>
								<!--end::Input group-->
								<!--begin::Input group-->
								<div class="py-4 border-bottom">
									<label class="form-check form-switch form-switch-sm form-check-custom form-check-solid flex-stack">
										<span class="form-check-label text-gray-700 fs-6 fw-semibold ms-0 me-2">Users</span>
										<input class="form-check-input" type="checkbox" value="1" />
									</label>
								</div>
								<!--end::Input group-->
								<!--begin::Actions-->
								<div class="d-flex justify-content-end pt-7">
									<button type="reset" class="btn btn-sm btn-light fw-bold btn-active-light-primary me-2" data-kt-search-element="preferences-dismiss">Cancel</button>
									<button type="submit" class="btn btn-sm fw-bold btn-primary">Save Changes</button>
								</div>
								<!--end::Actions-->
							</form>
							<!--end::Preferences-->
						</div>
						<!--end::Menu-->
					</div>
					<!--end::Search-->
				</div>
				<!--end::Search-->
				<!--begin::Activities-->
				<div class="app-navbar-item ms-1 ms-md-4">
					<!--begin::Drawer toggle-->
					<div class="btn btn-icon btn-custom btn-icon-muted btn-active-light btn-active-color-primary w-35px h-35px" id="kt_activities_toggle">
						<i class="ki-duotone ki-messages fs-2">
							<span class="path1"></span>
							<span class="path2"></span>
							<span class="path3"></span>
							<span class="path4"></span>
							<span class="path5"></span>
						</i>
					</div>
					<!--end::Drawer toggle-->
				</div>
				<!--end::Activities-->
 
				<!--begin::Chat-->
				<div class="app-navbar-item ms-1 ms-md-4">
					<!--begin::Menu wrapper-->
					<div class="btn btn-icon btn-custom btn-icon-muted btn-active-light btn-active-color-primary w-35px h-35px position-relative" id="kt_drawer_chat_toggle">
						<i class="ki-duotone ki-message-text-2 fs-2">
							<span class="path1"></span>
							<span class="path2"></span>
							<span class="path3"></span>
						</i>
						<span class="bullet bullet-dot bg-success h-6px w-6px position-absolute translate-middle top-0 start-50 animation-blink"></span>
					</div>
					<!--end::Menu wrapper-->
				</div>
				<!--end::Chat-->
 
				<!--begin::Theme mode-->
				<div class="app-navbar-item ms-1 ms-md-4">
					<!--begin::Menu toggle-->
					<a href="#" class="btn btn-icon btn-custom btn-icon-muted btn-active-light btn-active-color-primary w-35px h-35px" data-kt-menu-trigger="{default:'click', lg: 'hover'}" data-kt-menu-attach="parent" data-kt-menu-placement="bottom-end">
						<i class="ki-duotone ki-night-day theme-light-show fs-1">
							<span class="path1"></span>
							<span class="path2"></span>
							<span class="path3"></span>
							<span class="path4"></span>
							<span class="path5"></span>
							<span class="path6"></span>
							<span class="path7"></span>
							<span class="path8"></span>
							<span class="path9"></span>
							<span class="path10"></span>
						</i>
						<i class="ki-duotone ki-moon theme-dark-show fs-1">
							<span class="path1"></span>
							<span class="path2"></span>
						</i>
					</a>
					<!--begin::Menu toggle-->
					<!--begin::Menu-->
					<div class="menu menu-sub menu-sub-dropdown menu-column menu-rounded menu-title-gray-700 menu-icon-gray-500 menu-active-bg menu-state-color fw-semibold py-4 fs-base w-150px" data-kt-menu="true" data-kt-element="theme-mode-menu">
						<!--begin::Menu item-->
						<div class="menu-item px-3 my-0">
							<a href="#" class="menu-link px-3 py-2" data-kt-element="mode" data-kt-value="light">
								<span class="menu-icon" data-kt-element="icon">
									<i class="ki-duotone ki-night-day fs-2">
										<span class="path1"></span>
										<span class="path2"></span>
										<span class="path3"></span>
										<span class="path4"></span>
										<span class="path5"></span>
										<span class="path6"></span>
										<span class="path7"></span>
										<span class="path8"></span>
										<span class="path9"></span>
										<span class="path10"></span>
									</i>
								</span>
								<span class="menu-title">Light</span>
							</a>
						</div>
						<!--end::Menu item-->
						<!--begin::Menu item-->
						<div class="menu-item px-3 my-0">
							<a href="#" class="menu-link px-3 py-2" data-kt-element="mode" data-kt-value="dark">
								<span class="menu-icon" data-kt-element="icon">
									<i class="ki-duotone ki-moon fs-2">
										<span class="path1"></span>
										<span class="path2"></span>
									</i>
								</span>
								<span class="menu-title">Dark</span>
							</a>
						</div>
						<!--end::Menu item-->
						<!--begin::Menu item-->
						<div class="menu-item px-3 my-0">
							<a href="#" class="menu-link px-3 py-2" data-kt-element="mode" data-kt-value="system">
								<span class="menu-icon" data-kt-element="icon">
									<i class="ki-duotone ki-screen fs-2">
										<span class="path1"></span>
										<span class="path2"></span>
										<span class="path3"></span>
										<span class="path4"></span>
									</i>
								</span>
								<span class="menu-title">System</span>
							</a>
						</div>
						<!--end::Menu item-->
					</div>
					<!--end::Menu-->
				</div>
				<!--end::Theme mode-->
				<!--begin::User menu-->
				<div class="app-navbar-item ms-1 ms-md-4" id="kt_header_user_menu_toggle">
					<!--begin::Menu wrapper-->
					<div class="cursor-pointer symbol symbol-35px" data-kt-menu-trigger="{default: 'click', lg: 'hover'}" data-kt-menu-attach="parent" data-kt-menu-placement="bottom-end">
						<img src="~/media/avatars/300-3.jpg" class="rounded-3" alt="user" />
					</div>
					<!--begin::User account menu-->
					<div class="menu menu-sub menu-sub-dropdown menu-column menu-rounded menu-gray-800 menu-state-bg menu-state-color fw-semibold py-4 fs-6 w-275px" data-kt-menu="true">
						<!--begin::Menu item-->
						<div class="menu-item px-3">
							<div class="menu-content d-flex align-items-center px-3">
								<!--begin::Avatar-->
								<div class="symbol symbol-50px me-5">
									<img alt="Logo" src="~/media/avatars/300-3.jpg" />
								</div>
								<!--end::Avatar-->
								<!--begin::Username-->
								<div class="d-flex flex-column">
									<div class="fw-bold d-flex align-items-center fs-5">
										Robert Fox
										<span class="badge badge-light-success fw-bold fs-8 px-2 py-1 ms-2">Pro</span>
									</div>
									<a href="#" class="fw-semibold text-muted text-hover-primary fs-7">robert@kt.com</a>
								</div>
								<!--end::Username-->
							</div>
						</div>
						<!--end::Menu item-->
						<!--begin::Menu separator-->
						<div class="separator my-2"></div>
						<!--end::Menu separator-->
						<!--begin::Menu item-->
						<div class="menu-item px-5">
							<a href="account/overview.html" class="menu-link px-5">My Profile</a>
						</div>
						<!--end::Menu item-->
						<!--begin::Menu item-->
						<div class="menu-item px-5">
							<a href="apps/projects/list.html" class="menu-link px-5">
								<span class="menu-text">My Projects</span>
								<span class="menu-badge">
									<span class="badge badge-light-danger badge-circle fw-bold fs-7">3</span>
								</span>
							</a>
						</div>
						<!--end::Menu item-->
						<!--begin::Menu item-->
						<div class="menu-item px-5" data-kt-menu-trigger="{default: 'click', lg: 'hover'}" data-kt-menu-placement="left-start" data-kt-menu-offset="-15px, 0">
							<a href="#" class="menu-link px-5">
								<span class="menu-title">My Subscription</span>
								<span class="menu-arrow"></span>
							</a>
							<!--begin::Menu sub-->
							<div class="menu-sub menu-sub-dropdown w-175px py-4">
								<!--begin::Menu item-->
								<div class="menu-item px-3">
									<a href="account/referrals.html" class="menu-link px-5">Referrals</a>
								</div>
								<!--end::Menu item-->
								<!--begin::Menu item-->
								<div class="menu-item px-3">
									<a href="account/billing.html" class="menu-link px-5">Billing</a>
								</div>
								<!--end::Menu item-->
								<!--begin::Menu item-->
								<div class="menu-item px-3">
									<a href="account/statements.html" class="menu-link px-5">Payments</a>
								</div>
								<!--end::Menu item-->
					 
								<!--begin::Menu separator-->
								<div class="separator my-2"></div>
								<!--end::Menu separator-->
								<!--begin::Menu item-->
								<div class="menu-item px-3">
									<div class="menu-content px-3">
										<label class="form-check form-switch form-check-custom form-check-solid">
											<input class="form-check-input w-30px h-20px" type="checkbox" value="1" checked="checked" name="notifications" />
											<span class="form-check-label text-muted fs-7">Notifications</span>
										</label>
									</div>
								</div>
								<!--end::Menu item-->
							</div>
							<!--end::Menu sub-->
						</div>
						<!--end::Menu item-->
						<!--begin::Menu separator-->
						<div class="separator my-2"></div>
						<!--end::Menu separator-->
						<!--begin::Menu item-->
						<div class="menu-item px-5" data-kt-menu-trigger="{default: 'click', lg: 'hover'}" data-kt-menu-placement="left-start" data-kt-menu-offset="-15px, 0">
							<a href="#" class="menu-link px-5">
								<span class="menu-title position-relative">
									Mode
									<span class="ms-5 position-absolute translate-middle-y top-50 end-0">
										<i class="ki-duotone ki-night-day theme-light-show fs-2">
											<span class="path1"></span>
											<span class="path2"></span>
											<span class="path3"></span>
											<span class="path4"></span>
											<span class="path5"></span>
											<span class="path6"></span>
											<span class="path7"></span>
											<span class="path8"></span>
											<span class="path9"></span>
											<span class="path10"></span>
										</i>
										<i class="ki-duotone ki-moon theme-dark-show fs-2">
											<span class="path1"></span>
											<span class="path2"></span>
										</i>
									</span>
								</span>
							</a>
							<!--begin::Menu-->
							<div class="menu menu-sub menu-sub-dropdown menu-column menu-rounded menu-title-gray-700 menu-icon-gray-500 menu-active-bg menu-state-color fw-semibold py-4 fs-base w-150px" data-kt-menu="true" data-kt-element="theme-mode-menu">
								<!--begin::Menu item-->
								<div class="menu-item px-3 my-0">
									<a href="#" class="menu-link px-3 py-2" data-kt-element="mode" data-kt-value="light">
										<span class="menu-icon" data-kt-element="icon">
											<i class="ki-duotone ki-night-day fs-2">
												<span class="path1"></span>
												<span class="path2"></span>
												<span class="path3"></span>
												<span class="path4"></span>
												<span class="path5"></span>
												<span class="path6"></span>
												<span class="path7"></span>
												<span class="path8"></span>
												<span class="path9"></span>
												<span class="path10"></span>
											</i>
										</span>
										<span class="menu-title">Light</span>
									</a>
								</div>
								<!--end::Menu item-->
								<!--begin::Menu item-->
								<div class="menu-item px-3 my-0">
									<a href="#" class="menu-link px-3 py-2" data-kt-element="mode" data-kt-value="dark">
										<span class="menu-icon" data-kt-element="icon">
											<i class="ki-duotone ki-moon fs-2">
												<span class="path1"></span>
												<span class="path2"></span>
											</i>
										</span>
										<span class="menu-title">Dark</span>
									</a>
								</div>
								<!--end::Menu item-->
								<!--begin::Menu item-->
								<div class="menu-item px-3 my-0">
									<a href="#" class="menu-link px-3 py-2" data-kt-element="mode" data-kt-value="system">
										<span class="menu-icon" data-kt-element="icon">
											<i class="ki-duotone ki-screen fs-2">
												<span class="path1"></span>
												<span class="path2"></span>
												<span class="path3"></span>
												<span class="path4"></span>
											</i>
										</span>
										<span class="menu-title">System</span>
									</a>
								</div>
								<!--end::Menu item-->
							</div>
							<!--end::Menu-->
						</div>
						<!--end::Menu item-->
						<!--begin::Menu item (Language) -->
						<div class="menu-item px-5" data-kt-menu-trigger="{default: 'click', lg: 'hover'}" data-kt-menu-placement="left-start" data-kt-menu-offset="-15px, 0">
							<a href="#" class="menu-link px-5">
								<span class="menu-title position-relative">
									Language
									<span class="fs-8 rounded bg-light px-3 py-2 position-absolute translate-middle-y top-50 end-0">
										@activeLocalization.Name
										<img class="w-15px h-15px rounded-1 ms-2" src="@activeLocalization.Image" alt="" />
									</span>
								</span>
							</a>
							<div class="menu-sub menu-sub-dropdown w-175px py-4">
								@foreach (var localization in Model)
								{
									<div class="menu-item px-3">
										<a asp-controller="UI" asp-action="SetCulture" asp-route-culture="@localization.Culture" asp-route-returnUrl="@localization.RedirectUrl" class="menu-link d-flex px-5 @(localization.IsActive ? "active" : "")">
											<span class="symbol symbol-20px me-4">
												<img class="rounded-1" src="@localization.Image" alt="@localization.Name" />
											</span>
											@localization.Name
										</a>
									</div> 
								}
							</div>
						</div>
						<!--end::Menu item (Language) -->
						<!--begin::Menu item-->
						<div class="menu-item px-5 my-1">
							<a href="account/settings.html" class="menu-link px-5">Account Settings</a>
						</div>
						<!--end::Menu item-->
						<!--begin::Menu item-->
						<div class="menu-item px-5">
							<a href="authentication/layouts/corporate/sign-in.html" class="menu-link px-5">Sign Out</a>
						</div>
						<!--end::Menu item-->
					</div>
					<!--end::User account menu-->
					<!--end::Menu wrapper-->
				</div>
				<!--end::User menu-->
				<!--begin::Header menu toggle-->
				<div class="app-navbar-item d-lg-none ms-2 me-n2" title="Show header menu">
					<div class="btn btn-flex btn-icon btn-active-color-primary w-30px h-30px" id="kt_app_header_menu_toggle">
						<i class="ki-duotone ki-element-4 fs-1">
							<span class="path1"></span>
							<span class="path2"></span>
						</i>
					</div>
				</div>
				<!--end::Header menu toggle-->
				<!--begin::Aside toggle-->
				<!--end::Header menu toggle-->
			</div>
			<!--end::Navbar-->
		</div>
		<!--end::Header wrapper-->
	</div>
	<!--end::Header container-->
</div>