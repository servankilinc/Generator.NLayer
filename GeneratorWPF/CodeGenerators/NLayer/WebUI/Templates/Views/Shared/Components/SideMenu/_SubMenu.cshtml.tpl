@model MenuItem
@{
    string itemClass = "menu-item";
    if (Model.HasActiveChild) itemClass += " here show";
}

@if (!string.IsNullOrWhiteSpace(Model.GroupName))
{
    <div class="menu-item pt-5">
        <div class="menu-content">
            <span class="menu-heading fw-bold text-uppercase fs-7">@Model.GroupName</span>
        </div>
    </div>
}

@if (Model.SubMenuItems == null || !Model.SubMenuItems.Any())
{
    <div class="@(itemClass)">
        <a class="menu-link @(Model.IsActive ? "active" : "")" href="@Model.Path" target="@(Model.Target)">
            <span class="menu-icon">
                @Html.Raw(Model.Icon)
            </span>
            <span class="menu-title">@Model.Title</span>
        </a>
    </div>
}
else
{
    <div data-kt-menu-trigger="click" class="@(itemClass) menu-accordion">
        <span class="menu-link">
            <span class="menu-icon">
                @Html.Raw(Model.Icon)
            </span>
            <span class="menu-title">@Model.Title</span>
            <span class="menu-arrow"></span>
        </span>

        <div class="menu-sub menu-sub-accordion">
            @foreach (var subMenuItem in Model.SubMenuItems!)
            {
                <partial name="./_subMenu.cshtml" model="subMenuItem" />
            }
        </div>
    </div>
}